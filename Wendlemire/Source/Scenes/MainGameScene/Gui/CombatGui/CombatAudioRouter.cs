using Wendlemire.Audio;
using Wendlemire.Sim.Combat;

namespace Wendlemire.Scenes.MainGameScene.Gui.CombatGui;

public sealed class CombatAudioRouter
{
    private readonly IAudio _audio;

    public CombatAudioRouter(IAudio audio)
    {
        _audio = audio;
    }

    public void Handle(CombatLogEvent e)
    {
        switch (e.Kind)
        {
            case CombatEventKind.Damage:
                _audio.Play(e.IsCritical ? AudioCues.Crit : AudioCues.Hit);
                if (e.Blocked > 0)
                {
                    _audio.Play(AudioCues.Block);
                }

                PlaySubEffects(e.SubEffects);
                break;
            case CombatEventKind.Block:
                _audio.Play(AudioCues.Block);
                break;
            case CombatEventKind.Miss:
                _audio.Play(AudioCues.Miss);
                break;
            case CombatEventKind.Dodge:
                _audio.Play(AudioCues.Dodge);
                break;
            case CombatEventKind.Death:
                _audio.Play(AudioCues.Death);
                break;
            case CombatEventKind.PotionUsed:
                _audio.Play(AudioCues.Potion);
                break;
            case CombatEventKind.MedicalUsed:
                _audio.Play(AudioCues.Medical);
                break;
            case CombatEventKind.IncenseLit:
                _audio.Play(AudioCues.Incense);
                break;
            case CombatEventKind.PartSevered:
            case CombatEventKind.PartDestroyed:
                _audio.Play(AudioCues.Sever);
                break;
            case CombatEventKind.EquipmentDestroyed:
                _audio.Play(AudioCues.Destroy);
                break;
            case CombatEventKind.Heal:
            case CombatEventKind.DamageOverTime:
            case CombatEventKind.BuffApplied:
            case CombatEventKind.DebuffApplied:
            case CombatEventKind.StatusReflected:
            case CombatEventKind.System:
                break;
        }
    }

    private void PlaySubEffects(CombatSubEffect[] subEffects)
    {
        foreach (var sub in subEffects)
        {
            switch (sub.Kind)
            {
                case CombatEventKind.PartSevered:
                case CombatEventKind.PartDestroyed:
                    _audio.Play(AudioCues.Sever);
                    break;
                case CombatEventKind.EquipmentDestroyed:
                    _audio.Play(AudioCues.Destroy);
                    break;
            }
        }
    }
}
