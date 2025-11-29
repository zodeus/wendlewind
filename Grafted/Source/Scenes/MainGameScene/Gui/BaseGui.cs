using FontStashSharp;
using Grafted.Scenes.MainGameScene.Gui.Widgets;
using Grafted.Scenes.MainGameScene.Gui.Widgets.EntityWidgets;
using Grafted.Scenes.MainGameScene.Gui.Widgets.MiscWidgets.Boak;
using Grafted.Sim.Entities;

namespace Grafted.Scenes.MainGameScene.Gui;

public abstract class BaseGui : IDisposable
{
    private BookOfAllKnowledgeWindow? Boak;
    public Desktop Desktop { get; set; } = null!;
    public abstract WorldTextHandler WorldTextHandler { get; }

    public MouseAttachment? MouseAttachment;

    private ScreenMessageData? _screenMessage;
    private float _screenMessageTimeLeft;
    private readonly Window _entityViewerWindow = new();
    private Entity? _viewedEntity;
    private KeyValuePair<Entity, Point?>? _queuedEntityToView;
    private bool _deathWindowIsOpen;

    public virtual void Update(float deltaTime)
    {
        HandleInput();
        MouseAttachment?.Update();
        ShowEntityIfQueued();
        if (_viewedEntity?.IsDestroyed == true)
        {
            _viewedEntity = null;
            _entityViewerWindow.Close();
        }

        ((EntityPanelBase?)_entityViewerWindow.Content)?.Update();
        if (_screenMessageTimeLeft > 0)
        {
            _screenMessageTimeLeft -= deltaTime;
        }

        if (Core.Context.World.PlayerPawn.IsDead && _deathWindowIsOpen == false)
        {
            ShowDeathWindow();
        }
    }

    protected void ShowDeathWindow()
    {
        _deathWindowIsOpen = true;
        DeathWindow window = new();
        window.Closed += (_, _) => _deathWindowIsOpen = false;
        window.ShowModal(Desktop);
    }

    public virtual void ViewEntity(Entity entity, Point? position = null)
    {
        _queuedEntityToView = new KeyValuePair<Entity, Point?>(entity, position);
    }

    public virtual void HandleInput()
    {
        if (Keyboard.GetState().IsKeyDown(Keys.B))
        {
            OpenBoak();
        }
    }

    public void OpenBoak()
    {
        if (Boak != null)
        {
            Boak.Close();
        }

        Boak = new BookOfAllKnowledgeWindow();
        Boak.Show(Desktop, new Point(0, 0));
    }

    public virtual void Draw(SpriteBatch spriteBatch, float deltaTime)
    {
        Desktop.Render();
        spriteBatch.Begin(
            SpriteSortMode.Deferred,
            BlendState.NonPremultiplied,
            SamplerState.PointClamp,
            DepthStencilState.None,
            RasterizerState.CullNone
        );
        MouseAttachment?.Draw(spriteBatch);
        spriteBatch.End();

        if (_screenMessageTimeLeft > 0 && _screenMessage != null)
        {
            spriteBatch.Begin(
                SpriteSortMode.Deferred,
                BlendState.NonPremultiplied,
                SamplerState.PointClamp,
                DepthStencilState.None,
                RasterizerState.CullNone
            );
            int offset = (int)_screenMessage.Font.MeasureString(_screenMessage.Text).X / 2;
            Color colorA = _screenMessage.Color.Multiply(new Color(1f, 1f, 1f, Mathf.Lerp(0f, 1f, _screenMessageTimeLeft / _screenMessage.Duration)));
            Color colorB = _screenMessage.Color.Multiply(new Color(1f, 1f, 1f, Mathf.Lerp(0f, .6f, _screenMessageTimeLeft / _screenMessage.Duration)));
            int xOffsetA = Core.Random.Next(-2, 2);
            int yOffsetA = Core.Random.Next(-2, 2);
            int xOffsetB = Core.Random.Next(-8, 8);
            int yOffsetB = Core.Random.Next(-8, 8);

            spriteBatch.DrawString(_screenMessage.Font, _screenMessage.Text, new Vector2((Screen.Width / 2) - offset + xOffsetB, 400 + yOffsetB), colorB);
            //spriteBatch.DrawString(BaseContent.Fonts.Default.VeryLarge, _screenMessage, new Vector2((Screen.Width / 2) - offset + Core.Random.Next(-5, 5), 300 + Core.Random.Next(-5, 5)), colorB);
            spriteBatch.DrawString(_screenMessage.Font, _screenMessage.Text, new Vector2((Screen.Width / 2) - offset + xOffsetA, 400 + yOffsetA), colorA);
            spriteBatch.End();
        }
    }

    public void PushScreenMessage(ScreenMessageData message)
    {
        _screenMessage = message;
        _screenMessageTimeLeft = message.Duration;
    }

    public void ClearScreenMessage()
    {
        _screenMessage = null;
        _screenMessageTimeLeft = 0;
    }

    private void ShowEntityIfQueued()
    {
        if (_queuedEntityToView == null)
        {
            return;
        }

        if (_entityViewerWindow.IsPlaced)
        {
            _entityViewerWindow.Close();
        }

        _entityViewerWindow.Content = _queuedEntityToView.Value.Key.UiPanel(this, new EntityPanelProperties
        {
            ShowTitle = false, ShowCloseButton = false, Background = null
        });
        _entityViewerWindow.Title = _queuedEntityToView.Value.Key.Label;
        _entityViewerWindow.Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.DeepGold];
        
        if (_queuedEntityToView.Value.Value.HasValue)
        {
            _entityViewerWindow.Show(Desktop, _queuedEntityToView.Value.Value.Value);
        }
        else
        {
            _entityViewerWindow.Show(Desktop);
        }

        _viewedEntity = _queuedEntityToView.Value.Key;

        _queuedEntityToView = null;
    }

    public void CloseEntityWindow()
    {
        _entityViewerWindow.Close();
    }

    public void TransferScreenMessage(BaseGui gui)
    {
        gui._screenMessage = _screenMessage;
        gui._screenMessageTimeLeft = _screenMessageTimeLeft;
    }

    public abstract void Dispose();

    public void TickGame()
    {
        Core.Context.TickOnce();
    }
}

public class ScreenMessageData
{
    public string Text = "";
    public Color Color;
    public int Duration = 10000;
    public DynamicSpriteFont Font = BaseContent.Fonts.Default.VeryLarge;
}