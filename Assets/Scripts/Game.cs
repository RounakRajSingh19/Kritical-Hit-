using UnityEngine;
// Zero-setup bootstrap: just press Play in ANY scene.
public class Game : MonoBehaviour {
  public static Game I;
  enum M { Menu, Fight, Over }
  M mode = M.Menu; int p1, p2 = 1, stg; bool vs2P; Fighter a, b; GameObject world; Camera cam; AudioSource au;
  float shake, camX, stageW, aiT, holdT, msgT; Inp ai; string msg = ""; int w1, w2, round; float over;
  [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
  static void Boot() { var g = new GameObject("Game"); g.AddComponent<Game>(); }
  void Awake() {
    I = this; Data.Load(); au = gameObject.AddComponent<AudioSource>();
    cam = Camera.main; if (!cam) { var c = new GameObject("Cam"); cam = c.AddComponent<Camera>(); c.tag = "MainCamera"; }
    cam.orthographic = true; cam.orthographicSize = Data.H / 200f; cam.backgroundColor = Color.black; cam.clearFlags = CameraClearFlags.SolidColor;
    cam.transform.position = new Vector3(3.2f, 1.96f, -10);
  }
  public void Play(string n) { if (Data.Snd.TryGetValue(n, out var c) && c) au.PlayOneShot(c); }
  public void Shake(float s) { shake = Mathf.Max(shake, s); }
  void StartMatch() { w1 = w2 = round = 0; NewRound(); }
  void NewRound() {
    if (world) Destroy(world); world = new GameObject("World");
    var sd = Data.Stages[stg]; var bg = new GameObject("BG"); bg.transform.SetParent(world.transform);
    bg.AddComponent<SpriteRenderer>().sprite = sd.bg; stageW = sd.bg.rect.width * sd.s;
    float gy = sd.gy; a = Make(Data.Chars[p1], gy, stageW / 2 - 100, 1); b = Make(Data.Chars[p2], gy, stageW / 2 + 100, -1);
    a.foe = b; b.foe = a; mode = M.Fight; msg = "ROUND " + (++round); msgT = 1.2f; over = 0; Play("you");
  }
  Fighter Make(CharDef c, float gy, float x, int f) {
    var o = new GameObject(c.name); o.transform.SetParent(world.transform); var fi = o.AddComponent<Fighter>(); fi.Init(c, gy, x, f, 25, stageW - 25); return fi;
  }
  Inp P1() => new Inp { dir = (Input.GetKey(KeyCode.D) ? 1 : 0) - (Input.GetKey(KeyCode.A) ? 1 : 0), up = Input.GetKeyDown(KeyCode.W), down = Input.GetKey(KeyCode.S), block = Input.GetKey(KeyCode.H), punch = Input.GetKeyDown(KeyCode.F), kick = Input.GetKeyDown(KeyCode.G), special = Input.GetKeyDown(KeyCode.T) };
  Inp P2() => new Inp { dir = (Input.GetKey(KeyCode.RightArrow) ? 1 : 0) - (Input.GetKey(KeyCode.LeftArrow) ? 1 : 0), up = Input.GetKeyDown(KeyCode.UpArrow), down = Input.GetKey(KeyCode.DownArrow), block = Input.GetKey(KeyCode.Slash), punch = Input.GetKeyDown(KeyCode.Comma), kick = Input.GetKeyDown(KeyCode.Period), special = Input.GetKeyDown(KeyCode.Semicolon) };
  Inp AI() { // simple CPU brain
    aiT -= Time.deltaTime; var i = new Inp();
    if (holdT > 0) { holdT -= Time.deltaTime; i = ai; i.punch = i.kick = i.up = i.special = false; }
    if (aiT > 0 || b.st != St.Free) return i;
    aiT = Random.Range(.15f, .4f); float d = Mathf.Abs(a.x - b.x), to = a.x > b.x ? 1 : -1, r = Random.value; ai = new Inp();
    if (d > 200) { ai.dir = to; holdT = .5f; if (r < .15 && b.power >= 30) ai.special = true; }
    else if (d > 100) { ai.dir = to; holdT = .25f; if (r < .3) ai.kick = true; if (r > .9) ai.up = true; }
    else { if (r < .3) ai.punch = true; else if (r < .5) ai.kick = true; else if (r < .6) { ai.punch = true; ai.down = true; } else if (r < .7) { ai.punch = true; ai.up = true; } else if (r < .85) { ai.block = true; holdT = .4f; } else { ai.dir = -to; holdT = .3f; } }
    return ai.punch || ai.kick || ai.special || ai.up ? ai : i;
  }
  void FixedUpdate() { } // game logic is frame-locked to 60fps below
  float acc;
  void Update() {
    if (mode == M.Menu) return;
    acc += Time.deltaTime; int steps = 0;
    while (acc >= 1f / 60 && steps++ < 4) { acc -= 1f / 60; Step(); }
    if (msgT > 0) msgT -= Time.deltaTime;
    if (a && b) { camX = Mathf.Lerp(camX, Mathf.Clamp((a.x + b.x) / 2, 320, Mathf.Max(320, stageW - 320)), .15f);
      var sh = shake > 0 ? (Vector3)Random.insideUnitCircle * shake : Vector3.zero; shake = Mathf.Max(0, shake - .01f);
      cam.transform.position = new Vector3(camX / 100, 1.96f, -10) + sh; }
    if (Input.GetKeyDown(KeyCode.Escape)) { mode = M.Menu; if (world) Destroy(world); cam.transform.position = new Vector3(3.2f, 1.96f, -10); }
  }
  void Step() {
    if (mode == M.Over || msgT > .4f) { a.Tick(new Inp()); b.Tick(new Inp()); return; }
    a.Tick(P1()); b.Tick(vs2P ? P2() : AI());
    foreach (var f in new[] { a, b }) f.x = Mathf.Clamp(f.x, camX - 300, camX + 300);
    if (Mathf.Abs(a.x - b.x) < 24) { float p = (24 - Mathf.Abs(a.x - b.x)) / 2; float s = a.x < b.x ? 1 : -1; a.x -= p * s; b.x += p * s; }
    if (mode == M.Fight && (a.Dead || b.Dead || (a.hp <= 0 && a.st == St.Lie) || (b.hp <= 0 && b.st == St.Lie))) {
      bool aw = b.hp <= 0; if (aw) w1++; else w2++; (aw ? a : b).Win(); Play(w1 == 2 || w2 == 2 ? "final" : "win");
      msg = (aw ? Data.Chars[p1].name : Data.Chars[p2].name) + " WINS!"; msgT = 99; over = 3f; mode = M.Over;
    }
    if (mode == M.Over) { over -= 1f / 60; if (over <= 0) { if (w1 == 2 || w2 == 2) { mode = M.Menu; Destroy(world); cam.transform.position = new Vector3(3.2f, 1.96f, -10); } else NewRound(); } }
  }
  void Bar(Rect r, float v, Color c, bool right) {
    GUI.color = Color.black; GUI.DrawTexture(r, Texture2D.whiteTexture); GUI.color = c;
    float w = (r.width - 4) * Mathf.Clamp01(v); GUI.DrawTexture(new Rect(right ? r.xMax - 2 - w : r.x + 2, r.y + 2, w, r.height - 4), Texture2D.whiteTexture); GUI.color = Color.white;
  }
  void OnGUI() {
    float s = Screen.height / 392f; var big = new GUIStyle(GUI.skin.label) { fontSize = (int)(34 * s), fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
    if (mode == M.Menu) {
      var lab = new GUIStyle(GUI.skin.label) { fontSize = 18, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
      GUILayout.BeginArea(new Rect(20, 10, Screen.width - 40, Screen.height - 20));
      GUILayout.Label("MYSTIC KOMBAT 2D", new GUIStyle(lab) { fontSize = 30 });
      var names = System.Array.ConvertAll(Data.Chars, c => c.name);
      GUILayout.Label("PLAYER 1  (A/D move, W jump, S crouch, F punch, G kick, H block, T special)", lab); p1 = GUILayout.SelectionGrid(p1, names, 7, GUILayout.Height(60));
      GUILayout.Label(vs2P ? "PLAYER 2  (Arrows, , . / ;)" : "CPU OPPONENT", lab); p2 = GUILayout.SelectionGrid(p2, names, 7, GUILayout.Height(60));
      GUILayout.Label("STAGE", lab); stg = GUILayout.SelectionGrid(stg, System.Array.ConvertAll(Data.Stages, x => x.name), 6, GUILayout.Height(40));
      vs2P = GUILayout.Toggle(vs2P, " 2-Player mode");
      if (GUILayout.Button("FIGHT!", GUILayout.Height(50))) StartMatch();
      GUILayout.EndArea(); return;
    }
    if (!a) return;
    float w = Screen.width, bw = w * .38f, y = 12 * s;
    Bar(new Rect(20, y, bw, 22 * s), a.hp / 100, Color.green, false); Bar(new Rect(w - 20 - bw, y, bw, 22 * s), b.hp / 100, Color.green, true);
    Bar(new Rect(20, y + 26 * s, bw * .6f, 10 * s), a.power / 100, Color.cyan, false); Bar(new Rect(w - 20 - bw * .6f, y + 26 * s, bw * .6f, 10 * s), b.power / 100, Color.cyan, true);
    GUI.Label(new Rect(20, y + 38 * s, 300, 24), a.ch.name + "  W:" + w1); GUI.Label(new Rect(w - 320, y + 38 * s, 300, 24), "W:" + w2 + "  " + b.ch.name, new GUIStyle(GUI.skin.label) { alignment = TextAnchor.UpperRight });
    if (msgT > 0) { GUI.color = Color.black; GUI.Label(new Rect(2, Screen.height * .35f + 2, w, 80), msg, big); GUI.color = Color.yellow; GUI.Label(new Rect(0, Screen.height * .35f, w, 80), msg, big); GUI.color = Color.white; }
  }
}
