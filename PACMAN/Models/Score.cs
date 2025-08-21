namespace PACMAN.Models;

/// <summary>
/// Representa la puntuacion de un jugador.
/// </summary>
public class Score
{
    /// <summary>
    /// Obtiene o estable la puntuacion mas alta del jugador
    /// </summary>
    public int HighScore { get; set; }
    
    /// <summary>
    /// Obtiene el nombre del jugador asociado.
    /// Por defecto es "jugador".
    /// </summary>
    public string PlayerName { get; set; } = "Jugador";
}