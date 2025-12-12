namespace Grafted.Scenes.MainGameScene.Gui.Widgets.PawnRenderer;

/// <summary>
/// Internal widget that handles the actual body rendering.
/// </summary>
internal class PawnRenderArea : Widget
{
    private readonly PawnRenderer _renderer;
    private readonly Texture2D? _fallbackIcon;
    private readonly BodyPartDamageTextRenderer _damageTextRenderer;

    public PawnRenderer Renderer => _renderer;
    public bool HasValidLayout => _renderer.HasValidLayout;
    public BodyPartDamageTextRenderer DamageTextRenderer => _damageTextRenderer;

    public PawnRenderArea(PawnRenderer renderer, Texture2D? fallbackIcon)
    {
        _renderer = renderer;
        _fallbackIcon = fallbackIcon;
        _damageTextRenderer = new BodyPartDamageTextRenderer(renderer.Layout, renderer.NativeSize);
    }

    public override void InternalRender(RenderContext context)
    {
        base.InternalRender(context);

        var bounds = ActualBounds;
        var destRect = new Rectangle(bounds.X, bounds.Y, bounds.Width, bounds.Height);

        if (_renderer.HasValidLayout)
        {
            // Note: Render() should be called before Myra's render pass via PreRender()
            // to avoid render target switching during UI rendering which causes flickering.
            // Here we just draw the already-rendered texture.
            var texture = _renderer.RenderedTexture;
            if (texture != null)
            {
                context.Draw(texture, destRect, Color.White);
            }

            // Render damage text overlay
            var layoutScale = (float)bounds.Width / _renderer.NativeSize;
            _damageTextRenderer.Render(context, bounds, layoutScale);
        }
        else if (_fallbackIcon != null)
        {
            context.Draw(_fallbackIcon, destRect, Color.White);
        }
    }
}