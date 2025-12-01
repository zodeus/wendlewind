using System.Runtime.InteropServices;
using System.Text;
using Grafted.Scenes.MainGameScene.Gui.Widgets.CombatWidgets.BodyPartLayouts;
using Grafted.Scenes.MainGameScene.Gui.Widgets.EntityWidgets.PawnWidgets;

namespace Grafted.Scenes.MainGameScene.Gui.Widgets.CombatWidgets;

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
    
    public BodyPartTransformOverride(Vector2 position, float scale = 1f, float rotation = 0f, bool flipH = false, bool flipV = false, int renderOrder = 0)
    {
        Position = position;
        Scale = scale;
        Rotation = rotation;
        FlipHorizontal = flipH;
        FlipVertical = flipV;
        RenderOrder = renderOrder;
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
    
    // Editor state
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
    private readonly Button _flipHButton;
    private readonly Button _flipVButton;
    private readonly Grid _partsGrid;
    private readonly Dictionary<string, Button> _partButtons = new();

    public BodyPartEditorWindow(Pawn pawn, int renderSize = 512)
    {
        _pawn = pawn;
        _renderSize = renderSize;
        _layout = BodyPartLayoutRegistry.GetLayoutFor(pawn.Body);
        
        Title = $"Body Part Editor - {pawn.Label}";
        Width = 1200;
        Height = 800;
        
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
        _flipHButton = new Button(BaseContent.Styles.Button.Normal)
        {
            Content = new Label { Text = "Horz: Off" },
        };
        _flipHButton.Click += OnFlipHClicked;
        _flipVButton = new Button(BaseContent.Styles.Button.Normal)
        {
            Content = new Label { Text = "Vert: Off" },
        };
        _flipVButton.Click += OnFlipVClicked;
        flipPanel.Widgets.Add(_flipHButton);
        flipPanel.Widgets.Add(_flipVButton);
        rightPanel.Widgets.Add(flipPanel);
        
        // Action buttons
        var copyButton = new Button(BaseContent.Styles.Button.Normal)
        {
            Content = new Label { Text = "Copy to Clipboard" },
        };
        copyButton.Click += (_, _) => CopyPositionsToClipboard();
        rightPanel.Widgets.Add(copyButton);
        
        var resetButton = new Button(BaseContent.Styles.Button.Normal)
        {
            Content = new Label { Text = "Reset All" },
        };
        resetButton.Click += (_, _) => ResetAll();
        rightPanel.Widgets.Add(resetButton);
        
        var resetSelectedButton = new Button(BaseContent.Styles.Button.Normal)
        {
            Content = new Label { Text = "Reset Selected" },
        };
        resetSelectedButton.Click += (_, _) => ResetSelected();
        rightPanel.Widgets.Add(resetSelectedButton);
        
        var selectAllButton = new Button(BaseContent.Styles.Button.Normal)
        {
            Content = new Label { Text = "Select All Parts" },
        };
        selectAllButton.Click += (_, _) => SelectAllParts();
        rightPanel.Widgets.Add(selectAllButton);
        
        mainPanel.Widgets.Add(rightPanel);
        
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
                
                _overrides[part.Label] = new BodyPartTransformOverride(
                    info.Position,
                    info.Scale,
                    info.Rotation,
                    flipH,
                    flipV,
                    info.RenderOrder);
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
            var button = new Button(BaseContent.Styles.Button.Small)
            {
                Content = new Label(BaseContent.Styles.Label.Small) { Text = part.Label, TextColor = Color.White },
            };
            Grid.SetRow(button, row);
            Grid.SetColumn(button, col);
            
            var partLabel = part.Label; // Capture for closure
            button.Click += (_, _) => OnPartListItemClicked(partLabel);
            
            _partButtons[part.Label] = button;
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
            .Where(l => _overrides.ContainsKey(l))
            .Select(l => _overrides[l])
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
            .Where(l => _overrides.ContainsKey(l))
            .Select(l => _overrides[l])
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
            }
        }
        else
        {
            _selectedPartLabel_UI.Text = $"({_selectedPartLabels.Count} parts)";
            // Keep current slider values for multi-selection
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
            var orderA = _overrides.TryGetValue(a.part.Label, out var overA) ? overA.RenderOrder : a.info.RenderOrder;
            var orderB = _overrides.TryGetValue(b.part.Label, out var overB) ? overB.RenderOrder : b.info.RenderOrder;
            return orderB.CompareTo(orderA); // Descending for hit testing
        });
        
        foreach (var (part, info) in parts)
        {
            var over = _overrides.GetValueOrDefault(part.Label);
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
                return part.Label;
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
            var orderA = _overrides.TryGetValue(a.part.Label, out var overA) ? overA.RenderOrder : a.info.RenderOrder;
            var orderB = _overrides.TryGetValue(b.part.Label, out var overB) ? overB.RenderOrder : b.info.RenderOrder;
            return orderA.CompareTo(orderB);
        });
        
        // Calculate scale from native layout size to destination rect
        float scaleX = (float)destRect.Width / _layout.NativeSize;
        float scaleY = (float)destRect.Height / _layout.NativeSize;
        float layoutScale = Math.Min(scaleX, scaleY);
        
        var destOffset = new Vector2(destRect.X, destRect.Y);
        
        foreach (var (part, info) in parts)
        {
            var over = _overrides.GetValueOrDefault(part.Label);
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
            if (_isDraggingMultiple && _selectedPartLabels.Contains(part.Label))
            {
                tint = Color.Yellow;
            }
            else if (_draggedPartLabel == part.Label)
            {
                tint = Color.Yellow;
            }
            else if (_selectedPartLabels.Contains(part.Label))
            {
                tint = Color.Lerp(tint, Color.Cyan, 0.5f);
            }
            else if (_hoveredPartLabel == part.Label)
            {
                tint = Color.Lerp(tint, Color.Orange, 0.35f);
            }
            
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
        
        foreach (var partLabel in _selectedPartLabels)
        {
            var part = _pawn.Body.AllExternalParts.FirstOrDefault(p => p.Label == partLabel);
            if (part != null)
            {
                var renderInfo = _layout.GetRenderInfo(part);
                if (renderInfo.HasValue)
                {
                    var info = renderInfo.Value;
                    var flipH = (info.Effects & SpriteEffects.FlipHorizontally) != 0;
                    var flipV = (info.Effects & SpriteEffects.FlipVertically) != 0;
                    
                    _overrides[partLabel] = new BodyPartTransformOverride(
                        info.Position,
                        info.Scale,
                        info.Rotation,
                        flipH,
                        flipV,
                        info.RenderOrder);
                }
            }
        }
        
        UpdateSelectionUI();
    }

    private void CopyPositionsToClipboard()
    {
        var sb = new StringBuilder();
        sb.AppendLine("// Body part positions (native coordinates)");
        sb.AppendLine("private static readonly Dictionary<string, BodyPartLayoutData> PartLayoutMap = new()");
        sb.AppendLine("{");
        
        var partInfos = new List<(string label, Vector2 position, int renderOrder, float scale, float rotation, bool flipH, bool flipV)>();
        
        // Use the overrides dictionary which contains all parts we've been editing
        foreach (var (label, over) in _overrides)
        {
            partInfos.Add((
                label, 
                over.Position, 
                over.RenderOrder, 
                over.Scale, 
                over.Rotation, 
                over.FlipHorizontal, 
                over.FlipVertical));
        }
        
        partInfos.Sort((a, b) => a.renderOrder.CompareTo(b.renderOrder));
        
        foreach (var (label, position, renderOrder, scale, rotation, flipH, flipV) in partInfos)
        {
            var optionalParams = "";
            if (rotation != 0f) optionalParams += $", {rotation:F4}f";
            else if (flipH || flipV) optionalParams += ", 0f"; // Need to include rotation if we have flip params
            if (flipH) optionalParams += ", flipHorizontal: true";
            if (flipV) optionalParams += ", flipVertical: true";
            
            sb.AppendLine($"    {{ \"{label}\", new BodyPartLayoutData(new Vector2({position.X:F0}f, {position.Y:F0}f), {renderOrder}, {scale:F2}f{optionalParams}) }},");
        }
        
        sb.AppendLine("};");
        
        try
        {
            SetClipboardText(sb.ToString());
            Log.Info("Body part positions copied to clipboard!");
        }
        catch (Exception ex)
        {
            Log.Error($"Failed to copy to clipboard: {ex.Message}");
        }
    }

    #region Windows Clipboard
    
    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool OpenClipboard(IntPtr hWndNewOwner);
    
    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool CloseClipboard();
    
    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool EmptyClipboard();
    
    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetClipboardData(uint uFormat, IntPtr hMem);
    
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GlobalAlloc(uint uFlags, UIntPtr dwBytes);
    
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GlobalLock(IntPtr hMem);
    
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GlobalUnlock(IntPtr hMem);
    
    private const uint CF_UNICODETEXT = 13;
    private const uint GMEM_MOVEABLE = 0x0002;
    
    private static void SetClipboardText(string text)
    {
        if (!OpenClipboard(IntPtr.Zero))
            throw new Exception("Could not open clipboard");
        
        try
        {
            EmptyClipboard();
            
            var bytes = Encoding.Unicode.GetBytes(text + "\0");
            var hGlobal = GlobalAlloc(GMEM_MOVEABLE, (UIntPtr)bytes.Length);
            
            if (hGlobal == IntPtr.Zero)
                throw new Exception("Could not allocate memory for clipboard");
            
            var target = GlobalLock(hGlobal);
            if (target == IntPtr.Zero)
                throw new Exception("Could not lock clipboard memory");
            
            try
            {
                Marshal.Copy(bytes, 0, target, bytes.Length);
            }
            finally
            {
                GlobalUnlock(hGlobal);
            }
            
            if (SetClipboardData(CF_UNICODETEXT, hGlobal) == IntPtr.Zero)
                throw new Exception("Could not set clipboard data");
        }
        finally
        {
            CloseClipboard();
        }
    }
    
    #endregion
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

