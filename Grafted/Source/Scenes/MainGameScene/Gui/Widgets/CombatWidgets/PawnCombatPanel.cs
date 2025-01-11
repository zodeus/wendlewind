using Grafted.Graphics.Textures;
using Grafted.Scenes.MainGameScene.Gui.Widgets.EntityWidgets.PawnWidgets;

namespace Grafted.Scenes.MainGameScene.Gui.Widgets.CombatWidgets;

internal class PawnCombatPanel : HorizontalStackPanel
{
    public readonly Pawn Pawn;
    private readonly Encounter _encounter;
    private PawnBodySummary? _bodySummary;
    private AttackSpeedIcon _attackSpeed;
    private HorizontalProgressBar _bloodBar;
    private PawnEquipmentPanel? _pawnEquipmentPanel;
    private Dictionary<string, Image> _bodyPartImages = new();
    private CombatGui.ZoneGui _gui;

    public PawnCombatPanel(CombatGui.ZoneGui gui, Pawn pawn, Encounter encounter, bool isPlayer = true)
    {
        Pawn = pawn;
        ShowGridLines = false;
        //GridLinesColor = new Color(0.7f, 0.7f, 0.7f);
        _encounter = encounter;
        _gui = gui;
        if (isPlayer)
        {
            AddChild(GenerateEquipmentPanel());
        }

        AddChild(GeneratePawnPanel(isPlayer));

        if (isPlayer == false)
        {
            _bodySummary = new PawnBodySummary(_gui, Pawn.Body) { VerticalAlignment = VerticalAlignment.Bottom, Margin = new Thickness(0, 10, 0, 0) };
            AddChild(_bodySummary);
        }

        Update();
    }

    private Widget GenerateEquipmentPanel()
    {
        _pawnEquipmentPanel = new PawnEquipmentPanel(_gui, Pawn, (part, type) =>
        {
            if (part.Equipment[type] is { } item)
            {
                if (Pawn.PawnType == PawnType.Player && Input.RightMouseButtonReleased && item.ItemDef.ItemType == ItemType.Potion)
                {
                    _encounter.QueuePotion(item, Pawn);
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

        for (int i = 0; i < Pawn.Body.AllExternalParts.Count; i++)
        {
            if (Pawn.Body.AllExternalParts[i].Image is not null)
            {
                BodyPart bodyPart = Pawn.Body.AllExternalParts[i];
                Texture2D icon = bodyPart.Image;
                Image image = new() { Background = new TextureRegion(icon), Width = panelWidth, Height = panelWidth, BorderThickness = new Thickness(2) };
                image.TouchDown += (_, _) =>
                {
                    if (_gui.MouseAttachment == null)
                    {
                        _gui.ViewEntity(Pawn);
                    }
                };
                _bodyPartImages.Add(bodyPart.Label, image);
                panel.AddChild(image);
            }
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
            if (_bodyPartImages.ContainsKey(bodyPart.Label))
            {
                _bodyPartImages[bodyPart.Label].Visible = true;
            }
        }
    }

    private Widget GeneratePawnPanel(bool isPlayer = true)
    {
        VerticalStackPanel panel = new()
        {
            DefaultProportion = Proportion.Auto,
            ShowGridLines = false,
            GridLinesColor = new Color(255, 0, 0, 255),
        };
        int panelWidth = 400;
        if (Pawn.PawnType == PawnType.Enemy || Pawn.Race != Defs.Races.Journeyman)
        {
            Texture2D icon = Pawn.Icon.Flip(false, true);
            Image image = new() { Background = new TextureRegion(icon), Width = panelWidth, Height = panelWidth, BorderThickness = new Thickness(2) };
            Pawn.Died += _ => { image.Background = new ColoredRegion(new TextureRegion(icon), Color.Red); };
            image.TouchDown += (_, _) =>
            {
                if (_gui.MouseAttachment == null)
                {
                    _gui.ViewEntity(Pawn);
                }
            };
            panel.AddChild(image);
        }
        else
        {
            panel.AddChild(InitializeBodyPartImages(panelWidth));
        }

        _bloodBar = new HorizontalProgressBar
        {
            Width = panelWidth, Height = 30,
            Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Bar.FrameSmall],
            VerticalAlignment = VerticalAlignment.Center,
            Filler = new ColoredRegion(Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Bar.Neutral], Pawn.PawnDef.Body.BloodType.Color),
            Padding = new Thickness(3, 6, 3, 6)
        };
        panel.AddChild(_bloodBar);

        Label namePlate = new()
        {
            Text = Pawn.LabelShort,
            Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.MediumFrame],
            Padding = new Thickness(12),
            Width = panelWidth - 24 - 32 - 5
        };
        _attackSpeed = new AttackSpeedIcon(Pawn);
        panel.AddChild(new HorizontalStackPanel { Spacing = 5, Widgets = { namePlate, _attackSpeed } });

        return panel;
    }

    public void Update()
    {
        _bloodBar.Value = Pawn.Body.BloodPercent * 100;
        //((ColoredRegion) _bloodBar.Filler).Color = BodyPartColor.GetBloodColor(Pawn.Body.BloodPercent);

        _bodySummary?.Update();
        _pawnEquipmentPanel?.Update();
        _attackSpeed.Update();

        RenderBodyParts();
    }
}