namespace Wendlemire.Audio;

public static class AudioCatalog
{
    private static readonly Dictionary<string, string> Paths = new(StringComparer.OrdinalIgnoreCase)
    {
        [AudioCues.Hit] = "Audio/Combat/hit.wav",
        [AudioCues.Crit] = "Audio/Combat/crit.wav",
        [AudioCues.Block] = "Audio/Combat/block.wav",
        [AudioCues.Miss] = "Audio/Combat/miss.wav",
        [AudioCues.Dodge] = "Audio/Combat/dodge.wav",
        [AudioCues.Death] = "Audio/Combat/death.wav",
        [AudioCues.Potion] = "Audio/Combat/potion.wav",
        [AudioCues.Medical] = "Audio/Combat/medical.wav",
        [AudioCues.Incense] = "Audio/Combat/incense.wav",
        [AudioCues.Sever] = "Audio/Combat/sever.wav",
        [AudioCues.Destroy] = "Audio/Combat/destroy.wav"
    };

    public static bool TryGetRelativePath(string cue, out string path)
    {
        return Paths.TryGetValue(cue, out path!);
    }
}
