using Avalonia.Controls;
using System.Collections.Generic;

namespace PACMAN.Views;

/// <summary>
/// Ventana que muestra el tablero de puntuaciones.
/// </summary>
public partial class ScoreBoardWindow : Window
{
    public ScoreBoardWindow()
    {
        InitializeComponent();
        ScoreList.ItemsSource = new List<string>();
    }

    /// <summary>
    /// Inicializa una nueva instancia de la clase <see cref="ScoreBoardWindow"/>.
    /// Establece la lista de puntuaciones formateadas que se mostraran.
    /// </summary>
    /// <param name="formattedScores">Lista de puntuaciones formateadas para mostrar en el tablero.</param>
    public ScoreBoardWindow(List<string> formattedScores) : this()
    {
        ScoreList.ItemsSource = formattedScores;
    }
}
