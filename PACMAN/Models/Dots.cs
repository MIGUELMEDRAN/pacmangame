using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Media;
using PACMAN.Audio;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PACMAN.Models;

public class Dots
{
    private const int GridSpacing = 20;
    private const int GridStart = 30;
    private const int GridEnd = 570;
    private const double DotSize = 5;

    public List<Ellipse> SmallDots { get; } = new();
    public List<Control> BigDots { get; } = new();

    private readonly AudioPlayer _audioPlayer;
    private HashSet<(int X, int Y)> _reachableNodes = new();

    public event Action<int>? OnScore;
    public event Action? OnPowerUpCollected;
    public event Action? OnBoardCleared;

    public Dots(AudioPlayer audioPlayer)
    {
        _audioPlayer = audioPlayer ?? throw new ArgumentNullException(nameof(audioPlayer));
    }

    public void Reset(Canvas canvas)
    {
        foreach (var dot in SmallDots)
        {
            canvas.Children.Remove(dot);
        }

        foreach (var powerUp in BigDots)
        {
            canvas.Children.Remove(powerUp);
        }

        SmallDots.Clear();
        BigDots.Clear();
        _reachableNodes.Clear();
    }

    public void CreateDots(Canvas canvas, List<Rect> walls, double spawnX, double spawnY)
    {
        var candidates = new HashSet<(int X, int Y)>();

        for (var y = GridStart; y < GridEnd; y += GridSpacing)
        {
            for (var x = GridStart; x < GridEnd; x += GridSpacing)
            {
                var dotRect = new Rect(x, y, DotSize, DotSize);
                if (walls.Exists(wall => wall.Intersects(dotRect)))
                {
                    continue;
                }

                candidates.Add((x, y));
            }
        }

        if (candidates.Count == 0)
        {
            return;
        }

        var start = FindClosestNode(candidates, spawnX, spawnY);
        _reachableNodes = CalculateReachableNodes(candidates, start, GridSpacing);

        foreach (var (x, y) in _reachableNodes.OrderBy(node => node.Y).ThenBy(node => node.X))
        {
            var dot = new Ellipse
            {
                Width = DotSize,
                Height = DotSize,
                Fill = new SolidColorBrush(Color.Parse("#FFF3B0"))
            };

            Canvas.SetLeft(dot, x);
            Canvas.SetTop(dot, y);
            canvas.Children.Add(dot);
            SmallDots.Add(dot);
        }
    }

    public void CreatePowerUps(Canvas canvas, int level)
    {
        var positions = level switch
        {
            1 => new[] { (40d, 540d), (540d, 540d) },
            2 => new[] { (40d, 40d), (540d, 40d), (300d, 540d) },
            _ => new[] { (40d, 540d), (540d, 540d), (40d, 40d), (540d, 40d) }
        };

        foreach (var (x, y) in positions)
        {
            var placement = GetReachablePlacement(x, y);

            var powerUp = new Ellipse
            {
                Width = 18,
                Height = 18,
                Fill = new RadialGradientBrush
                {
                    GradientStops = new GradientStops
                    {
                        new GradientStop(Color.Parse("#FF4D6D"), 0.0),
                        new GradientStop(Color.Parse("#FFB703"), 1.0)
                    }
                },
                Stroke = new SolidColorBrush(Color.Parse("#FFD166")),
                StrokeThickness = 1.5
            };

            Canvas.SetLeft(powerUp, placement.X);
            Canvas.SetTop(powerUp, placement.Y);
            canvas.Children.Add(powerUp);
            BigDots.Add(powerUp);
        }
    }

    public void CheckDotCollision(Canvas canvas, Rect pacmanBounds)
    {
        for (var i = SmallDots.Count - 1; i >= 0; i--)
        {
            var dot = SmallDots[i];
            var dotBounds = new Rect(Canvas.GetLeft(dot), Canvas.GetTop(dot), dot.Width, dot.Height);
            if (!pacmanBounds.Intersects(dotBounds))
            {
                continue;
            }

            canvas.Children.Remove(dot);
            SmallDots.RemoveAt(i);
            _audioPlayer.Chomp();
            OnScore?.Invoke(10);
        }

        for (var i = BigDots.Count - 1; i >= 0; i--)
        {
            var powerUp = BigDots[i];
            var dotBounds = new Rect(Canvas.GetLeft(powerUp), Canvas.GetTop(powerUp), powerUp.Width, powerUp.Height);
            if (!pacmanBounds.Intersects(dotBounds))
            {
                continue;
            }

            canvas.Children.Remove(powerUp);
            BigDots.RemoveAt(i);
            _audioPlayer.PowerUp();
            OnScore?.Invoke(50);
            OnPowerUpCollected?.Invoke();
        }

        if (SmallDots.Count == 0 && BigDots.Count == 0)
        {
            OnBoardCleared?.Invoke();
        }
    }

    private (double X, double Y) GetReachablePlacement(double x, double y)
    {
        if (_reachableNodes.Count == 0)
        {
            return (x, y);
        }

        var nearest = _reachableNodes
            .OrderBy(node => Math.Abs(node.X - x) + Math.Abs(node.Y - y))
            .First();

        return (nearest.X, nearest.Y);
    }

    private static (int X, int Y) FindClosestNode(HashSet<(int X, int Y)> nodes, double x, double y)
    {
        return nodes.OrderBy(node => Math.Abs(node.X - x) + Math.Abs(node.Y - y)).First();
    }

    private static HashSet<(int X, int Y)> CalculateReachableNodes(HashSet<(int X, int Y)> candidates, (int X, int Y) start, int spacing)
    {
        var reachable = new HashSet<(int X, int Y)>();
        var queue = new Queue<(int X, int Y)>();

        queue.Enqueue(start);
        reachable.Add(start);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();

            foreach (var next in GetNeighbors(current, spacing))
            {
                if (!candidates.Contains(next) || reachable.Contains(next))
                {
                    continue;
                }

                reachable.Add(next);
                queue.Enqueue(next);
            }
        }

        return reachable;
    }

    private static IEnumerable<(int X, int Y)> GetNeighbors((int X, int Y) node, int spacing)
    {
        yield return (node.X + spacing, node.Y);
        yield return (node.X - spacing, node.Y);
        yield return (node.X, node.Y + spacing);
        yield return (node.X, node.Y - spacing);
    }
}
