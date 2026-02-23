using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Rendering;
using PACMAN.Services;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace PACMAN.Views;

/// <summary>
/// Representa la vista del menu principal.
/// </summary>
public partial class MainMenuView : UserControl
{
    /// <summary>
    /// Inicializa una nueva instancia de la clase <see cref="MainMenuView"/>.
    /// Tambien inicializa el servicio de puntajes.
    /// </summary>
    public MainMenuView()
    {
        InitializeComponent();
        ScoreService.Initialize();
    }

    private void StartGameClick(object? sender, RoutedEventArgs e)
    {
        ScoreService.ResetPlayerScore("Jugador");
        if (VisualRoot is MainWindow mainWindow)
        {
            mainWindow.LoadGameView();
        }
    }

    private async void ViewScoreClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            var scores = ScoreService
                .LoadScores()
                .OrderByDescending(score => score.HighScore)
                .Take(10)
                .ToList();

            if (scores.Count == 0)
            {
                await MessageBox("No se encontraron puntajes.");
                return;
            }

            var formattedScores = scores
                .Select((score, index) => $"#{index + 1}  {score.PlayerName}: {score.HighScore}")
                .ToList();

            var scoreWindow = new ScoreBoardWindow(formattedScores);
            var owner = TryGetOwnerWindow(VisualRoot);
            if (owner is not null)
            {
                await scoreWindow.ShowDialog(owner);
            }
            else
            {
                scoreWindow.Show();
            }
        }
        catch (Exception ex)
        {
            await MessageBox($"No se pudo cargar el puntaje.\nError: {ex.Message}");
        }
    }

    private void ExitClick(object? sender, RoutedEventArgs e)
    {
        (VisualRoot as MainWindow)?.Close();
    }

    private static Window? TryGetOwnerWindow(IRenderRoot? root)
    {
        return root as Window;
    }

    private async Task MessageBox(string message)
    {
        var dialog = new Window
        {
            Width = 340,
            Height = 180,
            Title = "Mensaje",
            CanResize = false,
            Background = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#0F172A"))
        };

        var okButton = new Button
        {
            Content = "Aceptar",
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(10),
            Width = 100,
        };

        okButton.Click += (_, _) => dialog.Close();

        dialog.Content = new StackPanel
        {
            Spacing = 8,
            Margin = new Thickness(10),
            Children =
            {
                new TextBlock
                {
                    Text = message,
                    TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                    Margin = new Thickness(10),
                    HorizontalAlignment = HorizontalAlignment.Stretch
                },
                okButton
            }
        };

        var owner = TryGetOwnerWindow(VisualRoot);
        if (owner is not null)
        {
            await dialog.ShowDialog(owner);
            return;
        }

        dialog.Show();
    }
}
