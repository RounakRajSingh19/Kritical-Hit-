using UnityEngine;

// Zero-setup bootstrap: press Play in ANY scene (an empty scene is fine).
public class Game : MonoBehaviour {
    public static Game I;
    public static int frame;
    public static GameScene scene;
    float acc, musT; int mus;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot() {
        if (I != null) return;
        GameObject g = new GameObject("MysticKombat"); g.AddComponent<Game>();
    }

    public static void Go(GameScene s) { scene = s; s.Enter(); }

    void Awake() {
        I = this;
        Application.runInBackground = true; Application.targetFrameRate = 60; QualitySettings.vSyncCount = 0;
        if (Camera.main == null) {
            GameObject c = new GameObject("Cam"); Camera cam = c.AddComponent<Camera>(); c.tag = "MainCamera";
            cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = Color.black;
        }
        AudioSource a1 = gameObject.AddComponent<AudioSource>(), a2 = gameObject.AddComponent<AudioSource>();
        a1.playOnAwake = false; a2.playOnAwake = false;
        Sfx.Init(a1, a2);
        Gfx.Init();
        Data.Load();
        Go(Scenes.Title);
    }

    void Update() {
        if (scene == null) return;
        acc += Mathf.Min(.1f, Time.unscaledDeltaTime);
        while (acc >= 1f / 60f) {
            frame++; scene.Update(); In.pressed.Clear(); acc -= 1f / 60f;
        }
        // procedural background music (same notes as the web game)
        musT += Time.unscaledDeltaTime;
        if (musT >= .23f) {
            musT = 0;
            if (!Sfx.Mute) {
                float[] nt = scene == Scenes.Fight
                    ? new float[] { 55, 55, 82.4f, 73.4f, 55, 55, 98, 82.4f }
                    : new float[] { 65.4f, 65.4f, 98, 87.3f, 65.4f, 77.8f, 98, 87.3f };
                float f = nt[mus % 8];
                Sfx.Beep(f * 2, .22f, "sawtooth", .014f);
                if (mus % 2 == 0) Sfx.Beep(f * 4 * new float[] { 1, 1.25f, 1.5f, 1.33f }[(mus >> 1) % 4], .12f, "triangle", .01f);
                mus++;
            }
        }
    }

    void OnApplicationFocus(bool focus) { if (!focus) In.keys.Clear(); }

    void OnGUI() {
        Event e = Event.current;
        if (e.type == EventType.KeyDown && e.keyCode != KeyCode.None) {
            KeyCode k = e.keyCode == KeyCode.KeypadEnter ? KeyCode.Return : e.keyCode;
            if (In.keys.Add(k)) { In.pressed.Add(k); if (k == KeyCode.M) Sfx.Mute = !Sfx.Mute; }
        } else if (e.type == EventType.KeyUp && e.keyCode != KeyCode.None) {
            KeyCode k = e.keyCode == KeyCode.KeypadEnter ? KeyCode.Return : e.keyCode;
            In.keys.Remove(k);
        }
        if (e.type != EventType.Repaint || scene == null) return;
        Gfx.Begin();
        scene.Draw();
        if (Sfx.Mute) Gfx.Txt("MUTE", Gfx.W - 28, Gfx.H - 10, 10, Gfx.Hex("#ff8888"), 3);
        Gfx.End();
    }
}
