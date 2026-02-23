using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace PACMAN.Models;

public class Pacman
{
    public double X { get; private set; } = 40;
    public double Y { get; private set; } = 140;
    public double MoveStep { get; set; } = 10;
    public bool IsMouthOpen { get; private set; } = true;

    public Image OpenImage { get; }
    public Image CloseImage { get; }

    public double Width => OpenImage.Width;
    public double Height => OpenImage.Height;

    public Pacman(Image openImage, Image closeImage)
    {
        OpenImage = openImage;
        CloseImage = closeImage;
    }

    public void Move(double newX, double newY)
    {
        X = newX;
        Y = newY;
        Canvas.SetLeft(OpenImage, X);
        Canvas.SetTop(OpenImage, Y);
        Canvas.SetLeft(CloseImage, X);
        Canvas.SetTop(CloseImage, Y);
    }

    public void Rotate(double angle)
    {
        var transform = new RotateTransform
        {
            Angle = angle,
            CenterX = Width / 2,
            CenterY = Height / 2
        };

        OpenImage.RenderTransform = transform;
        CloseImage.RenderTransform = transform;
    }

    public Rect GetBounds()
    {
        return new Rect(X, Y, Width, Height);
    }

    public void ToggleMouth()
    {
        IsMouthOpen = !IsMouthOpen;
        OpenImage.IsVisible = IsMouthOpen;
        CloseImage.IsVisible = !IsMouthOpen;
    }
}
