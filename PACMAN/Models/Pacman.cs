using Avalonia.Controls;
using Avalonia.Media;
using Avalonia;

namespace PACMAN.Models;

public class Pacman
{
    public double X { get; set; } = 40;
    public double Y { get; set; } = 140;
    public double MoveStep { get; set; } = 10;
    public bool IsMouthOpen { get; set; } = true;
    public Image OpenImage { get; set; }
    public Image CloseImage { get; set; }

    public Pacman(Image openImage, Image closeImage)
    {
        OpenImage = openImage;
        CloseImage = closeImage;
    }

    public void Move(double newX, double newY)
    {
        X = newX;
        Y = newY;
        UpdatePosition();
    }

    public void UpdatePosition()
    {
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
            CenterX = OpenImage.Bounds.Width / 2,
            CenterY = OpenImage.Bounds.Height / 2
        };
        
        OpenImage.RenderTransform = transform;
        CloseImage.RenderTransform = transform;
    }

    public Rect GetBounds()
    {
        return new Rect(X, Y, OpenImage.Bounds.Width, OpenImage.Bounds.Height);
    }

    public void ToggleMouth()
    {
        IsMouthOpen = !IsMouthOpen;
        OpenImage.IsVisible = IsMouthOpen;
        OpenImage.IsVisible = !IsMouthOpen;
    }
}