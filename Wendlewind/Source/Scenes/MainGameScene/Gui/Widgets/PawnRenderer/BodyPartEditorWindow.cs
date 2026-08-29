using System.IO;
using Wendlewind.Scenes.MainGameScene.Gui.Widgets.EntityWidgets.PawnWidgets;

namespace Wendlewind.Scenes.MainGameScene.Gui.Widgets.PawnRenderer;

/// <summary>
/// Data class to hold all transform overrides for a body part.
/// </summary>
public class BodyPartTransformOverride
{
    public Vector2 Position { get; set; }
    public float Scale { get; set; } = 1f;
    public float Rotation { get; set; } = 0f;
    public bool FlipHorizontal { get; set; } = false;
    public bool FlipVertical { get; set; } = false;
    public int RenderOrder { get; set; } = 0;
    
    // Equipment attachment properties
    public bool HasEquipmentAttachment { get; set; } = false;
    public Vector2 EquipmentOffset { get; set; } = Vector2.Zero;
    public float EquipmentRotation { get; set; } = 0f;
    public float EquipmentScale { get; set; } = 1f;
    public bool EquipmentFlipH { get; set; } = false;
    
    public BodyPartTransformOverride(Vector2 position, float scale = 1f, float rotation = 0f, bool flipH = false, bool flipV = false, int renderOrder = 0, EquipmentAttachmentData? equipmentAttachment = null)
    {
        Position = position;
        Scale = scale;
        Rotation = rotation;
        FlipHorizontal = flipH;
        FlipVertical = flipV;
        RenderOrder = renderOrder;
        
        if (equipmentAttachment.HasValue)
        {
            HasEquipmentAttachment = true;
            EquipmentOffset = equipmentAttachment.Value.Offset;
            EquipmentRotation = equipmentAttachment.Value.Rotation;
            EquipmentScale = equipmentAttachment.Value.Scale;
            EquipmentFlipH = equipmentAttachment.Value.FlipHorizontal;
        }
    }
}

/// <summary>
/// A dialog window for editing body part positions, rotations, and scales.
/// </summary>
public class BodyPartEditorWindow : Window
{
    private readonly Pawn _pawn;
    private readonly IBodyPartLayout? _layout;
    private readonly int _renderSize;
    private readonly Dictionary<string, BodyPartTransformOverride> _overrides = new();
    
    // Texture pixel data cache for per-pixel hit testing
    private readonly Dictionary<Texture2D, Color[]> _texturePixelCache = new();
    
    // Rendering
    private SpriteBatch? _spriteBatch;
    private readonly HashSet<string> _selectedPartLabels = new();
    private string? _hoveredPartLabel;
    private string? _draggedPartLabel;
    private Vector2 _dragOffset;
    private Dictionary<string, Vector2> _allPartsDragOffsets = new();
    private bool _isDragging;
    private bool _isDraggingMultiple;
    private MouseState _previousMouseState;
    private KeyboardState _previousKeyboardState;
    
    // UI Elements
    private readonly BodyPartEditorRenderArea _renderArea;
    private readonly Label _selectedPartLabel_UI;
    private readonly HorizontalSlider _scaleSlider;
    private readonly HorizontalSlider _rotationSlider;
    private readonly HorizontalSlider _renderOrderSlider;
    private readonly Label _scaleValueLabel;
    private readonly Label _rotationValueLabel;
    private readonly Label _renderOrderValueLabel;
    private readonly CursorButton _flipHButton;
    private readonly CursorButton _flipVButton;
    private readonly Grid _partsGrid;
    private readonly Dictionary<string, CursorButton> _partButtons = new();
    
    // Equipment attachment UI
    private readonly CursorButton _hasEquipmentButton;
    private readonly HorizontalSlider _equipOffsetXSlider;
    private readonly HorizontalSlider _equipOffsetYSlider;
    private readonly HorizontalSlider _equipRotationSlider;
    private readonly HorizontalSlider _equipScaleSlider;
    private readonly Label _equipOffsetXLabel;
    private readonly Label _equipOffsetYLabel;
    private readonly Label _equipRotationLabel;
    private readonly Label _equipScaleLabel;
    private readonly CursorButton _equipFlipHButton;
    private readonly Panel _equipmentPanel;

    public BodyPartEditorWindow(Pawn pawn, int renderSize = 512)
    {
        _pawn = pawn;
        _renderSize = renderSize;
        _layout = BodyPartLayoutRegistry.GetLayoutFor(pawn.Body);
        
        Title = $"Body Part Editor - {pawn.Label}";
        Width = 1200;
        Height = 1000;
        
        // Initialize overrides from current layout
        InitializeOverrides();
        
        // Main layout
        var mainPanel = new HorizontalStackPanel
        {
            Spacing = 10,
            Margin = new Thickness(10),
        };
        
        // Left side - render area
        var leftPanel = new VerticalStackPanel { Spacing = 5 };
        
        _renderArea = new BodyPartEditorRenderArea(this)
        {
            Width = _renderSize,
            Height = _renderSize,
            BorderThickness = new Thickness(2),
            Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.MediumFrame]
        };
        _renderArea.OnFrameUpdate = OnFrameUpdate;
        leftPanel.Widgets.Add(_renderArea);
        
        // Body parts grid below render area
        _partsGrid = new Grid
        {
            RowSpacing = 2,
            ColumnSpacing = 2,
            DefaultColumnProportion = Proportion.Auto,
            DefaultRowProportion = Proportion.Auto
        };
        leftPanel.Widgets.Add(_partsGrid);
        
        // Populate the parts list
        PopulatePartsList();
        
        mainPanel.Widgets.Add(leftPanel);
        
        // Right side - controls
        var rightPanel = new VerticalStackPanel { Spacing = 10 };
        
        // Selected part info
        rightPanel.Widgets.Add(new Label { Text = "Selected Part:" });
        _selectedPartLabel_UI = new Label { Text = "(none)", TextColor = Color.Yellow };
        rightPanel.Widgets.Add(_selectedPartLabel_UI);
        
        rightPanel.Widgets.Add(new Label { Text = "" }); // Spacer
        
        // Scale control
        rightPanel.Widgets.Add(new Label { Text = "Scale:" });
        var scalePanel = new HorizontalStackPanel { Spacing = 5 };
        _scaleSlider = new HorizontalSlider
        {
            Minimum = 0.1f,
            Maximum = 3.0f,
            Value = 1.0f,
            Width = 400
        };
        _scaleSlider.ValueChangedByUser += OnScaleChanged;
        _scaleValueLabel = new Label { Text = "1.00", Width = 40 };
        scalePanel.Widgets.Add(_scaleSlider);
        scalePanel.Widgets.Add(_scaleValueLabel);
        rightPanel.Widgets.Add(scalePanel);
        
        // Rotation control
        rightPanel.Widgets.Add(new Label { Text = "Rotation:" });
        var rotationPanel = new HorizontalStackPanel { Spacing = 5 };
        _rotationSlider = new HorizontalSlider
        {
            Minimum = -180f,
            Maximum = 180f,
            Value = 0f,
            Width = 400
        };
        _rotationSlider.ValueChangedByUser += OnRotationChanged;
        _rotationValueLabel = new Label { Text = "0°", Width = 40 };
        rotationPanel.Widgets.Add(_rotationSlider);
        rotationPanel.Widgets.Add(_rotationValueLabel);
        rightPanel.Widgets.Add(rotationPanel);
        
        // Render Order control
        rightPanel.Widgets.Add(new Label { Text = "Render Order:" });
        var renderOrderPanel = new HorizontalStackPanel { Spacing = 5 };
        _renderOrderSlider = new HorizontalSlider
        {
            Minimum = 0f,
            Maximum = 100f,
            Value = 0f,
            Width = 400
        };
        _renderOrderSlider.ValueChangedByUser += OnRenderOrderChanged;
        _renderOrderValueLabel = new Label { Text = "0", Width = 40 };
        renderOrderPanel.Widgets.Add(_renderOrderSlider);
        renderOrderPanel.Widgets.Add(_renderOrderValueLabel);
        rightPanel.Widgets.Add(renderOrderPanel);
        
        // Flip controls
        var flipPanel = new HorizontalStackPanel { Spacing = 10 };
        _flipHButton = new CursorButton(BaseContent.Styles.Button.Normal)
        {
            Content = new Label { Text = "Horz: Off" },
        };
        _flipHButton.Click += OnFlipHClicked;
        _flipVButton = new CursorButton(BaseContent.Styles.Button.Normal)
        {
            Content = new Label { Text = "Vert: Off" },
        };
        _flipVButton.Click += OnFlipVClicked;
        flipPanel.Widgets.Add(_flipHButton);
        flipPanel.Widgets.Add(_flipVButton);
        rightPanel.Widgets.Add(flipPanel);
        
        rightPanel.Widgets.Add(new Label { Text = "" }); // Spacer
        
        // Equipment Attachment section
        rightPanel.Widgets.Add(new Label { Text = "Equipment Attachment:", TextColor = Color.Orange });
        
        _hasEquipmentButton = new CursorButton(BaseContent.Styles.Button.Normal)
        {
            Content = new Label { Text = "Has Attachment: Off" },
        };
        _hasEquipmentButton.Click += OnHasEquipmentClicked;
        rightPanel.Widgets.Add(_hasEquipmentButton);
        
        _equipmentPanel = new Panel();
        var equipStack = new VerticalStackPanel { Spacing = 5 };
        
        // Equipment Offset X
        equipStack.Widgets.Add(new Label { Text = "Offset X:" });
        var equipOffsetXPanel = new HorizontalStackPanel { Spacing = 5 };
        _equipOffsetXSlider = new HorizontalSlider
        {
            Minimum = -200f,
            Maximum = 200f,
            Value = 0f,
            Width = 380
        };
        _equipOffsetXSlider.ValueChangedByUser += OnEquipOffsetXChanged;
        _equipOffsetXLabel = new Label { Text = "0", Width = 50 };
        equipOffsetXPanel.Widgets.Add(_equipOffsetXSlider);
        equipOffsetXPanel.Widgets.Add(_equipOffsetXLabel);
        equipStack.Widgets.Add(equipOffsetXPanel);
        
        // Equipment Offset Y
        equipStack.Widgets.Add(new Label { Text = "Offset Y:" });
        var equipOffsetYPanel = new HorizontalStackPanel { Spacing = 5 };
        _equipOffsetYSlider = new HorizontalSlider
        {
            Minimum = -200f,
            Maximum = 200f,
            Value = 0f,
            Width = 380
        };
        _equipOffsetYSlider.ValueChangedByUser += OnEquipOffsetYChanged;
        _equipOffsetYLabel = new Label { Text = "0", Width = 50 };
        equipOffsetYPanel.Widgets.Add(_equipOffsetYSlider);
        equipOffsetYPanel.Widgets.Add(_equipOffsetYLabel);
        equipStack.Widgets.Add(equipOffsetYPanel);
        
        // Equipment Rotation
        equipStack.Widgets.Add(new Label { Text = "Rotation:" });
        var equipRotPanel = new HorizontalStackPanel { Spacing = 5 };
        _equipRotationSlider = new HorizontalSlider
        {
            Minimum = -180f,
            Maximum = 180f,
            Value = 0f,
            Width = 380
        };
        _equipRotationSlider.ValueChangedByUser += OnEquipRotationChanged;
        _equipRotationLabel = new Label { Text = "0°", Width = 50 };
        equipRotPanel.Widgets.Add(_equipRotationSlider);
        equipRotPanel.Widgets.Add(_equipRotationLabel);
        equipStack.Widgets.Add(equipRotPanel);
        
        // Equipment Scale
        equipStack.Widgets.Add(new Label { Text = "Scale:" });
        var equipScalePanel = new HorizontalStackPanel { Spacing = 5 };
        _equipScaleSlider = new HorizontalSlider
        {
            Minimum = 0.1f,
            Maximum = 3.0f,
            Value = 1f,
            Width = 380
        };
        _equipScaleSlider.ValueChangedByUser += OnEquipScaleChanged;
        _equipScaleLabel = new Label { Text = "1.00", Width = 50 };
        equipScalePanel.Widgets.Add(_equipScaleSlider);
        equipScalePanel.Widgets.Add(_equipScaleLabel);
        equipStack.Widgets.Add(equipScalePanel);
        
        // Equipment Flip H
        _equipFlipHButton = new CursorButton(BaseContent.Styles.Button.Normal)
        {
            Content = new Label { Text = "Flip H: Off" },
        };
        _equipFlipHButton.Click += OnEquipFlipHClicked;
        equipStack.Widgets.Add(_equipFlipHButton);
        
        _equipmentPanel.Widgets.Add(equipStack);
        _equipmentPanel.Visible = false; // Hidden until a part with equipment is selected
        rightPanel.Widgets.Add(_equipmentPanel);
        
        rightPanel.Widgets.Add(new Label { Text = "" }); // Spacer
        
        // Action buttons
        var saveButton = new CursorButton(BaseContent.Styles.Button.Normal)
        {
            Content = new Label { Text = "Save XML" },
        };
        saveButton.Click += (_, _) => SaveXml();
        rightPanel.Widgets.Add(saveButton);
        
        var resetButton = new CursorButton(BaseContent.Styles.Button.Normal)
        {
            Content = new Label { Text = "Reset All" },
        };
        resetButton.Click += (_, _) => ResetAll();
        rightPanel.Widgets.Add(resetButton);
        
        var resetSelectedButton = new CursorButton(BaseContent.Styles.Button.Normal)
        {
            Content = new Label { Text = "Reset Selected" },
        };
        resetSelectedButton.Click += (_, _) => ResetSelected();
        rightPanel.Widgets.Add(resetSelectedButton);
        
        var selectAllButton = new CursorButton(BaseContent.Styles.Button.Normal)
        {
            Content = new Label { Text = "Select All Parts" },
        };
        selectAllButton.Click += (_, _) => SelectAllParts();
        rightPanel.Widgets.Add(selectAllButton);
        
        // Wrap right panel in scroll viewer for overflow
        var rightScrollViewer = new ScrollViewer
        {
            Content = rightPanel,
            ShowHorizontalScrollBar = false,
            ShowVerticalScrollBar = true,
            Height = 950
        };
        mainPanel.Widgets.Add(rightScrollViewer);
        
        Content = mainPanel;
    }

    private void InitializeOverrides()
    {
        if (_layout == null) return;
        
        foreach (var part in _pawn.Body.AllExternalParts)
        {
            if (part.IsSevered) continue;
            
            var renderInfo = _layout.GetRenderInfo(part);
            if (renderInfo.HasValue)
            {
                var info = renderInfo.Value;
                var flipH = (info.Effects & SpriteEffects.FlipHorizontally) != 0;
                var flipV = (info.Effects & SpriteEffects.FlipVertically) != 0;
                
                _overrides[part.InternalLabel] = new BodyPartTransformOverride(
                    info.Position,
                    info.Scale,
                    info.Rotation,
                    flipH,
                    flipV,
                    info.RenderOrder,
                    info.EquipmentAttachment);
            }
        }
    }

    private void PopulatePartsList()
    {
        _partsGrid.Widgets.Clear();
        _partButtons.Clear();
        
        if (_layout == null) return;
        
        var parts = _pawn.Body.AllExternalParts
            .Where(p => !p.IsSevered)
            .Select(p => (part: p, info: _layout.GetRenderInfo(p)))
            .Where(x => x.info.HasValue)
            .Select(x => (x.part, info: x.info!.Value))
            .OrderBy(x => x.info.RenderOrder)
            .ToList();
        
        const int columns = 3;
        int row = 0;
        int col = 0;
        
        foreach (var (part, info) in parts)
        {
            var button = new CursorButton(BaseContent.Styles.Button.Small)
            {
                // Show Label for display, but use Moniker for keying
                Content = new Label(BaseContent.Styles.Label.Small) { Text = part.Label, TextColor = Color.White },
            };
            Grid.SetRow(button, row);
            Grid.SetColumn(button, col);
            
            var partLabel = part.InternalLabel; // Capture for closure
            button.Click += (_, _) => OnPartListItemClicked(partLabel);
            
            _partButtons[part.InternalLabel] = button;
            _partsGrid.Widgets.Add(button);
            
            col++;
            if (col >= columns)
            {
                col = 0;
                row++;
            }
        }
    }

    private void OnPartListItemClicked(string partLabel)
    {
        var keyboardState = Keyboard.GetState();
        var shiftHeld = keyboardState.IsKeyDown(Keys.LeftShift) || keyboardState.IsKeyDown(Keys.RightShift);
        SelectPart(partLabel, shiftHeld);
    }

    private void SelectAllParts()
    {
        _selectedPartLabels.Clear();
        foreach (var label in _partButtons.Keys)
        {
            _selectedPartLabels.Add(label);
        }
        UpdateAllButtonHighlights();
        UpdateSelectionUI();
    }

    private void OnScaleChanged(object? sender, EventArgs e)
    {
        // Snap to 0.05 increments for easier control
        var newValue = MathF.Round(_scaleSlider.Value * 20f) / 20f;
        _scaleSlider.Value = newValue;
        _scaleValueLabel.Text = $"{newValue:F2}";
        
        foreach (var label in _selectedPartLabels)
        {
            if (_overrides.TryGetValue(label, out var over))
            {
                over.Scale = newValue;
            }
        }
    }

    private void OnRotationChanged(object? sender, EventArgs e)
    {
        // Snap to 1 degree increments for easier control
        var newValue = MathF.Round(_rotationSlider.Value);
        _rotationSlider.Value = newValue;
        _rotationValueLabel.Text = $"{newValue:F0}°";
        
        foreach (var label in _selectedPartLabels)
        {
            if (_overrides.TryGetValue(label, out var over))
            {
                over.Rotation = MathHelper.ToRadians(newValue);
            }
        }
    }

    private void OnRenderOrderChanged(object? sender, EventArgs e)
    {
        // Snap to integer increments for easier control
        var newValue = (int)MathF.Round(_renderOrderSlider.Value);
        _renderOrderSlider.Value = newValue;
        _renderOrderValueLabel.Text = $"{newValue}";
        
        foreach (var label in _selectedPartLabels)
        {
            if (_overrides.TryGetValue(label, out var over))
            {
                over.RenderOrder = newValue;
            }
        }
    }

    private void OnFlipHClicked(object? sender, EventArgs e)
    {
        if (_selectedPartLabels.Count == 0) return;
        
        // Toggle based on majority state
        var selectedOverrides = _selectedPartLabels
            .Where(label => _overrides.ContainsKey(label))
            .Select(label => _overrides[label])
            .ToList();
        var anyNotFlipped = selectedOverrides.Any(o => !o.FlipHorizontal);
        foreach (var over in selectedOverrides)
        {
            over.FlipHorizontal = anyNotFlipped;
        }
        UpdateFlipButtonLabels(selectedOverrides.FirstOrDefault());
    }

    private void OnFlipVClicked(object? sender, EventArgs e)
    {
        if (_selectedPartLabels.Count == 0) return;
        
        // Toggle based on majority state
        var selectedOverrides = _selectedPartLabels
            .Where(label => _overrides.ContainsKey(label))
            .Select(label => _overrides[label])
            .ToList();
        var anyNotFlipped = selectedOverrides.Any(o => !o.FlipVertical);
        foreach (var over in selectedOverrides)
        {
            over.FlipVertical = anyNotFlipped;
        }
        UpdateFlipButtonLabels(selectedOverrides.FirstOrDefault());
    }

    private void UpdateFlipButtonLabels(BodyPartTransformOverride? over)
    {
        if (_flipHButton.Content is Label hLabel)
        {
            hLabel.Text = over?.FlipHorizontal == true ? "Horz: ON" : "Horz: Off";
        }
        if (_flipVButton.Content is Label vLabel)
        {
            vLabel.Text = over?.FlipVertical == true ? "Vert: ON" : "Vert: Off";
        }
    }
    
    private void OnHasEquipmentClicked(object? sender, EventArgs e)
    {
        if (_selectedPartLabels.Count == 0) return;
        
        var selectedOverrides = _selectedPartLabels
            .Where(label => _overrides.ContainsKey(label))
            .Select(label => _overrides[label])
            .ToList();
        var anyWithout = selectedOverrides.Any(o => !o.HasEquipmentAttachment);
        foreach (var over in selectedOverrides)
        {
            over.HasEquipmentAttachment = anyWithout;
            if (anyWithout && over.EquipmentScale == 0)
            {
                over.EquipmentScale = 1f; // Default scale
            }
        }
        UpdateEquipmentUI(selectedOverrides.FirstOrDefault());
    }
    
    private void OnEquipOffsetXChanged(object? sender, EventArgs e)
    {
        var newValue = MathF.Round(_equipOffsetXSlider.Value);
        _equipOffsetXSlider.Value = newValue;
        _equipOffsetXLabel.Text = $"{newValue:F0}";
        
        foreach (var label in _selectedPartLabels)
        {
            if (_overrides.TryGetValue(label, out var over))
            {
                over.EquipmentOffset = new Vector2(newValue, over.EquipmentOffset.Y);
            }
        }
    }
    
    private void OnEquipOffsetYChanged(object? sender, EventArgs e)
    {
        var newValue = MathF.Round(_equipOffsetYSlider.Value);
        _equipOffsetYSlider.Value = newValue;
        _equipOffsetYLabel.Text = $"{newValue:F0}";
        
        foreach (var label in _selectedPartLabels)
        {
            if (_overrides.TryGetValue(label, out var over))
            {
                over.EquipmentOffset = new Vector2(over.EquipmentOffset.X, newValue);
            }
        }
    }
    
    private void OnEquipRotationChanged(object? sender, EventArgs e)
    {
        var newValue = MathF.Round(_equipRotationSlider.Value);
        _equipRotationSlider.Value = newValue;
        _equipRotationLabel.Text = $"{newValue:F0}°";
        
        foreach (var label in _selectedPartLabels)
        {
            if (_overrides.TryGetValue(label, out var over))
            {
                over.EquipmentRotation = MathHelper.ToRadians(newValue);
            }
        }
    }
    
    private void OnEquipScaleChanged(object? sender, EventArgs e)
    {
        var newValue = MathF.Round(_equipScaleSlider.Value * 20f) / 20f;
        _equipScaleSlider.Value = newValue;
        _equipScaleLabel.Text = $"{newValue:F2}";
        
        foreach (var label in _selectedPartLabels)
        {
            if (_overrides.TryGetValue(label, out var over))
            {
                over.EquipmentScale = newValue;
            }
        }
    }
    
    private void OnEquipFlipHClicked(object? sender, EventArgs e)
    {
        if (_selectedPartLabels.Count == 0) return;
        
        var selectedOverrides = _selectedPartLabels
            .Where(label => _overrides.ContainsKey(label))
            .Select(label => _overrides[label])
            .ToList();
        var anyNotFlipped = selectedOverrides.Any(o => !o.EquipmentFlipH);
        foreach (var over in selectedOverrides)
        {
            over.EquipmentFlipH = anyNotFlipped;
        }
        UpdateEquipFlipHButton(selectedOverrides.FirstOrDefault());
    }
    
    private void UpdateEquipFlipHButton(BodyPartTransformOverride? over)
    {
        if (_equipFlipHButton.Content is Label label)
        {
            label.Text = over?.EquipmentFlipH == true ? "Flip H: ON" : "Flip H: Off";
        }
    }
    
    private void UpdateEquipmentUI(BodyPartTransformOverride? over)
    {
        if (_hasEquipmentButton.Content is Label hasLabel)
        {
            hasLabel.Text = over?.HasEquipmentAttachment == true ? "Has Attachment: ON" : "Has Attachment: Off";
        }
        
        _equipmentPanel.Visible = over?.HasEquipmentAttachment == true;
        
        if (over != null && over.HasEquipmentAttachment)
        {
            _equipOffsetXSlider.Value = over.EquipmentOffset.X;
            _equipOffsetXLabel.Text = $"{over.EquipmentOffset.X:F0}";
            
            _equipOffsetYSlider.Value = over.EquipmentOffset.Y;
            _equipOffsetYLabel.Text = $"{over.EquipmentOffset.Y:F0}";
            
            var rotDegrees = MathHelper.ToDegrees(over.EquipmentRotation);
            _equipRotationSlider.Value = rotDegrees;
            _equipRotationLabel.Text = $"{rotDegrees:F0}°";
            
            _equipScaleSlider.Value = over.EquipmentScale;
            _equipScaleLabel.Text = $"{over.EquipmentScale:F2}";
            
            UpdateEquipFlipHButton(over);
        }
    }

    private void SelectPart(string partLabel, bool addToSelection)
    {
        if (addToSelection)
        {
            // Toggle selection
            if (_selectedPartLabels.Contains(partLabel))
            {
                _selectedPartLabels.Remove(partLabel);
            }
            else
            {
                _selectedPartLabels.Add(partLabel);
            }
        }
        else
        {
            // Clear and select single part
            _selectedPartLabels.Clear();
            _selectedPartLabels.Add(partLabel);
        }
        
        UpdateAllButtonHighlights();
        UpdateSelectionUI();
    }

    private void ClearSelection()
    {
        _selectedPartLabels.Clear();
        UpdateAllButtonHighlights();
        UpdateSelectionUI();
    }

    private void UpdateAllButtonHighlights()
    {
        foreach (var (label, button) in _partButtons)
        {
            if (button.Content is Label lbl)
            {
                lbl.TextColor = _selectedPartLabels.Contains(label) ? Color.Cyan : Color.White;
            }
        }
    }

    private void UpdateSelectionUI()
    {
        if (_selectedPartLabels.Count == 0)
        {
            _selectedPartLabel_UI.Text = "(none)";
            _scaleSlider.Value = 1f;
            _scaleValueLabel.Text = "1.00";
            _rotationSlider.Value = 0f;
            _rotationValueLabel.Text = "0°";
            _renderOrderSlider.Value = 0f;
            _renderOrderValueLabel.Text = "0";
            UpdateFlipButtonLabels(null);
            UpdateEquipmentUI(null);
        }
        else if (_selectedPartLabels.Count == 1)
        {
            var partLabel = _selectedPartLabels.First();
            _selectedPartLabel_UI.Text = partLabel;
            
            if (_overrides.TryGetValue(partLabel, out var over))
            {
                _scaleSlider.Value = over.Scale;
                _scaleValueLabel.Text = $"{over.Scale:F2}";
                
                var rotationDegrees = MathHelper.ToDegrees(over.Rotation);
                _rotationSlider.Value = rotationDegrees;
                _rotationValueLabel.Text = $"{rotationDegrees:F0}°";
                
                _renderOrderSlider.Value = over.RenderOrder;
                _renderOrderValueLabel.Text = $"{over.RenderOrder}";
                
                UpdateFlipButtonLabels(over);
                UpdateEquipmentUI(over);
            }
        }
        else
        {
            _selectedPartLabel_UI.Text = $"({_selectedPartLabels.Count} parts)";
            // Keep current slider values for multi-selection
            // Show equipment UI if any selected part has equipment
            var firstWithEquip = _selectedPartLabels
                .Where(label => _overrides.ContainsKey(label))
                .Select(label => _overrides[label])
                .FirstOrDefault(o => o.HasEquipmentAttachment);
            UpdateEquipmentUI(firstWithEquip);
        }
    }

    private void OnFrameUpdate()
    {
        var mouseState = Mouse.GetState();
        
        if (_layout != null)
        {
            var screenBounds = _renderArea.LastRenderBounds;
            var screenPos = new Point(mouseState.X, mouseState.Y);
            
            // Update hover state
            string? newHoveredPart = null;
            if (screenBounds.Contains(screenPos) && !_isDragging)
            {
                var nativePos = ScreenToNative(mouseState.X, mouseState.Y);
                newHoveredPart = HitTestPart(nativePos);
            }
            
            if (newHoveredPart != _hoveredPartLabel)
            {
                _hoveredPartLabel = newHoveredPart;
            }
            
            var keyboardState = Keyboard.GetState();
            var shiftHeld = keyboardState.IsKeyDown(Keys.LeftShift) || keyboardState.IsKeyDown(Keys.RightShift);
            
            // Detect mouse button press
            if (mouseState.LeftButton == ButtonState.Pressed && _previousMouseState.LeftButton == ButtonState.Released)
            {
                if (screenBounds.Contains(screenPos))
                {
                    var nativePos = ScreenToNative(mouseState.X, mouseState.Y);
                    var hitPart = HitTestPart(nativePos);
                    
                    if (hitPart != null)
                    {
                        // Select part (shift adds to selection)
                        SelectPart(hitPart, shiftHeld);
                        
                        // Start dragging selected parts
                        if (_selectedPartLabels.Count > 1)
                        {
                            _isDraggingMultiple = true;
                            _isDragging = true;
                            _allPartsDragOffsets.Clear();
                            foreach (var label in _selectedPartLabels)
                            {
                                if (_overrides.TryGetValue(label, out var over))
                                {
                                    _allPartsDragOffsets[label] = nativePos - over.Position;
                                }
                            }
                        }
                        else
                        {
                            _draggedPartLabel = hitPart;
                            _dragOffset = nativePos - _overrides[hitPart].Position;
                            _isDragging = true;
                            _isDraggingMultiple = false;
                        }
                    }
                }
            }
            
            // Handle dragging
            if (_isDragging)
            {
                if (mouseState.LeftButton == ButtonState.Pressed)
                {
                    var nativePos = ScreenToNative(mouseState.X, mouseState.Y);
                    
                    if (_isDraggingMultiple)
                    {
                        // Move all selected parts
                        foreach (var (label, offset) in _allPartsDragOffsets)
                        {
                            if (_overrides.TryGetValue(label, out var over))
                            {
                                over.Position = nativePos - offset;
                            }
                        }
                    }
                    else if (_draggedPartLabel != null)
                    {
                        var newPosition = nativePos - _dragOffset;
                        _overrides[_draggedPartLabel].Position = newPosition;
                    }
                }
                else
                {
                    // Drag ended
                    _draggedPartLabel = null;
                    _isDragging = false;
                    _isDraggingMultiple = false;
                    _allPartsDragOffsets.Clear();
                    SaveXml();
                }
            }
            
            _previousKeyboardState = keyboardState;
        }
        
        _previousMouseState = mouseState;
    }

    private Vector2 ScreenToNative(int screenX, int screenY)
    {
        var screenBounds = _renderArea.LastRenderBounds;
        var localX = screenX - screenBounds.X;
        var localY = screenY - screenBounds.Y;
        
        float scaleX = (float)(_layout?.NativeSize ?? _renderSize) / screenBounds.Width;
        float scaleY = (float)(_layout?.NativeSize ?? _renderSize) / screenBounds.Height;
        
        return new Vector2(localX * scaleX, localY * scaleY);
    }

    private string? HitTestPart(Vector2 nativePosition)
    {
        if (_layout == null) return null;
        
        var parts = _pawn.Body.AllExternalParts
            .Where(p => !p.IsSevered)
            .Select(p => (part: p, info: _layout.GetRenderInfo(p)))
            .Where(x => x.info.HasValue)
            .Select(x => (x.part, info: x.info!.Value))
            .ToList();
        
        // Sort by render order descending (front to back for hit testing), using override if available
        parts.Sort((a, b) =>
        {
            var orderA = _overrides.TryGetValue(a.part.InternalLabel, out var overA) ? overA.RenderOrder : a.info.RenderOrder;
            var orderB = _overrides.TryGetValue(b.part.InternalLabel, out var overB) ? overB.RenderOrder : b.info.RenderOrder;
            return orderB.CompareTo(orderA); // Descending for hit testing
        });
        
        foreach (var (part, info) in parts)
        {
            var over = _overrides.GetValueOrDefault(part.InternalLabel);
            var position = over?.Position ?? info.Position;
            var scale = over?.Scale ?? info.Scale;
            var rotation = over?.Rotation ?? info.Rotation;
            var flipH = over?.FlipHorizontal ?? (info.Effects & SpriteEffects.FlipHorizontally) != 0;
            var flipV = over?.FlipVertical ?? (info.Effects & SpriteEffects.FlipVertically) != 0;
            
            // Check if point is within the scaled texture bounds first (fast rejection)
            var partBounds = new RectangleF(
                position.X,
                position.Y,
                info.Texture.Width * scale,
                info.Texture.Height * scale);
            
            if (!partBounds.Contains(nativePosition))
                continue;
            
            // Calculate the pixel coordinate within the texture
            var localX = (nativePosition.X - position.X) / scale;
            var localY = (nativePosition.Y - position.Y) / scale;
            
            // Apply rotation transform (inverse rotation to go from world to local)
            if (rotation != 0f)
            {
                var centerX = info.Texture.Width / 2f;
                var centerY = info.Texture.Height / 2f;
                
                // Translate to center, rotate inversely, translate back
                var dx = localX - centerX;
                var dy = localY - centerY;
                var cos = (float)Math.Cos(-rotation);
                var sin = (float)Math.Sin(-rotation);
                localX = dx * cos - dy * sin + centerX;
                localY = dx * sin + dy * cos + centerY;
            }
            
            // Apply flip transforms
            if (flipH)
                localX = info.Texture.Width - 1 - localX;
            if (flipV)
                localY = info.Texture.Height - 1 - localY;
            
            var pixelX = (int)localX;
            var pixelY = (int)localY;
            
            // Bounds check
            if (pixelX < 0 || pixelX >= info.Texture.Width || pixelY < 0 || pixelY >= info.Texture.Height)
                continue;
            
            // Get pixel data and check alpha
            if (IsPixelOpaque(info.Texture, pixelX, pixelY))
            {
                return part.InternalLabel;
            }
        }
        
        return null;
    }
    
    /// <summary>
    /// Checks if a pixel at the given texture coordinates is opaque (alpha > threshold).
    /// </summary>
    private bool IsPixelOpaque(Texture2D texture, int x, int y, byte alphaThreshold = 32)
    {
        // Get or create cached pixel data
        if (!_texturePixelCache.TryGetValue(texture, out var pixels))
        {
            pixels = new Color[texture.Width * texture.Height];
            texture.GetData(pixels);
            _texturePixelCache[texture] = pixels;
        }
        
        var index = y * texture.Width + x;
        if (index < 0 || index >= pixels.Length)
            return false;
        
        return pixels[index].A > alphaThreshold;
    }

    public void Render(RenderContext context, Rectangle destRect)
    {
        if (_layout == null) return;
        
        // Render directly to the context without using a render target
        // This avoids render target switching issues during Myra's render pass
        RenderDirect(context, destRect);
    }
    
    private void RenderDirect(RenderContext context, Rectangle destRect)
    {
        if (_layout == null) return;
        
        // Flush Myra's batch so we can use our own SpriteBatch for advanced features
        context.Flush();
        
        _spriteBatch ??= new SpriteBatch(Core.GraphicsDevice);
        
        // Use scissor rectangle to clip to our bounds
        var previousScissor = Core.GraphicsDevice.ScissorRectangle;
        var previousRasterizerState = Core.GraphicsDevice.RasterizerState;
        
        Core.GraphicsDevice.ScissorRectangle = destRect;
        
        _spriteBatch.Begin(
            SpriteSortMode.Deferred, 
            BlendState.AlphaBlend, 
            SamplerState.PointClamp,
            null,
            new RasterizerState { ScissorTestEnable = true });
        
        var parts = _pawn.Body.AllExternalParts
            .Where(p => !p.IsSevered)
            .Select(p => (part: p, info: _layout.GetRenderInfo(p)))
            .Where(x => x.info.HasValue)
            .Select(x => (x.part, info: x.info!.Value))
            .ToList();
        
        // Sort by render order (back to front), using override if available
        parts.Sort((a, b) =>
        {
            var orderA = _overrides.TryGetValue(a.part.InternalLabel, out var overA) ? overA.RenderOrder : a.info.RenderOrder;
            var orderB = _overrides.TryGetValue(b.part.InternalLabel, out var overB) ? overB.RenderOrder : b.info.RenderOrder;
            return orderA.CompareTo(orderB);
        });
        
        // Calculate scale from native layout size to destination rect
        float scaleX = (float)destRect.Width / _layout.NativeSize;
        float scaleY = (float)destRect.Height / _layout.NativeSize;
        float layoutScale = Math.Min(scaleX, scaleY);
        
        var destOffset = new Vector2(destRect.X, destRect.Y);
        
        foreach (var (part, info) in parts)
        {
            var over = _overrides.GetValueOrDefault(part.InternalLabel);
            var position = over?.Position ?? info.Position;
            var partScale = over?.Scale ?? info.Scale;
            var rotation = over?.Rotation ?? info.Rotation;
            
            // Compute sprite effects from override or original
            var effects = SpriteEffects.None;
            if (over != null)
            {
                if (over.FlipHorizontal) effects |= SpriteEffects.FlipHorizontally;
                if (over.FlipVertical) effects |= SpriteEffects.FlipVertically;
            }
            else
            {
                effects = info.Effects;
            }
            
            var tint = BodyPartColor.Get(part);
            
            // Highlight selected/dragged/hovered part
            if (_isDraggingMultiple && _selectedPartLabels.Contains(part.InternalLabel))
            {
                tint = Color.Yellow;
            }
            else if (_draggedPartLabel == part.InternalLabel)
            {
                tint = Color.Yellow;
            }
            else if (_selectedPartLabels.Contains(part.InternalLabel))
            {
                tint = Color.Lerp(tint, Color.Cyan, 0.5f);
            }
            else if (_hoveredPartLabel == part.InternalLabel)
            {
                tint = Color.Lerp(tint, Color.Orange, 0.35f);
            }
            
            // Render equipped weapons BEFORE the body part (so they appear behind/underneath)
            // Build equipment attachment from override if available
            EquipmentAttachmentData? equipAttachment = null;
            if (over != null && over.HasEquipmentAttachment)
            {
                equipAttachment = new EquipmentAttachmentData(over.EquipmentOffset, over.EquipmentRotation, over.EquipmentScale, over.EquipmentFlipH);
            }
            BodyPartRenderHelper.RenderEquippedWeapons(_spriteBatch, part, info, position: position, scale: partScale, equipmentAttachment: equipAttachment, layoutScale: layoutScale, offset: destOffset);
            
            BodyPartRenderHelper.RenderBodyPart(
                _spriteBatch, 
                info, 
                position: position,
                scale: partScale,
                rotation: rotation,
                effects: effects,
                layoutScale: layoutScale, 
                tint: tint,
                offset: destOffset);
            
            // Render equipped armor AFTER the body part (so it appears on top)
            BodyPartRenderHelper.RenderEquippedArmor(
                _spriteBatch, 
                part, 
                info, 
                position: position,
                scale: partScale,
                equipmentAttachment: equipAttachment,
                layoutScale: layoutScale,
                offset: destOffset);
        }
        
        _spriteBatch.End();
        
        // Restore previous state
        Core.GraphicsDevice.ScissorRectangle = previousScissor;
        Core.GraphicsDevice.RasterizerState = previousRasterizerState;
    }
    
    private void ResetAll()
    {
        InitializeOverrides();
        ClearSelection();
    }

    private void ResetSelected()
    {
        if (_selectedPartLabels.Count == 0 || _layout == null) return;
        
        foreach (var internalLabel in _selectedPartLabels)
        {
            var part = _pawn.Body.AllExternalParts.FirstOrDefault(p => p.InternalLabel == internalLabel);
            if (part != null)
            {
                var renderInfo = _layout.GetRenderInfo(part);
                if (renderInfo.HasValue)
                {
                    var info = renderInfo.Value;
                    var flipH = (info.Effects & SpriteEffects.FlipHorizontally) != 0;
                    var flipV = (info.Effects & SpriteEffects.FlipVertically) != 0;
                    
                    _overrides[internalLabel] = new BodyPartTransformOverride(
                        info.Position,
                        info.Scale,
                        info.Rotation,
                        flipH,
                        flipV,
                        info.RenderOrder,
                        info.EquipmentAttachment);
                }
            }
        }
        
        UpdateSelectionUI();
    }

    private void SaveXml()
    {
        if (_layout == null)
        {
            return;
        }

        var cells = _overrides.Select(pair =>
        {
            EquipmentAttachmentData? attachment = null;
            if (pair.Value.HasEquipmentAttachment)
            {
                attachment = new EquipmentAttachmentData(
                    pair.Value.EquipmentOffset,
                    pair.Value.EquipmentRotation,
                    pair.Value.EquipmentScale,
                    pair.Value.EquipmentFlipH);
            }

            var data = new BodyPartLayoutData(
                pair.Value.Position,
                pair.Value.RenderOrder,
                pair.Value.Scale,
                pair.Value.Rotation,
                pair.Value.FlipHorizontal,
                pair.Value.FlipVertical,
                attachment);
            return (pair.Key, data);
        });

        try
        {
            var path = BodyPartLayoutXml.Write(_pawn.Body.Def, _layout.NativeSize, cells);
            Title = $"Body Part Editor - {_pawn.Label}  (saved {Path.GetFileName(path)})";
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to save body part layout: {ex.Message}");
        }
    }
}

/// <summary>
/// Render area widget for the body part editor.
/// </summary>
internal class BodyPartEditorRenderArea : Widget
{
    private readonly BodyPartEditorWindow _editor;
    
    public Action? OnFrameUpdate;
    public Rectangle LastRenderBounds { get; private set; }

    public BodyPartEditorRenderArea(BodyPartEditorWindow editor)
    {
        _editor = editor;
    }

    public override void InternalRender(RenderContext context)
    {
        base.InternalRender(context);
        
        var bounds = ActualBounds;
        
        // Calculate screen bounds using transform
        var transform = context.Transform;
        var topLeft = new Vector2(bounds.X, bounds.Y);
        var screenTopLeft = transform.Apply(topLeft);
        
        var bottomRight = new Vector2(bounds.X + bounds.Width, bounds.Y + bounds.Height);
        var screenBottomRight = transform.Apply(bottomRight);
        
        var screenWidth = (int)(screenBottomRight.X - screenTopLeft.X);
        var screenHeight = (int)(screenBottomRight.Y - screenTopLeft.Y);
        
        // Use screen coordinates for direct SpriteBatch rendering
        LastRenderBounds = new Rectangle((int)screenTopLeft.X, (int)screenTopLeft.Y, screenWidth, screenHeight);
        
        // Process input/updates (doesn't touch render targets)
        OnFrameUpdate?.Invoke();
        
        // Pass screen bounds since we're using direct SpriteBatch rendering
        _editor.Render(context, LastRenderBounds);
    }
}

