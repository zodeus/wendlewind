namespace Grafted.Scenes.MainGameScene.Gui.Widgets.PawnRenderer;

/// <summary>
/// Internal widget that handles the actual body rendering.
/// </summary>
internal class PawnRenderArea(PawnRenderer renderer, Texture2D? fallbackIcon) : Widget
{
    private readonly BodyPartDamageTextRenderer _damageTextRenderer = new(renderer.Layout, renderer.NativeSize);

    public PawnRenderer Renderer => renderer;
    public bool HasValidLayout => renderer.HasValidLayout;
    public BodyPartDamageTextRenderer DamageTextRenderer => _damageTextRenderer;

    public override void InternalRender(RenderContext context)
    {
        base.InternalRender(context);

        var bounds = ActualBounds;
        var destRect = new Rectangle(bounds.X, bounds.Y, bounds.Width, bounds.Height);

        if (renderer.HasValidLayout)
        {
            // Note: Render() should be called before Myra's render pass via PreRender()
            // to avoid render target switching during UI rendering which causes flickering.
            // Here we just draw the already-rendered texture.
            var texture = renderer.RenderedTexture;
            if (texture != null)
            {
                context.Draw(texture, destRect, Color.White);
            }

            // Render damage text overlay with clipping to keep text inside bounds
            var layoutScale = (float)bounds.Width / renderer.NativeSize;
            _damageTextRenderer.Render(context, bounds, layoutScale);
        }
        else if (fallbackIcon != null)
        {
            context.Draw(fallbackIcon, destRect, Color.White);
        }
    }
}