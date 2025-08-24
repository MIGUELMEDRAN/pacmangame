using LibVLCSharp.Shared;
using System;
using System.IO;
using System.Threading.Tasks;

namespace PACMAN.Audio;

/// <summary>
/// Reproduce efectos de sonido para el juego PACMAN utilizando LibCLV.
/// </summary>
public class AudioPlayer : IDisposable
{
    private readonly LibVLC libVLC;
    private readonly MediaPlayer mediaPlayer;
    private readonly string chompPath;
    private readonly string deadPath;
    
    private volatile bool IsGameOver = false;

    /// <summary>
    /// Inicializa una nueva instancia de la clase <see cref="AudioPlayer"/>
    /// Carga los archivos de audio y configura LibVLC.
    /// </summary>
    public AudioPlayer()
    {
        Core.Initialize();
        libVLC = new LibVLC("--quiet", "--no-xlib");
        mediaPlayer = new MediaPlayer(libVLC);
        
        chompPath = Path.Combine(AppContext.BaseDirectory, "Assets", "Sound", "chomp.mp3");
        deadPath = Path.Combine(AppContext.BaseDirectory, "Assets", "Sound", "dead.wav");
    }

    /// <summary>
    /// Reproduce el sonido "chomp" (comer punto).
    /// </summary>
    public void Chomp()
    {
        if (!IsGameOver)
        {
            Play(chompPath);
        }
    }

    /// <summary>
    /// Reproduce el sonido "dead" (muerte del pacman).
    /// </summary>
    public void Dead()
    {
        IsGameOver = true;

        Task.Run(() =>
        {
            lock (mediaPlayer)
            {
                if (mediaPlayer.IsPlaying)
                {
                    mediaPlayer.Stop();
                }

                using var media = new Media(libVLC, deadPath, FromType.FromPath);
                mediaPlayer.Media = media;
                mediaPlayer.Play();
            }
        });
    }

    private void Play(string path)
    {
        Task.Run(() =>
        {
            if (mediaPlayer.IsPlaying)
            {
                mediaPlayer.Stop();
            }
            
            using var media = new  Media(libVLC, path, FromType.FromPath);
            mediaPlayer.Media = media;
            mediaPlayer.Play();
        });
    }

    /// <summary>
    /// Libera los recursos utilizados por la instancia de <see cref="AudioPlayer"/>
    /// </summary>
    public void Dispose()
    {
        mediaPlayer.Dispose();
        libVLC.Dispose();
    }
}