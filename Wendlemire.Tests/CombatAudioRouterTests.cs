using Wendlemire.Audio;
using Wendlemire.Scenes.MainGameScene.Gui.CombatGui;
using Wendlemire.Sim.Combat;
using Xunit;

namespace Wendlemire.Tests;

public class CombatAudioRouterTests
{
    [Fact]
    public void DamagePlaysHit()
    {
        var audio = new FakeAudio();
        var router = new CombatAudioRouter(audio);

        router.Handle(Event(CombatEventKind.Damage));

        Assert.Equal([AudioCues.Hit], audio.Played);
    }

    [Fact]
    public void CriticalDamagePlaysCrit()
    {
        var audio = new FakeAudio();
        var router = new CombatAudioRouter(audio);

        router.Handle(Event(CombatEventKind.Damage) with { IsCritical = true });

        Assert.Equal([AudioCues.Crit], audio.Played);
    }

    [Fact]
    public void DamageWithBlockPlaysHitAndBlock()
    {
        var audio = new FakeAudio();
        var router = new CombatAudioRouter(audio);

        router.Handle(Event(CombatEventKind.Damage) with { Blocked = 4 });

        Assert.Equal([AudioCues.Hit, AudioCues.Block], audio.Played);
    }

    [Theory]
    [InlineData(CombatEventKind.Miss, AudioCues.Miss)]
    [InlineData(CombatEventKind.Dodge, AudioCues.Dodge)]
    [InlineData(CombatEventKind.Death, AudioCues.Death)]
    [InlineData(CombatEventKind.PotionUsed, AudioCues.Potion)]
    [InlineData(CombatEventKind.MedicalUsed, AudioCues.Medical)]
    [InlineData(CombatEventKind.IncenseLit, AudioCues.Incense)]
    [InlineData(CombatEventKind.PartSevered, AudioCues.Sever)]
    [InlineData(CombatEventKind.PartDestroyed, AudioCues.Sever)]
    [InlineData(CombatEventKind.EquipmentDestroyed, AudioCues.Destroy)]
    [InlineData(CombatEventKind.Block, AudioCues.Block)]
    public void EventKindPlaysExpectedCue(CombatEventKind kind, string cue)
    {
        var audio = new FakeAudio();
        var router = new CombatAudioRouter(audio);

        router.Handle(Event(kind));

        Assert.Equal([cue], audio.Played);
    }

    [Fact]
    public void DamageSubEffectsPlaySeverAndDestroy()
    {
        var audio = new FakeAudio();
        var router = new CombatAudioRouter(audio);

        router.Handle(Event(CombatEventKind.Damage) with
        {
            SubEffects =
            [
                new CombatSubEffect { Kind = CombatEventKind.PartSevered },
                new CombatSubEffect { Kind = CombatEventKind.EquipmentDestroyed },
                new CombatSubEffect { Kind = CombatEventKind.BuffApplied }
            ]
        });

        Assert.Equal([AudioCues.Hit, AudioCues.Sever, AudioCues.Destroy], audio.Played);
    }

    [Theory]
    [InlineData(CombatEventKind.Heal)]
    [InlineData(CombatEventKind.DamageOverTime)]
    [InlineData(CombatEventKind.System)]
    [InlineData(CombatEventKind.BuffApplied)]
    [InlineData(CombatEventKind.DebuffApplied)]
    [InlineData(CombatEventKind.StatusReflected)]
    public void LowSignalEventsPlayNothing(CombatEventKind kind)
    {
        var audio = new FakeAudio();
        var router = new CombatAudioRouter(audio);

        router.Handle(Event(kind));

        Assert.Empty(audio.Played);
    }

    private static CombatLogEvent Event(CombatEventKind kind) => new() { Kind = kind };

    private sealed class FakeAudio : IAudio
    {
        public List<string> Played { get; } = [];

        public void Play(string cue) => Played.Add(cue);

        public void SetBusVolume(AudioBus bus, float volume)
        {
        }

        public void SetMuted(bool muted)
        {
        }
    }
}
