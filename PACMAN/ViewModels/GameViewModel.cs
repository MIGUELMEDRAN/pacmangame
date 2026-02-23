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

public class GameViewModel : IDisposable
{
    private const double BoardSize = 600;
    private const double BorderThickness = 20;
    private const double CellSize = 10;
    private const double CollisionInset = 2;
    private const double LaneSnapTolerance = 4;

    private readonly GameView _view;
    private readonly Canvas _canvas;
    private readonly AudioPlayer _audio;
    private readonly DispatcherTimer _animationTimer;
    private readonly DispatcherTimer _movementTimer;
    private readonly DispatcherTimer _ghostTimer;
    private readonly DispatcherTimer _powerModeTimer;

    private readonly List<Rect> _walls = new();
    private readonly List<Rectangle> _wallShapes = new();
    private readonly List<GhostState> _ghostStates = new();
    private readonly Dictionary<int, LevelConfig> _levels;

    private int _score;
    private int _level;
    private int _lives;
    private bool _isPowerMode;
    private bool _bossSpawned;
    private bool _isGameFinished;
    private bool _disposed;

    private Direction _currentDirection = Direction.None;
    private Direction _desiredDirection = Direction.None;

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

        _animationTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(120) };
        _animationTimer.Tick += (_, _) => Pacman.ToggleMouth();

        _movementTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(50) };
        _movementTimer.Tick += (_, _) => UpdatePacman();

        _ghostTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(120) };
        _ghostTimer.Tick += (_, _) => MoveGhosts();

        _powerModeTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(7) };
        _powerModeTimer.Tick += (_, _) => DisablePowerMode();

        RetryGame();
    }

    public void RetryGame()
    {
        if (_disposed)
        {
            return;
        }

        _isGameFinished = false;
        _score = 0;
        _lives = 3;
        _currentDirection = Direction.None;
        _desiredDirection = Direction.None;

        _view.HideStatusOverlay();
        _view.UpdateScoreDisplay(_score);
        _view.UpdateLivesDisplay(_lives);
        ScoreService.UpdateScore(_score);

        LoadLevel(1);

        _animationTimer.Start();
        _movementTimer.Start();
        _ghostTimer.Start();
    }

    public void OnKeyDown(KeyEventArgs e)
    {
        if (_isGameFinished || _disposed)
        {
            return;
        }

        var direction = GetDirection(e.Key);
        if (direction == Direction.None)
        {
            return;
        }

        _desiredDirection = direction;
    }

    private static Direction GetDirection(Key key)
    {
        return key switch
        {
            Key.Up or Key.W => Direction.Up,
            Key.Down or Key.S => Direction.Down,
            Key.Left or Key.A => Direction.Left,
            Key.Right or Key.D => Direction.Right,
            _ => Direction.None
        };
    }

    private void UpdatePacman()
    {
        if (_isGameFinished || _disposed)
        {
            return;
        }

        TryTurnToDesiredDirection();

        if (_currentDirection == Direction.None)
        {
            return;
        }

        var (dx, dy) = DirectionVector(_currentDirection, Pacman.MoveStep);
        TryMovePacman(dx, dy);
        Dots.CheckDotCollision(_canvas, Pacman.GetBounds());
        CheckGhostCollision();
    }

    private void TryTurnToDesiredDirection()
    {
        if (_desiredDirection == Direction.None || _desiredDirection == _currentDirection)
        {
            return;
        }

        if (IsPerpendicular(_currentDirection, _desiredDirection))
        {
            if (!TrySnapToLane(_currentDirection))
            {
                return;
            }
        }

        var (dx, dy) = DirectionVector(_desiredDirection, Pacman.MoveStep);
        if (!CanMoveInDirection(dx, dy))
        {
            return;
        }

        _currentDirection = _desiredDirection;
        Pacman.Rotate(DirectionAngle(_currentDirection));
    }

    private bool CanMoveInDirection(double dx, double dy)
    {
        var nextX = Clamp(Pacman.X + dx, BorderThickness, BoardSize - BorderThickness - Pacman.Width);
        var nextY = Clamp(Pacman.Y + dy, BorderThickness, BoardSize - BorderThickness - Pacman.Height);
        return IsSpaceFree(nextX, nextY, Pacman.Width, Pacman.Height);
    }

    private void TryMovePacman(double dx, double dy)
    {
        var nextX = Clamp(Pacman.X + dx, BorderThickness, BoardSize - BorderThickness - Pacman.Width);
        var nextY = Clamp(Pacman.Y + dy, BorderThickness, BoardSize - BorderThickness - Pacman.Height);

        var finalX = Pacman.X;
        var finalY = Pacman.Y;

        if (IsSpaceFree(nextX, Pacman.Y, Pacman.Width, Pacman.Height))
        {
            finalX = nextX;
        }

        if (IsSpaceFree(finalX, nextY, Pacman.Width, Pacman.Height))
        {
            finalY = nextY;
        }

        if (finalX == Pacman.X && finalY == Pacman.Y)
        {
            _currentDirection = Direction.None;
            return;
        }

        Pacman.Move(finalX, finalY);
    }

    private bool TrySnapToLane(Direction currentDirection)
    {
        if (currentDirection is Direction.Left or Direction.Right)
        {
            var snappedY = SnapToCell(Pacman.Y);
            var deltaY = snappedY - Pacman.Y;
            if (Math.Abs(deltaY) > LaneSnapTolerance)
            {
                return false;
            }

            if (Math.Abs(deltaY) < 0.01)
            {
                return true;
            }

            var nextY = Pacman.Y + Math.Sign(deltaY) * Math.Min(Pacman.MoveStep / 2, Math.Abs(deltaY));
            if (!IsSpaceFree(Pacman.X, nextY, Pacman.Width, Pacman.Height))
            {
                return false;
            }

            Pacman.Move(Pacman.X, nextY);
            return false;
        }

        var snappedX = SnapToCell(Pacman.X);
        var deltaX = snappedX - Pacman.X;
        if (Math.Abs(deltaX) > LaneSnapTolerance)
        {
            return false;
        }

        if (Math.Abs(deltaX) < 0.01)
        {
            return true;
        }

        var nextX = Pacman.X + Math.Sign(deltaX) * Math.Min(Pacman.MoveStep / 2, Math.Abs(deltaX));
        if (!IsSpaceFree(nextX, Pacman.Y, Pacman.Width, Pacman.Height))
        {
            return false;
        }

        Pacman.Move(nextX, Pacman.Y);
        return false;
    }

    private void LoadLevel(int level)
    {
        _level = level;
        _isPowerMode = false;
        _bossSpawned = false;
        _powerModeTimer.Stop();

        var config = _levels[level];

        ResetPacmanPosition();
        BuildMaze(config);
        BuildDots();
        BuildGhosts(config);

        _view.UpdateTheme(config.CanvasColor);
        _view.UpdateLevelDisplay(level, config.ThemeName);
    }

    private void ResetPacmanPosition()
    {
        Pacman.Move(40, 140);
        _currentDirection = Direction.None;
        _desiredDirection = Direction.None;
        Pacman.Rotate(0);
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
                Fill = new LinearGradientBrush
                {
                    StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
                    EndPoint = new RelativePoint(1, 1, RelativeUnit.Relative),
                    GradientStops = new GradientStops
                    {
                        new GradientStop(config.WallColor, 0),
                        new GradientStop(Color.FromArgb(255, 16, 48, 96), 1)
                    }
                },
                RadiusX = 8,
                RadiusY = 8,
                Stroke = new SolidColorBrush(Color.FromArgb(180, 220, 235, 255)),
                StrokeThickness = 1
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
        if (_isGameFinished || _disposed)
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
            return;
        }

        var nextStep = _isPowerMode && !state.Ghost.IsBoss
            ? candidates.OrderByDescending(c => ManhattanDistance(c, target)).First()
            : FindStepTowardTarget(current, target, candidates, state.Ghost.Width, state.Ghost.Height, (int)state.Ghost.Speed);

        state.Ghost.Move(nextStep.X, nextStep.Y);
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
                if (visited.Contains(next) || !IsSpaceFree(next.X, next.Y, width, height))
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

            HandlePacmanHit();
            return;
        }
    }

    private void HandlePacmanHit()
    {
        _lives--;
        _view.UpdateLivesDisplay(_lives);

        if (_lives <= 0)
        {
            EndGame();
            return;
        }

        ResetPacmanPosition();
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
            _movementTimer.Stop();
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
        _movementTimer.Stop();
        _ghostTimer.Stop();
        _powerModeTimer.Stop();
        _audio.Dead();
        _view.ShowGameOver();
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        _animationTimer.Stop();
        _movementTimer.Stop();
        _ghostTimer.Stop();
        _powerModeTimer.Stop();

        Dots.OnScore -= AddScore;
        Dots.OnPowerUpCollected -= ActivatePowerMode;
        Dots.OnBoardCleared -= AdvanceLevel;
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

    private static bool IsPerpendicular(Direction a, Direction b)
    {
        return (a is Direction.Left or Direction.Right) && (b is Direction.Up or Direction.Down)
            || (a is Direction.Up or Direction.Down) && (b is Direction.Left or Direction.Right);
    }

    private static (double Dx, double Dy) DirectionVector(Direction direction, double speed)
    {
        return direction switch
        {
            Direction.Up => (0, -speed),
            Direction.Down => (0, speed),
            Direction.Left => (-speed, 0),
            Direction.Right => (speed, 0),
            _ => (0, 0)
        };
    }

    private static double DirectionAngle(Direction direction)
    {
        return direction switch
        {
            Direction.Up => 270,
            Direction.Down => 90,
            Direction.Left => 180,
            _ => 0
        };
    }

    private static double SnapToCell(double value)
    {
        return Math.Round(value / CellSize) * CellSize;
    }

    private static IEnumerable<(int X, int Y)> NextCells((int X, int Y) node, int step)
    {
        yield return (node.X + step, node.Y);
        yield return (node.X - step, node.Y);
        yield return (node.X, node.Y + step);
        yield return (node.X, node.Y - step);
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

    private enum Direction
    {
        None,
        Up,
        Down,
        Left,
        Right
    }
}
