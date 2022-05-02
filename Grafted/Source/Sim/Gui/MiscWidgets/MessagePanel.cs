using System.Linq;
using Myra.Graphics2D;
using Myra.Graphics2D.UI;
using Myra.Graphics2D.UI.Styles;

namespace Grafted.Sim.Gui.MiscWidgets;

public class MessagePanel : Panel {
    public MessagePanel(SimulationMessages messages) {
        Background = Stylesheet.Current.Atlas[BaseContent.Styles.Atlas.Panel.MediumFrame];
        Padding = new Thickness(8);
        VerticalStackPanel messageList = new() { Spacing = 10, Padding = new Thickness(10) };
        ScrollViewer scrollView = new() { Content = messageList };
        foreach (Message message in messages.All.TakeLast(1000)) {
            messageList.AddChild(new Label { Text = message.ToString(), Font = BaseContent.Fonts.Default.Small, Wrap = true });
        }

        AddChild(scrollView);
        messages.MessagePushed += message => {
            Label label = new() { Text = message.ToString(), Font = BaseContent.Fonts.Default.Small, Wrap = true };
            if (message.TextColor != null) {
                label.TextColor = message.TextColor.Value;
            }

            messageList.AddChild(label);
            if (messageList.Widgets.Count > 1000) {
                messageList.Widgets.First()!.RemoveFromParent();
            }

            scrollView.UpdateLayout();
            scrollView.ScrollPosition = scrollView.ScrollMaximum;
        };
    }
}