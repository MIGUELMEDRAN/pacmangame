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
    private const double BoardSize = 600;
    private const double BorderThickness = 20;
    private const double CellSize = 10;
    private const double CollisionInset = 1.25;

    private readonly GameView _view;
    private readonly Canvas _canvas;
    private readonly AudioPlayer _audio;
    private readonly DispatcherTimer _animationTimer;
    private readonly DispatcherTimer _ghostTimer;
    private readonly DispatcherTimer _powerModeTimer;

    private readonly List<Rect> _walls = new();
    private readonly List<Rectangle> _wallShapes = new();
    private readonly List<GhostState> _ghostStates = new();
    private readonly Dictionary<int, LevelConfig> _levels;

    private int _score;
    private int _level;
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

        Dots.OnScore += AddScore;
        Dots.OnPowerUpCollected += ActivatePowerMode;
        Dots.OnBoardCleared += AdvanceLevel;

        _animationTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(155) };
        _animationTimer.Tick += (_, _) => Pacman.ToggleMouth();

        _ghostTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(120) };
        _ghostTimer.Tick += (_, _) => MoveGhosts();

        _powerModeTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(7) };
        _powerModeTimer.Tick += (_, _) => DisablePowerMode();

        RetryGame();
    }

    public void RetryGame()
    {
        _isGameFinished = false;
        _score = 0;
        _view.HideStatusOverlay();
        _view.UpdateScoreDisplay(_score);
        ScoreService.UpdateScore(_score);

        LoadLevel(1);

        _animationTimer.Start();
        _ghostTimer.Start();
    }

    public void OnKeyDown(KeyEventArgs e)
    {
        if (_isGameFinished)
        {
            return;
        }

        var movement = GetMovement(e.Key);
        if (movement == default)
        {
            return;
        }

        Pacman.Rotate(movement.Angle);

        var targetX = Clamp(Pacman.X + movement.Dx, BorderThickness, BoardSize - BorderThickness - Pacman.Width);
        var targetY = Clamp(Pacman.Y + movement.Dy, BorderThickness, BoardSize - BorderThickness - Pacman.Height);

        var finalX = Pacman.X;
        var finalY = Pacman.Y;

        if (IsSpaceFree(targetX, Pacman.Y, Pacman.Width, Pacman.Height))
        {
            finalX = targetX;
        }

        if (IsSpaceFree(finalX, targetY, Pacman.Width, Pacman.Height))
        {
            finalY = targetY;
        }

        if (finalX == Pacman.X && finalY == Pacman.Y)
        {
            return;
        }

        Pacman.Move(finalX, finalY);
        Dots.CheckDotCollision(_canvas, Pacman.GetBounds());
        CheckGhostCollision();
    }

    private static (double Dx, double Dy, double Angle) GetMovement(Key key)
    {
        return key switch
        {
            Key.Up or Key.W => (0, -CellSize, 270),
            Key.Down or Key.S => (0, CellSize, 90),
            Key.Left or Key.A => (-CellSize, 180),
            Key.Right or Key.D => (CellSize, 0, 0),
            _ => default
        };
    }

    private void LoadLevel(int level)
    {
        _level = level;
        _isPowerMode = false;
        _bossSpawned = false;
        _powerModeTimer.Stop();

        var config = _levels[level];

        Pacman.Move(40, 140);
        Pacman.Rotate(0);

        BuildMaze(config);
        BuildDots();
        BuildGhosts(config);

        _view.UpdateTheme(config.CanvasColor);
        _view.UpdateLevelDisplay(level, config.ThemeName);
    }

    private void BuildMaze(LevelConfig config)
    {
        foreach (var wall in _wallShapes)
        {
            _canvas.Children.Remove(wall);
        }

        _wallShapes.Clear();
        _walls.Clear();

        foreach (var (x, y, w, h) in config.Walls)
        {
            var wall = new Rectangle
            {
                Width = w,
                Height = h,
                Fill = new SolidColorBrush(config.WallColor),
                RadiusX = 5,
                RadiusY = 5
            };

            Canvas.SetLeft(wall, x);
            Canvas.SetTop(wall, y);
            _canvas.Children.Add(wall);

            _wallShapes.Add(wall);
            _walls.Add(new Rect(x, y, w, h));
        }
    }

    private void BuildDots()
    {
        Dots.Reset(_canvas);
        Dots.CreateDots(_canvas, _walls);
        Dots.CreatePowerUps(_canvas, _level);
    }

    private void BuildGhosts(LevelConfig config)
    {
        _ghostStates.Clear();

        for (var i = 0; i < _view.GhostImages.Length; i++)
        {
            var image = _view.GhostImages[i];
            image.IsVisible = i < config.GhostCount;
            image.Opacity = 1;

            if (!image.IsVisible)
            {
                continue;
            }

            var spawn = config.Spawns[i];
            var ghost = new Ghost(spawn.X, spawn.Y)
            {
                Width = image.Width,
                Height = image.Height,
                Speed = CellSize
            };

            ghost.PositionChanged += (x, y) =>
            {
                Canvas.SetLeft(image, x);
                Canvas.SetTop(image, y);
            };

            ghost.Move(spawn.X, spawn.Y);
            _ghostStates.Add(new GhostState(ghost, image));
        }

        _view.BossImage.IsVisible = false;
        _view.BossImage.Opacity = 1;
    }

    private void MoveGhosts()
    {
        if (_isGameFinished)
        {
            return;
        }

        foreach (var state in _ghostStates.Where(s => s.Ghost.IsActive).ToList())
        {
            MoveGhost(state);
        }

        CheckGhostCollision();
    }

    private void MoveGhost(GhostState state)
    {
        var current = Quantize(state.Ghost.X, state.Ghost.Y);
        var target = Quantize(Pacman.X, Pacman.Y);

        var candidates = NextCells(current, (int)state.Ghost.Speed)
            .Where(next => IsSpaceFree(next.X, next.Y, state.Ghost.Width, state.Ghost.Height))
            .ToList();

        if (candidates.Count == 0)
        {
            var recovered = RecoverGhostPosition(state);
            if (!recovered)
            {
                return;
            }

            current = Quantize(state.Ghost.X, state.Ghost.Y);
            candidates = NextCells(current, (int)state.Ghost.Speed)
                .Where(next => IsSpaceFree(next.X, next.Y, state.Ghost.Width, state.Ghost.Height))
                .ToList();

            if (candidates.Count == 0)
            {
                return;
            }
        }

        var nextStep = _isPowerMode && !state.Ghost.IsBoss
            ? candidates.OrderByDescending(c => ManhattanDistance(c, target)).First()
            : FindStepTowardTarget(current, target, candidates, state.Ghost.Width, state.Ghost.Height, (int)state.Ghost.Speed);

        state.Ghost.Move(nextStep.X, nextStep.Y);
    }

    private bool RecoverGhostPosition(GhostState state)
    {
        var origin = Quantize(state.Ghost.X, state.Ghost.Y);

        for (var radius = 1; radius <= 3; radius++)
        {
            var range = radius * (int)CellSize;
            var candidates = new (int X, int Y)[]
            {
                (origin.X + range, origin.Y),
                (origin.X - range, origin.Y),
                (origin.X, origin.Y + range),
                (origin.X, origin.Y - range)
            };

            var candidate = candidates.FirstOrDefault(c => IsSpaceFree(c.X, c.Y, state.Ghost.Width, state.Ghost.Height));
            if (candidate != default)
            {
                state.Ghost.Move(candidate.X, candidate.Y);
                return true;
            }
        }

        return false;
    }

    private (int X, int Y) FindStepTowardTarget(
        (int X, int Y) current,
        (int X, int Y) target,
        List<(int X, int Y)> candidates,
        double width,
        double height,
        int step)
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

            foreach (var next in NextCells(node, step))
            {
                if (visited.Contains(next))
                {
                    continue;
                }

                if (!IsSpaceFree(next.X, next.Y, width, height))
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
            return candidates.OrderBy(c => ManhattanDistance(c, target)).First();
        }

        var pathNode = target;
        while (parent.TryGetValue(pathNode, out var previous) && previous != current)
        {
            pathNode = previous;
        }

        return candidates.Contains(pathNode)
            ? pathNode
            : candidates.OrderBy(c => ManhattanDistance(c, target)).First();
    }

    private static IEnumerable<(int X, int Y)> NextCells((int X, int Y) node, int step)
    {
        yield return (node.X + step, node.Y);
        yield return (node.X - step, node.Y);
        yield return (node.X, node.Y + step);
        yield return (node.X, node.Y - step);
    }

    private void ActivatePowerMode()
    {
        _isPowerMode = true;
        _powerModeTimer.Stop();
        _powerModeTimer.Start();

        foreach (var state in _ghostStates.Where(s => s.Ghost.IsActive && !s.Ghost.IsBoss))
        {
            state.Ghost.IsVulnerable = true;
            state.Image.Opacity = 0.45;
        }
    }

    private void DisablePowerMode()
    {
        _isPowerMode = false;
        _powerModeTimer.Stop();

        foreach (var state in _ghostStates.Where(s => s.Ghost.IsActive))
        {
            state.Ghost.IsVulnerable = false;
            state.Image.Opacity = 1;
        }
    }

    private void CheckGhostCollision()
    {
        var pacmanBounds = Pacman.GetBounds();

        foreach (var state in _ghostStates.Where(s => s.Ghost.IsActive).ToList())
        {
            if (!state.Ghost.GetBounds().Intersects(pacmanBounds))
            {
                continue;
            }

            if (state.Ghost.IsVulnerable)
            {
                state.Ghost.IsActive = false;
                state.Image.IsVisible = false;
                AddScore(200);

                if (_level == 3 && !_bossSpawned && _ghostStates.Where(s => !s.Ghost.IsBoss).All(s => !s.Ghost.IsActive))
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

        var ghost = new Ghost(280, 280)
        {
            Width = bossImage.Width,
            Height = bossImage.Height,
            Speed = CellSize,
            IsBoss = true
        };

        ghost.PositionChanged += (x, y) =>
        {
            Canvas.SetLeft(bossImage, x);
            Canvas.SetTop(bossImage, y);
        };

        ghost.Move(280, 280);
        _ghostStates.Add(new GhostState(ghost, bossImage));
    }

    private void AddScore(int points)
    {
        _score += points;
        _view.UpdateScoreDisplay(_score);
        ScoreService.UpdateScore(_score);
    }

    private void AdvanceLevel()
    {
        if (_level >= 3)
        {
            _isGameFinished = true;
            _animationTimer.Stop();
            _ghostTimer.Stop();
            _powerModeTimer.Stop();
            _view.ShowWinMessage();
            return;
        }

        _audio.LevelUp();
        LoadLevel(_level + 1);
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

    private bool IsSpaceFree(double x, double y, double width, double height)
    {
        if (x < BorderThickness || y < BorderThickness)
        {
            return false;
        }

        if (x + width > BoardSize - BorderThickness || y + height > BoardSize - BorderThickness)
        {
            return false;
        }

        var safeBounds = new Rect(
            x + CollisionInset,
            y + CollisionInset,
            Math.Max(1, width - (CollisionInset * 2)),
            Math.Max(1, height - (CollisionInset * 2)));

        return !_walls.Any(w => w.Intersects(safeBounds));
    }

    private static (int X, int Y) Quantize(double x, double y)
    {
        return ((int)Math.Round(x / CellSize) * (int)CellSize, (int)Math.Round(y / CellSize) * (int)CellSize);
    }

    private static int ManhattanDistance((int X, int Y) a, (int X, int Y) b)
    {
        return Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y);
    }

    private static double Clamp(double value, double min, double max)
    {
        return Math.Max(min, Math.Min(max, value));
    }

    private static Dictionary<int, LevelConfig> BuildLevelConfigs()
    {
        return new Dictionary<int, LevelConfig>
        {
            [1] = new LevelConfig(
                "Aurora",
                Color.Parse("#0D1B2A"),
                Color.Parse("#3A86FF"),
                2,
                new[] { (300d, 300d), (260d, 300d), (320d, 260d), (240d, 260d) },
                new (double x, double y, double w, double h)[]
                {
                    (0, 0, 600, 20), (0, 580, 600, 20), (0, 0, 20, 600), (580, 0, 20, 600),
                    (80, 80, 140, 20), (80, 80, 20, 140), (220, 160, 160, 20), (380, 80, 20, 180),
                    (120, 260, 180, 20), (300, 360, 180, 20), (180, 460, 240, 20), (500, 220, 20, 180)
                }),
            [2] = new LevelConfig(
                "Cobalt Lab",
                Color.Parse("#1B1B2F"),
                Color.Parse("#06D6A0"),
                3,
                new[] { (300d, 300d), (240d, 300d), (360d, 300d), (300d, 240d) },
                new (double x, double y, double w, double h)[]
                {
                    (0, 0, 600, 20), (0, 580, 600, 20), (0, 0, 20, 600), (580, 0, 20, 600),
                    (70, 70, 20, 220), (70, 70, 180, 20), (350, 70, 180, 20), (510, 70, 20, 220),
                    (170, 200, 260, 20), (170, 200, 20, 170), (410, 200, 20, 170), (120, 430, 360, 20),
                    (280, 290, 40, 140)
                }),
            [3] = new LevelConfig(
                "Crimson Forge",
                Color.Parse("#2B0A0A"),
                Color.Parse("#FF7F11"),
                4,
                new[] { (300d, 300d), (260d, 300d), (340d, 300d), (300d, 260d) },
                new (double x, double y, double w, double h)[]
                {
                    (0, 0, 600, 20), (0, 580, 600, 20), (0, 0, 20, 600), (580, 0, 20, 600),
                    (90, 70, 420, 20), (90, 70, 20, 170), (490, 70, 20, 170), (170, 170, 260, 20),
                    (170, 170, 20, 180), (410, 170, 20, 180), (110, 390, 380, 20), (110, 390, 20, 130),
                    (470, 390, 20, 130), (200, 510, 200, 20)
                })
        };
    }

    private sealed record GhostState(Ghost Ghost, Image Image);

    private sealed record LevelConfig(
        string ThemeName,
        Color CanvasColor,
        Color WallColor,
        int GhostCount,
        (double X, double Y)[] Spawns,
        (double x, double y, double w, double h)[] Walls);
}
