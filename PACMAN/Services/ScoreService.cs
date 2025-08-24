using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using PACMAN.Models;

namespace PACMAN.Services;

public class ScoreService
{
    private static readonly string path = Path.Combine(AppContext.BaseDirectory, "Assets", "Data", "score.json");
    private static readonly JsonSerializerOptions options = new JsonSerializerOptions { WriteIndented = true };

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

    public static List<Score> LoadScores()
    {
        if (!File.Exists(path))
        {
            return new List<Score>();
        }
        
        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<List<Score>>(json) ?? new List<Score>();
    }

    public static void SaveScore(List<Score> scores)
    {
        var json = JsonSerializer.Serialize(scores, options);
        File.WriteAllText(path, json);
    }

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