using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using PACMAN.ViewModels;
using PACMAN.Audio;

namespace PACMAN.Views;

public partial class GameView : UserControl
{
    private GameViewModel _viewModel;
    private AudioPlayer _audioPlayer;
    
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

    public void UpdateScoreDisplay(int score)
    {
        ScoreText.Text = $"Puntaje: {score}";
    }

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