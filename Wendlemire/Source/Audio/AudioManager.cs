using System.IO;
using Microsoft.Xna.Framework.Audio;
using Wendlemire.Scenes.ArenaScene;

namespace Wendlemire.Audio;

public sealed class AudioManager : IAudio, IDisposable
{
    public const int MaxOverlappingPerCue = 4;
    private const float PitchJitter = 0.08f;
    private const float VolumeJitter = 0.08f;

    private readonly string _contentRoot;
    private readonly Dictionary<string, SoundEffect?> _cache = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, List<SoundEffectInstance>> _active = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _loggedMissing = new(StringComparer.OrdinalIgnoreCase);
    private readonly float[] _busVolumes = [1f, 1f, 1f];
    private readonly Random _rng = new();
    private bool _muted;

    public AudioManager(string contentRoot)
    {
        _contentRoot = contentRoot;
    }

    public void Apply(ClientSettings settings)
    {
        SetBusVolume(AudioBus.Master, settings.MasterVolume);
        SetBusVolume(AudioBus.Sfx, settings.SfxVolume);
        SetMuted(settings.AudioMuted);
    }

    public void Play(string cue)
    {
        if (_muted || string.IsNullOrEmpty(cue))
        {
            return;
        }

        var effect = Load(cue);
        if (effect == null)
        {
            return;
        }

        PruneFinished(cue);
        if (ActiveCount(cue) >= MaxOverlappingPerCue)
        {
            return;
        }

        var instance = effect.CreateInstance();
        var volume = _busVolumes[(int)AudioBus.Master] * _busVolumes[(int)AudioBus.Sfx];
        volume *= 1f + ((float)_rng.NextDouble() * 2f - 1f) * VolumeJitter;
        instance.Volume = Math.Clamp(volume, 0f, 1f);
        instance.Pitch = ((float)_rng.NextDouble() * 2f - 1f) * PitchJitter;
        instance.Play();
        Track(cue, instance);
    }

    public void SetBusVolume(AudioBus bus, float volume)
    {
        _busVolumes[(int)bus] = Math.Clamp(volume, 0f, 1f);
    }

    public void SetMuted(bool muted)
    {
        _muted = muted;
        if (!muted)
        {
            return;
        }

        foreach (var instances in _active.Values)
        {
            foreach (var instance in instances)
            {
                instance.Stop();
            }
        }
    }

    public void Dispose()
    {
        foreach (var instances in _active.Values)
        {
            foreach (var instance in instances)
            {
                instance.Dispose();
            }
        }

        _active.Clear();

        foreach (var effect in _cache.Values)
        {
            effect?.Dispose();
        }

        _cache.Clear();
    }

    private SoundEffect? Load(string cue)
    {
        if (_cache.TryGetValue(cue, out var cached))
        {
            return cached;
        }

        if (!AudioCatalog.TryGetRelativePath(cue, out var relative))
        {
            LogMissing(cue, "unknown cue");
            _cache[cue] = null;
            return null;
        }

        var fullPath = Path.Combine(_contentRoot, relative.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(fullPath))
        {
            LogMissing(cue, fullPath);
            _cache[cue] = null;
            return null;
        }

        try
        {
            using var stream = File.OpenRead(fullPath);
            var effect = SoundEffect.FromStream(stream);
            _cache[cue] = effect;
            return effect;
        }
        catch (Exception ex)
        {
            Log.Warning($"Audio cue '{cue}' failed to load from '{fullPath}': {ex.Message}");
            _cache[cue] = null;
            return null;
        }
    }

    private void Track(string cue, SoundEffectInstance instance)
    {
        if (!_active.TryGetValue(cue, out var list))
        {
            list = [];
            _active[cue] = list;
        }

        list.Add(instance);
    }

    private int ActiveCount(string cue)
    {
        return _active.TryGetValue(cue, out var list) ? list.Count : 0;
    }

    private void PruneFinished(string cue)
    {
        if (!_active.TryGetValue(cue, out var list))
        {
            return;
        }

        for (var i = list.Count - 1; i >= 0; i--)
        {
            if (list[i].State != SoundState.Stopped)
            {
                continue;
            }

            list[i].Dispose();
            list.RemoveAt(i);
        }
    }

    private void LogMissing(string cue, string detail)
    {
        if (!_loggedMissing.Add(cue))
        {
            return;
        }

        Log.Warning($"Audio cue '{cue}' is silent ({detail})");
    }
}
