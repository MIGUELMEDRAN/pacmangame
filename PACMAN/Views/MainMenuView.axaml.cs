using Avalonia.Controls;
using System;
using System.Collections.Generic;
using Avalonia.Interactivity;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Layout;
using PACMAN.Services;

namespace PACMAN.Views;

public partial class MainMenuView : UserControl
{
    public MainMenuView()
    {
        InitializeComponent();
        ScoreService.Initialize();
    }

    private void StartGameClick(object sender, RoutedEventArgs e)
    {
        ScoreService.ResetPlayerScore("Jugador");
        if (this.VisualRoot is MainWindow mainWindow)
        {
            mainWindow.LoadGameView();
        }
    }

    private async void ViewScoreClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            var scores = ScoreService.LoadScores();

            if (scores != null && scores.Count > 0)
            {
                var formattedScores = new List<string>();

                foreach (var score in scores)
                {
                    formattedScores.Add($"{score.PlayerName}: {score.HighScore}");
                }

                var scoreWindow = new ScoreBoardWindow(formattedScores);
                await scoreWindow.ShowDialog((Window)this.VisualRoot);
            }
            else
            {
                await MessageBox("No se encontraron puntajes.");
            }
        }
        catch (Exception ex)
        {
            await MessageBox($"No se pudo cargar el puntaje.\nError: {ex.Message}");
        }
    }

    private void ExitClick(object? sender, RoutedEventArgs e)
    {
        (this.VisualRoot as MainWindow)?.Close();
    }

    private async Task MessageBox(string message)
    {
        var dialog = new Window
        {
            Width = 300,
            Height = 150,
            Title = "Mensaje"
        };

        var okButton = new Button
        {
            Content = "Ok",
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(10),
            Width = 80,
        };
        
        okButton.Click += (_, _) => dialog.Close();

        dialog.Content = new StackPanel
        {
            Children =
            {
                new TextBlock { Text = message, Margin = new Thickness(10) },
                okButton
            }
        };
        
        await dialog.ShowDialog((Window)this.VisualRoot);
    }
}