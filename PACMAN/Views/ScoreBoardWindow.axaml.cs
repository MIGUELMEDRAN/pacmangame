using Avalonia.Controls;
using System.Collections.Generic;

namespace PACMAN.Views;

/// <summary>
/// Ventana que muestra el tablero de puntuaciones.
/// </summary>
public partial class ScoreBoardWindow : Window
{
    /// <summary>
    /// Inicializa una nueva instancia de la clase <see cref="ScoreBoardWindow"/>
    /// Establece la lista de puntuaciones formateadas que se mostraran
    /// </summary>
    /// <param name="formattedSCores">Lista de puntuaciones formateadas para mostrar en el tablero.</param>
    public ScoreBoardWindow(List<string> formattedSCores)
    {
        InitializeComponent();
        
        ScoreList.ItemsSource = formattedSCores;
    }
}