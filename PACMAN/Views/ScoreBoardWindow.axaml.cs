using Avalonia.Controls;
using System.Collections.Generic;

namespace PACMAN.Views;

public partial class ScoreBoardWindow : Window
{
    public ScoreBoardWindow(List<string> formattedSCores)
    {
        InitializeComponent();
        
        ScoreList.ItemsSource = formattedSCores;
    }
}