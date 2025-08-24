using Avalonia.Controls; 
using Avalonia.Controls.Shapes; 
using Avalonia.Media; 
using Avalonia.Platform; 
using Avalonia; 
using System; 
using System.Collections.Generic; 
using PACMAN.Audio; 
using PACMAN.Services; 

namespace PACMAN.Models;

public class Dots
{
    public List<Ellipse> SmallDots { get; private set; } = new();
    public List<Control> BigDots { get; private set; } = new();
    
    private AudioPlayer audioPlayer;

    private int currentScore = 0;

    public event Action<int>? OnScore;

    public Dots()
    {
        audioPlayer = audioPlayer ?? throw new ArgumentNullException(nameof(audioPlayer));
    }

    public void CreateDots(Canvas canvas, List<Rect> walls)
    {
        double spacing = 30;
        double dotSize = 6;

        for (double y = 30; y < 570; y += spacing)
        {
            for (double x = 30; x < 570; x += spacing)
            {
                var dotRect = new Rect(x, y, dotSize, dotSize);
                bool collides = walls.Exists(wall => wall.Contains(dotRect));

                if (collides)
                {
                    continue;
                }

                var dot = new Ellipse
                {
                    Width = dotSize,
                    Height = dotSize,
                    Fill = Brushes.White,
                };
                
                Canvas.SetLeft(dot, x);
                Canvas.SetTop(dot, y);
                canvas.Children.Add(dot);
                SmallDots.Add(dot);
            }
        }
    }
    
    public void CreatePowerUps(Canvas gameCanvas)
    {
        var positions = new (double x, double y)[]
        {
            (40, 540),
            (540, 540)
        };

        foreach (var (x,y) in positions)
        {
            var uri = new Uri("avares://PACMAN2/Assets/Images/cherrys.jpg");
            var assetStream = AssetLoader.Open(uri);

            var powerUp = new Image
            {
                Width = 20,
                Height = 20,
                Source = new Avalonia.Media.Imaging.Bitmap(assetStream)
            };
            
            Canvas.SetLeft(powerUp, x);
            Canvas.SetTop(powerUp, y);
            gameCanvas.Children.Add(powerUp);
            BigDots.Add(powerUp);
        }
    }

    public void CheckDotCollision(Canvas gameCanvas, Rect pacmanBounds)
    {
        for (int i = SmallDots.Count - 1; i >= 0; i--)
        {
            var dot = SmallDots[i];
            double x = Canvas.GetLeft(dot);
            double y = Canvas.GetTop(dot);
            var dotBounds = new Rect(x, y, dot.Width, dot.Height);

            if (pacmanBounds.Intersects(dotBounds))
            {
                gameCanvas.Children.Remove(dot);
                SmallDots.RemoveAt(i);
                currentScore += 10;
                audioPlayer.Chomp();
                OnScore?.Invoke(10);
                ScoreService.UpdateScore(currentScore);
            }
        }

        for (int i = BigDots.Count - 1; i >= 0; i--)
        {
            var dot = BigDots[i];
            double x = Canvas.GetLeft(dot);
            double y = Canvas.GetTop(dot);
            var dotBounds = new Rect(x, y, dot.Width, dot.Height);

            if (pacmanBounds.Intersects(dotBounds))
            {
                gameCanvas.Children.Remove(dot);
                BigDots.RemoveAt(i);
                currentScore += 50;
                audioPlayer.Chomp();
                OnScore?.Invoke(50);
                ScoreService.UpdateScore(currentScore);
            }
        }
    }
}