namespace Wendlemire.Scenes.MainGameScene.Gui.Widgets.PawnRenderer;

/// <summary>
/// Represents a single blood droplet particle.
/// </summary>
public class BloodDroplet
{
    public Vector2 Position;
    public Vector2 Velocity;
    public float Size;
    public float Lifetime;
    public float MaxLifetime;
    public float Opacity;
    
    public bool IsExpired => Lifetime <= 0;
    
    public void Update(float deltaTime)
    {
        // Apply gravity
        Velocity.Y += 400f * deltaTime;
        
        // Apply some drag to X velocity
        Velocity.X *= 0.98f;
        
        // Update position
        Position += Velocity * deltaTime;
        
        // Update lifetime
        Lifetime -= deltaTime;
        
        // Fade out in the last 30% of lifetime
        var fadeThreshold = MaxLifetime * 0.3f;
        if (Lifetime < fadeThreshold)
        {
            Opacity = Lifetime / fadeThreshold;
        }
    }
}

/// <summary>
/// Represents a blood spurt emitter at an open socket location.
/// </summary>
public class BloodSpurtEmitter
{
    public Vector2 Position;
    public Vector2 Direction;
    public Color BloodColor;
    public float SpawnTimer;
    public float SpawnInterval;
    public bool IsActive;
    
    private static readonly Random _random = new();
    
    public BloodSpurtEmitter(Vector2 position, Vector2 direction, Color bloodColor)
    {
        Position = position;
        Direction = direction;
        BloodColor = bloodColor;
        SpawnTimer = 0;
        SpawnInterval = 0.02f; // Spawn particles every 20ms
        IsActive = true;
    }
    
    public BloodDroplet? TrySpawnDroplet(float deltaTime)
    {
        if (!IsActive) return null;
        
        SpawnTimer += deltaTime;
        
        if (SpawnTimer >= SpawnInterval)
        {
            SpawnTimer -= SpawnInterval;
            
            // Random spread around the direction
            var angle = (float)(Math.Atan2(Direction.Y, Direction.X) + (_random.NextDouble() - 0.5) * 1.2);
            var speed = 80f + (float)_random.NextDouble() * 120f;
            
            return new BloodDroplet
            {
                Position = Position + new Vector2(
                    (float)(_random.NextDouble() - 0.5) * 6f,
                    (float)(_random.NextDouble() - 0.5) * 6f
                ),
                Velocity = new Vector2(
                    (float)Math.Cos(angle) * speed,
                    (float)Math.Sin(angle) * speed - 50f // Initial upward bias for spurt effect
                ),
                Size = 2f + (float)_random.NextDouble() * 3f,
                Lifetime = 0.4f + (float)_random.NextDouble() * 0.3f,
                MaxLifetime = 0.7f,
                Opacity = 1f
            };
        }
        
        return null;
    }
}

/// <summary>
/// Renders blood spurts from open, unsealed sockets on a pawn's body.
/// </summary>
public class BloodSpurtRenderer
{
    private readonly List<BloodDroplet> _droplets = new();
    private readonly Dictionary<int, BloodSpurtEmitter> _emittersBySocketId = new();
    private readonly IBodyPartLayout? _layout;
    private readonly Pawn _pawn;
    private float _elapsedTime;
    
    public BloodSpurtRenderer(Pawn pawn, IBodyPartLayout? layout)
    {
        _pawn = pawn;
        _layout = layout;
    }
    
    /// <summary>
    /// Updates emitters based on current socket states and advances particle simulation.
    /// </summary>
    public void Update(float deltaTime)
    {
        if (_layout == null) return;
        
        _elapsedTime += deltaTime;
        
        // Update emitters based on open sockets
        UpdateEmitters();
        
        // Spawn new droplets from active emitters
        foreach (var emitter in _emittersBySocketId.Values)
        {
            var droplet = emitter.TrySpawnDroplet(deltaTime);
            if (droplet != null)
            {
                _droplets.Add(droplet);
            }
        }
        
        // Update existing droplets
        for (var i = _droplets.Count - 1; i >= 0; i--)
        {
            _droplets[i].Update(deltaTime);
            if (_droplets[i].IsExpired)
            {
                _droplets.RemoveAt(i);
            }
        }
    }
    
    /// <summary>
    /// Updates emitters to match the current state of open sockets.
    /// Preserves existing emitters to maintain spawn timer state.
    /// </summary>
    private void UpdateEmitters()
    {
        var bloodColor = _pawn.PawnDef.Body.BloodType?.Color ?? Color.DarkRed;
        var activeSocketIds = new HashSet<int>();
        
        // Check all external body parts for open sockets
        foreach (var part in _pawn.Body.AllExternalParts)
        {
            if (part.IsSevered) continue;
            
            foreach (var socket in part.Sockets)
            {
                // Only spurt blood from sockets that are open and not sealed
                // Skip sockets that still have a part attached
                if (socket.AttachedPart != null) continue;
                // Skip sealed sockets (cauterized, bandaged, etc.)
                if (socket.IsSealed) continue;
                // Skip internal organ sockets (they don't spurt blood outward)
                if (!socket.IsExternal) continue;
                // Skip minion sockets (they don't bleed)
                if (socket.Def.AllowedBodyPartTypes.Contains(BodyPartType.Minion)) continue;
                
                activeSocketIds.Add(socket.Id);
                
                // Create emitter if it doesn't exist
                if (!_emittersBySocketId.ContainsKey(socket.Id))
                {
                    var position = GetSocketPosition(part, socket);
                    if (position.HasValue)
                    {
                        var direction = GetSpurtDirection(socket);
                        _emittersBySocketId[socket.Id] = new BloodSpurtEmitter(position.Value, direction, bloodColor);
                    }
                }
            }
        }
        
        // Remove emitters for sockets that are no longer open
        var toRemove = _emittersBySocketId.Keys.Where(id => !activeSocketIds.Contains(id)).ToList();
        foreach (var id in toRemove)
        {
            _emittersBySocketId.Remove(id);
        }
    }
    
    /// <summary>
    /// Gets the position where blood should spurt from for a given socket.
    /// This looks up where the severed part WOULD be rendered, not the parent part.
    /// </summary>
    private Vector2? GetSocketPosition(BodyPart parentPart, BodyPartSocket socket)
    {
        if (_layout == null) return null;
        
        // Try to find the position where the missing part would be rendered
        // by constructing what its label would have been
        var missingPartLabel = GetMissingPartLabel(parentPart, socket);
        if (missingPartLabel != null)
        {
            // Try to get render info for the missing part by creating a dummy lookup
            var position = TryGetLayoutPosition(missingPartLabel);
            if (position.HasValue)
            {
                return position.Value;
            }
        }
        
        // Fallback: use parent part position with offset
        var renderInfo = _layout.GetRenderInfo(parentPart);
        if (!renderInfo.HasValue) return null;
        
        var basePosition = renderInfo.Value.Position;
        var texture = renderInfo.Value.Texture;
        var scale = renderInfo.Value.Scale;
        
        var partCenter = basePosition + new Vector2(
            texture.Width * scale / 2f,
            texture.Height * scale / 2f
        );
        
        var offset = GetSocketOffset(socket.Position, texture.Width * scale, texture.Height * scale);
        return partCenter + offset;
    }
    
    /// <summary>
    /// Constructs what the label of the missing body part would have been.
    /// </summary>
    private string? GetMissingPartLabel(BodyPart parentPart, BodyPartSocket socket)
    {
        if (socket.Def.AllowedBodyPartTypes.Count == 0) return null;
        
        // Get the body part type name (e.g., "Hand", "Foot")
        var partType = socket.Def.AllowedBodyPartTypes[0];
        var partTypeName = partType.ToString();
        
        // Get the position - socket's position, or inherit from parent
        var position = socket.Position;
        
        // Construct the label like BodyPart.GenerateLabel() does
        var label = "";
        if (position != null)
        {
            // Split camel case (e.g., "FrontLeft" -> "Front Left")
            label = string.Join(" ", System.Text.RegularExpressions.Regex.Split(
                position.ToString()!, @"(?<!^)(?=[A-Z])")) + " ";
        }
        label += partTypeName;
        
        return label;
    }
    
    /// <summary>
    /// Tries to get the center position for a part label from the layout.
    /// </summary>
    private Vector2? TryGetLayoutPosition(string partLabel)
    {
        // We need to check if this label exists in the layout
        // Since we can't directly query the layout by label, we use a workaround:
        // Check against known humanoid part names
        var knownPositions = GetKnownPartPositions();
        if (knownPositions.TryGetValue(partLabel, out var position))
        {
            return position;
        }
        return null;
    }
    
    /// <summary>
    /// Gets known body part positions from the layout.
    /// This is a workaround since we can't query the layout directly by label.
    /// </summary>
    private Dictionary<string, Vector2> GetKnownPartPositions()
    {
        // These positions are taken from the layout definitions
        // and represent the CENTER of where the part would be rendered
        return _layout switch
        {
            HumanBodyPartLayout => new Dictionary<string, Vector2>
            {
                { "Right Hand", new Vector2(150f, 310f) },
                { "Left Hand", new Vector2(320f, 315f) },
                { "Right Foot", new Vector2(155f, 450f) },
                { "Left Foot", new Vector2(300f, 455f) },
                { "Head", new Vector2(230f, 115f) },
                { "Right Arm", new Vector2(150f, 200f) },
                { "Left Arm", new Vector2(280f, 205f) },
                { "Right Leg", new Vector2(155f, 340f) },
                { "Left Leg", new Vector2(240f, 345f) },
            },
            _ => new Dictionary<string, Vector2>()
        };
    }
    
    /// <summary>
    /// Gets the offset from part center based on socket position (fallback).
    /// </summary>
    private Vector2 GetSocketOffset(BodyPartPosition? position, float partWidth, float partHeight)
    {
        return position switch
        {
            BodyPartPosition.Left => new Vector2(-partWidth * 0.4f, 0),
            BodyPartPosition.Right => new Vector2(partWidth * 0.4f, 0),
            BodyPartPosition.FrontLeft => new Vector2(-partWidth * 0.3f, -partHeight * 0.2f),
            BodyPartPosition.FrontRight => new Vector2(partWidth * 0.3f, -partHeight * 0.2f),
            BodyPartPosition.RearLeft => new Vector2(-partWidth * 0.3f, partHeight * 0.3f),
            BodyPartPosition.RearRight => new Vector2(partWidth * 0.3f, partHeight * 0.3f),
            BodyPartPosition.MiddleLeft => new Vector2(-partWidth * 0.35f, 0),
            BodyPartPosition.MiddleRight => new Vector2(partWidth * 0.35f, 0),
            _ => new Vector2(0, partHeight * 0.4f) // Default: bottom of part
        };
    }
    
    /// <summary>
    /// Gets the direction blood should spurt based on socket position.
    /// </summary>
    private Vector2 GetSpurtDirection(BodyPartSocket socket)
    {
        return socket.Position switch
        {
            BodyPartPosition.Left => new Vector2(-1f, -0.3f),
            BodyPartPosition.Right => new Vector2(1f, -0.3f),
            BodyPartPosition.FrontLeft => new Vector2(-0.7f, -0.7f),
            BodyPartPosition.FrontRight => new Vector2(0.7f, -0.7f),
            BodyPartPosition.RearLeft => new Vector2(-0.7f, 0.5f),
            BodyPartPosition.RearRight => new Vector2(0.7f, 0.5f),
            BodyPartPosition.MiddleLeft => new Vector2(-1f, -0.2f),
            BodyPartPosition.MiddleRight => new Vector2(1f, -0.2f),
            _ => new Vector2(0, 1f) // Default: downward
        };
    }
    
    /// <summary>
    /// Renders all blood droplets to the sprite batch.
    /// </summary>
    public void Render(SpriteBatch spriteBatch, float layoutScale)
    {
        if (_droplets.Count == 0) return;
        
        var pixel = Core.Graphics.PixelTexture;
        var bloodColor = _pawn.PawnDef.Body.BloodType?.Color ?? Color.DarkRed;
        
        foreach (var droplet in _droplets)
        {
            var color = bloodColor * droplet.Opacity;
            var scaledPosition = droplet.Position * layoutScale;
            var size = (int)(droplet.Size * layoutScale);
            
            // Draw droplet as a small rectangle (simple but effective)
            var rect = new Rectangle(
                (int)(scaledPosition.X - size / 2f),
                (int)(scaledPosition.Y - size / 2f),
                Math.Max(1, size),
                Math.Max(1, size)
            );
            
            spriteBatch.Draw(pixel, rect, pixel.SourceRect, color);
            
            // Add a slightly larger, more transparent droplet for a glow effect
            var glowSize = size + 2;
            var glowRect = new Rectangle(
                (int)(scaledPosition.X - glowSize / 2f),
                (int)(scaledPosition.Y - glowSize / 2f),
                Math.Max(1, glowSize),
                Math.Max(1, glowSize)
            );
            spriteBatch.Draw(pixel, glowRect, pixel.SourceRect, color * 0.3f);
        }
    }
    
    /// <summary>
    /// Checks if there are any active blood spurts.
    /// </summary>
    public bool HasActiveSpurts => _emittersBySocketId.Count > 0 || _droplets.Count > 0;
}
