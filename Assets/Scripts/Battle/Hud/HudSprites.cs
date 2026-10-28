using UnityEngine;

namespace MysticKombat
{
    static class HudSprites
    {
        static Sprite edgeFade, circle, glow, star;

        static Texture2D NewTex(int w, int h)
        {
            return new Texture2D(w, h, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };
        }

        // White in the middle, fading to black at both sides. Tinted at runtime,
        // so (tint colour) in the centre -> black at the left and right ends.
        public static Sprite EdgeFade()
        {
            if (edgeFade != null) return edgeFade;
            const int w = 256, h = 2;
            var tex = NewTex(w, h);
            for (int x = 0; x < w; x++)
            {
                float u = x / (w - 1f);
                float t = 1f - Mathf.Abs(u * 2f - 1f);                      // 0 at the sides, 1 in the middle
                float v = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t * 1.8f));
                for (int y = 0; y < h; y++) tex.SetPixel(x, y, new Color(v, v, v, 1f));
            }
            tex.Apply();
            edgeFade = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f);
            return edgeFade;
        }

        public static Sprite Circle()
        {
            if (circle != null) return circle;
            const int n = 64;
            var tex = NewTex(n, n);
            float c = (n - 1) * 0.5f, r = n * 0.5f - 1f;
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float d = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c));
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(r - d + 0.5f)));
                }
            tex.Apply();
            circle = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f);
            return circle;
        }

        public static Sprite Glow()
        {
            if (glow != null) return glow;
            const int n = 128;
            var tex = NewTex(n, n);
            float c = (n - 1) * 0.5f;
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float d = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c)) / (n * 0.5f);
                    float a = Mathf.Clamp01(1f - d);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, a * a));
                }
            tex.Apply();
            glow = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f);
            return glow;
        }

        // Four-pointed throwing star with a small hole in the middle.
        public static Sprite Star()
        {
            if (star != null) return star;
            const int n = 96;
            var tex = NewTex(n, n);
            float c = (n - 1) * 0.5f;
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float u = (x - c) / c, v = (y - c) / c;
                    float f = Mathf.Pow(Mathf.Abs(u), 0.6f) + Mathf.Pow(Mathf.Abs(v), 0.6f);   // < 1 inside the star
                    float a = Mathf.Clamp01((1f - f) * 5f);
                    float r = Mathf.Sqrt(u * u + v * v);
                    a *= Mathf.Clamp01((r - 0.13f) * 20f);                                      // centre hole
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
            tex.Apply();
            star = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f);
            return star;
        }
    }
}
