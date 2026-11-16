using UnityEngine;

namespace GunVerse
{
    public static class ArtPalette
    {
        public static class Ui
        {
            public static readonly Color Ink = UiColor("#0E1319");
            public static readonly Color Surface = UiColor("#1B2430");
            public static readonly Color Cream = UiColor("#F4F0E6");
            public static readonly Color Primary = UiColor("#C6FF3D");
            public static readonly Color Secondary = UiColor("#32D5E6");
            public static readonly Color Danger = UiColor("#FF4F7B");
            public static readonly Color Accent = UiColor("#FFB12E");
            public static readonly Color SurfaceRaised = Lighten(Surface, 0.09f);
            public static readonly Color CreamMuted = WithAlpha(Cream, 0.70f);
            public static readonly Color InkPanel = WithAlpha(Ink, 0.92f);
            public static readonly Color SurfacePanel = WithAlpha(Surface, 0.97f);
            public static readonly Color PauseBackdrop = WithAlpha(Ink, 0.78f);
        }

        public static class BopBuy
        {
            public static readonly Color Floor = WorldColor("#3A4048");
            public static readonly Color Wall = WorldColor("#F2F0EA");
            public static readonly Color Ceiling = WorldColor("#D9DDE2");
            public static readonly Color Blue = WorldColor("#1F4FA8");
            public static readonly Color Yellow = WorldColor("#F4C542");
            public static readonly Color Metal = WorldColor("#5A626D");
            public static readonly Color Accent = WorldColor("#E44D3A");
            public static readonly Color Carpet = Color.Lerp(Floor, Blue, 0.35f);
            public static readonly Color CharcoalCarpet = Darken(Floor, 0.75f);
            public static readonly Color CoinRubber = Darken(Floor, 0.55f);
            public static readonly Color WhiteBlock = Wall;
            public static readonly Color WhiteCounter = Wall;
            public static readonly Color Chrome = Lighten(Metal, 0.35f);
            public static readonly Color BlueFabric = Darken(Blue, 0.85f);
            public static readonly Color WoodAccent = Color.Lerp(Floor, Yellow, 0.25f);
            public static readonly Color Counter = Color.Lerp(Blue, Floor, 0.18f);
            public static readonly Color Fog = Color.Lerp(Floor, Ceiling, 0.35f);
            public static readonly Color Ambient = Color.Lerp(Floor, Ceiling, 0.60f);
            public static readonly Color KeyLight = Color.Lerp(Wall, Blue, 0.10f);
            public static readonly Color PointLight = Color.Lerp(Wall, Yellow, 0.35f);
            public static readonly Color CameraBackground = Color.Lerp(Floor, Blue, 0.20f);
        }

        public static class GroceryStore
        {
            public static readonly Color Floor = WorldColor("#6B6258");
            public static readonly Color Wall = WorldColor("#EFE6D4");
            public static readonly Color Ceiling = WorldColor("#DCD5C8");
            public static readonly Color Green = WorldColor("#2F6B4F");
            public static readonly Color Tomato = WorldColor("#C94A3A");
            public static readonly Color Wood = WorldColor("#9A7B57");
            public static readonly Color Accent = WorldColor("#E0A62E");
            public static readonly Color ShelfMetal = Color.Lerp(Floor, Wall, 0.35f);
            public static readonly Color ShelfBack = Wall;
            public static readonly Color Checkout = Green;
            public static readonly Color CheckoutTop = Color.Lerp(Floor, Wall, 0.55f);
            public static readonly Color ProduceFloor = Darken(Green, 0.80f);
            public static readonly Color Fog = Color.Lerp(Floor, Ceiling, 0.45f);
            public static readonly Color Ambient = Color.Lerp(Floor, Ceiling, 0.65f);
            public static readonly Color KeyLight = Color.Lerp(Wall, Accent, 0.22f);
            public static readonly Color PointLight = Color.Lerp(Wall, Accent, 0.35f);
            public static readonly Color CameraBackground = Color.Lerp(Floor, Wood, 0.25f);
        }

        public static Color UiColor(string html)
        {
            return Parse(html);
        }

        public static Color WorldColor(string html)
        {
            return Parse(html).linear;
        }

        public static string ToHtmlRgb(Color color)
        {
            return ColorUtility.ToHtmlStringRGB(color);
        }

        public static string ToHtml(Color color)
        {
            return color.a < 0.999f
                ? ColorUtility.ToHtmlStringRGBA(color)
                : ColorUtility.ToHtmlStringRGB(color);
        }

        public static Color WithAlpha(Color color, float alpha)
        {
            color.a = alpha;
            return color;
        }

        public static Color Lighten(Color color, float amount)
        {
            return Color.Lerp(color, Color.white, amount);
        }

        public static Color Darken(Color color, float multiplier)
        {
            return new Color(color.r * multiplier, color.g * multiplier, color.b * multiplier, color.a);
        }

        private static Color Parse(string html)
        {
            return ColorUtility.TryParseHtmlString(html, out Color color)
                ? color
                : Color.magenta;
        }
    }
}
