using System.Collections.Generic;
using UnityEngine;

// Immediate-mode renderer that emulates the 640x392 canvas used by the web game.
public static class Gfx {
    public const float W = 640f, H = 392f;
    public static float GA = 1f;
    static Matrix4x4 baseM = Matrix4x4.identity, cur = Matrix4x4.identity;
    static Stack<Matrix4x4> stack = new Stack<Matrix4x4>();
    static Stack<float> gaStack = new Stack<float>();
    public static Texture2D White, Circle, Ring, Radial, Vig;
    public static Texture2D ParaFill, ParaLine, ParaLine3;
    static Font font; static bool impact; static GUIStyle style;
    static Dictionary<string, Texture2D> grads = new Dictionary<string, Texture2D>();
    static Dictionary<Texture2D, Dictionary<string, Texture2D>> tints = new Dictionary<Texture2D, Dictionary<string, Texture2D>>();

    // ------------------------------------------------------------ colours
    public static Color Rgba(float r, float g, float b, float a) { return new Color(r / 255f, g / 255f, b / 255f, a); }
    public static Color Hex(string h) { Color c = Color.white; ColorUtility.TryParseHtmlString(h, out c); return c; }
    public static Color Hex(string h, float a) { Color c = Hex(h); c.a = a; return c; }
    public static Color Hsl(float h, float s, float l, float a) {
        h = ((h % 360f) + 360f) % 360f / 360f;
        float q = l < .5f ? l * (1 + s) : l + s - l * s, p = 2 * l - q;
        return new Color(HueC(p, q, h + 1f / 3f), HueC(p, q, h), HueC(p, q, h - 1f / 3f), a);
    }
    static float HueC(float p, float q, float t) {
        if (t < 0) t += 1; if (t > 1) t -= 1;
        if (t < 1f / 6f) return p + (q - p) * 6 * t;
        if (t < .5f) return q;
        if (t < 2f / 3f) return p + (q - p) * (2f / 3f - t) * 6;
        return p;
    }

    // ------------------------------------------------------------ init / generated textures
    static Texture2D Gen(int w, int h, System.Func<int, int, Color> fn, FilterMode fm) {
        Texture2D t = new Texture2D(w, h, TextureFormat.RGBA32, false);
        Color[] px = new Color[w * h];
        for (int y = 0; y < h; y++) for (int x = 0; x < w; x++) px[y * w + x] = fn(x, y);
        t.SetPixels(px); t.Apply(); t.filterMode = fm; t.wrapMode = TextureWrapMode.Clamp; return t;
    }

    public static void Init() {
        White = Texture2D.whiteTexture;
        Circle = Gen(64, 64, (x, y) => { float d = Vector2.Distance(new Vector2(x + .5f, y + .5f), new Vector2(32, 32)); return new Color(1, 1, 1, Mathf.Clamp01(32f - d)); }, FilterMode.Bilinear);
        Ring = Gen(128, 128, (x, y) => { float d = Vector2.Distance(new Vector2(x + .5f, y + .5f), new Vector2(64, 64)) / 64f; float a = Mathf.Clamp01((1f - d) * 64f) * Mathf.Clamp01((d - .86f) * 64f); return new Color(1, 1, 1, a); }, FilterMode.Bilinear);
        Radial = Gen(128, 128, (x, y) => { float d = Vector2.Distance(new Vector2(x + .5f, y + .5f), new Vector2(64, 64)) / 64f; return new Color(1, 1, 1, Mathf.Clamp01(1f - d)); }, FilterMode.Bilinear);
        Vig = Gen(128, 80, (x, y) => {
            float px = (x + .5f) / 128f * W, py = (y + .5f) / 80f * H;
            float d = Mathf.Sqrt((px - W / 2) * (px - W / 2) + (py - H / 2) * (py - H / 2));
            float a = Mathf.Clamp01((d - H * .35f) / (W * .62f - H * .35f)) * .45f; return new Color(0, 0, 0, a); }, FilterMode.Bilinear);
        ParaFill = MakePara(70, 62, 8, 0); ParaLine = MakePara(70, 62, 8, 2); ParaLine3 = MakePara(70, 62, 8, 4);
        string[] names = Font.GetOSInstalledFontNames();
        impact = System.Array.IndexOf(names, "Impact") >= 0;
        font = Font.CreateDynamicFontFromOSFont(new string[] { "Impact", "Haettenschweiler", "Arial Black", "Arial" }, 48);
    }

    static Texture2D MakePara(int w, int h, int sk, int line) {
        int tw = w - sk;
        return Gen(w, h, (x, yy) => {
            int r = h - 1 - yy; // row from top
            float left = sk * (1f - (float)r / h), right = tw + left;
            if (x < left || x >= right) return new Color(1, 1, 1, 0);
            if (line == 0) return Color.white;
            bool edge = x < left + line || x >= right - line || r < line || r >= h - line;
            return new Color(1, 1, 1, edge ? 1f : 0f);
        }, FilterMode.Point);
    }

    // ------------------------------------------------------------ pixel-processed copies of sheets
    static Color32[] ReadPixels(Texture2D src, out int w, out int h) {
        w = src.width; h = src.height;
        if (src.isReadable) return src.GetPixels32();
        RenderTexture rt = RenderTexture.GetTemporary(w, h, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Default);
        Graphics.Blit(src, rt);
        RenderTexture prev = RenderTexture.active; RenderTexture.active = rt;
        Texture2D t = new Texture2D(w, h, TextureFormat.RGBA32, false);
        t.ReadPixels(new Rect(0, 0, w, h), 0, 0); t.Apply();
        RenderTexture.active = prev; RenderTexture.ReleaseTemporary(rt);
        Color32[] px = t.GetPixels32(); Object.Destroy(t); return px;
    }
    static Texture2D FromPixels(Color32[] px, int w, int h) {
        Texture2D t = new Texture2D(w, h, TextureFormat.RGBA32, false);
        t.SetPixels32(px); t.Apply(); t.filterMode = FilterMode.Point; t.wrapMode = TextureWrapMode.Clamp; return t;
    }
    public static Texture2D MakeSwap(Texture2D src) {
        int w, h; Color32[] px = ReadPixels(src, out w, out h);
        for (int i = 0; i < px.Length; i++) if (px[i].a != 0) { byte r = px[i].r, g = px[i].g, b = px[i].b; px[i].r = g; px[i].g = b; px[i].b = r; }
        return FromPixels(px, w, h);
    }
    // same as tinted(): fills every opaque pixel with the colour, keeps alpha
    public static Texture2D Tinted(Texture2D src, Color col) {
        Dictionary<string, Texture2D> per;
        if (!tints.TryGetValue(src, out per)) { per = new Dictionary<string, Texture2D>(); tints[src] = per; }
        string key = ColorUtility.ToHtmlStringRGB(col);
        Texture2D t;
        if (per.TryGetValue(key, out t)) return t;
        int w, h; Color32[] px = ReadPixels(src, out w, out h);
        Color32 c = col;
        for (int i = 0; i < px.Length; i++) { byte a = px[i].a; px[i] = c; px[i].a = a; }
        t = FromPixels(px, w, h); per[key] = t; return t;
    }

    // ------------------------------------------------------------ frame setup
    public static void Begin() {
        float sc = Mathf.Min(Screen.width / W, Screen.height / H);
        float ox = (Screen.width - W * sc) * .5f, oy = (Screen.height - H * sc) * .5f;
        baseM = Matrix4x4.TRS(new Vector3(ox, oy, 0), Quaternion.identity, new Vector3(sc, sc, 1));
        cur = Matrix4x4.identity; stack.Clear(); gaStack.Clear(); GA = 1f;
        GUI.matrix = Matrix4x4.identity; GUI.contentColor = Color.white; GUI.color = Color.black;
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), White);
    }
    public static void End() {
        GUI.matrix = Matrix4x4.identity; GUI.color = Color.black;
        float sc = baseM.m00, ox = baseM.m03, oy = baseM.m13, w = W * sc, h = H * sc;
        GUI.DrawTexture(new Rect(0, 0, ox, Screen.height), White);
        GUI.DrawTexture(new Rect(ox + w, 0, Screen.width - ox - w, Screen.height), White);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, oy), White);
        GUI.DrawTexture(new Rect(0, oy + h, Screen.width, Screen.height - oy - h), White);
        GUI.color = Color.white;
    }

    // ------------------------------------------------------------ transform stack
    public static void Save() { stack.Push(cur); gaStack.Push(GA); }
    public static void Restore() { cur = stack.Pop(); GA = gaStack.Pop(); }
    public static void Translate(float x, float y) { cur = cur * Matrix4x4.Translate(new Vector3(x, y, 0)); }
    public static void Scale(float sx, float sy) { cur = cur * Matrix4x4.Scale(new Vector3(sx, sy, 1)); }
    public static void Rotate(float rad) { cur = cur * Matrix4x4.Rotate(Quaternion.Euler(0, 0, rad * Mathf.Rad2Deg)); }
    static void Apply() { GUI.matrix = baseM * cur; }

    // ------------------------------------------------------------ primitives
    public static void Fill(float x, float y, float w, float h, Color c) {
        Apply(); c.a *= GA; GUI.color = c; GUI.DrawTexture(new Rect(x, y, w, h), White);
    }
    public static void Tex(Texture2D t, float x, float y, float w, float h, Color c) {
        Apply(); c.a *= GA; GUI.color = c; GUI.DrawTexture(new Rect(x, y, w, h), t);
    }
    public static void Img(Texture2D t, float sx, float sy, float sw, float sh, float dx, float dy, float dw, float dh, float a, Color tint) {
        Apply(); tint.a = a * GA; GUI.color = tint;
        GUI.DrawTextureWithTexCoords(new Rect(dx, dy, dw, dh), t, new Rect(sx / t.width, 1f - (sy + sh) / t.height, sw / t.width, sh / t.height));
    }
    public static void Img(Texture2D t, float sx, float sy, float sw, float sh, float dx, float dy, float dw, float dh) {
        Img(t, sx, sy, sw, sh, dx, dy, dw, dh, 1f, Color.white);
    }
    public static void Line(float x1, float y1, float x2, float y2, float w, Color c) {
        float dx = x2 - x1, dy = y2 - y1, len = Mathf.Sqrt(dx * dx + dy * dy);
        Save(); Translate(x1, y1); Rotate(Mathf.Atan2(dy, dx)); Fill(0, -w / 2, len, w, c); Restore();
    }
    public static void Circ(float x, float y, float r, Color c) { Tex(Circle, x - r, y - r, r * 2, r * 2, c); }
    public static void RingAt(float x, float y, float r, Color c) { Tex(Ring, x - r, y - r, r * 2, r * 2, c); }
    public static void Glow(float x, float y, float r, Color c) { Tex(Radial, x - r, y - r, r * 2, r * 2, c); }
    public static void Stage(StageDef st, float sx, float sy, float sw, float sh, float dx, float dy, float dw, float dh) {
        Img(st.tex, sx / st.s, sy / st.s, sw / st.s, sh / st.s, dx, dy, dw, dh);
    }
    public static void StrokeRect(float x, float y, float w, float h, float lw, Color c) {
        Fill(x - lw / 2, y - lw / 2, w + lw, lw, c); Fill(x - lw / 2, y + h - lw / 2, w + lw, lw, c);
        Fill(x - lw / 2, y + lw / 2, lw, h - lw, c); Fill(x + w - lw / 2, y + lw / 2, lw, h - lw, c);
    }
    public static void VGrad(float x, float y, float w, float h, Color top, Color bot) {
        string key = top.ToString() + bot.ToString();
        Texture2D t;
        if (!grads.TryGetValue(key, out t)) {
            t = Gen(1, 32, (xx, yy) => Color.Lerp(bot, top, yy / 31f), FilterMode.Bilinear);
            grads[key] = t;
        }
        Tex(t, x, y, w, h, Color.white);
    }
    public static void Frame(Texture2D sheet, Fr f, float x, float y, int facing, float s, float a, Color tint) {
        Save(); Translate(Mathf.Round(x), Mathf.Round(y)); if (facing < 0) Scale(-1, 1);
        Img(sheet, f.x, f.y, f.w, f.h, Mathf.Round(-f.px * s), -f.h * s, f.w * s, f.h * s, a, tint);
        Restore();
    }
    public static void Fx(string n, float x, float y, float s, float al, bool bas) {
        FxDef f; if (!Data.Fx.TryGetValue(n, out f)) return;
        Img(Data.FxTex, f.x, f.y, f.w, f.h, Mathf.Round(x - f.w * s / 2), Mathf.Round(bas ? y - f.h * s : y - f.h * s / 2), f.w * s, f.h * s, Mathf.Max(0, al), Color.white);
    }
    public static void Fx(string n, float x, float y, float s) { Fx(n, x, y, s, 1f, false); }

    // ------------------------------------------------------------ text (outlined, optional gold gradient)
    static void DrawLabel(string s, float px, float py, int fs, int align, Color c) {
        GUI.color = Color.white;
        style.fontSize = fs; style.normal.textColor = c; style.hover.textColor = c; style.active.textColor = c; style.focused.textColor = c;
        float h = fs * 2f;
        Rect r;
        if (align < 0) { style.alignment = TextAnchor.MiddleLeft; r = new Rect(px, py - fs, 4000, h); }
        else if (align > 0) { style.alignment = TextAnchor.MiddleRight; r = new Rect(px - 4000, py - fs, 4000, h); }
        else { style.alignment = TextAnchor.MiddleCenter; r = new Rect(px - 2000, py - fs, 4000, h); }
        GUI.Label(r, s, style);
    }

    public static void Txt(string s, float x, float y, float size, Color fill, float sw, Color stroke, int align, bool gold) {
        if (style == null) {
            style = new GUIStyle(GUI.skin.label);
            style.padding = new RectOffset(0, 0, 0, 0); style.margin = new RectOffset(0, 0, 0, 0);
            style.wordWrap = false; style.clipping = TextClipping.Overflow; style.richText = false;
            style.font = font; style.fontStyle = impact ? FontStyle.Normal : FontStyle.Bold;
        }
        Matrix4x4 m = baseM * cur;
        Vector3 p = m.MultiplyPoint3x4(new Vector3(x, y, 0));
        float k = new Vector2(m.m00, m.m10).magnitude;
        int fs = Mathf.Clamp(Mathf.RoundToInt(size * k), 4, 380);
        if (sw < 0) sw = Mathf.Max(2f, size / 6f);
        float r = Mathf.Max(1f, sw * .5f * k);
        GUI.matrix = Matrix4x4.identity; GUI.color = Color.white; GUI.contentColor = Color.white;
        Color sc = stroke; sc.a *= GA;
        int n = r <= 1.6f ? 4 : (r <= 4f ? 8 : 12);
        for (int i = 0; i < n; i++) {
            float a = (n == 4 ? (i * 90f + 45f) : i * 45f) * Mathf.Deg2Rad;
            DrawLabel(s, p.x + Mathf.Cos(a) * r, p.y + Mathf.Sin(a) * r, fs, align, sc);
        }
        if (!gold) { Color fc = fill; fc.a *= GA; DrawLabel(s, p.x, p.y, fs, align, fc); return; }
        Color body = Hex("#f4b400"); body.a = GA;
        DrawLabel(s, p.x, p.y, fs, align, body);
        // lighter top band and darker bottom band to imitate the canvas gradient
        Color top = Hex("#fff6a0"); top.a = GA; Color bot = Hex("#c23a00"); bot.a = GA;
        GUI.BeginGroup(new Rect(0, p.y - fs * .6f, Screen.width, fs * .42f));
        DrawLabel(s, p.x, fs * .6f, fs, align, top);
        GUI.EndGroup();
        GUI.BeginGroup(new Rect(0, p.y + fs * .16f, Screen.width, fs * .5f));
        DrawLabel(s, p.x, -fs * .16f, fs, align, bot);
        GUI.EndGroup();
    }
    public static void Txt(string s, float x, float y, float size, Color fill, float sw) { Txt(s, x, y, size, fill, sw, Color.black, 0, false); }
    public static void Txt(string s, float x, float y, float size, Color fill) { Txt(s, x, y, size, fill, -1f, Color.black, 0, false); }
    public static void Txt(string s, float x, float y, float size) { Txt(s, x, y, size, Color.white, -1f, Color.black, 0, false); }
    public static void TxtA(string s, float x, float y, float size, Color fill, float sw, int align) { Txt(s, x, y, size, fill, sw, Color.black, align, false); }
    public static void Gold(string s, float x, float y, float size, float sw) { Txt(s, x, y, size, Color.white, sw, Color.black, 0, true); }
    public static void GoldA(string s, float x, float y, float size, float sw, int align) { Txt(s, x, y, size, Color.white, sw, Color.black, align, true); }
}
