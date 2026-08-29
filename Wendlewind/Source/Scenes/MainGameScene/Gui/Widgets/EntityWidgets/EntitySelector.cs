using Myra.Graphics2D.Brushes;

namespace Wendlewind.Scenes.MainGameScene.Gui.Widgets.EntityWidgets;

public class EntitySelector : Window {
    public EntitySelector(IEnumerable<Entity> entities, Action<Entity> selectionAction) {
        Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.IconFrame];
        Padding = new Thickness(5);
        /*const int hitBoxSize = 50;
         VisualElement hitBox = new VisualElement {
            style = {
                paddingBottom = hitBoxSize, paddingLeft = hitBoxSize, paddingRight = hitBoxSize, paddingTop = hitBoxSize,
                position = Position.Absolute, left = evt.mousePosition.x - hitBoxSize, top = evt.mousePosition.y - hitBoxSize
            }
        };
        hitBox.RegisterCallback<MouseMoveEvent>(MouseMoveEvent);*/
        MouseLeft += (_, _) => Close();
        VerticalStackPanel panel = new() { Spacing = 3 };
        Content = panel;
        foreach (var entity in entities) {
            HorizontalStackPanel row = new() { Spacing = 10, OverBackground = new SolidBrush(new Color(20, 25, 20)) };
            row.TouchUp += (sender, args) => {
                selectionAction(entity);
                Close();
            };
            row.Widgets.Add(new Image { Height = 32, Width = 32, Background = new TextureRegion(entity.Icon), Margin = new Thickness(10, 0, 0, 0) });
            Label label = new() { Text = entity is Item item ? item.LabelWithStackSize : entity.Label, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 10, 0) };
            row.Widgets.Add(label);
            panel.Widgets.Add(row);
            panel.Widgets.Add(new HorizontalSeparator());
        }

        /*void MouseMoveEvent(MouseMoveEvent e) {
            const float maxDistance = 50;
            float distance = e.mousePosition.DistanceToRect(popupElement.Children().First().worldBound);
            popupElement.style.opacity = Mathf.InverseLerp(maxDistance, 0, distance);
            if (popupElement.style.opacity.value < 0.2f) {
                popupElement.RemoveFromHierarchy();
            }
    
            e.StopPropagation();
        }*/
    }
}