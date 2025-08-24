using Avalonia.Controls;

namespace PACMAN.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        MainContent.Content = new MainMenuView();
    }

    public void LoadGameView()
    {
        MainContent.Content = new GameView();
    }

    public void LoadMainMenuView()
    {
        MainContent.Content = new MainMenuView();
    }
}