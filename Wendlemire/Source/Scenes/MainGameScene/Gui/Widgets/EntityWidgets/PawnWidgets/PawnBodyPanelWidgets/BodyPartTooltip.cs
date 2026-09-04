using Image = Myra.Graphics2D.UI.Image;

namespace Wendlemire.Scenes.MainGameScene.Gui.Widgets.EntityWidgets.PawnWidgets.PawnBodyPanelWidgets;

internal sealed class BodyPartTooltip : VerticalStackPanel
{
    private readonly BodyPart _part;
    private readonly EntityCardChrome.CardMetrics _card;
    private readonly ColoredRegion _iconTint;
    private readonly Label _title;
    private readonly Label _hpValue;
    private readonly Label _typeValue;
    private readonly Label _substanceValue;
    private readonly Label _relation;
    private readonly VerticalStackPanel _problems;
    private readonly VerticalStackPanel _internals;
    private readonly VerticalStackPanel _equipped;
    private readonly VerticalStackPanel _modifiers;
    private readonly List<(BodyPart Part, Label Health)> _internalHealth = [];
    private readonly List<(BodyPartModifier Modifier, Label Time)> _modifierTimes = [];

    private int _internalSignature = int.MinValue;
    private int _equippedSignature = int.MinValue;
    private int _modifierSignature = int.MinValue;
    private int _problemSignature = int.MinValue;

    public BodyPartTooltip(BodyPart part)
    {
        _part = part;
        _card = EntityCardChrome.ApplyCard(this);

        _iconTint = new ColoredRegion(new TextureRegion(part.GetIcon()), Color.White);
        var iconFrame = new Panel
        {
            Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.DeepGold],
            Padding = new Thickness(8),
            Width = EntityCardChrome.Frame,
            Height = EntityCardChrome.Frame
        };
        iconFrame.Widgets.Add(new Image
        {
            Background = _iconTint,
            Width = EntityCardChrome.Icon,
            Height = EntityCardChrome.Icon,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center
        });

        _title = new Label("small") { Text = part.Label };
        var info = new VerticalStackPanel
        {
            Spacing = 3,
            VerticalAlignment = VerticalAlignment.Center,
            Widgets = { _title }
        };

        var description = part.Def.Description;
        if (!string.IsNullOrWhiteSpace(description) && description != "undefined")
        {
            info.Widgets.Add(new Label("small")
            {
                Text = description,
                Wrap = true,
                MaxWidth = _card.FlavorWidth,
                TextColor = EntityCardChrome.Flavor
            });
        }

        Widgets.Add(new HorizontalStackPanel { Spacing = 12, Widgets = { iconFrame, info } });
        Widgets.Add(EntityCardChrome.Hairline());

        Widgets.Add(EntityCardChrome.StatStrip(new Widget[]
        {
            EntityCardChrome.StatChip("Type", part.Type.ToString(), EntityCardChrome.Gold, out _typeValue),
            EntityCardChrome.StatChip("Health", FormatHealth(part), Color.White, out _hpValue),
            EntityCardChrome.StatChip("Substance", part.Substance.ToString(), EntityCardChrome.Tan, out _substanceValue)
        }));

        _relation = EntityCardChrome.BodyLabel("", EntityCardChrome.Muted, _card.BodyWidth);
        Widgets.Add(_relation);

        _problems = new VerticalStackPanel { Spacing = 4 };
        Widgets.Add(_problems);

        _internals = new VerticalStackPanel { Spacing = 4 };
        Widgets.Add(_internals);

        _equipped = new VerticalStackPanel { Spacing = 4 };
        Widgets.Add(_equipped);

        _modifiers = new VerticalStackPanel { Spacing = 4 };
        Widgets.Add(_modifiers);

        Refresh();
    }

    public void Refresh()
    {
        var tint = BodyPartColor.Get(_part);
        _iconTint.Color = tint;
        _title.Text = _part.Label;
        _title.TextColor = tint;
        _typeValue.Text = _part.Type.ToString();
        _hpValue.Text = FormatHealth(_part);
        _hpValue.TextColor = tint;
        _substanceValue.Text = _part.Substance.ToString();
        _relation.Text = RelationText(_part);
        _relation.Visible = !string.IsNullOrWhiteSpace(_relation.Text);

        RefreshProblems();
        RefreshInternals();
        RefreshEquipped();
        RefreshModifiers();
    }

    private void RefreshProblems()
    {
        var flags = ProblemFlags(_part);
        if (flags == _problemSignature)
        {
            return;
        }

        _problemSignature = flags;
        _problems.Widgets.Clear();
        var lines = new List<string>();
        if (_part.IsDestroyed)
        {
            lines.Add("Mutilated — this part is destroyed.");
        }

        if (_part.IsSevered)
        {
            lines.Add("Severed from the body.");
        }

        if (_part.IsCracked)
        {
            lines.Add("Cracked — cover no longer seals.");
        }

        if (_part.IsBleeding)
        {
            lines.Add("Bleeding — open flesh.");
        }

        if (_part.HasBrokenBones)
        {
            lines.Add("Broken bones underneath.");
        }

        if (!_part.IsArteryFunctional)
        {
            lines.Add("Artery destroyed — blood does not reach here.");
        }

        if (!_part.HasMobility)
        {
            lines.Add("No mobility.");
        }

        if (!_part.IsFunctional)
        {
            lines.Add("Non-functional.");
        }

        var crisis = _part.Body?.OrganCrisis;
        if (crisis != null && OrganCrisis.IsInCrisis(_part) && OrganCrisis.IsDelayedType(_part.Type))
        {
            if (crisis.IsActive(_part.Type))
            {
                lines.Add($"Organ crisis — collapse in {FormatSeconds(crisis.TicksRemaining(_part.Type))}.");
            }
            else if (_part.Type == BodyPartType.Kidney)
            {
                OrganCrisis.CountType(_part.Body!, BodyPartType.Kidney, out var failed, out var total);
                lines.Add($"Waiting on the other kidney — both must fail before collapse starts ({failed}/{total}).");
            }
        }

        if (lines.Count == 0)
        {
            return;
        }

        _problems.Widgets.Add(EntityCardChrome.SectionHeader("Status"));
        _problems.Widgets.Add(EntityCardChrome.MechanicsBlock(lines, _card.BodyWidth));
    }

    private void RefreshInternals()
    {
        var internals = OrderedInternals(_part);
        var signature = 0;
        foreach (var internalPart in internals)
        {
            signature = HashCode.Combine(signature, internalPart.Id);
        }

        if (signature != _internalSignature)
        {
            _internalSignature = signature;
            _internalHealth.Clear();
            _internals.Widgets.Clear();
            if (internals.Count == 0)
            {
                return;
            }

            _internals.Widgets.Add(EntityCardChrome.SectionHeader("Internals"));
            var rows = new List<Widget>();
            foreach (var internalPart in internals)
            {
                var health = new Label("small");
                rows.Add(PartRow(internalPart.Label, health, internalPart.IsVital || internalPart.IsOrgan));
                _internalHealth.Add((internalPart, health));
            }

            _internals.Widgets.Add(EntityCardChrome.InsetBlock(_card.BodyWidth, rows.ToArray()));
        }

        foreach (var (internalPart, health) in _internalHealth)
        {
            var tint = BodyPartColor.Get(internalPart);
            health.Text = FormatHealth(internalPart);
            health.TextColor = tint;
        }
    }

    private void RefreshEquipped()
    {
        var entries = EquipmentEntries(_part);
        var signature = 0;
        foreach (var (slot, item) in entries)
        {
            signature = HashCode.Combine(signature, slot, item?.Id, item?.Label, item?.Durability);
        }

        var covering = _part.CoveringArmor();
        if (covering != null && !entries.Any(entry => entry.Item == covering))
        {
            signature = HashCode.Combine(signature, covering.Id, "cover");
        }

        if (signature == _equippedSignature)
        {
            return;
        }

        _equippedSignature = signature;
        _equipped.Widgets.Clear();
        if (entries.Count == 0 && covering == null)
        {
            return;
        }

        _equipped.Widgets.Add(EntityCardChrome.SectionHeader("Equipped"));
        var rows = new List<Widget>();
        foreach (var (slot, item) in entries)
        {
            if (item == null)
            {
                rows.Add(SlotRow(SlotName(slot), "Empty", EntityCardChrome.Muted));
                continue;
            }

            var detail = item.MaxDurability > 0
                ? $"{item.Label}  {item.Durability:0}/{item.MaxDurability:0}"
                : item.Label;
            rows.Add(SlotRow(SlotName(slot), detail, Color.White));
        }

        if (covering != null && !entries.Any(entry => entry.Item == covering))
        {
            rows.Add(SlotRow("Covered by", covering.Label, EntityCardChrome.Tan));
        }

        _equipped.Widgets.Add(EntityCardChrome.InsetBlock(_card.BodyWidth, rows.ToArray()));
    }

    private void RefreshModifiers()
    {
        var signature = 0;
        foreach (var modifier in _part.Modifiers)
        {
            signature = HashCode.Combine(signature, modifier.Id, modifier.Label);
        }

        if (signature != _modifierSignature)
        {
            _modifierSignature = signature;
            _modifierTimes.Clear();
            _modifiers.Widgets.Clear();
            if (_part.Modifiers.Count == 0)
            {
                return;
            }

            _modifiers.Widgets.Add(EntityCardChrome.SectionHeader("Modifiers"));
            var rows = new List<Widget>();
            foreach (var modifier in _part.Modifiers)
            {
                var time = new Label("small") { TextColor = EntityCardChrome.Muted };
                rows.Add(new VerticalStackPanel
                {
                    Spacing = 1,
                    Widgets =
                    {
                        new HorizontalStackPanel
                        {
                            Spacing = 8,
                            Widgets =
                            {
                                new Label("small")
                                {
                                    Text = modifier.Label,
                                    TextColor = modifier.Def.Color
                                },
                                time
                            }
                        }
                    }
                });

                var description = modifier.Def.Description;
                if (!string.IsNullOrWhiteSpace(description) && description != "undefined")
                {
                    ((VerticalStackPanel)rows[^1]).Widgets.Add(new Label("small")
                    {
                        Text = description,
                        Wrap = true,
                        MaxWidth = _card.BodyWidth - 24,
                        TextColor = EntityCardChrome.Flavor
                    });
                }

                _modifierTimes.Add((modifier, time));
            }

            _modifiers.Widgets.Add(EntityCardChrome.InsetBlock(_card.BodyWidth, rows.ToArray()));
        }

        foreach (var (modifier, time) in _modifierTimes)
        {
            time.Text = modifier.DurationInTicks == 0
                ? "Permanent"
                : FormatSeconds(modifier.TicksRemaining);
        }
    }

    private static string RelationText(BodyPart part)
    {
        var bits = new List<string>();
        if (part.IsVital)
        {
            bits.Add("Vital");
        }

        if (part.IsOrgan)
        {
            bits.Add("Organ");
        }

        bits.Add(part.IsExternal ? "External" : "Internal");

        var parent = part.Socket?.ParentPart?.Label;
        if (parent != null)
        {
            bits.Add("on " + parent);
        }

        var children = part.ExternalParts;
        if (children.Count > 0)
        {
            bits.Add("holds " + string.Join(", ", children.Select(child => child.Label)));
        }

        return string.Join(" · ", bits);
    }

    private static List<BodyPart> OrderedInternals(BodyPart part)
    {
        return part.AllInternalParts
            .OrderBy(RankInternal)
            .ThenBy(internalPart => internalPart.Label, StringComparer.Ordinal)
            .ToList();
    }

    private static int RankInternal(BodyPart part)
    {
        if (part.Type == BodyPartType.Skin)
        {
            return 0;
        }

        if (part.Substance == SubstanceType.Bone)
        {
            return 1;
        }

        if (part.Type == BodyPartType.Artery)
        {
            return 2;
        }

        return part.IsOrgan ? 3 : 4;
    }

    private static List<(EquipmentSlotType Slot, Item? Item)> EquipmentEntries(BodyPart part)
    {
        var entries = new List<(EquipmentSlotType Slot, Item? Item)>();
        if (part.EquipmentSlots == null)
        {
            return entries;
        }

        foreach (var slot in part.EquipmentSlots)
        {
            part.Equipment.TryGetValue(slot, out var item);
            if (item is { IsDestroyed: false })
            {
                entries.Add((slot, item));
                continue;
            }

            if (!PotionSlots.IsPotionSlot(slot))
            {
                entries.Add((slot, null));
            }
        }

        return entries;
    }

    private static Widget PartRow(string name, Label health, bool vital)
    {
        return new HorizontalStackPanel
        {
            Spacing = 8,
            Widgets =
            {
                new Label("small")
                {
                    Text = vital ? name + "  · vital" : name,
                    TextColor = vital ? EntityCardChrome.Gold : EntityCardChrome.Mechanic
                },
                health
            }
        };
    }

    private static Widget SlotRow(string slot, string value, Color valueColor)
    {
        return new HorizontalStackPanel
        {
            Spacing = 8,
            Widgets =
            {
                new Label("small")
                {
                    Text = slot,
                    TextColor = EntityCardChrome.Muted,
                    Width = 88
                },
                new Label("small")
                {
                    Text = value,
                    Wrap = true,
                    MaxWidth = 280,
                    TextColor = valueColor
                }
            }
        };
    }

    private static string SlotName(EquipmentSlotType slot) => slot switch
    {
        EquipmentSlotType.HandWeapon => "Weapon",
        EquipmentSlotType.HandArmor => "Glove",
        EquipmentSlotType.FootWeapon => "Foot",
        EquipmentSlotType.FootArmor => "Boot",
        EquipmentSlotType.LegArmor => "Legs",
        EquipmentSlotType.ArmArmor => "Arms",
        EquipmentSlotType.TorsoArmor => "Torso",
        EquipmentSlotType.NeckArmor => "Neck",
        EquipmentSlotType.HeadArmor => "Helm",
        EquipmentSlotType.Bag => "Bag",
        EquipmentSlotType.Cloak => "Cloak",
        EquipmentSlotType.Necklace => "Necklace",
        EquipmentSlotType.BuiltIn => "Built-in",
        _ => slot.ToString()
    };

    private static int ProblemFlags(BodyPart part)
    {
        var flags = 0;
        if (part.IsDestroyed) flags |= 1;
        if (part.IsSevered) flags |= 2;
        if (part.IsCracked) flags |= 4;
        if (part.IsBleeding) flags |= 8;
        if (part.HasBrokenBones) flags |= 16;
        if (!part.IsArteryFunctional) flags |= 32;
        if (!part.HasMobility) flags |= 64;
        if (!part.IsFunctional) flags |= 128;
        if (part.Body?.OrganCrisis.IsActive(part.Type) == true && OrganCrisis.IsInCrisis(part))
        {
            flags |= 256 | (part.Body.OrganCrisis.TicksRemaining(part.Type) / 6);
        }
        else if (part.Body?.OrganCrisis.IsPending(part) == true)
        {
            flags |= 512;
        }
        return flags;
    }

    private static string FormatHealth(BodyPart part)
    {
        if (part.MaxHitPoints <= 0)
        {
            return "0/0";
        }

        var current = part.HitPoints < 2
            ? $"{part.HitPoints:0.#}"
            : $"{Math.Ceiling(part.HitPoints):0}";
        return $"{current}/{part.MaxHitPoints:0}  ({part.HealthPercent * 100:0}%)";
    }

    private static string FormatSeconds(int ticks) =>
        $"{ticks / (float)GameContext.TicksPerSecond:0.#}s";
}
