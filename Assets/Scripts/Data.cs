using System.Collections.Generic;
using UnityEngine;

public struct Fr { public float x, y, w, h, px; }

[System.Serializable]
public class CharDef {
    public string id, name, title, sp1, sp2, type, rng, ease;
    public float speed, dmg, sc;
    public string[] bio;
    public int[] air;
    public float[] frames;
    public int[] idle, walkMap, punch, kick, jk, up, hurt, fall, get, throwF, taunt, jump, crouch, walk;
    [System.NonSerialized] public Texture2D tex, swap;
    public float Sc { get { return sc > 0 ? sc : 3f; } }
    public Fr F(int i) {
        int k = Mathf.Clamp(i, 0, frames.Length / 5 - 1) * 5;
        Fr r; r.x = frames[k]; r.y = frames[k + 1]; r.w = frames[k + 2]; r.h = frames[k + 3]; r.px = frames[k + 4]; return r;
    }
    public int[] Map(string n) {
        switch (n) {
            case "idle": return idle; case "punch": return punch; case "kick": return kick; case "jk": return jk;
            case "up": return up; case "hurt": return hurt; case "fall": return fall; case "get": return get;
            case "throw": return throwF; case "taunt": return taunt; case "crouch": return crouch;
        }
        return idle;
    }
    public int Fm(string n, int i) {
        int[] a = Map(n);
        if (a == null || a.Length == 0) return 0;
        return a[Mathf.Clamp(i, 0, a.Length - 1)];
    }
}

[System.Serializable]
public class StageDef {
    public string id, name; public float gy, s;
    [System.NonSerialized] public Texture2D tex;
    public float World { get { return tex.width * s; } }
    public float IW { get { return tex.width * s; } }
    public float IH { get { return tex.height * s; } }
}

[System.Serializable] public class FxDef { public string k; public float x, y, w, h; }
[System.Serializable] public class GameData { public CharDef[] chars; public StageDef[] stages; public FxDef[] fx; }

// global match settings (same as S in the web game)
public static class S {
    public static string mode = "1p";
    public static int diff = 1;
    public static int[] p = { 0, 1 };
    public static int stage = 0;
    public static bool same = false;
    public static readonly string[] DIFFS = { "EASY", "NORMAL", "HARD" };
}

public static class Data {
    public static CharDef[] Chars; public static StageDef[] Stages;
    public static Dictionary<string, FxDef> Fx = new Dictionary<string, FxDef>();
    public static Texture2D FxTex;

    static Texture2D Load(string path) {
        Texture2D t = Resources.Load<Texture2D>(path);
        if (t == null) { Debug.LogError("Missing texture Resources/" + path); return null; }
        t.filterMode = FilterMode.Point; t.wrapMode = TextureWrapMode.Clamp;
        return t;
    }

    public static void Load() {
        TextAsset ta = Resources.Load<TextAsset>("gamedata");
        GameData gd = JsonUtility.FromJson<GameData>(ta.text);
        Chars = gd.chars; Stages = gd.stages;
        foreach (CharDef c in Chars) { c.tex = Load("Sheets/" + c.id); c.swap = Gfx.MakeSwap(c.tex); }
        foreach (StageDef s in Stages) s.tex = Load("Stages/" + s.id);
        FxTex = Load("FX/fx");
        foreach (FxDef f in gd.fx) Fx[f.k] = f;
    }
}
