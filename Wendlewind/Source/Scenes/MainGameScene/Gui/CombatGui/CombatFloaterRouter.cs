using Wendlewind.Scenes.MainGameScene.Gui.Widgets.EntityWidgets.PawnWidgets.PawnBodyPanelWidgets;

namespace Wendlewind.Scenes.MainGameScene.Gui.CombatGui;

internal sealed class CombatFloaterRouter
{
    private const float TickFlushSeconds = 0.5f;

    private readonly CombatPartyPanel _playerParty;
    private readonly CombatPartyPanel _enemyParty;
    private readonly PawnBodyPanel _playerBody;
    private readonly PawnBodyPanel _enemyBody;
    private readonly IReadOnlyList<Pawn> _playerPawns;
    private readonly IReadOnlyList<Pawn> _enemyPawns;
    private readonly Dictionary<(int PawnId, string PartKey), TickAgg> _tickAgg = new();

    public event Action<CombatLogEvent>? PotionUsed;
    public event Action<CombatLogEvent>? MedicalUsed;

    public CombatFloaterRouter(
        CombatPartyPanel playerParty,
        CombatPartyPanel enemyParty,
        PawnBodyPanel playerBody,
        PawnBodyPanel enemyBody,
        IReadOnlyList<Pawn> playerPawns,
        IReadOnlyList<Pawn> enemyPawns)
    {
        _playerParty = playerParty;
        _enemyParty = enemyParty;
        _playerBody = playerBody;
        _enemyBody = enemyBody;
        _playerPawns = playerPawns;
        _enemyPawns = enemyPawns;
    }

    public void Handle(CombatLogEvent e)
    {
        if (e.Kind is CombatEventKind.Heal or CombatEventKind.DamageOverTime)
        {
            QueueTick(e);
            return;
        }

        Emit(e);
    }

    public void Update(float deltaTime)
    {
        if (_tickAgg.Count == 0)
        {
            return;
        }

        var toFlush = new List<(int PawnId, string PartKey)>();
        foreach (var (key, agg) in _tickAgg)
        {
            agg.Elapsed += deltaTime;
            if (agg.Elapsed >= TickFlushSeconds)
            {
                toFlush.Add(key);
            }
        }

        foreach (var key in toFlush)
        {
            var agg = _tickAgg[key];
            _tickAgg.Remove(key);
            if (Math.Abs(agg.Amount) < 0.05)
            {
                continue;
            }

            EmitTick(agg);
        }
    }

    private void QueueTick(CombatLogEvent e)
    {
        var partKey = e.BodyPartKey ?? "";
        var key = (e.SubjectPawnId, partKey);
        if (!_tickAgg.TryGetValue(key, out var agg))
        {
            agg = new TickAgg
            {
                PawnId = e.SubjectPawnId,
                PartKey = e.BodyPartKey,
                Kind = e.Kind
            };
            _tickAgg[key] = agg;
        }

        var signed = e.Kind == CombatEventKind.Heal ? e.Amount : -e.Amount;
        agg.Amount += signed;
        if (e.Kind == CombatEventKind.Heal && agg.Amount > 0)
        {
            agg.Kind = CombatEventKind.Heal;
        }
        else if (agg.Amount < 0)
        {
            agg.Kind = CombatEventKind.DamageOverTime;
        }
    }

    private void EmitTick(TickAgg agg)
    {
        var kind = agg.Amount >= 0 ? CombatEventKind.Heal : CombatEventKind.DamageOverTime;
        var magnitude = Math.Abs(agg.Amount);
        var text = kind == CombatEventKind.Heal ? $"+{magnitude:N0}" : $"-{magnitude:N0}";
        var (color, font) = Style(kind, isCritical: false);
        Show(agg.PawnId, agg.PartKey, text, color, font, kind);
    }

    private void Emit(CombatLogEvent e)
    {
        switch (e.Kind)
        {
            case CombatEventKind.Damage:
                EmitDamage(e);
                break;
            case CombatEventKind.Miss:
                Show(e.SourcePawnId ?? e.SubjectPawnId, null, "missed", Style(e.Kind, false));
                break;
            case CombatEventKind.Dodge:
                Show(e.SubjectPawnId, e.BodyPartKey, "dodged", Style(e.Kind, false));
                break;
            case CombatEventKind.PotionUsed:
                Show(e.SubjectPawnId, null, e.ItemLabel ?? "potion", Style(e.Kind, false));
                PotionUsed?.Invoke(e);
                break;
            case CombatEventKind.MedicalUsed:
                Show(e.SubjectPawnId, e.BodyPartKey, e.ItemLabel ?? "medical", Style(e.Kind, false));
                MedicalUsed?.Invoke(e);
                break;
            case CombatEventKind.Death:
                Show(e.SubjectPawnId, null, "died", Style(e.Kind, false));
                break;
            case CombatEventKind.System:
                break;
            default:
                if (!string.IsNullOrEmpty(e.Message))
                {
                    Show(e.SubjectPawnId, e.BodyPartKey, e.Message, Style(e.Kind, e.IsCritical));
                }
                break;
        }
    }

    private void EmitDamage(CombatLogEvent e)
    {
        if (e.Amount > 0)
        {
            var text = $"{e.Amount:N0}";
            Show(e.SubjectPawnId, e.BodyPartKey, text, Style(CombatEventKind.Damage, e.IsCritical));
        }

        if (e.Blocked > 0)
        {
            Show(e.SubjectPawnId, e.BodyPartKey, $"{e.Blocked:N0}", Style(CombatEventKind.Block, false));
        }

        foreach (var sub in e.SubEffects)
        {
            var pawnId = sub.SubjectPawnId != 0 ? sub.SubjectPawnId : e.SubjectPawnId;
            var partKey = sub.BodyPartKey ?? e.BodyPartKey;
            var text = sub.Kind switch
            {
                CombatEventKind.BuffApplied or CombatEventKind.DebuffApplied => sub.Label ?? "",
                CombatEventKind.PartSevered => $"{sub.BodyPartLabel} severed",
                CombatEventKind.PartDestroyed => $"{sub.BodyPartLabel} destroyed",
                CombatEventKind.StatusReflected => sub.ItemLabel ?? sub.Label ?? "",
                CombatEventKind.EquipmentDestroyed => $"{sub.ItemLabel} destroyed",
                _ => sub.Label ?? ""
            };
            if (string.IsNullOrEmpty(text))
            {
                continue;
            }

            Show(pawnId, partKey, text, Style(sub.Kind, false));
        }
    }

    private void Show(int pawnId, string? partKey, string text, (Color Color, DynamicSpriteFont Font) style)
    {
        Show(pawnId, partKey, text, style.Color, style.Font, CombatEventKind.Damage);
    }

    private void Show(int pawnId, string? partKey, string text, Color color, DynamicSpriteFont font, CombatEventKind kind)
    {
        var pawn = FindPawn(pawnId);
        if (pawn == null)
        {
            return;
        }

        var part = pawn.Body.FindPartByKey(partKey);
        var party = pawn.PawnType == PawnType.Player ? _playerParty : _enemyParty;
        var bodyPanel = pawn.PawnType == PawnType.Player ? _playerBody : _enemyBody;

        party.GetPanelForPawn(pawn)?.BodyWidget?.AddDamageText(part, text, font, color, 1.6f);

        if (part != null && IsPartTargeted(kind))
        {
            bodyPanel.AddDamageText(part, text, font, color, 1.15f);
            bodyPanel.FlashPart(part, color);
        }
    }

    private static bool IsPartTargeted(CombatEventKind kind) => kind is
        CombatEventKind.Damage or CombatEventKind.Block or CombatEventKind.Heal
        or CombatEventKind.DamageOverTime or CombatEventKind.BuffApplied
        or CombatEventKind.DebuffApplied or CombatEventKind.PartSevered
        or CombatEventKind.PartDestroyed;

    private Pawn? FindPawn(int id)
    {
        foreach (var pawn in _playerPawns)
        {
            if (pawn.Id == id) return pawn;
        }

        foreach (var pawn in _enemyPawns)
        {
            if (pawn.Id == id) return pawn;
        }

        return null;
    }

    private static (Color Color, DynamicSpriteFont Font) Style(CombatEventKind kind, bool isCritical)
    {
        var color = kind switch
        {
            CombatEventKind.Damage => new Color(186, 22, 0),
            CombatEventKind.Block => new Color(0, 150, 237),
            CombatEventKind.Dodge => new Color(0, 150, 237),
            CombatEventKind.Miss => Color.Orange,
            CombatEventKind.Heal => Color.GreenYellow,
            CombatEventKind.DamageOverTime => new Color(168, 40, 90),
            CombatEventKind.BuffApplied => Color.GreenYellow,
            CombatEventKind.DebuffApplied => new Color(237, 51, 0),
            CombatEventKind.StatusReflected => Color.Purple,
            CombatEventKind.PartSevered or CombatEventKind.PartDestroyed => new Color(186, 22, 0),
            CombatEventKind.EquipmentDestroyed => new Color(0, 150, 237),
            CombatEventKind.Death => Color.AntiqueWhite,
            CombatEventKind.PotionUsed => Color.Goldenrod,
            CombatEventKind.MedicalUsed => new Color(120, 200, 160),
            _ => Color.White
        };

        var font = kind switch
        {
            CombatEventKind.Damage or CombatEventKind.Heal => BaseContent.Fonts.Default.VerySmall,
            CombatEventKind.Death => BaseContent.Fonts.Default.Small,
            CombatEventKind.PotionUsed or CombatEventKind.MedicalUsed => BaseContent.Fonts.Default.Small,
            _ => BaseContent.Fonts.Default.Smallest
        };

        if (isCritical)
        {
            font = BaseContent.Fonts.Default.Normal;
            color = Color.Red;
        }

        return (color, font);
    }

    private sealed class TickAgg
    {
        public int PawnId;
        public string? PartKey;
        public CombatEventKind Kind;
        public double Amount;
        public float Elapsed;
    }
}
