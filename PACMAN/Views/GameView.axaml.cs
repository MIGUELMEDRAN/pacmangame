using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using PACMAN.Audio;
using PACMAN.ViewModels;

namespace PACMAN.Views;

public partial class GameView : UserControl
{
    private GameViewModel? _viewModel;
    private AudioPlayer? _audioPlayer;

    public Image[] GhostImages => [RedGhost, BlueGhost, PurpleGhost, OrangeGhost];
    public Image BossImage => BossGhost;

    public GameView()
    {
        InitializeComponent();

        AttachedToVisualTree += (_, _) =>
        {
            _audioPlayer = new AudioPlayer();
            _viewModel = new GameViewModel(this, PacmanOpen, PacmanClosed, GameCanvas, _audioPlayer);
            Focus();
        };
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        _viewModel?.OnKeyDown(e);
    }

    private void OnBackToMenuClick(object? sender, RoutedEventArgs e)
    {
        if (VisualRoot is MainWindow mainWindow)
        {
            mainWindow.LoadMainMenuView();
        }
    }

    public void UpdateScoreDisplay(int score)
    {
        ScoreText.Text = $"Score: {score}";
    }

    public void UpdateLevelDisplay(int level, string theme)
    {
        LevelText.Text = $"Nivel: {level}";
        ThemeText.Text = $"Tema: {theme}";
    }

    public void UpdateTheme(Color canvasColor)
    {
        GameCanvas.Background = new SolidColorBrush(canvasColor);
    }

    public void ShowGameOver()
    {
        GameOverText.IsVisible = true;
    }

    public void ShowWinMessage()
    {
        WinText.IsVisible = true;
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        _audioPlayer?.Dispose();
    }
}
