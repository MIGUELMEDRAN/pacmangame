using Avalonia.Controls;

namespace PACMAN.Views;

/// <summary>
/// Ventana principal que administra la navegacion entre ventanas
/// </summary>
public partial class MainWindow : Window
{
    /// <summary>
    /// Inicializa una nueva instancia de la clase <see cref="MainWindow"/>
    /// Establece la vista principal del menu al iniciar la aplicacion.
    /// </summary>
    public MainWindow()
    {
        InitializeComponent();

        MainContent.Content = new MainMenuView();
    }

    /// <summary>
    /// Carga la vista del juego en el contenedor pincipal.
    /// </summary>
    public void LoadGameView()
    {
        MainContent.Content = new GameView();
    }

    /// <summary>
    /// Carga la vista del menu principal en el contenedor principal.
    /// </summary>
    public void LoadMainMenuView()
    {
        MainContent.Content = new MainMenuView();
    }
}