using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Media;
using PACMAN.Audio;
using System;
using System.Collections.Generic;

namespace PACMAN.Models;

public class Dots
{
    public List<Ellipse> SmallDots { get; } = new();
    public List<Control> BigDots { get; } = new();

    private readonly AudioPlayer _audioPlayer;

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
    }

    public void CreateDots(Canvas canvas, List<Rect> walls)
    {
        const double spacing = 30;
        const double dotSize = 6;

        for (double y = 30; y < 570; y += spacing)
        {
            for (double x = 30; x < 570; x += spacing)
            {
                var dotRect = new Rect(x, y, dotSize, dotSize);
                if (walls.Exists(wall => wall.Intersects(dotRect)))
                {
                    continue;
                }

                var dot = new Ellipse
                {
                    Width = dotSize,
                    Height = dotSize,
                    Fill = new SolidColorBrush(Color.Parse("#FFF3B0"))
                };

                Canvas.SetLeft(dot, x);
                Canvas.SetTop(dot, y);
                canvas.Children.Add(dot);
                SmallDots.Add(dot);
            }
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

            Canvas.SetLeft(powerUp, x);
            Canvas.SetTop(powerUp, y);
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
}
