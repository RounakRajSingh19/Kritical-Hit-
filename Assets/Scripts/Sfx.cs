using System.Collections.Generic;
using UnityEngine;

public static class Sfx {
    public static bool Mute;
    static AudioSource src, src2;
    static Dictionary<string, AudioClip> snd = new Dictionary<string, AudioClip>();
    static Dictionary<string, AudioClip> beeps = new Dictionary<string, AudioClip>();

    public static void Init(AudioSource a, AudioSource b) {
        src = a; src2 = b;
        foreach (string n in new string[] { "bigPunch", "littleKick", "win", "final", "you" })
            snd[n] = Resources.Load<AudioClip>("Sounds/" + n);
    }

    public static void Play(string n, float vol) {
        if (Mute || src == null) return;
        AudioClip c;
        if (snd.TryGetValue(n, out c) && c != null) src.PlayOneShot(c, Mathf.Min(1f, vol * 1.2f));
    }

    static AudioClip MakeBeep(float f, float d, string type, float v) {
        string key = f.ToString("F1") + "_" + d.ToString("F3") + "_" + type + "_" + v.ToString("F3");
        AudioClip c;
        if (beeps.TryGetValue(key, out c)) return c;
        int rate = 44100, n = Mathf.Max(1, (int)(d * rate));
        float[] data = new float[n];
        for (int i = 0; i < n; i++) {
            float t = (float)i / rate, ph = t * f, w;
            float frac = ph - Mathf.Floor(ph);
            if (type == "square") w = frac < 0.5f ? 1f : -1f;
            else if (type == "sawtooth") w = 2f * frac - 1f;
            else w = 4f * Mathf.Abs(frac - 0.5f) - 1f;
            float amp = v * Mathf.Pow(0.0001f / v, t / d);
            data[i] = w * amp;
        }
        c = AudioClip.Create("beep", n, 1, rate, false);
        c.SetData(data, 0);
        beeps[key] = c; return c;
    }

    public static void Beep(float f, float d, string type, float v) {
        if (Mute || src == null) return;
        src.PlayOneShot(MakeBeep(f, d, type, v), 1f);
    }
    public static void Beep(float f) { Beep(f, .07f, "square", .04f); }
    public static void Move() { Beep(520f, .05f, "square", .04f); }
    public static void Back() { Beep(300f, .08f, "square", .04f); }
    public static void Ok() {
        Beep(660f, .08f, "square", .04f);
        if (Mute || src2 == null) return;
        src2.clip = MakeBeep(880f, .1f, "square", .04f); src2.PlayDelayed(.07f);
    }
}
