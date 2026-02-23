using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using PACMAN.Models;

namespace PACMAN.Services;

/// <summary>
/// Servicio responsable de gestionar las puntuaciones de los jugadores.
/// </summary>
public class ScoreService
{
    private static readonly string PathToScores = Path.Combine(AppContext.BaseDirectory, "Assets", "Data", "score.json");
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    /// <summary>
    /// Inicializa el archivo de puntuaciones si no existe, creando una entrada inicial.
    /// </summary>
    public static void Initialize()
    {
        var directory = Path.GetDirectoryName(PathToScores);
        if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        if (!File.Exists(PathToScores))
        {
            SaveScore([new Score { PlayerName = "Jugador", HighScore = 0 }]);
        }
    }

    /// <summary>
    /// Carga las puntaciones desde el archivo JSON.
    /// </summary>
    /// <returns>Una lista de objetos <see cref="Score"/> representando las puntuaciones.</returns>
    public static List<Score> LoadScores()
    {
        if (!File.Exists(PathToScores))
        {
            return [];
        }

        var json = File.ReadAllText(PathToScores);
        return JsonSerializer.Deserialize<List<Score>>(json) ?? [];
    }

    /// <summary>
    /// Guarda la lista de puntuaciones en el archivo JSON.
    /// </summary>
    /// <param name="scores">Lista de objetos <see cref="Score"/> a guardar.</param>
    public static void SaveScore(List<Score> scores)
    {
        var orderedScores = scores
            .OrderByDescending(s => s.HighScore)
            .ThenBy(s => s.PlayerName, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var json = JsonSerializer.Serialize(orderedScores, JsonOptions);
        File.WriteAllText(PathToScores, json);
    }

    /// <summary>
    /// Actualiza la puntuacion del jugador actual. Si no existe, se crea una nueva entrada.
    /// </summary>
    /// <param name="newScore">Nueva puntuacion a guardar.</param>
    public static void UpdateScore(int newScore)
    {
        var scores = LoadScores();
        var player = scores.Find(s => s.PlayerName.Equals("Jugador", StringComparison.OrdinalIgnoreCase));

        if (player is null)
        {
            scores.Add(new Score { PlayerName = "Jugador", HighScore = Math.Max(0, newScore) });
        }
        else
        {
            player.HighScore = Math.Max(player.HighScore, newScore);
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

        if (player is null)
        {
            scores.Add(new Score { PlayerName = playerName, HighScore = 0 });
        }
        else
        {
            player.HighScore = 0;
        }

        SaveScore(scores);
    }
}
