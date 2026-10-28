using System; using UnityEngine; using System.Collections.Generic;
[Serializable] public class Fr { public int x, y, w, h; public float px; }
[Serializable] public class CharDef {
  public string id, name; public float speed, dmg, sc; public Fr[] frames;
  public int[] idle, walk, punch, kick, jk, up, hurt, fall, get, taunt, jump, crouch;
  [NonSerialized] public Sprite[] spr;
  public Sprite S(int[] a, int i) => spr[a[Mathf.Clamp(i, 0, a.Length - 1)]];
}
[Serializable] public class StageDef { public string id, name; public float gy, s; [NonSerialized] public Sprite bg; }
[Serializable] class CL { public CharDef[] list; }
[Serializable] class SL { public StageDef[] list; }
public static class Data {
  public const float W = 640, H = 392, PPU = 100;
  public static CharDef[] Chars; public static StageDef[] Stages; public static Dictionary<string, AudioClip> Snd = new Dictionary<string, AudioClip>();
  public static Vector3 P(float x, float y) => new Vector3(x / PPU, (H - y) / PPU, 0);
  public static void Load() {
    Chars = JsonUtility.FromJson<CL>(Resources.Load<TextAsset>("chars").text).list;
    Stages = JsonUtility.FromJson<SL>(Resources.Load<TextAsset>("stages").text).list;
    foreach (var c in Chars) {
      var t = Resources.Load<Texture2D>("Sheets/" + c.id); c.spr = new Sprite[c.frames.Length];
      for (int i = 0; i < c.frames.Length; i++) { var f = c.frames[i];
        c.spr[i] = Sprite.Create(t, new Rect(f.x, t.height - f.y - f.h, f.w, f.h), new Vector2(f.px / f.w, 0), PPU, 0, SpriteMeshType.FullRect); }
    }
    foreach (var s in Stages) { var t = Resources.Load<Texture2D>("Stages/" + s.id);
      s.bg = Sprite.Create(t, new Rect(0, 0, t.width, t.height), new Vector2(0, 0), PPU / s.s, 0, SpriteMeshType.FullRect); }
    foreach (var n in new[] { "bigPunch", "littleKick", "win", "final", "you" }) Snd[n] = Resources.Load<AudioClip>("Sounds/" + n);
  }
}
