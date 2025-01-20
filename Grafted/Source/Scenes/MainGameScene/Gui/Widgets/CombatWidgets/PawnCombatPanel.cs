using Grafted.Graphics.Textures;
using Grafted.Scenes.MainGameScene.Gui.Widgets.EntityWidgets.PawnWidgets;
using Grafted.Sim.Entities.Items.Trinkets;

namespace Grafted.Scenes.MainGameScene.Gui.Widgets.CombatWidgets;

internal sealed class PawnCombatPanel : HorizontalStackPanel
{
    public readonly Pawn Pawn;
    private readonly Encounter _encounter;
    private readonly PawnBodySummary? _bodySummary;
    private AttackSpeedIcon _attackSpeed = null!;
    private HorizontalProgressBar _bloodBar = null!;
    private PawnEquipmentPanel? _pawnEquipmentPanel;
    private readonly Dictionary<string, Image> _bodyPartImages = new();
    private readonly CombatGui.ZoneGui _gui;
    private readonly TrinketBar? _trinketBar;

    public PawnCombatPanel(CombatGui.ZoneGui gui, Pawn pawn, Encounter encounter, bool isPlayer = true)
    {
        Pawn = pawn;
        ShowGridLines = false;
        //GridLinesColor = new Color(0.7f, 0.7f, 0.7f);
        _encounter = encounter;
        _gui = gui;
        if (isPlayer)
        {
            _trinketBar = new TrinketBar(gui, pawn.Inventory.Entities, TrinketType.Combat, HandleTrinketClick)
            {
                VerticalAlignment = VerticalAlignment.Bottom, HorizontalAlignment = HorizontalAlignment.Right
            };
            Widgets.Add(new VerticalStackPanel
            {
                Proportions = { Proportion.Auto, Proportion.Fill },
                Widgets =
                {
                    GenerateEquipmentPanel(),
                    _trinketBar,
                }
            });
        }

        Widgets.Add(GeneratePawnPanel());

        if (isPlayer == false)
        {
            _bodySummary = new PawnBodySummary(_gui, Pawn.Body) { VerticalAlignment = VerticalAlignment.Bottom, Margin = new Thickness(0, 10, 0, 0) };
            Widgets.Add(_bodySummary);
        }

        Update();
    }

    private void HandleTrinketClick(Item item)
    {
        if (item.TrinketHandler?.IsActive == true)
        {
            _encounter.CombatHandler?.DeActivateTrinketForPawn(item, Pawn);
        }
        else
        {
            _encounter.CombatHandler?.ActivateTrinketForPawn(item, Pawn);
        }
    }

    private Widget GenerateEquipmentPanel()
    {
        _pawnEquipmentPanel = new PawnEquipmentPanel(_gui, Pawn, (part, type) =>
        {
            if (part.Equipment[type] is { } item)
            {
                if (Pawn.PawnType == PawnType.Player && Input.RightMouseButtonReleased && item.ItemDef.ItemType == ItemType.Potion)
                {
                    _encounter.CombatHandler?.QueueItemForPawn(item, Pawn);
                    return;
                }

                _gui.ViewEntity(item);
            }
        });
        return _pawnEquipmentPanel;
    }

    private Panel InitializeBodyPartImages(int panelWidth)
    {
        Panel panel = new();

        for (var i = 0; i < Pawn.Body.AllExternalParts.Count; i++)
        {
            if (Pawn.Body.AllExternalParts[i].Image is null) continue;

            var bodyPart = Pawn.Body.AllExternalParts[i];
            var icon = bodyPart.Image!;
            Image image = new() { Background = new TextureRegion(icon), Width = panelWidth, Height = panelWidth, BorderThickness = new Thickness(2) };
            image.TouchDown += (_, _) =>
            {
                if (_gui.MouseAttachment == null)
                {
                    _gui.ViewEntity(Pawn);
                }
            };
            _bodyPartImages.Add(bodyPart.Label, image);
            panel.Widgets.Add(image);
        }

        return panel;
    }

    private void RenderBodyParts()
    {
        foreach (var bodyPartImage in _bodyPartImages)
        {
            bodyPartImage.Value.Visible = false;
        }

        foreach (var bodyPart in Pawn.Body.AllExternalParts)
        {
            if (_bodyPartImages.TryGetValue(bodyPart.Label, out var image))
            {
                image.Visible = true;
            }
        }
    }

    private Widget GeneratePawnPanel()
    {
        VerticalStackPanel panel = new()
        {
            DefaultProportion = Proportion.Auto,
            ShowGridLines = false,
            GridLinesColor = new Color(255, 0, 0, 255),
        };
        var panelWidth = 400;
        if (Pawn.PawnType == PawnType.Enemy || Pawn.Race != Defs.Races.Journeyman)
        {
            var icon = Pawn.Icon.Flip(false, true);
            Image image = new() { Background = new TextureRegion(icon), Width = 360, Height = 360, BorderThickness = new Thickness(2) };
            Pawn.Died += _ => { image.Background = new ColoredRegion(new TextureRegion(icon), Color.Red); };
            image.TouchDown += (_, _) =>
            {
                if (_gui.MouseAttachment == null)
                {
                    _gui.ViewEntity(Pawn);
                }
            };
            panel.Widgets.Add(image);
        }
        else
        {
            panel.Widgets.Add(InitializeBodyPartImages(360));
        }

        _bloodBar = new BloodBar(Pawn) { Width = panelWidth, Height = 30 };
        panel.Widgets.Add(_bloodBar);

        Label namePlate = new()
        {
            Text = Pawn.LabelShort,
            Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.MediumFrame],
            Padding = new Thickness(12),
            Width = panelWidth - 24 - 32 - 5
        };
        _attackSpeed = new AttackSpeedIcon(Pawn);
        panel.Widgets.Add(new HorizontalStackPanel { Spacing = 5, Widgets = { namePlate, _attackSpeed } });

        return panel;
    }

    public void Update()
    {
        _bloodBar.Value = Pawn.Body.BloodPercent * 100;
        _bodySummary?.Update();
        _pawnEquipmentPanel?.Update();
        _attackSpeed.Update();
        _trinketBar?.Update();
        RenderBodyParts();
    }
}