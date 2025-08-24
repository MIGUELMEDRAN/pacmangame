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

/// <summary>
/// Representa los puntos pequeños y grandes (power-ups).
/// </summary>
public class Dots
{
    /// <summary>
    /// Obtiene la lista de puntos pequeños en el juego.
    /// </summary>
    public List<Ellipse> SmallDots { get; private set; } = new();
    
    /// <summary>
    /// Obtiene la lista de puntos grandes (power-ups) en el juego.
    /// </summary>
    public List<Control> BigDots { get; private set; } = new();
    
    private AudioPlayer audioPlayer;

    private int currentScore = 0;

    /// <summary>
    /// Evento que se dispara cuando el jugador gana puntos.
    /// </summary>
    public event Action<int>? OnScore;

    /// <summary>
    /// Inicializa una nueva instancia de la clase <see cref="Dots"/> con un reproductor de audio.
    /// </summary>
    /// <param name="audioPlayer">Instancia de <see cref="AudioPlayer"/> usada para reproducir sonidos.</param>
    /// <exception cref="ArgumentNullException">Se lanza si <paramref name="audioPlayer"/> es null.</exception>
    public Dots(AudioPlayer audioPlayer)
    {
        this.audioPlayer = audioPlayer ?? throw new ArgumentNullException(nameof(audioPlayer));
    }

    /// <summary>
    /// Crea y dibuja los puntos pequeños (blancos) en el canvas, evitando las paredes.
    /// </summary>
    /// <param name="canvas">El canvas donde se dibujan los puntos.</param>
    /// <param name="walls">Lista de rectangulos que representan las paredes del juego.</param>
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
    
    /// <summary>
    /// Crea y dibuja los puntos grandes (power-ups) en posiciones fijas dentro del canvas.
    /// </summary>
    /// <param name="canvas">El canvas del juego donde se dibujan los power-ups.</param>
    public void CreatePowerUps(Canvas canvas)
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
            canvas.Children.Add(powerUp);
            BigDots.Add(powerUp);
        }
    }

    /// <summary>
    /// Verifica colisiones entre pacman y los puntos y actualiza la puntuacion.
    /// </summary>
    /// <param name="canvas">El canvas del juego para eliminar visualmente los puntos recogidos.</param>
    /// <param name="pacmanBounds">Los limites del jugador pacman utilizados para detectar colisiones.</param>
    public void CheckDotCollision(Canvas canvas, Rect pacmanBounds)
    {
        for (int i = SmallDots.Count - 1; i >= 0; i--)
        {
            var dot = SmallDots[i];
            double x = Canvas.GetLeft(dot);
            double y = Canvas.GetTop(dot);
            var dotBounds = new Rect(x, y, dot.Width, dot.Height);

            if (pacmanBounds.Intersects(dotBounds))
            {
                canvas.Children.Remove(dot);
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
                canvas.Children.Remove(dot);
                BigDots.RemoveAt(i);
                currentScore += 50;
                audioPlayer.Chomp();
                OnScore?.Invoke(50);
                ScoreService.UpdateScore(currentScore);
            }
        }
    }
}