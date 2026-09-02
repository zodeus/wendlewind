using Wendlemire.NetCode;
using Wendlemire.NetCode.Contracts;

namespace Wendlemire.Scenes.ArenaScene.Gui;

public sealed class ArenaRunEndScreen : Panel
{
    private static readonly Color Void = new(7, 5, 4);
    private static readonly Color IronFill = new(42, 26, 20);
    private static readonly Color IronHover = new(58, 28, 20);
    private static readonly Color IronPressed = new(20, 12, 10);
    private static readonly Color IronEdge = new(60, 46, 38);
    private static readonly Color FrameOuter = new(10, 6, 4);
    private static readonly Color FrameWood = new(42, 22, 16);
    private static readonly Color FrameInset = new(106, 58, 40);
    private static readonly Color Rust = new(110, 42, 28);
    private static readonly Color Bone = new(203, 184, 150);
    private static readonly Color Dust = new(122, 110, 88);
    private static readonly Color Veil = new(7, 5, 4, 88);
    private static readonly Color HeaderVeil = new(7, 5, 4, 170);
    private static readonly Color TabletFill = new(16, 10, 8, 230);

    public ArenaRunEndScreen(
        GameContext context,
        Action onMenu,
        ArenaRunRecord? finished = null,
        ArenaRankDisplay? rank = null)
    {
        HorizontalAlignment = HorizontalAlignment.Stretch;
        VerticalAlignment = VerticalAlignment.Stretch;
        Background = new SolidBrush(Void);

        Widgets.Add(new CoverImage(BaseContent.Textures.RunEndSplash));
        Widgets.Add(new Panel
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
            Background = new SolidBrush(Veil)
        });

        var run = context.ArenaRun ?? throw new InvalidOperationException("Run end requires an ArenaRun.");
        var overlay = new Grid
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
            Padding = new Thickness(36, 28)
        };
        overlay.RowsProportions.Add(new Proportion(ProportionType.Auto));
        overlay.RowsProportions.Add(new Proportion(ProportionType.Fill));
        overlay.RowsProportions.Add(new Proportion(ProportionType.Auto));

        var header = BuildHeader(run);
        var tablet = BuildTablet(run, finished, rank, onMenu);
        overlay.Widgets.Add(header);
        overlay.Widgets.Add(tablet);
        Grid.SetRow(tablet, 2);
        Widgets.Add(overlay);
    }

    private static Widget BuildHeader(ArenaRun run)
    {
        var victory = run.IsVictory;
        return new Panel
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Background = new SolidBrush(HeaderVeil),
            Padding = new Thickness(24, 10, 24, 16),
            Widgets =
            {
                new VerticalStackPanel
                {
                    Spacing = 10,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Widgets =
                    {
                        new Label
                        {
                            Text = victory ? "ARENA CHAMPION" : "RUN OVER",
                            Font = BaseContent.Fonts.Display.Huge,
                            TextColor = victory ? Color.Goldenrod : new Color(196, 90, 58),
                            HorizontalAlignment = HorizontalAlignment.Center
                        },
                        new EmberRule { Width = 420, Height = 3, HorizontalAlignment = HorizontalAlignment.Center }
                    }
                }
            }
        };
    }

    private static Widget BuildTablet(
        ArenaRun run,
        ArenaRunRecord? finished,
        ArenaRankDisplay? rank,
        Action onMenu)
    {
        var body = new VerticalStackPanel
        {
            Spacing = 12,
            HorizontalAlignment = HorizontalAlignment.Center,
            Widgets =
            {
                BodyLabel($"{run.Wins} wins   /   {run.Losses} losses", Bone),
                BodyLabel($"{run.Gold} gold remaining", Dust)
            }
        };

        if (finished is { MarksAwarded: > 0 })
        {
            body.Widgets.Add(BodyLabel($"+{finished.MarksAwarded} marks", new Color(150, 186, 122)));
        }
        else if (finished != null)
        {
            body.Widgets.Add(BodyLabel("No marks awarded", Dust));
        }

        if (rank is { } current)
        {
            body.Widgets.Add(new RankBadge(current, badgeSize: 72, showRating: false)
            {
                HorizontalAlignment = HorizontalAlignment.Center
            });
        }

        if (finished?.RatingBefore is int before && finished.RatingAfter is int after)
        {
            var beforeRank = ArenaRank.FromRating(
                before,
                Math.Max(0, (rank?.RatedRuns ?? 1) - (finished.RankApplied ? 1 : 0)));
            if (!finished.RankApplied)
            {
                body.Widgets.Add(BodyLabel("Rank unchanged  ·  no player opponents", Dust));
            }
            else
            {
                var afterRank = rank ?? ArenaRank.FromRating(after, rank?.RatedRuns ?? 0);
                if (beforeRank.LeagueName != afterRank.LeagueName)
                {
                    body.Widgets.Add(BodyLabel($"{beforeRank.LeagueName}  →  {afterRank.LeagueName}", Bone));
                }

                var delta = finished.RatingDelta ?? (after - before);
                var sign = delta >= 0 ? "+" : "";
                body.Widgets.Add(BodyLabel(
                    $"{before}  →  {after}   ({sign}{delta})",
                    delta >= 0 ? new Color(150, 186, 122) : new Color(196, 90, 58)));
            }
        }

        body.Widgets.Add(IronButton("Main Menu", onMenu));

        var inset = new Panel
        {
            Background = new SolidBrush(TabletFill),
            Border = new SolidBrush(FrameInset),
            BorderThickness = new Thickness(1),
            Padding = new Thickness(28, 22),
            Widgets = { body }
        };

        return new Panel
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            Background = new SolidBrush(FrameWood),
            Border = new SolidBrush(FrameOuter),
            BorderThickness = new Thickness(1),
            Padding = new Thickness(7),
            Widgets = { inset }
        };
    }

    private static CursorButton IronButton(string text, Action onClick)
    {
        var button = new CursorButton
        {
            Content = new Label
            {
                Text = text,
                Font = BaseContent.Fonts.Display.Normal,
                TextColor = Bone,
                HorizontalAlignment = HorizontalAlignment.Center
            },
            Width = 260,
            HorizontalAlignment = HorizontalAlignment.Center,
            Padding = new Thickness(16, 10),
            Background = new SolidBrush(IronFill),
            OverBackground = new SolidBrush(IronHover),
            PressedBackground = new SolidBrush(IronPressed),
            Border = new SolidBrush(IronEdge),
            BorderThickness = new Thickness(1)
        };
        button.MouseEntered += (_, _) => button.Border = new SolidBrush(Rust);
        button.MouseLeft += (_, _) => button.Border = new SolidBrush(IronEdge);
        button.Click += (_, _) => onClick();
        return button;
    }

    private static Label BodyLabel(string text, Color color)
    {
        return new Label(BaseContent.Styles.Label.Medium)
        {
            Text = text,
            TextColor = color,
            HorizontalAlignment = HorizontalAlignment.Center
        };
    }

    private sealed class CoverImage : Widget
    {
        private readonly Texture2D _texture;

        public CoverImage(Texture2D texture)
        {
            _texture = texture;
            HorizontalAlignment = HorizontalAlignment.Stretch;
            VerticalAlignment = VerticalAlignment.Stretch;
            ClipToBounds = true;
        }

        public override void InternalRender(RenderContext context)
        {
            base.InternalRender(context);
            var bounds = ActualBounds;
            if (bounds.Width <= 0 || bounds.Height <= 0 || _texture.Width <= 0 || _texture.Height <= 0)
            {
                return;
            }

            var scale = Math.Max(bounds.Width / (float)_texture.Width, bounds.Height / (float)_texture.Height);
            var width = (int)MathF.Ceiling(_texture.Width * scale);
            var height = (int)MathF.Ceiling(_texture.Height * scale);
            context.Draw(_texture, new Rectangle(
                bounds.X + (bounds.Width - width) / 2,
                bounds.Y + (bounds.Height - height) / 2,
                width,
                height), Color.White);
        }
    }

    private sealed class EmberRule : Widget
    {
        private static Texture2D? _pixel;

        public override void InternalRender(RenderContext context)
        {
            base.InternalRender(context);
            var bounds = ActualBounds;
            if (bounds.Width <= 0 || bounds.Height <= 0)
            {
                return;
            }

            var pixel = Pixel();
            var y = bounds.Y + bounds.Height / 2;
            for (var x = 0; x < bounds.Width; x++)
            {
                var t = x / (float)Math.Max(1, bounds.Width - 1);
                var edge = t < 0.5f ? t * 2f : (1f - t) * 2f;
                var color = Color.Lerp(Rust, new Color(201, 160, 112), 1f - MathF.Abs(t - 0.5f) * 2f);
                color *= 0.35f + edge * 0.65f;
                context.Draw(pixel, new Rectangle(bounds.X + x, y, 1, bounds.Height), color);
            }
        }

        private static Texture2D Pixel()
        {
            if (_pixel != null)
            {
                return _pixel;
            }

            _pixel = new Texture2D(Core.GraphicsDevice, 1, 1);
            _pixel.SetData([Color.White]);
            return _pixel;
        }
    }
}
