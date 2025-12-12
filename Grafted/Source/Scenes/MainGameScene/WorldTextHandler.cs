namespace Grafted.Scenes.MainGameScene;

public class WorldTextHandler
{
    private List<WorldSpaceText> _texts = new();

    public void Tick()
    {
        for (var index = _texts.Count - 1; index >= 0; index--)
        {
            var worldSpaceText = _texts[index];
            worldSpaceText.TicksLeft--;
            if (worldSpaceText.TicksLeft <= 0)
            {
                _texts.RemoveAt(index);
                continue;
            }

            worldSpaceText.Tick();
        }
    }

    public void Render(SpriteBatch spriteBatch, float deltaTime)
    {
        spriteBatch.Begin(SpriteSortMode.Deferred, null, null, null, null, null, null);
        foreach (var text in _texts)
        {
            text.Render(spriteBatch);
        }

        spriteBatch.End();
    }

    public void Add(WorldSpaceText text)
    {
        _texts.Add(text);
    }

    public void Clear()
    {
        _texts.Clear();
    }
}

public class WorldSpaceText
{
    public DynamicSpriteFont Font = BaseContent.Fonts.Default.Large;
    public string Text = null!;
    public Vector2 Position;
    public Color Color = Color.White;
    private int _durationInTicks;
    public float Speed = -3;

    public int DurationInTicks
    {
        get => _durationInTicks;
        set
        {
            _durationInTicks = value;
            TicksLeft = value;
        }
    }

    public int TicksLeft { get; set; }
    public Action<WorldSpaceText>? TickAction { get; set; } = DefaultTickAction;
    public Action<SpriteBatch, WorldSpaceText>? RenderAction { get; set; } = PlainTextRenderer;

    public void Tick()
    {
        TickAction?.Invoke(this);
    }

    public void Render(SpriteBatch spriteBatch)
    {
        RenderAction?.Invoke(spriteBatch, this);
    }

    public static void DefaultTickAction(WorldSpaceText text)
    {
        text.Position += new Vector2(0, text.Speed);
    }

    public static void PlainTextRenderer(SpriteBatch spriteBatch, WorldSpaceText text)
    {
        var transparency = (float)text.TicksLeft / text.DurationInTicks;
        spriteBatch.DrawString(text.Font, text.Text, text.Position, text.Color * transparency);
    }

    // public static void DefaultRenderAction(SpriteBatch spriteBatch, WorldSpaceText text)
    // {
    //     var transparency = (float)text.TicksLeft / text.DurationInTicks;
    //     spriteBatch.DrawString(text.Font, text.Text, text.Position, transparency);
    // }

    public static void VibratingRenderAction(SpriteBatch spriteBatch, WorldSpaceText text)
    {
        var transparency = (float)text.TicksLeft / text.DurationInTicks;
        var xOffsetA = Core.Random.Next(-1, 1);
        var yOffsetA = Core.Random.Next(-1, 1);
        var xOffsetB = Core.Random.Next(-3, 3);
        var yOffsetB = Core.Random.Next(-3, 3);
        spriteBatch.DrawString(text.Font, text.Text, new Vector2(text.Position.X - xOffsetB, text.Position.Y + yOffsetB), text.Color * transparency);
        spriteBatch.DrawString(text.Font, text.Text, new Vector2(text.Position.X + xOffsetA, text.Position.Y + yOffsetA), text.Color * transparency);
    }
}