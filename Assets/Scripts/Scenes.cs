using System.Collections.Generic;
using UnityEngine;

public static class In {
    public static HashSet<KeyCode> keys = new HashSet<KeyCode>(), pressed = new HashSet<KeyCode>();
    public static bool Down(KeyCode c) { return keys.Contains(c); }
    public static bool Hit(params KeyCode[] cs) { foreach (KeyCode c in cs) if (pressed.Contains(c)) return true; return false; }
}

public abstract class GameScene {
    public virtual void Enter() { }
    public abstract void Update();
    public abstract void Draw();
}

public class Fx { public string k, s; public float x, y, vx, vy, t, life, h = -1; public int side; public Color col; public float[] px, py; }
public class Proj { public float x, y, vx; public int t; public Fighter o; public bool dead; }
public class PauseMenu { public int sel; public string[] items = { "RESUME", "MOVE LIST", "QUIT TO MENU" }; }

public static class Scenes {
    public static TitleScene Title = new TitleScene();
    public static CharSelScene CharSel = new CharSelScene();
    public static StageSelScene StageSel = new StageSelScene();
    public static VsScene VS = new VsScene();
    public static MoveListScene MoveList = new MoveListScene();
    public static ControlsScene Controls = new ControlsScene();
    public static FightScene Fight = new FightScene();
    public static FightResumeScene FightResume = new FightResumeScene();
    public static ResultScene Result = new ResultScene();
}

public static class Preview {
    public static void Draw(CharDef c, int face, float x, float y, float s, bool swap, int t, float h) {
        Fr f = c.F(c.Fm("idle", (t / 9) % 4));
        float sc = h > 0 ? h / f.h : s * c.Sc / 3f;
        Gfx.Frame(swap ? c.swap : c.tex, f, x, y, face, sc, 1f, Color.white);
    }
    public static CharDef Find(string id) { foreach (CharDef c in Data.Chars) if (c.id == id) return c; return Data.Chars[0]; }
}

public static class Bg {
    public static void Grad(float t) {
        Gfx.VGrad(0, 0, Gfx.W, Gfx.H, Gfx.Hex("#12000a"), Gfx.Hex("#3a0008"));
        Color c = Gfx.Hex("#ff3030", .08f);
        for (int i = -4; i < 20; i++) {
            float x = i * 50 + (t % 50);
            // slanted stripe: from (x+12,0) down to (x-102,H)
            float dx = -114f, len = Mathf.Sqrt(dx * dx + Gfx.H * Gfx.H);
            Gfx.Save(); Gfx.Translate(x + 12, 0); Gfx.Rotate(Mathf.Atan2(Gfx.H, dx)); Gfx.Fill(0, -12, len, 24, c); Gfx.Restore();
        }
    }
    static float[][] emb;
    public static void Embers() {
        if (emb == null) {
            emb = new float[55][];
            for (int i = 0; i < 55; i++) emb[i] = new float[] { Random.value * Gfx.W, Random.value * Gfx.H, .4f + Random.value * 1.3f, 1 + Random.value * 2.2f };
        }
        foreach (float[] m in emb) {
            m[1] -= m[2]; m[0] += Mathf.Sin((m[1] + m[3] * 50) / 25f) * .5f;
            if (m[1] < -4) { m[1] = Gfx.H + 4; m[0] = Random.value * Gfx.W; }
            Gfx.Fill(m[0], m[1], m[3], m[3] * 1.6f, Gfx.Rgba(255, 120 + (int)(m[3] * 30), 40, .35f + .25f * Mathf.Sin(Game.frame / 6f + m[0])));
        }
        if (Game.frame % 150 < 3) Gfx.Fill(0, 0, Gfx.W, Gfx.H, Gfx.Rgba(170, 190, 255, .22f));
    }
}

// ============================================================ TITLE
public class TitleScene : GameScene {
    string[] items = { "1 PLAYER", "2 PLAYERS", "TRAINING", "MOVE LIST", "CONTROLS", "DIFFICULTY" }; int sel;
    public override void Update() {
        int n = items.Length;
        if (In.Hit(KeyCode.W, KeyCode.UpArrow)) { sel = (sel + n - 1) % n; Sfx.Move(); }
        if (In.Hit(KeyCode.S, KeyCode.DownArrow)) { sel = (sel + 1) % n; Sfx.Move(); }
        string it = items[sel];
        if (it == "DIFFICULTY") {
            if (In.Hit(KeyCode.D, KeyCode.RightArrow, KeyCode.Return, KeyCode.Space)) { S.diff = (S.diff + 1) % 3; Sfx.Move(); }
            if (In.Hit(KeyCode.A, KeyCode.LeftArrow)) { S.diff = (S.diff + 2) % 3; Sfx.Move(); }
            return;
        }
        if (In.Hit(KeyCode.Return, KeyCode.Space, KeyCode.F, KeyCode.Comma)) {
            Sfx.Ok();
            if (it == "1 PLAYER") { S.mode = "1p"; Game.Go(Scenes.CharSel); }
            else if (it == "2 PLAYERS") { S.mode = "2p"; Game.Go(Scenes.CharSel); }
            else if (it == "TRAINING") { S.mode = "train"; Game.Go(Scenes.CharSel); }
            else if (it == "MOVE LIST") { Scenes.MoveList.back = Scenes.Title; Game.Go(Scenes.MoveList); }
            else if (it == "CONTROLS") Game.Go(Scenes.Controls);
        }
    }
    public override void Draw() {
        float W = Gfx.W, H = Gfx.H; int frame = Game.frame;
        StageDef st = Data.Stages[0]; Gfx.Stage(st, 0, 0, W, H, 0, 0, W, H);
        Gfx.Fill(0, 0, W, H, Gfx.Rgba(10, 0, 8, .72f));
        Preview.Draw(Preview.Find("batman"), 1, 120, 372, 5, false, frame, 0);
        Preview.Draw(Preview.Find("taker"), -1, W - 120, 372, 5, false, frame, 0);
        Bg.Embers();
        float pulse = .5f + .5f * Mathf.Sin(frame / 8f);
        Gfx.Glow(W / 2, 74, 190, Gfx.Rgba(255, 34, 0, .22f + .18f * pulse));
        Gfx.Gold("MYSTIC", W / 2, 46, 58, 8); Gfx.Gold("KOMBAT", W / 2, 98, 58, 8);
        Gfx.Txt("2D  EDITION", W / 2, 134, 18, Gfx.Hex("#ff5040"));
        for (int i = 0; i < items.Length; i++) {
            string it = items[i]; float y = 180 + i * 31; bool on = i == sel;
            string label = it == "DIFFICULTY" ? "DIFFICULTY:  < " + S.DIFFS[S.diff] + " >" : it;
            Gfx.Txt((on ? "> " : "") + label, W / 2, y, on ? 26 : 22, Gfx.Hex(on ? "#ffd23a" : "#dddddd"), 5);
        }
        Gfx.Txt("W/S or UP/DOWN select  |  ENTER confirm  |  M: mute", W / 2, 376, 12, Gfx.Hex("#aaaaaa"), 3);
    }
}

// ============================================================ CHARACTER SELECT
public class CharSelScene : GameScene {
    const int COLS = 4; int n; int[] ci = new int[2]; bool[] lockd = new bool[2]; int done, cpuT;
    public override void Enter() { n = Data.Chars.Length + 1; ci[0] = 0; ci[1] = 1; lockd[0] = lockd[1] = false; done = 0; cpuT = 0; }
    void MoveSel(int i, int dx, int dy) {
        if (lockd[i]) return;
        int C = COLS, rows = (n + C - 1) / C, c = ci[i] % C, r = ci[i] / C;
        c = (c + dx + C) % C; r = (r + dy + rows) % rows;
        int k = r * C + c;
        if (k >= n) k = dy != 0 ? (r == rows - 1 ? n - 1 : k % n) : (dx > 0 ? 0 : n - 1);
        ci[i] = Mathf.Min(k, n - 1); Sfx.Move();
    }
    void Pick(int i) { int c = ci[i]; S.p[i] = c >= Data.Chars.Length ? (int)(Random.value * Data.Chars.Length) % Data.Chars.Length : c; }
    public override void Update() {
        bool two = S.mode == "2p";
        if (!lockd[0]) {
            KeyCode[] L = two ? new KeyCode[] { KeyCode.A } : new KeyCode[] { KeyCode.A, KeyCode.LeftArrow };
            KeyCode[] R = two ? new KeyCode[] { KeyCode.D } : new KeyCode[] { KeyCode.D, KeyCode.RightArrow };
            KeyCode[] U = two ? new KeyCode[] { KeyCode.W } : new KeyCode[] { KeyCode.W, KeyCode.UpArrow };
            KeyCode[] D = two ? new KeyCode[] { KeyCode.S } : new KeyCode[] { KeyCode.S, KeyCode.DownArrow };
            if (In.Hit(L)) MoveSel(0, -1, 0); if (In.Hit(R)) MoveSel(0, 1, 0); if (In.Hit(U)) MoveSel(0, 0, -1); if (In.Hit(D)) MoveSel(0, 0, 1);
            bool ok = two ? In.Hit(KeyCode.F, KeyCode.G, KeyCode.Space) : In.Hit(KeyCode.F, KeyCode.G, KeyCode.Space, KeyCode.Return, KeyCode.Comma);
            if (ok) { lockd[0] = true; Sfx.Ok(); Pick(0); }
        }
        if (two && !lockd[1]) {
            if (In.Hit(KeyCode.LeftArrow)) MoveSel(1, -1, 0); if (In.Hit(KeyCode.RightArrow)) MoveSel(1, 1, 0);
            if (In.Hit(KeyCode.UpArrow)) MoveSel(1, 0, -1); if (In.Hit(KeyCode.DownArrow)) MoveSel(1, 0, 1);
            if (In.Hit(KeyCode.Comma, KeyCode.Period, KeyCode.Return)) { lockd[1] = true; Sfx.Ok(); Pick(1); }
        }
        if (!two && lockd[0] && !lockd[1]) {
            if (++cpuT > 45) { ci[1] = (int)(Random.value * Data.Chars.Length) % Data.Chars.Length; lockd[1] = true; Pick(1); }
            else if (cpuT % 4 == 0) ci[1] = (ci[1] + 1) % Data.Chars.Length;
        }
        if (lockd[0] && lockd[1]) { if (++done > 40) Game.Go(Scenes.StageSel); }
        if (In.Hit(KeyCode.Escape, KeyCode.Backspace)) {
            if (lockd[0] || lockd[1]) { lockd[0] = lockd[1] = false; cpuT = 0; done = 0; Sfx.Back(); }
            else { Sfx.Back(); Game.Go(Scenes.Title); }
        }
    }
    string[] Info(CharDef c) {
        if (!string.IsNullOrEmpty(c.type)) return new string[] { c.type, c.rng, c.ease };
        string t = c.dmg >= 1.12f ? "Power" : c.speed >= 2.9f ? "Speed" : "All-Rounder";
        return new string[] { t, c.id == "aquaman" ? "Long-Range" : (c.speed >= 2.9f || c.dmg >= 1.12f) ? "Close-Range" : "Mid-Range", c.dmg >= 1.12f ? "Easy" : "Medium" };
    }
    public override void Draw() {
        float W = Gfx.W, H = Gfx.H; int frame = Game.frame; int NC = Data.Chars.Length;
        StageDef st = Data.Stages[2 % Data.Stages.Length];
        Gfx.Stage(st, 0, 0, st.IW, st.IH, 0, 0, W, H);
        Gfx.Fill(0, 0, W, H, Gfx.Rgba(14, 4, 30, .80f));
        Gfx.Glow(90, 330, 200, Gfx.Rgba(255, 40, 120, .45f)); Gfx.Glow(W - 80, 120, 220, Gfx.Rgba(0, 200, 255, .4f)); Gfx.Glow(W / 2, 20, 160, Gfx.Rgba(160, 80, 255, .35f));
        for (float y = 0; y < H; y += 4) Gfx.Fill(0, y, W, 1, Gfx.Rgba(0, 0, 0, .18f));
        Gfx.Txt("CHARACTER SELECT", W / 2, 24, 28, Gfx.Hex("#e9b6ff"), 6, Gfx.Hex("#4a1a7a"), 0, false);
        int C = COLS; float tw = 62, th = 62, gap = 7, sk = 8, x0 = Mathf.Round((W - (tw * C + gap * (C - 1))) / 2) - 4, y0 = 50;
        for (int i = 0; i < n; i++) {
            float x = x0 + (i % C) * (tw + gap), y = y0 + (i / C) * (th + gap);
            Gfx.Tex(Gfx.ParaFill, x, y, tw + sk, th, Gfx.Hex("#1b0f33"));
            string nm;
            if (i < NC) {
                CharDef cd = Data.Chars[i]; Fr f = cd.F(cd.Fm("idle", 0));
                float sh = f.h * .6f, kk = (th - 2) / sh, sw = Mathf.Min(f.w, (tw + sk) / kk), sx = Mathf.Max(f.x, Mathf.Min(f.x + f.w - sw, f.x + f.px - sw / 2));
                Gfx.Img(cd.tex, sx, f.y, sw, sh, x + (tw + sk - sw * kk) / 2, y + 2, sw * kk, sh * kk);
                Gfx.VGrad(x, y + th - 18, tw + sk, 18, Gfx.Rgba(10, 0, 25, 0), Gfx.Rgba(10, 0, 25, .85f));
                nm = cd.name.Replace("THE ", "").Replace("ALLEN O'NEIL", "ALLEN").Replace("UNDERTAKER", "TAKER");
            } else { Gfx.Fx("q", x + tw / 2 + 4, y + th / 2, 1.1f); nm = "RANDOM"; }
            Gfx.Tex(Gfx.ParaLine, x, y, tw + sk, th, Gfx.Rgba(190, 160, 255, .55f));
            Gfx.Txt(nm.Length > 11 ? nm.Substring(0, 11) : nm, x + tw / 2 + 4, y + th - 6, 9, Color.white, 3);
        }
        float pulse = .6f + .4f * Mathf.Sin(frame / 5f);
        for (int p = 0; p < 2; p++) {
            int i = ci[p]; float x = x0 + (i % C) * (tw + gap), y = y0 + (i / C) * (th + gap);
            Color col = Gfx.Hex(p == 1 ? "#3a9cff" : "#ff3050"); float o = p == 1 ? 3 : 0;
            Gfx.Tex(Gfx.ParaLine3, x - 5 + o, y - 5 + o, tw + sk + 6, th + 6, new Color(col.r, col.g, col.b, (lockd[p] ? .5f : .3f * pulse)));
            Gfx.Tex(Gfx.ParaLine3, x - 2 + o, y - 2 + o, tw + sk, th, col);
            string label = p == 1 ? (S.mode == "2p" ? "P2" : "CPU") : "P1";
            float lx = p == 1 ? x + tw - 4 : x + 18, ly = y + 10;
            Gfx.Fill(lx - 15, ly - 7, 30, 14, col); Gfx.Txt(label, lx, ly, 10, Color.white, 2, Gfx.Rgba(0, 0, 0, .5f), 0, false);
            bool left = p == 0; float bx = left ? 100 : W - 100; int al = left ? -1 : 1; float tx = left ? 14 : W - 14;
            if (i < NC) {
                CharDef c = Data.Chars[i]; Fr f0 = c.F(c.Fm("idle", 0));
                float hh = Mathf.Min(c.sc > 0 ? Mathf.Min(270f, f0.h * c.sc * 2.1f) : 240f, 165f * f0.h / f0.w);
                Preview.Draw(c, left ? 1 : -1, bx, 346, 4, p == 1 && ci[0] == ci[1], frame, hh);
                Gfx.TxtA(c.name, tx, 52, Mathf.Min(32f, 168f / (c.name.Length * .66f)), Color.white, 6, al);
                Gfx.TxtA(c.title, tx, 74, 10, col, 3, al);
                if (left || lockd[1] || S.mode == "2p") {
                    string[] inf = Info(c); string[] lab = { "Type", "Effective Range", "Ease of Use" };
                    for (int k = 0; k < 3; k++) {
                        float yy = 276 + k * 30;
                        Gfx.Fill(left ? 8 : W - 158, yy - 10, 150, 28, new Color(0, 0, 0, .45f));
                        Gfx.TxtA(lab[k], tx, yy, 9, Gfx.Hex("#b9a6ff"), 2, al);
                        Gfx.TxtA(inf[k], tx, yy + 12, 14, Color.white, 3, al);
                    }
                }
            } else Gfx.GoldA("RANDOM", tx, 52, 32, 6, al);
            string stat = lockd[p] ? "READY!" : (p == 1 ? (S.mode == "2p" ? "P2 CHOOSING..." : lockd[0] ? "CPU CHOOSING..." : "CPU") : S.mode == "2p" ? "P1 CHOOSING..." : "CHOOSE YOUR FIGHTER");
            Gfx.TxtA(stat, tx, 94, 11, Gfx.Hex(lockd[p] ? "#7dff9a" : "#dddddd"), 3, al);
        }
        Gfx.Txt("Arrows / WASD move  |  F / ENTER confirm  |  ESC back", W / 2, 382, 10, Gfx.Hex("#aaaaaa"), 3);
    }
}

// ============================================================ STAGE SELECT
public class StageSelScene : GameScene {
    int i;
    public override void Enter() { i = 0; }
    public override void Update() {
        int n = Data.Stages.Length + 1;
        if (In.Hit(KeyCode.A, KeyCode.LeftArrow)) { i = (i + n - 1) % n; Sfx.Move(); }
        if (In.Hit(KeyCode.D, KeyCode.RightArrow)) { i = (i + 1) % n; Sfx.Move(); }
        if (In.Hit(KeyCode.W, KeyCode.UpArrow)) { i = (i - 5 + n) % n; Sfx.Move(); }
        if (In.Hit(KeyCode.S, KeyCode.DownArrow)) { i = (i + 5) % n; Sfx.Move(); }
        if (In.Hit(KeyCode.Return, KeyCode.Space, KeyCode.F, KeyCode.Comma)) {
            Sfx.Ok(); S.stage = i == Data.Stages.Length ? (int)(Random.value * Data.Stages.Length) % Data.Stages.Length : i; Game.Go(Scenes.VS);
        }
        if (In.Hit(KeyCode.Escape, KeyCode.Backspace)) { Sfx.Back(); Game.Go(Scenes.CharSel); }
    }
    public override void Draw() {
        float W = Gfx.W; int frame = Game.frame;
        Bg.Grad(frame);
        Gfx.Gold("CHOOSE YOUR ARENA", W / 2, 30, 32, 6);
        int n = Data.Stages.Length + 1; float tw = 114, th = 66, gap = 10, x0 = (W - (tw * 5 + gap * 4)) / 2, y0 = 66;
        for (int k = 0; k < n; k++) {
            float x = x0 + (k % 5) * (tw + gap), y = y0 + (k / 5) * (th + gap + 8);
            Gfx.Fill(x - 3, y - 3, tw + 6, th + 6, Color.black);
            if (k < Data.Stages.Length) {
                StageDef st = Data.Stages[k]; float sw = Mathf.Min(st.IW, st.IH * tw / th), sx = (st.IW - sw) / 2;
                Gfx.Stage(st, sx, 0, sw, st.IH, x, y, tw, th);
            } else { Gfx.Fill(x, y, tw, th, Gfx.Hex("#220011")); Gfx.Fx("q", x + tw / 2, y + th / 2, 1.1f); }
            if (k == i) Gfx.StrokeRect(x - 5, y - 5, tw + 10, th + 10, 4, Gfx.Hex(frame % 24 < 12 ? "#ffd23a" : "#ff7a1a"));
        }
        Gfx.Gold(i == Data.Stages.Length ? "RANDOM ARENA" : Data.Stages[i].name, W / 2, 262, 28, 6);
        Gfx.Txt(Data.Chars[S.p[0]].name + "  VS  " + Data.Chars[S.p[1]].name, W / 2, 300, 16, Gfx.Hex("#dddddd"));
        Gfx.Txt("Arrows / WASD choose  |  ENTER confirm  |  ESC back", W / 2, 376, 12, Gfx.Hex("#aaaaaa"), 3);
    }
}

// ============================================================ VERSUS SPLASH
public class VsScene : GameScene {
    int t;
    public override void Enter() { t = 0; S.same = S.p[0] == S.p[1]; Sfx.Play("you", .7f); }
    public override void Update() {
        t++;
        if (t > 150 || (t > 30 && In.Hit(KeyCode.Return, KeyCode.Space, KeyCode.F))) { Scenes.Fight.Setup(); Game.Go(Scenes.Fight); }
    }
    public override void Draw() {
        float W = Gfx.W; int frame = Game.frame;
        Bg.Grad(frame * 3);
        float k = Mathf.Min(1f, t / 20f), off = (1 - k) * 300;
        Preview.Draw(Data.Chars[S.p[0]], 1, 150 - off, 320, 6, false, frame, 0);
        Preview.Draw(Data.Chars[S.p[1]], -1, W - 150 + off, 320, 6, S.same, frame, 0);
        Gfx.Fx("vs", W / 2, 150, 5 + Mathf.Sin(t / 5f) * .3f);
        Gfx.Gold(Data.Chars[S.p[0]].name, 150, 350, 26, -1); Gfx.Gold(Data.Chars[S.p[1]].name, W - 150, 350, 26, -1);
        Gfx.Txt(Data.Stages[S.stage].name, W / 2, 360, 16, Gfx.Hex("#cccccc"));
        Gfx.Txt(S.mode == "1p" ? "CPU: " + S.DIFFS[S.diff] : S.mode == "train" ? "TRAINING" : "PLAYER 2", W - 150, 372, 11, Gfx.Hex("#99bbbb"), 3);
    }
}

// ============================================================ MOVE LIST / CONTROLS
public class MoveListScene : GameScene {
    public GameScene back;
    public override void Update() {
        if (In.Hit(KeyCode.Escape, KeyCode.Return, KeyCode.Backspace, KeyCode.Space)) { Sfx.Back(); Game.Go(back != null ? back : Scenes.Title); }
    }
    public override void Draw() {
        float W = Gfx.W;
        Bg.Grad(Game.frame); Gfx.Gold("MOVE LIST", W / 2, 28, 32, 6);
        string[][] rows = {
            new[] { "PUNCH", "F  /  ,", "Fast high jab. Can be ducked." },
            new[] { "KICK", "G  /  .", "Mid kick, longer reach." },
            new[] { "UPPERCUT", "DOWN + PUNCH", "Launches opponent. Beats jumpers." },
            new[] { "LOW KICK", "DOWN + KICK", "Must be blocked while crouching." },
            new[] { "THROW", "BLOCK + PUNCH (close)", "Unblockable. Flips opponent behind you." },
            new[] { "JUMP PUNCH / KICK", "JUMP, then PUNCH / KICK", "Air attacks; jump kick hits hardest." },
            new[] { "SPECIAL 1", "DOWN, FORWARD + PUNCH", "Rush strike, knockdown. Costs 30 power." },
            new[] { "SPECIAL 2", "BACK, FORWARD + KICK", "Sliding kick, knockdown. Costs 30 power." },
            new[] { "SPECIAL 3", "DOWN, FORWARD + KICK", "Energy blast projectile. Costs 30 power." },
            new[] { "BLOCK", "H  /  /  (hold)", "Hold DOWN too to guard low attacks." },
            new[] { "TAUNT", "T  /  ;", "Gives a little power." } };
        for (int i = 0; i < rows.Length; i++) {
            float y = 60 + i * 27;
            Gfx.TxtA(rows[i][0], 24, y, 15, Gfx.Hex("#ffd23a"), 3, -1);
            Gfx.TxtA(rows[i][1], 190, y, 13, Color.white, 3, -1);
            Gfx.TxtA(rows[i][2], 190, y + 13, 10, Gfx.Hex("#aaaaaa"), 2, -1);
        }
        CharDef a = Data.Chars[0], b = Data.Chars[1];
        Gfx.Txt(a.name + ": " + a.sp1 + " / " + a.sp2, W / 2, 366, 12, Gfx.Hex("#ff9a7a"), 3);
        Gfx.Txt(b.name + ": " + b.sp1 + " / " + b.sp2, W / 2, 380, 12, Gfx.Hex("#9ab0ff"), 3);
    }
}

public class ControlsScene : GameScene {
    public override void Update() { if (In.Hit(KeyCode.Escape, KeyCode.Return, KeyCode.Backspace, KeyCode.Space)) { Sfx.Back(); Game.Go(Scenes.Title); } }
    public override void Draw() {
        float W = Gfx.W;
        Bg.Grad(Game.frame); Gfx.Gold("CONTROLS", W / 2, 36, 34, 6);
        string[][] rows = {
            new[] { "", "PLAYER 1", "PLAYER 2" }, new[] { "MOVE", "A / D", "LEFT / RIGHT" }, new[] { "JUMP", "W", "UP" }, new[] { "DUCK", "S", "DOWN" },
            new[] { "PUNCH", "F", "," }, new[] { "KICK", "G", "." }, new[] { "BLOCK", "H", "/" }, new[] { "TAUNT", "T", ";" } };
        for (int i = 0; i < rows.Length; i++) {
            float y = 90 + i * 32;
            Gfx.TxtA(rows[i][0], 140, y, 18, Gfx.Hex("#ffd23a"), 4, -1);
            Gfx.Txt(rows[i][1], 320, y, 18, Gfx.Hex(i > 0 ? "#ffffff" : "#ff6a5a"), 4);
            Gfx.Txt(rows[i][2], 470, y, 18, Gfx.Hex(i > 0 ? "#ffffff" : "#6fb0ff"), 4);
        }
        Gfx.Txt("P  or  ESC pauses the fight", W / 2, 350, 14, Gfx.Hex("#bbbbbb"), 3);
        Gfx.Txt("In 1-player / training modes, either key set controls you.", W / 2, 368, 12, Gfx.Hex("#999999"), 3);
    }
}

public class FightResumeScene : GameScene {
    public override void Update() {
        if (In.Hit(KeyCode.Escape, KeyCode.Return, KeyCode.Backspace, KeyCode.Space)) { Sfx.Back(); Game.scene = Scenes.Fight; Scenes.Fight.pause = new PauseMenu(); }
    }
    public override void Draw() { Scenes.MoveList.Draw(); }
}

// ============================================================ RESULT
public class ResultScene : GameScene {
    public int winner; int sel, t;
    public override void Enter() { sel = 0; t = 0; Sfx.Play("you", .8f); }
    public override void Update() {
        t++;
        if (In.Hit(KeyCode.W, KeyCode.UpArrow)) { sel = (sel + 2) % 3; Sfx.Move(); }
        if (In.Hit(KeyCode.S, KeyCode.DownArrow)) { sel = (sel + 1) % 3; Sfx.Move(); }
        if (t > 20 && In.Hit(KeyCode.Return, KeyCode.Space, KeyCode.F, KeyCode.Comma)) {
            Sfx.Ok();
            if (sel == 0) { Scenes.Fight.Setup(); Game.Go(Scenes.Fight); } else if (sel == 1) Game.Go(Scenes.CharSel); else Game.Go(Scenes.Title);
        }
    }
    public override void Draw() {
        float W = Gfx.W;
        Bg.Grad(Game.frame);
        Fighter w = Scenes.Fight.f[winner];
        Preview.Draw(w.ch, 1, W / 2, 270, 6, w.sheet != w.ch.tex, Game.frame, 0);
        Gfx.Gold(w.ch.name + " WINS", W / 2, 50, 46, 8);
        Gfx.Txt(S.mode == "1p" ? (winner == 0 ? "YOU DEFEATED THE CPU" : "THE CPU WINS THIS ONE") : "PLAYER " + (winner + 1) + " IS VICTORIOUS", W / 2, 90, 16, Gfx.Hex("#dddddd"));
        string[] its = { "REMATCH", "CHARACTER SELECT", "MAIN MENU" };
        for (int i = 0; i < 3; i++) Gfx.Txt((i == sel ? "> " : "") + its[i], W / 2, 300 + i * 28, i == sel ? 24 : 20, Gfx.Hex(i == sel ? "#ffd23a" : "#dddddd"), 5);
    }
}
