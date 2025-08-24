using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using PACMAN.Models;

namespace PACMAN.Services;

/// <summary>
/// Servicio responsable de gestionar las puntuaciones de los jugadores.
/// </summary>
public class ScoreService
{
    private static readonly string path = Path.Combine(AppContext.BaseDirectory, "Assets", "Data", "score.json");
    private static readonly JsonSerializerOptions options = new JsonSerializerOptions { WriteIndented = true };

    /// <summary>
    /// Inicializa el archivo de puntuaciones si no existe, creando una entrada inicial.
    /// </summary>
    public static void Initialize()
    {
        if (!File.Exists(path))
        {
            var initialScores = new List<Score>
            {
                new Score { PlayerName = "Jugador", HighScore = 0 }
            };
            SaveScore(initialScores);
        }
    }

    /// <summary>
    /// Carga las puntaciones desde el archivo JSON.
    /// </summary>
    /// <returns>Una lista de objetos <see cref="Score"/> representando las puntuaciones.</returns>
    public static List<Score> LoadScores()
    {
        if (!File.Exists(path))
        {
            return new List<Score>();
        }
        
        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<List<Score>>(json) ?? new List<Score>();
    }

    /// <summary>
    /// Guarda la lista de puntuaciones en el archivo JSON.
    /// </summary>
    /// <param name="scores">Lista de objetos <see cref="Score"/> a guardar.</param>
    public static void SaveScore(List<Score> scores)
    {
        var json = JsonSerializer.Serialize(scores, options);
        File.WriteAllText(path, json);
    }

    /// <summary>
    /// Actualiza la puntuacion del jugador actual. Si no existe, se crea una nueva entrada.
    /// </summary>
    /// <param name="newScore">Nueva puntuacion a guardar.</param>
    public static void UpdateScore(int newScore)
    {
        var scores = LoadScores();

        if (scores.Count > 0)
        {
            scores[0].HighScore = newScore;
        }
        else
        {
            scores.Add(new Score { PlayerName = "Jugador", HighScore = newScore });
        }
        SaveScore(scores);
    }

    /// <summary>
    /// Reinicia la puntuacion del jugador especificado a cero.
    /// </summary>
    /// <param name="playerName">Nombre del jugador cuya puntuacion se desea reiniciar.</param>
    public static void ResetPlayerScore(string playerName)
    {
        var scores = LoadScores();
        
        var player = scores.Find(s => s.PlayerName.Equals(playerName, StringComparison.OrdinalIgnoreCase));

        if (player != null)
        {
            player.HighScore = 0;
            SaveScore(scores);
        }
    }
}