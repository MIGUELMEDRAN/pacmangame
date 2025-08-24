using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using PACMAN.ViewModels;
using PACMAN.Audio;

namespace PACMAN.Views;

/// <summary>
/// Vista principal del juego.
/// </summary>
public partial class GameView : UserControl
{
    private GameViewModel _viewModel;
    private AudioPlayer _audioPlayer;
    
    /// <summary>
    /// Inicializa una nueva instancia de la clase <see cref="GameView"/>
    /// </summary>
    public GameView()
    {
        InitializeComponent();

        this.AttachedToVisualTree += (_, _) =>
        {
            _audioPlayer = new AudioPlayer();

            _viewModel = new GameViewModel(this, PacmanOpen, PacmanClosed, GameCanvas, _audioPlayer);

            this.Focus();
        };
    }

    private void OnKeyDown(object sender, KeyEventArgs e)
    {
        _viewModel?.OnKeyDown(e, GameCanvas);
    }

    private void OnBackToMenuClick(object sender, RoutedEventArgs e)
    {
        if (this.VisualRoot is MainWindow mainWindow)
        {
            mainWindow.LoadMainMenuView();
        }
    }

    /// <summary>
    /// Actualiza el texto en pantalla que muestra el puntaje.
    /// </summary>
    /// <param name="score">Puntaje actual del jugador.</param>
    public void UpdateScoreDisplay(int score)
    {
        ScoreText.Text = $"Score: {score}";
    }

    /// <summary>
    /// Muestra el mensaje de Game Over en la pantalla.
    /// </summary>
    public void ShowGameOver()
    {
        GameOverText.IsVisible = true;
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        _audioPlayer?.Dispose();
    }
}