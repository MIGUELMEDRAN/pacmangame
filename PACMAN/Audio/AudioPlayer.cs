using LibVLCSharp.Shared;
using System;
using System.IO;
using System.Threading.Tasks;

namespace PACMAN.Audio;

public class AudioPlayer : IDisposable
{
    private readonly LibVLC _libVlc;
    private readonly MediaPlayer _mediaPlayer;
    private readonly string _chompPath;
    private readonly string _deadPath;

    private volatile bool _isGameOver;

    public AudioPlayer()
    {
        Core.Initialize();
        _libVlc = new LibVLC("--quiet", "--no-xlib");
        _mediaPlayer = new MediaPlayer(_libVlc);

        _chompPath = Path.Combine(AppContext.BaseDirectory, "Assets", "Sound", "chomp.mp3");
        _deadPath = Path.Combine(AppContext.BaseDirectory, "Assets", "Sound", "dead.wav");
    }

    public void Chomp()
    {
        if (_isGameOver)
        {
            return;
        }

        Play(_chompPath);
    }

    public void PowerUp()
    {
        if (_isGameOver)
        {
            return;
        }

        Play(_chompPath);
    }

    public void LevelUp()
    {
        if (_isGameOver)
        {
            return;
        }

        Play(_chompPath);
    }

    public void Dead()
    {
        _isGameOver = true;

        Task.Run(() =>
        {
            lock (_mediaPlayer)
            {
                if (_mediaPlayer.IsPlaying)
                {
                    _mediaPlayer.Stop();
                }

                using var media = new Media(_libVlc, _deadPath, FromType.FromPath);
                _mediaPlayer.Media = media;
                _mediaPlayer.Play();
            }
        });
    }

    private void Play(string path)
    {
        Task.Run(() =>
        {
            lock (_mediaPlayer)
            {
                if (_mediaPlayer.IsPlaying)
                {
                    _mediaPlayer.Stop();
                }

                using var media = new Media(_libVlc, path, FromType.FromPath);
                _mediaPlayer.Media = media;
                _mediaPlayer.Play();
            }
        });
    }

    public void Dispose()
    {
        _mediaPlayer.Dispose();
        _libVlc.Dispose();
    }
}
