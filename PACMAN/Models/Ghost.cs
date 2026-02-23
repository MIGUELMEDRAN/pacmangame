using System;
using Avalonia;

namespace PACMAN.Models;

public class Ghost
{
    public double X { get; private set; }
    public double Y { get; private set; }
    public double Width { get; set; } = 30;
    public double Height { get; set; } = 30;
    public double Speed { get; set; } = 10;
    public bool IsVulnerable { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsBoss { get; set; }

    public event Action<double, double>? PositionChanged;

    public Ghost(double startX, double startY)
    {
        X = startX;
        Y = startY;
    }

    public void Move(double newX, double newY)
    {
        X = newX;
        Y = newY;
        PositionChanged?.Invoke(X, Y);
    }

    public Rect GetBounds()
    {
        return new Rect(X, Y, Width, Height);
    }
}
