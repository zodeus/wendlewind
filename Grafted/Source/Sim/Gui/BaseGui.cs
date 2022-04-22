using System.Collections.Generic;
using FontStashSharp;
using Grafted.Maths;
using Grafted.Sim.Entities;
using Grafted.Sim.Gui.EntityWidgets;
using Grafted.Sim.Gui.MiscWidgets;
using Grafted.Utils;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Myra.Graphics2D.UI;

namespace Grafted.Sim.Gui;

public abstract class BaseGui {
    public Desktop Desktop { get; set; } = null!;

    public MouseAttachment? MouseAttachment;

    private ScreenMessageData _screenMessage;
    private float _screenMessageTimeLeft;
    private readonly Window _entityViewerWindow = new();
    private KeyValuePair<Entity, Point?>? _queuedEntityToView;

    public virtual void Update(float deltaTime) {
        MouseAttachment?.Update();
        ShowEntityIfQueued();
        ((EntityPanelBase?) _entityViewerWindow.Content)?.Update();

        if (_screenMessageTimeLeft > 0) {
            _screenMessageTimeLeft -= deltaTime;
        }
    }

    public virtual void ViewEntity(Entity entity, Point? position = null) {
        _queuedEntityToView = new KeyValuePair<Entity, Point?>(entity, position);
    }

    public virtual void HandleInput() { }

    public virtual void Render(SpriteBatch spriteBatch, float deltaTime) {
        Desktop.Render();
        spriteBatch.Begin(
            SpriteSortMode.Deferred,
            BlendState.NonPremultiplied,
            SamplerState.PointClamp,
            DepthStencilState.None,
            RasterizerState.CullNone
        );
        MouseAttachment?.Render(spriteBatch);
        spriteBatch.End();
        
        if (_screenMessageTimeLeft > 0) {
            spriteBatch.Begin(
                SpriteSortMode.Deferred,
                BlendState.NonPremultiplied,
                SamplerState.PointClamp,
                DepthStencilState.None,
                RasterizerState.CullNone
            );
            int offset = (int) _screenMessage.Font.MeasureString(_screenMessage.Text).X / 2;
            Color colorA = _screenMessage.Color.Multiply(new Color(1f, 1f, 1f, Mathf.Lerp(0f, 1f, (float) _screenMessageTimeLeft / _screenMessage.Duration)));
            Color colorB = _screenMessage.Color.Multiply(new Color(1f, 1f, 1f, Mathf.Lerp(0f, .6f, (float) _screenMessageTimeLeft / _screenMessage.Duration)));
            int xOffsetA = Core.Random.Next(-2, 2);
            int yOffsetA = Core.Random.Next(-2, 2);
            int xOffsetB = Core.Random.Next(-8, 8);
            int yOffsetB = Core.Random.Next(-8, 8);

            spriteBatch.DrawString(_screenMessage.Font, _screenMessage.Text, new Vector2((Screen.Width / 2) - offset + xOffsetB, 50 + yOffsetB), colorB);
            //spriteBatch.DrawString(BaseContent.Fonts.Default.VeryLarge, _screenMessage, new Vector2((Screen.Width / 2) - offset + Core.Random.Next(-5, 5), 300 + Core.Random.Next(-5, 5)), colorB);
            spriteBatch.DrawString(_screenMessage.Font, _screenMessage.Text, new Vector2((Screen.Width / 2) - offset + xOffsetA, 50 + yOffsetA), colorA);
            spriteBatch.End();
        }
    }

    public void PushScreenMessage(ScreenMessageData message) {
        _screenMessage = message;
        _screenMessageTimeLeft = message.Duration;
    }

    private void ShowEntityIfQueued() {
        if (_queuedEntityToView == null) {
            return;
        }

        _entityViewerWindow.Content = _queuedEntityToView.Value.Key.UiPanel(new EntityPanelProperties {
            ShowTitle = false, ShowCloseButton = false, Background = null
        });
        _entityViewerWindow.Title = _queuedEntityToView.Value.Key.Label;
        if (_entityViewerWindow.IsPlaced == false) {
            _entityViewerWindow.Show(Desktop, _queuedEntityToView.Value.Value);
        }

        _queuedEntityToView = null;
    }
}

public class ScreenMessageData {
    public string Text;
    public Color Color;
    public int Duration = 10000;
    public DynamicSpriteFont Font = BaseContent.Fonts.Default.VeryLarge;
}