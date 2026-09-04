namespace Wendlemire.Audio;

public interface IAudio
{
    void Play(string cue);
    void SetBusVolume(AudioBus bus, float volume);
    void SetMuted(bool muted);
}
