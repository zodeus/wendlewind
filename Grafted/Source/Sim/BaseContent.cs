using System.IO;
using FontStashSharp;

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

        public static Texture2D MainMenuBackground = null!;
        public static Texture2D MainMenuPlay = null!;
        public static Texture2D MainMenuPlayOver = null!;
        public static Texture2D MainMenuQuit = null!;

        public static void Load() {
            BadTexture = Core.Content.Load<Texture2D>(BadTexturePath);

            // Main Menu
            MainMenuBackground = Core.Content.Load<Texture2D>("UI/MainMenu/Background");
            MainMenuPlay = Core.Content.Load<Texture2D>("UI/MainMenu/Play");
            MainMenuPlayOver = Core.Content.Load<Texture2D>("UI/MainMenu/PlayOver");
            MainMenuQuit = Core.Content.Load<Texture2D>("UI/MainMenu/Quit");
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
            public const string Durability = "durability";
        }

        public static class Label {
            public const string Error = "error";
            public const string Success = "success";
            public const string Small = "small";
            public const string Normal = "normal";
            public const string Medium = "medium";
            public const string Large = "large";
            public const string Huge = "huge";
        }

        public static class Button {
            public const string Icon = "icon";
            public const string Small = "small";
            public const string Normal = "normal";
            public const string Large = "large";
            public const string LargeGold = "large-gold";
            public const string Plus24 = "plus-24";
            public const string Plus64 = "plus-64";
            public const string Minus24 = "minus-24";
            public const string Money24 = "money-24";
        }


        public static class Atlas {
            public const string White = "white";
            public static class Icon {
                public const string Minus = "icon-minus-32";
                public const string Close = "icon-close";
                public const string Pause = "icon-pause";
                public const string Play = "icon-play";
                public const string Speed025x = "icon-speed-025x";
                public const string Speed2x = "icon-speed-2x";
                public const string Speed4x = "icon-speed-4x";
                public const string Speed6x = "icon-speed-6x";
                public const string AttackSpeed = "icon-attack-speed";
                public const string Achievements = "icon-achievements";
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
                public const string BagSlot = "icon-bag-slot";
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
                public const string MediumFrameBright = "panel-frame-medium-bright";
                public const string MediumFrameRed = "panel-frame-medium-red";
                public const string RoundWhite24 = "panel-round-white-24";
                public const string RoundWhite28 = "panel-round-white-28";
                public const string RoundWhite42 = "panel-round-white-42";
                public const string RoundWhite64 = "panel-round-white-64";
                public const string RoundDark32 = "panel-round-dark-32";
                public const string RoundDark64 = "panel-round-dark-64";
                public const string RoundElite64 = "panel-round-elite-64";
                public const string Red = "panel-red";
                public const string Loot = "panel-loot";
                public const string DeepGold = "panel-deep-gold";
                public const string SimpleWhite = "panel-simple-white";
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
            public DynamicSpriteFont Huge { get; init; }
        }

        public static FontData Default { get; set; }
        public static FontData Fancy { get; set; }

        public static void Load() {
            // Ordinary DynamicSpriteFont
            FontSystem monoFont = new();
            monoFont.AddFont(File.ReadAllBytes("Content/Fonts/JetBrainsMono-Regular.ttf"));
            FontSystem fancyFont = new();
            fancyFont.AddFont(File.ReadAllBytes("Content/Fonts/JetBrainsMono-Regular.ttf"));
            //fancyFont.AddFont(File.ReadAllBytes("Content/Fonts/RedAlert.ttf"));

            Default = new FontData {
                VerySmall = monoFont.GetFont(16),
                Small = monoFont.GetFont(24),
                Normal = monoFont.GetFont(32),
                Medium = monoFont.GetFont(40),
                Large = monoFont.GetFont(48),
                VeryLarge = monoFont.GetFont(64),
                Huge = monoFont.GetFont(80)
            };  
            
            Fancy = new FontData {
                VerySmall = fancyFont.GetFont(16),
                Small = fancyFont.GetFont(24),
                Normal = fancyFont.GetFont(32),
                Medium = fancyFont.GetFont(40),
                Large = fancyFont.GetFont(48),
                VeryLarge = fancyFont.GetFont(64),
                Huge = fancyFont.GetFont(80)
            };
        }
    }
}