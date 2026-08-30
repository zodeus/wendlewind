using System.Text;

namespace Wendlewind.Sim.Combat;

public static class CombatLogFormatter
{
    public static string? Format(CombatLogEvent e)
    {
        return e.Kind switch
        {
            CombatEventKind.Damage => FormatDamage(e),
            CombatEventKind.Miss =>
                $"/c[{TC.Attacker}]{e.SourceName} /c[{TC.Blue}]missed /c[{TC.Victim}]{e.SubjectName}.",
            CombatEventKind.Dodge =>
                $"/c[{TC.Victim}]{e.SubjectName} /c[{TC.Blue}]dodged attack",
            CombatEventKind.PotionUsed => FormatPotionUsed(e),
            CombatEventKind.MedicalUsed => FormatMedicalUsed(e),
            CombatEventKind.Death =>
                $"/f[default, 32]/c[{TC.Victim}]{e.SubjectName} /cddied from /c[{TC.Red}]{e.Message}\n",
            CombatEventKind.System => FormatSystem(e),
            CombatEventKind.Heal => null,
            CombatEventKind.DamageOverTime => null,
            _ => e.Message
        };
    }

    private static string FormatMedicalUsed(CombatLogEvent e)
    {
        if (!string.IsNullOrEmpty(e.BodyPartLabel))
        {
            return $"/c[{TC.Attacker}]{e.SubjectName} /c[{TC.Yellow}]used /c[{TC.Item}]{e.ItemLabel} /c[{TC.Default}]on /c[{TC.BodyPart}]{e.BodyPartLabel}";
        }

        return $"/c[{TC.Attacker}]{e.SubjectName} /c[{TC.Yellow}]used /c[{TC.Item}]{e.ItemLabel}";
    }

    private static string FormatPotionUsed(CombatLogEvent e)
    {
        if (!string.IsNullOrEmpty(e.Message) && e.Message.Contains("/c["))
        {
            return e.Message;
        }

        return $"/c[{TC.Attacker}]{e.SubjectName} /c[{TC.Yellow}]used /c[{TC.Item}]{e.ItemLabel}";
    }

    private static string FormatSystem(CombatLogEvent e)
    {
        if (!string.IsNullOrEmpty(e.Message) && e.Message.Contains("/c["))
        {
            return e.Message;
        }

        if (e.Message == "Battle is over")
        {
            return $"/f[default, 48]/c[{TC.Golden}]Battle is over\n";
        }

        if (e.Message?.Contains("no usable weapons") == true)
        {
            return $"/c[{TC.Attacker}]{e.SubjectName} has no usable weapons";
        }

        return e.Message ?? "";
    }

    private static string FormatDamage(CombatLogEvent e)
    {
        var weaponColor = e.IsTrinket ? TC.Purple2 : TC.Item;
        var sb = new StringBuilder();
        sb.Append($"/c[{TC.Attacker}]{e.SourceName} /c[{TC.Default}]hit /c[{TC.Victim}]{e.SubjectName}'s /c[{TC.BodyPart}]{e.BodyPartLabel}");
        sb.Append($"/c[{TC.Default}] with /c[{weaponColor}]{e.ItemLabel} /c[{TC.Golden}]({e.WeaponManeuverLabel})");
        sb.Append($"/c[{TC.Default}] for /c[{TC.Red}]{e.Amount:N0} /c[{TC.Golden}]{e.DamageType}/c[{TC.Default}] damage,");
        sb.Append($" blocked /c[#00e6ff]{e.Blocked}");

        foreach (var sub in e.SubEffects)
        {
            sb.Append('\n');
            sb.Append(sub.Kind switch
            {
                CombatEventKind.EquipmentDestroyed =>
                    $"  /c[{TC.Equipment}]{sub.ItemLabel} /c[{TC.Red}]destroyed",
                CombatEventKind.BuffApplied or CombatEventKind.DebuffApplied =>
                    $"  /c[{TC.BodyPart}]{sub.BodyPartLabel} /c[{TC.Default}]afflicted with /c[{TC.Yellow}]{sub.Label}",
                CombatEventKind.PartDestroyed when sub.IsVital =>
                    $"  /c[{TC.Red}]Vital part /c[{TC.BodyPart}]{sub.BodyPartLabel} /c[{TC.Red}]destroyed",
                CombatEventKind.PartDestroyed =>
                    $"  /c[{TC.BodyPart}]{sub.BodyPartLabel} /c[{TC.Red}]destroyed",
                CombatEventKind.PartSevered =>
                    $"  /c[{TC.BodyPart}]{sub.BodyPartLabel} /c[{TC.Red}]SEVERED",
                CombatEventKind.StatusReflected =>
                    $"/c[{TC.Purple2}]{sub.SubjectName}/c[{TC.Default}]'s {sub.Label}",
                _ => $"  {sub.Label}"
            });
        }

        return sb.ToString();
    }
}
