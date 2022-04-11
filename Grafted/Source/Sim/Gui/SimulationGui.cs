using System.Collections.Generic;
using System.Linq;
using FontStashSharp;
using Grafted.Maths;
using Grafted.Sim.Entities;
using Grafted.Sim.Gui.EntityWidgets;
using Grafted.Utils;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Myra.Graphics2D.UI;

namespace Grafted.Sim.Gui;

public abstract class SimulationGui {
    public Desktop Desktop { get; set; } = null!;

    public MouseAttachment? MouseAttachment;

    private readonly Dictionary<Entity, EntityPanelBase> _entityPanels = new();
    private readonly Dictionary<Entity, Window> _entityWindows = new();
    private readonly HashSet<Entity> _windowsToRemove = new();

    private ScreenMessageData _screenMessage;
    private int _screenMessageTicksLeft;

    public virtual void ViewEntity(Entity entity, Point? position = null) {
        if (_entityWindows.ContainsKey(entity)) {
            return;
        }

        _entityWindows[entity] = new Window {
            Background = null,
            TitleGrid = {
                Visible = false
            }
        };
        var entityPanel = entity.UiPanel(new EntityPanelProperties { ShowTitle = true, ShowCloseButton = true, CloseButtonAction = () => _entityWindows[entity].Close() });
        _entityPanels[entity] = entityPanel;
        _entityWindows[entity].Content = entityPanel;
        _entityWindows[entity].DragHandle = entityPanel.Header;
        _entityWindows[entity].Closed += (_, _) => {
            _entityPanels.Remove(entity);
            _entityWindows.Remove(entity);
        };
        _entityWindows[entity].Show(Desktop, position ?? new Point(Core.GraphicsDevice.Viewport.Width / 2 - 150, 150));

    }

    public virtual void Update(float deltaTime) {
        MouseAttachment?.Update();
        foreach ((Entity entity, EntityPanelBase panel) in _entityPanels) {
            panel.Update();

            /*if (entity.IsDestroyed) {
                _windowsToRemove.Add(entity);
            }*/
        }

        if (_windowsToRemove.Any()) {
            foreach (Entity entity in _windowsToRemove) {
                _entityWindows[entity].Close();
            }

            _windowsToRemove.Clear();
        }

        if (_screenMessageTicksLeft > 1) {
            _screenMessageTicksLeft--;
        }
    }

    public virtual void HandleInput() { }

    public virtual void Render(SpriteBatch spriteBatch) {
        
        
        if (_screenMessageTicksLeft > 1) {
            spriteBatch.Begin(
                SpriteSortMode.Deferred,
                BlendState.NonPremultiplied,
                SamplerState.PointClamp,
                DepthStencilState.None,
                RasterizerState.CullNone
            );
            int offset = (int) _screenMessage.Font.MeasureString(_screenMessage.Text).X / 2;
            Color colorA = _screenMessage.Color.Multiply(new Color(1f, 1f, 1f, Mathf.Lerp(0f, 1f, (float) _screenMessageTicksLeft / _screenMessage.Duration)));
            Color colorB = _screenMessage.Color.Multiply(new Color(1f, 1f, 1f, Mathf.Lerp(0f, .6f, (float) _screenMessageTicksLeft / _screenMessage.Duration)));
            int xOffsetA = Core.Random.Next(-2, 2);
            int yOffsetA = Core.Random.Next(-2, 2);
            int xOffsetB = Core.Random.Next(-8, 8);
            int yOffsetB = Core.Random.Next(-8, 8);

            spriteBatch.DrawString(_screenMessage.Font, _screenMessage.Text, new Vector2((Screen.Width / 2) - offset + xOffsetB, 150 + yOffsetB), colorB);
            //spriteBatch.DrawString(BaseContent.Fonts.Default.VeryLarge, _screenMessage, new Vector2((Screen.Width / 2) - offset + Core.Random.Next(-5, 5), 300 + Core.Random.Next(-5, 5)), colorB);
            spriteBatch.DrawString(_screenMessage.Font, _screenMessage.Text, new Vector2((Screen.Width / 2) - offset + xOffsetA, 150 + yOffsetA), colorA);
            spriteBatch.End();
        }
        
    }

    public void PushScreenMessage(ScreenMessageData message) {
        _screenMessage = message;
        _screenMessageTicksLeft = message.Duration;
    }
}

public class ScreenMessageData {
    public string Text;
    public Color Color;
    public int Duration = 10000;
    public DynamicSpriteFont Font = BaseContent.Fonts.Default.VeryLarge;
}