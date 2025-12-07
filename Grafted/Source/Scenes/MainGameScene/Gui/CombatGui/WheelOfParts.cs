using Grafted.Sim.Combat;
using Grafted.Sim.Entities.Pawns.Bodies;

namespace Grafted.Scenes.MainGameScene.Gui.CombatGui;

/// <summary>
/// A wheel of fortune style widget that displays missing body part types.
/// The wheel spins and lands on a random part type which can then be restored.
/// </summary>
internal sealed class WheelOfParts : VerticalStackPanel
{
    public event Action? OnSkipped;
    private readonly Pawn _pawn;
    private readonly ShrineProperties _shrine;
    private readonly WheelRenderWidget _wheelWidget;
    private readonly Button _spinButton;
    private readonly Button _skipButton;
    private readonly Label _resultLabel;
    private readonly int _maxSpins;
    private int _spinsUsed;
    private List<BodyPartType>? _pendingPartTypesUpdate;

    public WheelOfParts(Pawn pawn, ShrineProperties shrine)
    {
        _pawn = pawn;
        _shrine = shrine;
        _maxSpins = shrine.PartsToRestore.RandomValue;

        HorizontalAlignment = HorizontalAlignment.Center;
        VerticalAlignment = VerticalAlignment.Center;
        Spacing = 20;

        // Title
        Widgets.Add(new Label(BaseContent.Styles.Label.Huge)
        {
            Margin = new Thickness(0, 50, 0, 0),
            Text = "Wheel of Parts",
            HorizontalAlignment = HorizontalAlignment.Center,
            TextColor = BaseContent.Colors.Text.Golden
        });

        // Wheel render widget
        var missingPartTypes = GetMissingPartTypes();
        _wheelWidget = new WheelRenderWidget(missingPartTypes)
        {
            Width = 500,
            Height = 500,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        _wheelWidget.OnSpinComplete += HandleSpinComplete;
        Widgets.Add(_wheelWidget);

        // Buttons panel
        var buttonsPanel = new HorizontalStackPanel
        {
            Spacing = 30,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 10, 0, 0)
        };

        _spinButton = new Button(BaseContent.Styles.Button.LargeGold)
        {
            Content = new Label(BaseContent.Styles.Label.Large) { Text = "Spin!" },
            Width = 200,
            Enabled = missingPartTypes.Count > 0
        };
        _spinButton.Click += OnSpinClicked;
        buttonsPanel.Widgets.Add(_spinButton);

        _skipButton = new Button(BaseContent.Styles.Button.Large)
        {
            Content = new Label(BaseContent.Styles.Label.Large) { Text = "Leave" },
            Width = 200
        };
        _skipButton.Click += OnSkipClicked;
        buttonsPanel.Widgets.Add(_skipButton);

        Widgets.Add(buttonsPanel);

        // Result label (initially empty)
        _resultLabel = new Label(BaseContent.Styles.Label.Large)
        {
            Text = "",
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 10, 0, 10),
            MinHeight = 160
            
        };
        Widgets.Add(_resultLabel);

        // Info label
        if (missingPartTypes.Count == 0)
        {
            _resultLabel.Text = "Your body is whole again!";
            _spinButton.Enabled = false;
        }
    }

    public void Update(float deltaTime)
    {
        _wheelWidget.Update(deltaTime);
    }

    private List<BodyPartType> GetMissingPartTypes()
    {
        // Get all unsocketed sockets from external body parts
        var unsocketedSockets = _pawn.Body.AllExternalParts
            .SelectMany(p => p.Sockets.Where(s => s.AttachedPart == null))
            .ToList();

        // Get unique body part types from allowed types on those sockets
        // Filter to only include types that the shrine can restore
        var partTypes = unsocketedSockets
            .SelectMany(s => s.Def.AllowedBodyPartTypes)
            .Where(t => _shrine.RestorablePartTypes.Contains(t))
            .Distinct()
            .ToList();

        return partTypes;
    }

    private BodyPartSocket? FindSocketForPartType(BodyPartType partType)
    {
        // Find an empty socket that can accept this part type
        return _pawn.Body.AllExternalParts
            .SelectMany(p => p.Sockets)
            .Where(s => s.AttachedPart == null && s.Def.AllowedBodyPartTypes.Contains(partType))
            .InRandomOrder()
            .FirstOrNull();
    }

    private void OnSpinClicked(object? sender, EventArgs e)
    {
        if (_wheelWidget.IsSpinning)
            return;

        // Apply pending wheel update from previous spin
        if (_pendingPartTypesUpdate != null)
        {
            _wheelWidget.UpdatePartTypes(_pendingPartTypesUpdate);
            _pendingPartTypesUpdate = null;
        }

        var missingParts = GetMissingPartTypes();
        if (missingParts.Count == 0)
            return;

        _resultLabel.Text = "";
        _spinButton.Enabled = false;
        _skipButton.Enabled = false;
        _wheelWidget.StartSpin();
    }

    private void OnSkipClicked(object? sender, EventArgs e)
    {
        if (_wheelWidget.IsSpinning)
            return;

        OnSkipped?.Invoke();
    }

    private void HandleSpinComplete(BodyPartType selectedPartType)
    {
        _spinsUsed++;
        
        // Check if it landed on a blank slot
        if (selectedPartType == BodyPartType.Undefined)
        {
            _resultLabel.Text = $"{_shrine.GodLabel} looks away...\nNothing happens.";
            _resultLabel.TextColor = Color.Gray;
        }
        else
        {
            // Find a socket that can accept this part type
            var socket = FindSocketForPartType(selectedPartType);
            
            if (socket == null)
            {
                // No socket available for this part type (shouldn't happen but handle it)
                _resultLabel.Text = $"No socket available for {selectedPartType}...";
                _resultLabel.TextColor = Color.Gray;
            }
            else
            {
                // Try to restore the part
                TryRestorePart(socket, selectedPartType);
            }
        }

        // Check if we can spin again
        var remainingParts = GetMissingPartTypes();
        bool canSpinAgain = _spinsUsed < _maxSpins && remainingParts.Count > 0;

        if (canSpinAgain)
        {
            _spinButton.Enabled = true;
            _skipButton.Enabled = true;
            // Defer wheel update until next spin so player can see what they landed on
            _pendingPartTypesUpdate = remainingParts;
        }
        else
        {
            // No more spins or no more parts to restore
            _spinButton.Visible = false;
            ((Label)_skipButton.Content!).Text = "Continue";
            _skipButton.Enabled = true;
            
            if (remainingParts.Count == 0)
            {
                _resultLabel.Text += $"\n\n/c[{TC.Default}]Your body is now whole!";
            }
            else if (_spinsUsed >= _maxSpins)
            {
                _resultLabel.Text += $"\n\n/c[{TC.Default}]{_shrine.GodLabel}'s blessing fades...";
            }
        }
    }

    private void TryRestorePart(BodyPartSocket socket, BodyPartType partType)
    {
        string? restoredPartName = null;

        // Restore based on part type (matching the ShrineScreen logic)
        switch (partType)
        {
            case BodyPartType.Finger:
                HumanBodyGenerator.MakeFingerForSocket(socket, Defs.BodyParts.HumanFinger);
                restoredPartName = socket.AttachedPart?.Label;
                break;
            case BodyPartType.Thumb:
                HumanBodyGenerator.MakeFingerForSocket(socket, Defs.BodyParts.HumanThumb);
                restoredPartName = socket.AttachedPart?.Label;
                break;
            case BodyPartType.Hand:
                HumanBodyGenerator.MakeHandForSocket(socket);
                restoredPartName = socket.AttachedPart?.Label;
                break;
            case BodyPartType.Foot:
                HumanBodyGenerator.MakeFootForSocket(socket);
                restoredPartName = socket.AttachedPart?.Label;
                break;
            default:
                // Generic part restoration - just attach the part directly if possible
                var partDef = DefRepository<BodyPartDef>.Defs.FirstOrDefault(d => d.BodyPartType == partType);
                if (partDef != null)
                {
                    socket.TryAttachPart(partDef);
                    restoredPartName = socket.AttachedPart?.Label;
                }
                break;
        }

        // Show the result
        if (restoredPartName != null)
        {
            _resultLabel.Text = $"{restoredPartName}";
            _resultLabel.TextColor = BaseContent.Colors.Text.PartTextColor;
        }
        else
        {
            _resultLabel.Text = $"{_shrine.GodLabel} gazes upon the wound...\nThe restoration failed.";
            _resultLabel.TextColor = Color.Crimson;
        }
    }
}

/// <summary>
/// Custom widget that renders the spinning wheel with 16 slots (4 blanks).
/// Features procedurally generated textures for a dark, mystical appearance.
/// </summary>
internal sealed class WheelRenderWidget : Widget
{
    public event Action<BodyPartType>? OnSpinComplete;

    private const int SlotCount = 16;
    private const int BlankSlotCount = 6;
    private const int PartSlotCount = SlotCount - BlankSlotCount;
    
    private readonly List<BodyPartType> _uniquePartTypes = new();
    private readonly List<BodyPartType> _wheelSlots = new();

    private float _rotation;
    private float _spinVelocity;
    private bool _isSpinning;
    private float _time; // For animated effects
    
    // Spin physics - time-based for predictable duration
    private const float SpinDuration = 3.0f; // Total spin time in seconds
    private const float InitialSpinSpeed = 15f; // For visual effects only
    private float _spinElapsed; // Time elapsed since spin started
    private float _spinStartRotation;
    private float _targetRotation;
    
    // Generated textures
    private Texture2D? _wheelTexture;
    private Texture2D? _glowTexture;
    private Texture2D? _centerHubTexture;
    private const int TextureSize = 512;
    private bool _texturesGenerated;
    
    // Cached SpriteBatch to avoid allocating every frame
    private SpriteBatch? _spriteBatch;

    public bool IsSpinning => _isSpinning;

    public WheelRenderWidget(List<BodyPartType> partTypes)
    {
        _uniquePartTypes.AddRange(partTypes.Count > 0 ? partTypes : new List<BodyPartType> { BodyPartType.Undefined });
        FillWheelSlots();
    }

    private void FillWheelSlots()
    {
        _wheelSlots.Clear();
        
        for (int i = 0; i < PartSlotCount; i++)
        {
            _wheelSlots.Add(_uniquePartTypes[i % _uniquePartTypes.Count]);
        }
        
        for (int i = 0; i < BlankSlotCount; i++)
        {
            _wheelSlots.Add(BodyPartType.Undefined);
        }
        
        ShuffleSlots();
    }

    private void ShuffleSlots()
    {
        for (int i = _wheelSlots.Count - 1; i > 0; i--)
        {
            int j = Core.Random.Next(i + 1);
            (_wheelSlots[i], _wheelSlots[j]) = (_wheelSlots[j], _wheelSlots[i]);
        }
    }

    public void UpdatePartTypes(List<BodyPartType> partTypes)
    {
        _uniquePartTypes.Clear();
        _uniquePartTypes.AddRange(partTypes.Count > 0 ? partTypes : new List<BodyPartType> { BodyPartType.Undefined });
        FillWheelSlots();
        RegenerateWheelTexture();
    }

    private void GenerateTextures()
    {
        if (_texturesGenerated) return;
        _texturesGenerated = true;

        GenerateGlowTexture();
        GenerateCenterHubTexture();
        GenerateWheelTexture();
    }

    private void RegenerateWheelTexture()
    {
        _wheelTexture?.Dispose();
        _wheelTexture = null;
        GenerateWheelTexture();
    }

    private void GenerateGlowTexture()
    {
        var size = 128;
        var data = new Color[size * size];
        var center = size / 2f;
        
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - center;
                float dy = y - center;
                float dist = MathF.Sqrt(dx * dx + dy * dy) / center;
                
                // Soft radial gradient
                float alpha = MathF.Max(0, 1f - dist);
                alpha = alpha * alpha * alpha; // Cubic falloff for softer glow
                
                data[y * size + x] = new Color((byte)255, (byte)200, (byte)100, (byte)(alpha * 255));
            }
        }
        
        _glowTexture = new Texture2D(Core.GraphicsDevice, size, size);
        _glowTexture.SetData(data);
    }

    private void GenerateCenterHubTexture()
    {
        var size = 128;
        var data = new Color[size * size];
        var center = size / 2f;
        
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - center;
                float dy = y - center;
                float dist = MathF.Sqrt(dx * dx + dy * dy) / center;
                float angle = MathF.Atan2(dy, dx);
                
                if (dist > 1f)
                {
                    data[y * size + x] = Color.Transparent;
                    continue;
                }
                
                // Metallic hub with beveled edge
                float metallic = 0.5f + 0.3f * MathF.Sin(angle * 2 + 0.5f);
                
                // Edge highlight (simulated bevel)
                float edgeBrightness = dist > 0.8f ? (1f - (dist - 0.8f) * 5f) * 0.5f + 0.5f : 1f;
                
                // Central gem effect
                float gemDist = dist / 0.4f;
                if (gemDist < 1f)
                {
                    // Radial gradient for gem
                    float gemHighlight = 1f - gemDist * 0.5f;
                    byte r = (byte)(200 * gemHighlight);
                    byte g = (byte)(140 * gemHighlight);
                    byte b = (byte)(50 * gemHighlight);
                    data[y * size + x] = new Color(r, g, b, (byte)255);
                }
                else
                {
                    // Outer metal ring
                    byte r = (byte)(60 * metallic * edgeBrightness);
                    byte g = (byte)(45 * metallic * edgeBrightness);
                    byte b = (byte)(30 * metallic * edgeBrightness);
                    data[y * size + x] = new Color(r, g, b, (byte)255);
                }
            }
        }
        
        _centerHubTexture = new Texture2D(Core.GraphicsDevice, size, size);
        _centerHubTexture.SetData(data);
    }

    private void GenerateWheelTexture()
    {
        var data = new Color[TextureSize * TextureSize];
        var center = TextureSize / 2f;
        var radius = center - 10;
        float segmentAngle = MathF.PI * 2 / SlotCount;
        
        for (int y = 0; y < TextureSize; y++)
        {
            for (int x = 0; x < TextureSize; x++)
            {
                float dx = x - center;
                float dy = y - center;
                float dist = MathF.Sqrt(dx * dx + dy * dy);
                float angle = MathF.Atan2(dy, dx) + MathF.PI / 2; // Offset so 0 is at top
                if (angle < 0) angle += MathF.PI * 2;
                
                // Outside wheel
                if (dist > radius)
                {
                    data[y * TextureSize + x] = Color.Transparent;
                    continue;
                }
                
                // Skip center pixels (covered by hub, avoids angle calculation instability near origin)
                if (dist < 20)
                {
                    data[y * TextureSize + x] = Color.Transparent;
                    continue;
                }
                
                // Determine segment
                int segmentIndex = (int)(angle / segmentAngle) % SlotCount;
                var partType = _wheelSlots[segmentIndex];
                bool isBlank = partType == BodyPartType.Undefined;
                
                // Normalized distance from center (0-1)
                float normalizedDist = dist / radius;
                
                // Base color with gradient
                Color baseColor;
                if (isBlank)
                {
                    // Dark slate for blank slots with subtle blue tint
                    float darkness = 0.15f + normalizedDist * 0.1f;
                    baseColor = new Color(
                        (byte)(35 * darkness * 2.5f),
                        (byte)(38 * darkness * 2.5f),
                        (byte)(45 * darkness * 2.5f)
                    );
                }
                else
                {
                    // Rich crimson/blood red with depth
                    float redBase = 0.35f + normalizedDist * 0.15f;
                    // Alternate between two red tones for visual interest
                    bool altColor = segmentIndex % 2 == 0;
                    baseColor = altColor
                        ? new Color((byte)(180 * redBase), (byte)(35 * redBase), (byte)(35 * redBase))
                        : new Color((byte)(140 * redBase), (byte)(25 * redBase), (byte)(30 * redBase));
                }
                
                // Add radial gradient (darker toward center)
                float radialShading = 0.6f + normalizedDist * 0.4f;
                
                // Add noise/grunge
                float noise = PerlinNoise(x * 0.03f, y * 0.03f) * 0.15f;
                noise += PerlinNoise(x * 0.08f, y * 0.08f) * 0.1f;
                
                // Edge darkening within each segment
                float angleInSegment = (angle % segmentAngle) / segmentAngle;
                float edgeDarkening = 1f - MathF.Pow(MathF.Abs(angleInSegment - 0.5f) * 2f, 3f) * 0.2f;
                
                // Combine all effects
                float finalBrightness = radialShading * edgeDarkening * (1f + noise);
                
                byte r = (byte)Math.Clamp(baseColor.R * finalBrightness, 0, 255);
                byte g = (byte)Math.Clamp(baseColor.G * finalBrightness, 0, 255);
                byte b = (byte)Math.Clamp(baseColor.B * finalBrightness, 0, 255);
                
                data[y * TextureSize + x] = new Color(r, g, b, (byte)255);
            }
        }
        
        // Draw segment dividers
        for (int i = 0; i < SlotCount; i++)
        {
            float angle = i * segmentAngle - MathF.PI / 2;
            DrawLineOnTexture(data, TextureSize, center, center, 
                center + MathF.Cos(angle) * radius, 
                center + MathF.Sin(angle) * radius,
                new Color(15, 12, 12), 3);
        }
        
        _wheelTexture = new Texture2D(Core.GraphicsDevice, TextureSize, TextureSize);
        _wheelTexture.SetData(data);
    }

    private void DrawLineOnTexture(Color[] data, int size, float x1, float y1, float x2, float y2, Color color, int thickness)
    {
        float dx = x2 - x1;
        float dy = y2 - y1;
        float length = MathF.Sqrt(dx * dx + dy * dy);
        dx /= length;
        dy /= length;
        
        for (float t = 0; t < length; t += 0.5f)
        {
            float cx = x1 + dx * t;
            float cy = y1 + dy * t;
            
            for (int ox = -thickness; ox <= thickness; ox++)
            {
                for (int oy = -thickness; oy <= thickness; oy++)
                {
                    if (ox * ox + oy * oy <= thickness * thickness)
                    {
                        int px = (int)(cx + ox);
                        int py = (int)(cy + oy);
                        if (px >= 0 && px < size && py >= 0 && py < size)
                        {
                            data[py * size + px] = color;
                        }
                    }
                }
            }
        }
    }

    private float PerlinNoise(float x, float y)
    {
        // Simplified value noise
        int ix = (int)MathF.Floor(x);
        int iy = (int)MathF.Floor(y);
        float fx = x - ix;
        float fy = y - iy;
        
        float v00 = Hash(ix, iy);
        float v10 = Hash(ix + 1, iy);
        float v01 = Hash(ix, iy + 1);
        float v11 = Hash(ix + 1, iy + 1);
        
        // Smooth interpolation
        fx = fx * fx * (3 - 2 * fx);
        fy = fy * fy * (3 - 2 * fy);
        
        float v0 = v00 + (v10 - v00) * fx;
        float v1 = v01 + (v11 - v01) * fx;
        
        return v0 + (v1 - v0) * fy;
    }

    private float Hash(int x, int y)
    {
        int n = x + y * 57;
        n = (n << 13) ^ n;
        return (1.0f - ((n * (n * n * 15731 + 789221) + 1376312589) & 0x7fffffff) / 1073741824.0f);
    }

    public void StartSpin()
    {
        if (_isSpinning || _uniquePartTypes.Count == 0)
            return;

        _isSpinning = true;
        _spinElapsed = 0f;
        _spinStartRotation = _rotation;
        
        // Spin 3-5 full rotations plus a random partial rotation for variety
        float fullRotations = 3f + (float)Core.Random.NextDouble() * 2f;
        float totalRotation = fullRotations * MathHelper.TwoPi;
        _targetRotation = _spinStartRotation + totalRotation;
    }

    public void Update(float deltaTime)
    {
        _time += deltaTime;

        // Update spin physics using time-based easing
        if (_isSpinning)
        {
            _spinElapsed += deltaTime;
            float progress = Math.Clamp(_spinElapsed / SpinDuration, 0f, 1f);
            
            // Ease-out cubic: starts fast, slows down naturally at the end
            float easedProgress = 1f - MathF.Pow(1f - progress, 3f);
            
            // Directly interpolate rotation from start to target based on eased progress
            float totalRotation = _targetRotation - _spinStartRotation;
            _rotation = _spinStartRotation + totalRotation * easedProgress;
            
            // Calculate current velocity for visual effects (derivative of ease-out cubic)
            float velocityFactor = 3f * MathF.Pow(1f - progress, 2f);
            _spinVelocity = (totalRotation / SpinDuration) * velocityFactor;

            if (progress >= 1f)
            {
                _isSpinning = false;
                _spinVelocity = 0;
                _rotation = _targetRotation;
                DetermineSelectedSegment();
            }
        }
    }

    public override void InternalRender(RenderContext context)
    {
        base.InternalRender(context);
        
        GenerateTextures();

        var bounds = ActualBounds;
        var transform = context.Transform;
        var topLeft = transform.Apply(new Vector2(bounds.X, bounds.Y));
        var bottomRight = transform.Apply(new Vector2(bounds.X + bounds.Width, bounds.Y + bounds.Height));
        
        var screenWidth = bottomRight.X - topLeft.X;
        var screenHeight = bottomRight.Y - topLeft.Y;
        var centerX = topLeft.X + screenWidth / 2;
        var centerY = topLeft.Y + screenHeight / 2;
        var radius = Math.Min(screenWidth, screenHeight) / 2 - 30;

        context.Flush();

        // Use cached SpriteBatch to avoid allocations every frame
        _spriteBatch ??= new SpriteBatch(Core.GraphicsDevice);
        var spriteBatch = _spriteBatch;
        
        // First pass: Additive blend for glow effects
        spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, SamplerState.LinearClamp);
        
        // Outer glow
        if (_glowTexture != null)
        {
            float glowPulse = 0.7f + MathF.Sin(_time * 2f) * 0.15f;
            if (_isSpinning) glowPulse = 1f + _spinVelocity * 0.02f;
            
            var glowSize = radius * 2.4f * glowPulse;
            spriteBatch.Draw(_glowTexture,
                new Rectangle((int)(centerX - glowSize / 2), (int)(centerY - glowSize / 2), (int)glowSize, (int)glowSize),
                new Color(180, 80, 30, 60));
        }
        
        spriteBatch.End();
        
        // Second pass: Normal blend for main wheel
        spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp);
        
        // Draw main wheel texture (rotated)
        if (_wheelTexture != null)
        {
            var wheelSize = radius * 2;
            spriteBatch.Draw(_wheelTexture,
                new Vector2(centerX, centerY),
                null,
                Color.White,
                _rotation,
                new Vector2(TextureSize / 2f, TextureSize / 2f),
                wheelSize / TextureSize,
                SpriteEffects.None,
                0);
        }
        
        spriteBatch.End();
        
        // Third pass: Draw decorative elements
        spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);
        
        // Draw segment labels
        DrawSegmentLabels(spriteBatch, centerX, centerY, radius);
        
        // Draw decorative pegs/notches around the rim
        DrawRimNotches(spriteBatch, centerX, centerY, radius);
        
        // Draw outer rim
        DrawDecorativeRim(spriteBatch, centerX, centerY, radius);
        
        // Draw center hub
        if (_centerHubTexture != null)
        {
            float hubSize = radius * 0.25f;
            spriteBatch.Draw(_centerHubTexture,
                new Rectangle((int)(centerX - hubSize), (int)(centerY - hubSize), (int)(hubSize * 2), (int)(hubSize * 2)),
                Color.White);
        }
        
        // Draw pointer
        DrawPointer(spriteBatch, centerX, centerY - radius - 15);
        
        // Draw highlight on selected segment when spinning
        if (_isSpinning)
        {
            DrawSpinHighlight(spriteBatch, centerX, centerY, radius);
        }
        
        spriteBatch.End();
    }

    private void DrawSegmentLabels(SpriteBatch spriteBatch, float cx, float cy, float radius)
    {
        float segmentAngle = MathHelper.TwoPi / SlotCount;
        int currentPointedSegment = GetCurrentPointedSegment();
        
        for (int i = 0; i < SlotCount; i++)
        {
            float startAngle = _rotation + i * segmentAngle - MathHelper.PiOver2;
            float midAngle = startAngle + segmentAngle / 2;
            float labelRadius = radius * 0.65f;
            
            var labelX = cx + (float)Math.Cos(midAngle) * labelRadius;
            var labelY = cy + (float)Math.Sin(midAngle) * labelRadius;

            var font = BaseContent.Fonts.Default.VerySmall;
            bool isBlank = _wheelSlots[i] == BodyPartType.Undefined;
            var partName = isBlank ? "---" : _wheelSlots[i].ToString();
            var textSize = font.MeasureString(partName);
            
            bool isPointed = i == currentPointedSegment;
            Color textColor;
            if (isPointed)
            {
                // Glowing gold when pointed
                float pulse = 0.8f + MathF.Sin(_time * 8f) * 0.2f;
                textColor = isBlank 
                    ? new Color((byte)(150 * pulse), (byte)(150 * pulse), (byte)(160 * pulse))
                    : new Color((byte)(255 * pulse), (byte)(220 * pulse), (byte)(150 * pulse));
            }
            else
            {
                textColor = isBlank ? new Color(90, 90, 100) : new Color(200, 180, 160);
            }
            
            // Draw text with shadow
            font.DrawText(spriteBatch, partName,
                new Vector2(labelX - textSize.X / 2 + 1, labelY - textSize.Y / 2 + 1),
                new Color(0, 0, 0, 220));
            font.DrawText(spriteBatch, partName,
                new Vector2(labelX - textSize.X / 2, labelY - textSize.Y / 2),
                textColor);
        }
    }

    private void DrawRimNotches(SpriteBatch spriteBatch, float cx, float cy, float radius)
    {
        var pixel = GetPixelTexture();
        const int notchCount = 32;
        float notchRadius = radius + 8;
        
        for (int i = 0; i < notchCount; i++)
        {
            float angle = i * MathHelper.TwoPi / notchCount;
            float notchX = cx + MathF.Cos(angle) * notchRadius;
            float notchY = cy + MathF.Sin(angle) * notchRadius;
            
            // Alternating gold/bronze colors
            var color = i % 2 == 0 
                ? new Color(160, 120, 50) 
                : new Color(120, 90, 40);
            
            // Draw small circles as notches
            DrawFilledCirclePrimitive(spriteBatch, pixel, notchX, notchY, 5, color);
            DrawFilledCirclePrimitive(spriteBatch, pixel, notchX - 1, notchY - 1, 3, new Color(200, 160, 80));
        }
    }

    private void DrawDecorativeRim(SpriteBatch spriteBatch, float cx, float cy, float radius)
    {
        var pixel = GetPixelTexture();
        
        // Multiple concentric rings for depth
        DrawCircleOutline(spriteBatch, pixel, cx, cy, radius + 14, new Color(50, 40, 25), 4);
        DrawCircleOutline(spriteBatch, pixel, cx, cy, radius + 10, new Color(160, 120, 50), 6);
        DrawCircleOutline(spriteBatch, pixel, cx, cy, radius + 5, new Color(100, 75, 35), 3);
        DrawCircleOutline(spriteBatch, pixel, cx, cy, radius + 2, new Color(40, 30, 20), 2);
        
        // Inner decorative ring
        DrawCircleOutline(spriteBatch, pixel, cx, cy, radius * 0.18f, new Color(120, 90, 45), 3);
    }

    private void DrawSpinHighlight(SpriteBatch spriteBatch, float cx, float cy, float radius)
    {
        // Motion blur effect - draw faint trailing segments
        var pixel = GetPixelTexture();
        float intensity = Math.Min(_spinVelocity / InitialSpinSpeed, 1f);
        
        for (int i = 0; i < 8; i++)
        {
            float trailAngle = -MathHelper.PiOver2 - (i * 0.05f * _spinVelocity);
            float trailX = cx + MathF.Cos(trailAngle) * (radius * 0.9f);
            float trailY = cy + MathF.Sin(trailAngle) * (radius * 0.9f);
            
            byte alpha = (byte)(40 * intensity * (1f - i / 8f));
            DrawFilledCirclePrimitive(spriteBatch, pixel, trailX, trailY, 4, new Color((byte)255, (byte)200, (byte)100, alpha));
        }
    }

    private int GetCurrentPointedSegment()
    {
        float segmentAngle = MathHelper.TwoPi / SlotCount;
        float index = -_rotation / segmentAngle;
        int segmentIndex = ((int)Math.Floor(index) % SlotCount + SlotCount) % SlotCount;
        return segmentIndex;
    }

    private void DrawPointer(SpriteBatch spriteBatch, float cx, float cy)
    {
        var pixel = GetPixelTexture();
        const float pointerWidth = 24;
        const float pointerHeight = 40;
        
        var p1 = new Vector2(cx, cy + pointerHeight);
        var p2 = new Vector2(cx - pointerWidth, cy);
        var p3 = new Vector2(cx + pointerWidth, cy);

        // Shadow
        DrawTriangle(spriteBatch, pixel, 
            p1 + new Vector2(4, 4), 
            p2 + new Vector2(4, 4), 
            p3 + new Vector2(4, 4), 
            new Color(0, 0, 0, 120));
        
        // Main pointer - rich gold gradient effect
        DrawTriangle(spriteBatch, pixel, p1, p2, p3, new Color(180, 140, 50));
        
        // Inner highlight triangle
        var innerScale = 0.7f;
        var innerOffset = new Vector2(0, pointerHeight * 0.15f);
        DrawTriangle(spriteBatch, pixel,
            new Vector2(cx, cy + pointerHeight * innerScale) + innerOffset,
            new Vector2(cx - pointerWidth * innerScale, cy + pointerHeight * 0.2f) + innerOffset,
            new Vector2(cx + pointerWidth * innerScale, cy + pointerHeight * 0.2f) + innerOffset,
            new Color(220, 180, 80));
        
        // Outline
        DrawLine(spriteBatch, pixel, p1, p2, new Color(100, 70, 25), 3);
        DrawLine(spriteBatch, pixel, p2, p3, new Color(100, 70, 25), 3);
        DrawLine(spriteBatch, pixel, p3, p1, new Color(100, 70, 25), 3);
        
        // Top edge highlight
        DrawLine(spriteBatch, pixel, p2, p3, new Color(240, 200, 120), 2);
        
        // Pulsing glow at tip when pointing at part slot
        int pointed = GetCurrentPointedSegment();
        if (_wheelSlots[pointed] != BodyPartType.Undefined)
        {
            float pulse = 0.5f + MathF.Sin(_time * 6f) * 0.5f;
            DrawFilledCirclePrimitive(spriteBatch, pixel, cx, cy + pointerHeight, 6, 
                new Color((byte)(255 * pulse), (byte)(200 * pulse), (byte)(100 * pulse), (byte)(150 * pulse)));
        }
    }

    private void DrawFilledCirclePrimitive(SpriteBatch spriteBatch, Texture2D pixel, float cx, float cy, float radius, Color color)
    {
        for (float y = cy - radius; y <= cy + radius; y++)
        {
            float dy = y - cy;
            float halfWidth = MathF.Sqrt(radius * radius - dy * dy);
            spriteBatch.Draw(pixel, 
                new Rectangle((int)(cx - halfWidth), (int)y, (int)(halfWidth * 2), 1), 
                color);
        }
    }

    private void DrawCircleOutline(SpriteBatch spriteBatch, Texture2D pixel, float cx, float cy, float radius, Color color, int thickness)
    {
        const int segments = 64;
        float angleStep = MathHelper.TwoPi / segments;

        for (int i = 0; i < segments; i++)
        {
            float a1 = i * angleStep;
            float a2 = (i + 1) * angleStep;

            var p1 = new Vector2(cx + MathF.Cos(a1) * radius, cy + MathF.Sin(a1) * radius);
            var p2 = new Vector2(cx + MathF.Cos(a2) * radius, cy + MathF.Sin(a2) * radius);

            DrawLine(spriteBatch, pixel, p1, p2, color, thickness);
        }
    }

    private void DrawTriangle(SpriteBatch spriteBatch, Texture2D pixel, Vector2 p1, Vector2 p2, Vector2 p3, Color color)
    {
        if (p1.Y > p2.Y) (p1, p2) = (p2, p1);
        if (p1.Y > p3.Y) (p1, p3) = (p3, p1);
        if (p2.Y > p3.Y) (p2, p3) = (p3, p2);

        for (float y = p1.Y; y <= p3.Y; y++)
        {
            float x1, x2;
            
            if (y < p2.Y)
            {
                x1 = Lerp(p1.X, p2.X, (y - p1.Y) / Math.Max(0.001f, p2.Y - p1.Y));
                x2 = Lerp(p1.X, p3.X, (y - p1.Y) / Math.Max(0.001f, p3.Y - p1.Y));
            }
            else
            {
                x1 = Lerp(p2.X, p3.X, (y - p2.Y) / Math.Max(0.001f, p3.Y - p2.Y));
                x2 = Lerp(p1.X, p3.X, (y - p1.Y) / Math.Max(0.001f, p3.Y - p1.Y));
            }

            if (x1 > x2) (x1, x2) = (x2, x1);
            
            spriteBatch.Draw(pixel, new Rectangle((int)x1, (int)y, (int)(x2 - x1) + 1, 1), color);
        }
    }

    private static float Lerp(float a, float b, float t) => a + (b - a) * Math.Clamp(t, 0, 1);

    private void DrawLine(SpriteBatch spriteBatch, Texture2D pixel, Vector2 start, Vector2 end, Color color, int thickness)
    {
        var direction = end - start;
        var length = direction.Length();
        if (length < 0.5f) return;
        
        var angle = MathF.Atan2(direction.Y, direction.X);

        // Position at start point, rotate around left-center of rectangle
        spriteBatch.Draw(pixel,
            new Rectangle((int)start.X, (int)start.Y, (int)length, thickness),
            null, color, angle, new Vector2(0, thickness / 2f), SpriteEffects.None, 0);
    }

    private Texture2D? _pixelTexture;
    
    private Texture2D GetPixelTexture()
    {
        if (_pixelTexture == null)
        {
            _pixelTexture = new Texture2D(Core.GraphicsDevice, 1, 1);
            _pixelTexture.SetData(new[] { Color.White });
        }
        return _pixelTexture;
    }

    private void DetermineSelectedSegment()
    {
        var selectedIndex = GetCurrentPointedSegment();
        OnSpinComplete?.Invoke(_wheelSlots[selectedIndex]);
    }
}
