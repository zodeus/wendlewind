using System.IO;
using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Grafted.Sim;

public static partial class BaseContent {
    public static void Initialize() {
        Fonts.Load();
        Textures.Load();
    }
}

public static partial class BaseContent {
    public static class Textures {
        public static string BadTexturePath = "BadTexture";
        public static Texture2D BadTexture = null!;

        public static Texture2D MilgrethImage = null!;
        public static Texture2D MilgrethTitle = null!;
        public static Texture2D MilgrethPlay = null!;
        public static Texture2D MilgrethPlayOver = null!;
        public static Texture2D MilgrethQuit = null!;
        public static Texture2D MedKit = null!;
        public static Texture2D Cauterize = null!;
        public static Texture2D QuestionMark = null!;
        public static Texture2D Village = null!;
        public static Texture2D ZoneBgPeacefulMeadow = null!;

        public static void Load() {
            BadTexture = Core.Content.Load<Texture2D>(BadTexturePath);

            // Main Menu
            ZoneBgPeacefulMeadow = Core.Content.Load<Texture2D>("UI/Zones/PeacefulMeadow");
            MilgrethImage = Core.Content.Load<Texture2D>("UI/MainMenu/Milgreth");
            MilgrethTitle = Core.Content.Load<Texture2D>("UI/MainMenu/MilgrethTitle");
            MilgrethPlay = Core.Content.Load<Texture2D>("UI/MainMenu/Play");
            MilgrethPlayOver = Core.Content.Load<Texture2D>("UI/MainMenu/PlayOver");
            MilgrethQuit = Core.Content.Load<Texture2D>("UI/MainMenu/Quit");
            MedKit = Core.Content.Load<Texture2D>("Entities/Item/Consumables/MedKit");
            Cauterize = Core.Content.Load<Texture2D>("Entities/Item/Consumables/Cauterize");
            QuestionMark = Core.Content.Load<Texture2D>("Entities/Item/Consumables/Cauterize");

            // Village
            Village = Core.Content.Load<Texture2D>("UI/Village/Village");
        }
    }
}

public static partial class BaseContent {
    public static class Colors {
        public static Color DefaultBorder = new(45, 45, 50);

        public static class Text {
            public static Color Golden = new(232, 170, 0);
        }
    }
}

public static partial class BaseContent {
    public static class Styles {
        public static class Bar {
            public const string Health = "health";
            public const string Xp = "xp";
        }

        public static class Label {
            public const string Error = "error";
            public const string Success = "success";
            public const string Small = "small";
            public const string Medium = "medium";
            public const string Large = "large";
        }

        public static class Button {
            public const string Icon = "icon";
            public const string Small = "small";
            public const string Normal = "normal";
            public const string Large = "large";
            public const string Plus24 = "plus-24";
            public const string Minus24 = "minus-24";
            public const string Money24 = "money-24";
        }


        public static class Atlas {
            public static class Icon {
                public const string Minus = "icon-minus-32";
                public const string Close = "icon-close";
                public const string Pause = "icon-pause";
                public const string Play = "icon-play";
                public const string Speed025x = "icon-speed-025x";
                public const string Speed2x = "icon-speed-2x";
                public const string Speed4x = "icon-speed-4x";
                public const string Speed6x = "icon-speed-6x";
                public const string ArrowNeutral = "icon-arrow-neutral";
                public const string ArrowNegative = "icon-arrow-negative";
                public const string ArrowPositive = "icon-arrow-positive";
                public const string Retreat = "icon-retreat";
                public const string Skull = "icon-skull";
                public const string Combat = "icon-combat";
                public const string Brain = "icon-brain";
                public const string Citizen = "icon-citizen";
                public const string Priorities = "icon-priorities";
                public const string Build = "icon-build";
                public const string Boak = "icon-boak";
                public const string QuestionMark = "icon-question-mark-32-default";
                public const string Trash = "icon-trash";
                public const string PotionSlot = "icon-potion-slot";
                public const string Coin = "icon-gold-coin";
                public const string SoulCoin = "icon-soul-coin";
                public const string Blood = "icon-blood-64";
                public const string Walking = "icon-walking-64";
                public const string Thermometer = "icon-thermometer";
                public const string Human = "icon-human";
                public const string StomachOutline = "icon-stomach-outline";
                public const string Energy = "icon-energy";
                public const string Mind = "icon-mind";
            }

            public static class Panel {
                public const string IconFrame = "panel-icon-frame";
                public const string SmallFrame = "panel-frame-small";
                public const string MediumFrame = "panel-frame-medium";
                public const string RoundDark32 = "panel-round-dark-32";
                public const string RoundDark64 = "panel-round-dark-64";
                public const string Red = "panel-red";
                public const string DeepGold = "panel-deep-gold";
                public const string FancyDark = "panel-bar-fancy-dark";
            }

            public static class Bar {
                public const string FrameSmall = "bar-frame-small";
                public const string Health = "bar-health";
                public const string Neutral = "bar-neutral";
            }
        }
    }
}

public static partial class BaseContent {
    public static class Fonts {
        public readonly struct FontData {
            public DynamicSpriteFont VerySmall { get; init; }
            public DynamicSpriteFont Small { get; init; }
            public DynamicSpriteFont Normal { get; init; }
            public DynamicSpriteFont Medium { get; init; }
            public DynamicSpriteFont Large { get; init; }
            public DynamicSpriteFont VeryLarge { get; init; }
        }

        public static FontData Default { get; set; }
        public static FontData Fancy { get; set; }

        public static void Load() {
            // Ordinary DynamicSpriteFont
            FontSystem monoFont = new();
            monoFont.AddFont(File.ReadAllBytes("Content/Fonts/JetBrainsMono-Regular.ttf"));

            Default = new FontData {
                VerySmall = monoFont.GetFont(10),
                Small = monoFont.GetFont(14),
                Normal = monoFont.GetFont(16),
                Medium = monoFont.GetFont(20),
                Large = monoFont.GetFont(24),
                VeryLarge = monoFont.GetFont(32)
            };

            Fancy = new FontData {
                VerySmall = monoFont.GetFont(10),
                Small = monoFont.GetFont(14),
                Normal = monoFont.GetFont(16),
                Medium = monoFont.GetFont(20),
                Large = monoFont.GetFont(24),
                VeryLarge = monoFont.GetFont(32)
            };

        }
    }
}