using Wendlemire.PawnLayout;
using Num = System.Numerics;

namespace Wendlemire.Editor;

/// <summary>
/// Composites a pawn's external parts (and equipped weapons/armor) into a
/// render target that ImGui can display via <see cref="ImGui.Image"/>.
/// </summary>
public sealed class EditorPawnRenderer : IDisposable
{
    private readonly GraphicsDevice _graphicsDevice;
    private readonly ImGuiRenderer _imGui;
    private readonly SpriteBatch _spriteBatch;
    private RenderTarget2D? _target;
    private IntPtr _textureId;
    private int _size;

    public EditorPawnRenderer(GraphicsDevice graphicsDevice, ImGuiRenderer imGui, int size = 512)
    {
        _graphicsDevice = graphicsDevice;
        _imGui = imGui;
        _spriteBatch = new SpriteBatch(graphicsDevice);
        EnsureTarget(size);
    }

    public IntPtr TextureId => _textureId;

    public int Size => _size;

    public void Render(Pawn? pawn, IBodyPartLayout? layout, IReadOnlyDictionary<string, BodyPartLayoutData>? overrides = null)
    {
        var previous = _graphicsDevice.GetRenderTargets();
        _graphicsDevice.SetRenderTarget(_target);
        _graphicsDevice.Clear(Color.Transparent);

        if (pawn != null && layout != null)
        {
            var layoutScale = (float)_size / Math.Max(layout.NativeSize, 1);
            var renderList = CollectParts(pawn, layout, overrides);
            renderList.Sort((a, b) => a.Info.RenderOrder.CompareTo(b.Info.RenderOrder));

            _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp);
            foreach (var (part, info) in renderList)
            {
                BodyPartRenderHelper.RenderEquippedWeapons(_spriteBatch, part, info, layoutScale: layoutScale);
                BodyPartRenderHelper.RenderBodyPart(_spriteBatch, info, layoutScale: layoutScale, tint: PawnPartTint.Get(part));
                BodyPartRenderHelper.RenderEquippedArmor(_spriteBatch, part, info, layoutScale: layoutScale);
            }

            _spriteBatch.End();
        }

        _graphicsDevice.SetRenderTargets(previous);
    }

    public bool DrawImage(float displaySize, int nativeSize, out Microsoft.Xna.Framework.Vector2 nativeMouse)
    {
        var size = new Num.Vector2(displaySize, displaySize);
        ImGui.Image(_textureId, size);
        var hovered = ImGui.IsItemHovered();
        var min = ImGui.GetItemRectMin();
        var mouse = ImGui.GetMousePos() - min;
        var scale = nativeSize / displaySize;
        nativeMouse = new Microsoft.Xna.Framework.Vector2(mouse.X * scale, mouse.Y * scale);
        return hovered;
    }

    private static List<(BodyPart Part, BodyPartRenderInfo Info)> CollectParts(
        Pawn pawn,
        IBodyPartLayout layout,
        IReadOnlyDictionary<string, BodyPartLayoutData>? overrides)
    {
        var renderList = new List<(BodyPart Part, BodyPartRenderInfo Info)>();
        foreach (var part in OverlayBodyPartLayout.VisibleParts(pawn.Body))
        {

            BodyPartRenderInfo? info;
            if (overrides != null && overrides.TryGetValue(part.InternalLabel, out var overridden))
            {
                info = new BodyPartRenderInfo(PawnTextures.GetIcon(part), overridden);
            }
            else
            {
                info = layout.GetRenderInfo(part);
            }

            if (info == null)
            {
                continue;
            }

            renderList.Add((part, info.Value));
        }

        return renderList;
    }

    private void EnsureTarget(int size)
    {
        if (_target != null && _size == size)
        {
            return;
        }

        if (_textureId != IntPtr.Zero)
        {
            _imGui.UnbindTexture(_textureId);
            _textureId = IntPtr.Zero;
        }

        _target?.Dispose();
        _size = size;
        _target = new RenderTarget2D(
            _graphicsDevice,
            size,
            size,
            false,
            SurfaceFormat.Color,
            DepthFormat.None,
            0,
            RenderTargetUsage.PreserveContents);
        _textureId = _imGui.BindTexture(_target);
    }

    public void Dispose()
    {
        if (_textureId != IntPtr.Zero)
        {
            _imGui.UnbindTexture(_textureId);
            _textureId = IntPtr.Zero;
        }

        _target?.Dispose();
        _spriteBatch.Dispose();
    }
}
