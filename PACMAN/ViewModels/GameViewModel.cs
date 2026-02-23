using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using PACMAN.Audio;
using PACMAN.Models;
using PACMAN.Services;
using PACMAN.Views;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PACMAN.ViewModels;

public class GameViewModel
{
    private readonly GameView _view;
    private readonly Canvas _canvas;
    private readonly AudioPlayer _audio;
    private readonly DispatcherTimer _animationTimer;
    private readonly DispatcherTimer _ghostTimer;
    private readonly DispatcherTimer _powerModeTimer;

    private readonly List<Rect> _walls = new();
    private readonly List<Rectangle> _wallShapes = new();
    private readonly List<GhostState> _ghostStates = new();
    private readonly Random _random = new();

    private readonly Dictionary<int, LevelConfig> _levels;

    private int _score;
    private int _level = 1;
    private bool _isPowerMode;
    private bool _bossSpawned;
    private bool _isGameFinished;

    public Pacman Pacman { get; }
    public Dots Dots { get; }

    public GameViewModel(GameView view, Image open, Image closed, Canvas canvas, AudioPlayer audio)
    {
        _view = view;
        _canvas = canvas;
        _audio = audio;
        Pacman = new Pacman(open, closed);
        Dots = new Dots(audio);

        _levels = BuildLevelConfigs();

        Dots.OnScore += points =>
        {
            _score += points;
            _view.UpdateScoreDisplay(_score);
            ScoreService.UpdateScore(_score);
        };
        Dots.OnPowerUpCollected += ActivatePowerMode;
        Dots.OnBoardCleared += AdvanceLevel;

        _animationTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(180) };
        _animationTimer.Tick += (_, _) => Pacman.ToggleMouth();

        _ghostTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(120) };
        _ghostTimer.Tick += (_, _) => MoveGhosts();

        _powerModeTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(8) };
        _powerModeTimer.Tick += (_, _) => DisablePowerMode();

        LoadLevel(1, true);

        _animationTimer.Start();
        _ghostTimer.Start();
    }

    public void OnKeyDown(KeyEventArgs e)
    {
        if (_isGameFinished)
        {
            return;
        }

        var newX = Pacman.X;
        var newY = Pacman.Y;
        var angle = 0d;

        switch (e.Key)
        {
            case Key.Up:
            case Key.W:
                newY -= Pacman.MoveStep;
                angle = 270;
                break;
            case Key.Down:
            case Key.S:
                newY += Pacman.MoveStep;
                angle = 90;
                break;
            case Key.Left:
            case Key.A:
                newX -= Pacman.MoveStep;
                angle = 180;
                break;
            case Key.Right:
            case Key.D:
                newX += Pacman.MoveStep;
                angle = 0;
                break;
            default:
                return;
        }

        var futureBounds = new Rect(newX, newY, Pacman.OpenImage.Bounds.Width, Pacman.OpenImage.Bounds.Height);
        if (_walls.Exists(w => w.Intersects(futureBounds)))
        {
            return;
        }

        Pacman.Move(newX, newY);
        Pacman.Rotate(angle);

        Dots.CheckDotCollision(_canvas, Pacman.GetBounds());
        CheckGhostCollision();
    }

    private void LoadLevel(int level, bool resetScore)
    {
        _level = level;
        var config = _levels[level];

        if (resetScore)
        {
            _score = 0;
            ScoreService.UpdateScore(0);
            _view.UpdateScoreDisplay(0);
        }

        _isPowerMode = false;
        _bossSpawned = false;
        _powerModeTimer.Stop();

        Pacman.MoveStep = 10;
        Pacman.Move(40, 140);
        Pacman.Rotate(0);

        BuildMaze(config);
        BuildDots();
        BuildGhosts(config);

        _view.UpdateTheme(config.CanvasColor);
        _view.UpdateLevelDisplay(level, config.ThemeName);
    }

    private void BuildDots()
    {
        Dots.Reset(_canvas);
        Dots.CreateDots(_canvas, _walls);
        Dots.CreatePowerUps(_canvas, _level);
    }

    private void BuildMaze(LevelConfig config)
    {
        foreach (var shape in _wallShapes)
        {
            _canvas.Children.Remove(shape);
        }

        _wallShapes.Clear();
        _walls.Clear();

        foreach (var (x, y, w, h) in config.Walls)
        {
            var wall = new Rectangle
            {
                Width = w,
                Height = h,
                Fill = new SolidColorBrush(config.WallColor)
            };

            Canvas.SetLeft(wall, x);
            Canvas.SetTop(wall, y);
            _canvas.Children.Add(wall);
            _wallShapes.Add(wall);
            _walls.Add(new Rect(x, y, w, h));
        }
    }

    private void BuildGhosts(LevelConfig config)
    {
        _ghostStates.Clear();

        var images = _view.GhostImages;

        for (var i = 0; i < images.Length; i++)
        {
            var image = images[i];
            var isVisible = i < config.GhostCount;
            image.IsVisible = isVisible;

            if (!isVisible)
            {
                continue;
            }

            var spawn = config.SpawnPoints[i % config.SpawnPoints.Length];
            var ghost = new Ghost(spawn.X, spawn.Y)
            {
                Speed = config.GhostSpeed
            };

            ghost.PositionChanged += (newX, newY) =>
            {
                Canvas.SetLeft(image, newX);
                Canvas.SetTop(image, newY);
            };
            ghost.Move(spawn.X, spawn.Y);

            _ghostStates.Add(new GhostState(ghost, image, spawn.X, spawn.Y));
        }

        _view.BossImage.IsVisible = false;
    }

    private void MoveGhosts()
    {
        if (_isGameFinished)
        {
            return;
        }

        foreach (var state in _ghostStates.Where(x => x.Ghost.IsActive).ToList())
        {
            MoveSingleGhost(state);
        }

        CheckGhostCollision();
    }

    private void MoveSingleGhost(GhostState state)
    {
        var current = Quantize(state.Ghost.X, state.Ghost.Y);
        var target = Quantize(Pacman.X, Pacman.Y);
        var candidates = new List<(int X, int Y)>
        {
            (current.X + 10, current.Y),
            (current.X - 10, current.Y),
            (current.X, current.Y + 10),
            (current.X, current.Y - 10)
        };

        var valid = candidates
            .Where(step => IsCellValid(step.X, step.Y, state.Ghost.Width, state.Ghost.Height))
            .ToList();

        if (valid.Count == 0)
        {
            return;
        }

        (int X, int Y) best;
        if (_isPowerMode && !state.Ghost.IsBoss)
        {
            best = valid.OrderByDescending(step => Distance(step, target)).First();
        }
        else
        {
            best = FindBestStepByPath(current, target, valid);
        }

        var jitterX = state.Ghost.IsBoss ? _random.Next(-1, 2) * 2 : 0;
        var jitterY = state.Ghost.IsBoss ? _random.Next(-1, 2) * 2 : 0;
        state.Ghost.Move(best.X + jitterX, best.Y + jitterY);
    }

    private (int X, int Y) FindBestStepByPath((int X, int Y) current, (int X, int Y) target, List<(int X, int Y)> valid)
    {
        var queue = new Queue<(int X, int Y)>();
        var visited = new HashSet<(int X, int Y)> { current };
        var parent = new Dictionary<(int X, int Y), (int X, int Y)>();

        queue.Enqueue(current);

        while (queue.Count > 0)
        {
            var node = queue.Dequeue();
            if (node == target)
            {
                break;
            }

            foreach (var next in NextCells(node))
            {
                if (visited.Contains(next))
                {
                    continue;
                }

                if (!IsCellValid(next.X, next.Y, 30, 30))
                {
                    continue;
                }

                visited.Add(next);
                parent[next] = node;
                queue.Enqueue(next);
            }
        }

        if (!visited.Contains(target))
        {
            return valid.OrderBy(step => Distance(step, target)).First();
        }

        var walk = target;
        while (parent.TryGetValue(walk, out var previous) && previous != current)
        {
            walk = previous;
        }

        if (valid.Contains(walk))
        {
            return walk;
        }

        return valid.OrderBy(step => Distance(step, target)).First();
    }

    private IEnumerable<(int X, int Y)> NextCells((int X, int Y) point)
    {
        yield return (point.X + 10, point.Y);
        yield return (point.X - 10, point.Y);
        yield return (point.X, point.Y + 10);
        yield return (point.X, point.Y - 10);
    }

    private bool IsCellValid(int x, int y, double width, double height)
    {
        if (x < 20 || y < 20 || x + width > 580 || y + height > 580)
        {
            return false;
        }

        var bounds = new Rect(x, y, width, height);
        return !_walls.Exists(w => w.Intersects(bounds));
    }

    private static int Distance((int X, int Y) a, (int X, int Y) b)
    {
        return Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y);
    }

    private static (int X, int Y) Quantize(double x, double y)
    {
        return ((int)Math.Round(x / 10d) * 10, (int)Math.Round(y / 10d) * 10);
    }

    private void ActivatePowerMode()
    {
        _isPowerMode = true;
        _powerModeTimer.Stop();
        _powerModeTimer.Start();

        foreach (var state in _ghostStates.Where(x => x.Ghost.IsActive && !x.Ghost.IsBoss))
        {
            state.Ghost.IsVulnerable = true;
            state.Image.Opacity = 0.45;
        }
    }

    private void DisablePowerMode()
    {
        _isPowerMode = false;
        _powerModeTimer.Stop();

        foreach (var state in _ghostStates.Where(x => x.Ghost.IsActive))
        {
            state.Ghost.IsVulnerable = false;
            state.Image.Opacity = 1;
        }
    }

    private void CheckGhostCollision()
    {
        var pacmanBounds = Pacman.GetBounds();
        foreach (var state in _ghostStates.Where(x => x.Ghost.IsActive).ToList())
        {
            if (!state.Ghost.GetBounds().Intersects(pacmanBounds))
            {
                continue;
            }

            if (state.Ghost.IsVulnerable)
            {
                state.Ghost.IsActive = false;
                state.Image.IsVisible = false;
                _score += 200;
                _view.UpdateScoreDisplay(_score);
                ScoreService.UpdateScore(_score);

                if (_level == 3 && !_bossSpawned && _ghostStates.Where(g => !g.Ghost.IsBoss).All(g => !g.Ghost.IsActive))
                {
                    SpawnBoss();
                }

                continue;
            }

            EndGame();
            return;
        }
    }

    private void SpawnBoss()
    {
        _bossSpawned = true;

        var bossImage = _view.BossImage;
        bossImage.IsVisible = true;
        bossImage.Opacity = 1;

        var ghost = new Ghost(280, 280)
        {
            Width = 55,
            Height = 55,
            Speed = 10,
            IsBoss = true,
            IsVulnerable = false
        };

        ghost.PositionChanged += (newX, newY) =>
        {
            Canvas.SetLeft(bossImage, newX);
            Canvas.SetTop(bossImage, newY);
        };

        ghost.Move(280, 280);
        _ghostStates.Add(new GhostState(ghost, bossImage, 280, 280));
    }

    private void AdvanceLevel()
    {
        if (_level >= 3)
        {
            _isGameFinished = true;
            _animationTimer.Stop();
            _ghostTimer.Stop();
            _view.ShowWinMessage();
            return;
        }

        _audio.LevelUp();
        LoadLevel(_level + 1, false);
    }

    private void EndGame()
    {
        _isGameFinished = true;
        _animationTimer.Stop();
        _ghostTimer.Stop();
        _powerModeTimer.Stop();
        _audio.Dead();
        _view.ShowGameOver();
    }

    private static Dictionary<int, LevelConfig> BuildLevelConfigs()
    {
        return new Dictionary<int, LevelConfig>
        {
            [1] = new LevelConfig(
                "Classic",
                Color.Parse("#0B0D10"),
                Color.Parse("#2A4BFF"),
                2,
                10,
                new[] { (300d, 300d), (260d, 300d), (320d, 260d), (240d, 260d) },
                new (double x, double y, double w, double h)[]
                {
                    (0, 0, 600, 20), (0, 580, 600, 20), (0, 0, 20, 600), (580, 0, 20, 600),
                    (100, 100, 120, 20), (100, 200, 20, 130), (200, 280, 150, 20), (380, 100, 20, 180),
                    (420, 320, 120, 20), (160, 420, 20, 130), (260, 460, 180, 20)
                }),
            [2] = new LevelConfig(
                "Neon Grid",
                Color.Parse("#160F29"),
                Color.Parse("#43D9AD"),
                3,
                10,
                new[] { (300d, 300d), (240d, 300d), (360d, 300d), (300d, 240d) },
                new (double x, double y, double w, double h)[]
                {
                    (0, 0, 600, 20), (0, 580, 600, 20), (0, 0, 20, 600), (580, 0, 20, 600),
                    (80, 80, 20, 220), (80, 80, 200, 20), (320, 80, 200, 20), (500, 80, 20, 220),
                    (150, 350, 300, 20), (150, 350, 20, 170), (430, 350, 20, 170), (250, 200, 100, 20)
                }),
            [3] = new LevelConfig(
                "Volcanic Core",
                Color.Parse("#2B0A0A"),
                Color.Parse("#FF8A00"),
                4,
                10,
                new[] { (300d, 300d), (260d, 300d), (340d, 300d), (300d, 260d) },
                new (double x, double y, double w, double h)[]
                {
                    (0, 0, 600, 20), (0, 580, 600, 20), (0, 0, 20, 600), (580, 0, 20, 600),
                    (100, 80, 400, 20), (100, 80, 20, 170), (480, 80, 20, 170), (180, 220, 240, 20),
                    (180, 220, 20, 220), (400, 220, 20, 220), (100, 500, 400, 20), (280, 320, 40, 180)
                })
        };
    }

    private sealed record GhostState(Ghost Ghost, Image Image, double SpawnX, double SpawnY);

    private sealed record LevelConfig(
        string ThemeName,
        Color CanvasColor,
        Color WallColor,
        int GhostCount,
        double GhostSpeed,
        (double X, double Y)[] SpawnPoints,
        (double x, double y, double w, double h)[] Walls);
}
