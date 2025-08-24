using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using PACMAN.Models;
using PACMAN.Services;
using PACMAN.Views;
using PACMAN.Audio;
using System;
using System.Collections.Generic;

namespace PACMAN.ViewModels;

/// <summary>
/// ViewModel principal que gestiona la logica del juego.
/// </summary>
public class GameViewModel
{
    /// <summary>
    /// Obtiene la instancia actual del juagdor.
    /// </summary>
    public Pacman Pacman { get; private set; }
    
    /// <summary>
    /// Obtiene la coleccion de puntos.
    /// </summary>
    public Dots Dots { get; private set; }
    
    /// <summary>
    /// Obtiene la lista de muros.
    /// </summary>
    public List<Rect> Walls { get; private set; } = new();
    
    /// <summary>
    /// Obtiene la lista de fantasmas.
    /// </summary>
    public List<Ghost> Ghosts { get; private set; } = new();
    
    private DispatcherTimer _animationTimer;
    private DispatcherTimer _ghostTimer;

    private GameView _gameView;
    private int _score;
    private Random _rand = new();
    
    private readonly AudioPlayer _audioPlayer;

    /// <summary>
    /// Inicializa una nueva instancia de la clase <see cref="GameViewModel"/>.
    /// </summary>
    /// <param name="gameView">Vista del juego.</param>
    /// <param name="open">Imagen de pacman con la boca abierta.</param>
    /// <param name="closed">IMagen de pacman con la boca cerrada.</param>
    /// <param name="canvas">Lienzo principal del juego.</param>
    /// <param name="audioPlayer">Reproductor de sonidos.</param>
    /// <exception cref="ArgumentNullException">Se lanza si alguno de los parametros requeridos es nulo.</exception>
    public GameViewModel(GameView gameView, Image open, Image closed, Canvas canvas, AudioPlayer audioPlayer)
    {
        _gameView = gameView ?? throw new ArgumentNullException(nameof(gameView));
        _audioPlayer = audioPlayer ?? throw new ArgumentNullException(nameof(audioPlayer));
        
        Pacman = new Pacman(open, closed);
        Dots = new Dots(_audioPlayer);

        Dots.OnScore += HandleScoreUpdate;

        CreateMaze(canvas);
        Dots.CreateDots(canvas, Walls);
        Dots.CreatePowerUps(canvas);

        _animationTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(300)
        };
        _animationTimer.Tick += (_, _) => Pacman.ToggleMouth();
        _animationTimer.Start();
        
        CreateGhost(gameView.RedGhost, 300, 300);
        CreateGhost(gameView.BlueGhost, 200, 200);

        _ghostTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(250)
        };
        _ghostTimer.Tick += (_, _) => MoveGhosts();
        _ghostTimer.Start();
        
        ScoreService.UpdateScore(0);
        _gameView.UpdateScoreDisplay(0);
    }

    private void CreateGhost(Image image, double x, double y)
    {
        var ghost = new Ghost(x, y);
        ghost.PositionChanged += (newX, newY) =>
        {
            Canvas.SetLeft(image, newX);
            Canvas.SetTop(image, newY);
        };
        ghost.Move(x, y);
        Ghosts.Add(ghost);
    }

    /// <summary>
    /// Maneja el evento de presionar una tecla para mover a pacman.
    /// </summary>
    /// <param name="e">Datos del evento de tecla presionada.</param>
    /// <param name="canvas">Lienzo del juego.</param>
    public void OnKeyDown(KeyEventArgs e, Canvas canvas)
    {
        double newX = Pacman.X, newY = Pacman.Y;
        double angle = 0;
        var playerWidth = Pacman.OpenImage.Bounds.Width;
        var playerHeight = Pacman.OpenImage.Bounds.Height;

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
        
        var futureBounds = new Rect(newX, newY, playerWidth, playerHeight);
        bool collides = Walls.Exists(w => w.Intersects(futureBounds));
        if (collides)
        {
            return;
        }
        
        Pacman.Move(newX, newY);
        Pacman.Rotate(angle);
        Dots.CheckDotCollision(canvas, Pacman.GetBounds());
        CheckGhostCollision();
    }

    private void MoveGhosts()
    {
        foreach (var ghost in Ghosts)
        {
            var dir = GetDirectionToPacman(ghost);
            var newX = ghost.X + dir.X + ghost.Speed;
            var newY = ghost.Y + dir.Y + ghost.Speed;
            
            var bounds = new Rect(newX, newY, ghost.Width, ghost.Height);
            if (!Walls.Exists(w => w.Intersects(bounds)))
            {
                ghost.Move(newX, newY);
                CheckGhostCollision();
            }
        }
    }

    private (int X, int Y) GetDirectionToPacman(Ghost ghost)
    {
        int dx = 0, dy = 0;
        
        if (Pacman.X < ghost.X) dx = -1;
        else if (Pacman.X > ghost.X) dx = 1;
        
        if (Pacman.Y < ghost.Y) dy = -1;
        else if (Pacman.Y > ghost.Y) dy = 1;

        if (_rand.Next(0, 5) == 0)
        {
            dx = _rand.Next(-1, 2);
            dy = _rand.Next(-1, 2);
        }

        return (dx, dy);
    }

    private void CheckGhostCollision()
    {
        var pacmanBounds = Pacman.GetBounds();
        foreach (var ghost in Ghosts)
        {
            if (ghost.GetBounds().Intersects(pacmanBounds))
            {
                GameOver();
                break;
            }
        }
    }

    private void GameOver()
    {
        _animationTimer.Stop();
        _ghostTimer.Stop();
        _audioPlayer.Dead();
        _gameView.ShowGameOver();
    }

    private void CreateMaze(Canvas canvas)
    {
        var wallColor = Brushes.Blue;
        var wallSpecs = new (double x, double y, double w, double h)[]
        {
            (0, 0, 600, 20), (0, 580, 600, 20),
            (0, 0, 20, 600), (580, 0, 20, 600),
            (100, 100, 100, 20), (200, 100, 20, 100),
            (250, 200, 150, 20), (400, 100, 20, 150),
            (300, 1, 20, 150), (150, 300, 100, 20),
            (350, 300, 100, 20), (100, 400, 20, 100),
            (480, 400, 20, 100)
        };

        foreach (var (x, y, w, h) in wallSpecs)
        {
            var rect = new Rectangle
            {
                Width = w,
                Height = h,
                Fill = wallColor
            };
            Canvas.SetLeft(rect, x);
            Canvas.SetTop(rect, y);
            canvas.Children.Add(rect);
            Walls.Add(new Rect(x, y, w, h));
        }
    }

    private void HandleScoreUpdate(int points)
    {
        _score += points;
        _gameView.UpdateScoreDisplay(_score);
        ScoreService.UpdateScore(_score);
    }
}