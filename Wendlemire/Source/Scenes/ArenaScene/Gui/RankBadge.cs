using Wendlemire.NetCode;

namespace Wendlemire.Scenes.ArenaScene.Gui;

public sealed class RankBadge : HorizontalStackPanel
{
    public RankBadge(ArenaRankDisplay rank, int badgeSize = 64, bool showRating = true)
    {
        Spacing = 8;
        VerticalAlignment = VerticalAlignment.Center;

        if (Core.Content.TryLoad<Texture2D>(rank.BadgeTexturePath, out var texture) && texture != null)
        {
            var inset = Math.Max(4, badgeSize / 10);
            var medal = new Image
            {
                Background = new TextureRegion(texture),
                Width = badgeSize - inset,
                Height = badgeSize - inset,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            if (Core.Content.TryLoad<Texture2D>("UI/Ranks/Frame", out var frame) && frame != null)
            {
                Widgets.Add(new Panel
                {
                    Background = new TextureRegion(frame),
                    Width = badgeSize,
                    Height = badgeSize,
                    VerticalAlignment = VerticalAlignment.Center,
                    Widgets = { medal }
                });
            }
            else
            {
                medal.Width = badgeSize;
                medal.Height = badgeSize;
                Widgets.Add(medal);
            }
        }

        Widgets.Add(new Label(BaseContent.Styles.Label.Medium)
        {
            Text = showRating ? rank.Label : rank.LeagueName,
            TextColor = Color.Goldenrod,
            VerticalAlignment = VerticalAlignment.Center
        });
    }
}
