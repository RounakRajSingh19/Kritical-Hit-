using System.Collections.Generic;
using UnityEngine;

public class FightScene : GameScene {
    public static FightScene I;
    public FightScene() { I = this; }
    const float W = Gfx.W, H = Gfx.H;

    public Fighter[] f = new Fighter[2];
    StageDef st; Human c0, c1h; CpuAI c1a;
    public int round, hitstop, sflash, slowmo, pt, tick, timer, lastDmg;
    public float shake, zoom, camx;
    public List<Fx> fx = new List<Fx>(); List<Proj> proj = new List<Proj>();
    public PauseMenu pause; bool matchOver, flawless; string phase, banner = "", sub = "", lb = ""; int bt; Fighter winner;
    float[][] motes;

    public static KeyMap P1 = new KeyMap { l = KeyCode.A, r = KeyCode.D, u = KeyCode.W, d = KeyCode.S, p = KeyCode.F, k = KeyCode.G, b = KeyCode.H, t = KeyCode.T };
    public static KeyMap P2 = new KeyMap { l = KeyCode.LeftArrow, r = KeyCode.RightArrow, u = KeyCode.UpArrow, d = KeyCode.DownArrow, p = KeyCode.Comma, k = KeyCode.Period, b = KeyCode.Slash, t = KeyCode.Semicolon };

    public void Setup() {
        st = Data.Stages[S.stage];
        f[0] = new Fighter(Data.Chars[S.p[0]], 0, Data.Chars[S.p[0]].tex);
        bool same = S.p[0] == S.p[1];
        f[1] = new Fighter(Data.Chars[S.p[1]], 1, same ? Data.Chars[S.p[1]].swap : Data.Chars[S.p[1]].tex);
        bool two = S.mode == "2p";
        c0 = two ? new Human(P1) : new Human(P1, P2);
        c1h = null; c1a = null;
        if (two) c1h = new Human(P2); else if (S.mode != "train") c1a = new CpuAI(S.diff);
        round = 0; fx.Clear(); proj.Clear(); hitstop = 0; shake = 0; pause = null; matchOver = false;
        camx = 0; lastDmg = 0; zoom = 0; sflash = 0; slowmo = 0; lb = "";
        NewRound();
    }

    void NewRound() {
        round++; float mid = st.World / 2;
        f[0].Reset(mid - 120, st.gy, 1); f[1].Reset(mid + 120, st.gy, -1);
        camx = Mathf.Max(0, Mathf.Min(st.World - W, mid - W / 2));
        phase = "intro"; pt = 0; timer = 99; tick = 0; fx.Clear(); proj.Clear(); banner = ""; sub = ""; winner = null; flawless = false;
    }

    // ---------------------------------------------------------------- effects
    public void Dust(float x, float y) {
        fx.Add(new Fx { k = "puff", x = x, y = y + 2, life = 22 });
        for (int i = 0; i < 6; i++) fx.Add(new Fx { k = "dust", x = x + (Random.value - .5f) * 40, y = y - 2, vx = (Random.value - .5f) * 2, vy = -Random.value * 1.2f, life = 20 });
    }
    void Spark(float x, float y, string k) {
        fx.Add(new Fx { k = k, x = x, y = y, life = k == "block" ? 10 : 12 });
        int n = k == "hit" ? 12 : 5;
        for (int i = 0; i < n; i++)
            fx.Add(new Fx { k = "ember", x = x, y = y, vx = (Random.value - .5f) * (k == "hit" ? 11 : 7), vy = (Random.value - .8f) * 7, life = 16 + (int)(Random.value * 14), h = k == "block" ? 200 : -1 });
        if (k == "hit") fx.Add(new Fx { k = "ring", x = x, y = y, life = 14 });
    }
    public void Callout(Fighter a) {
        string nm = a.mname == "dash" ? a.ch.sp1 : a.mname == "slide" ? a.ch.sp2 : "ENERGY BLAST";
        fx.Add(new Fx { k = "call", side = a.side, s = nm, col = Gfx.Hex(a.side == 1 ? "#3a9cff" : "#ff3050"), life = 56 });
        zoom = Mathf.Max(zoom, .03f); sflash = 2; hitstop = Mathf.Max(hitstop, 5);
    }
    public void DoThrow(Fighter a, Fighter d) {
        Sfx.Play("bigPunch", .9f); hitstop = 8; shake = 12; zoom = .06f; sflash = 3;
        d.move = null; d.state = St.Fall; d.x = a.x + a.facing * 28; d.y = d.gy - 40; d.vy = -9; d.vx = -a.facing * 4.2f; d.facing = a.facing; d.bounced = false; d.flash = 5;
        float dmg = Moves.M["throw"].dmg * a.ch.dmg;
        d.hp = Mathf.Max(0, d.hp - dmg); a.power = Mathf.Min(100, a.power + 10); d.power = Mathf.Min(100, d.power + 6);
        a.combo = 1; Spark(d.x, d.y - 60, "hit"); lastDmg = (int)dmg;
    }

    void TryHit(Fighter a, Fighter d) {
        Move m = a.move;
        if (m == null || m.thr || m.box == null || a.moveHit) return;
        if (a.mt < m.hs || a.mt > m.he) return;
        if (d.inv > 0 || d.state == St.Fall || d.state == St.Lie || d.state == St.GetUp || d.state == St.Dead) return;
        float ax0 = a.x + a.facing * m.box[0], ax1 = a.x + a.facing * m.box[1];
        float hx0 = Mathf.Min(ax0, ax1), hx1 = Mathf.Max(ax0, ax1), hy0 = a.y - m.box[3], hy1 = a.y - m.box[2];
        float h = d.crouch ? 72 : 118, dx0 = d.x - 22, dx1 = d.x + 22, dy0 = d.y - h, dy1 = d.y;
        if (hx1 < dx0 || hx0 > dx1 || hy1 < dy0 || hy0 > dy1) return;
        a.moveHit = true;
        int dir = a.x < d.x ? 1 : -1;
        bool blocked = d.state == St.Free && d.block && d.move == null && !d.Air && !(m.lvl == 'l' && !d.crouch);
        float cx = (Mathf.Max(hx0, dx0) + Mathf.Min(hx1, dx1)) / 2, cy = (Mathf.Max(hy0, dy0) + Mathf.Min(hy1, dy1)) / 2;
        if (blocked) {
            d.hp = Mathf.Max(1, d.hp - m.dmg * 0.08f); d.state = St.Hurt; d.stun = (int)m.bstun; d.vx = dir * m.kb * .7f; d.blockedHit = true; d.facing = -dir;
            a.vx = -dir * m.kb * .4f; a.power = Mathf.Min(100, a.power + 2); d.power = Mathf.Min(100, d.power + 2);
            Spark(cx, cy, "block"); Sfx.Play("littleKick", .35f); hitstop = 3; return;
        }
        d.blockedHit = false;
        a.combo = d.state == St.Hurt ? a.combo + 1 : 1; a.comboT = 100;
        float scale = Mathf.Max(.5f, 1 - .1f * (a.combo - 1)); bool crit = Random.value < .1f;
        float dmg = m.dmg * a.ch.dmg * scale * (crit ? 1.5f : 1f);
        d.hp = Mathf.Max(0, d.hp - dmg); lastDmg = Mathf.RoundToInt(dmg);
        a.power = Mathf.Min(100, a.power + dmg * .9f); d.power = Mathf.Min(100, d.power + dmg * .6f);
        d.facing = -dir; d.move = null; d.crouch = false;
        if (m.kd || d.hp <= 0 || d.Air) {
            d.state = St.Fall; d.y = Mathf.Min(d.y, d.gy - 1); d.vy = m.launch != 0 ? m.launch : -6.5f; d.vx = dir * (m.kd ? 4.2f : 3f); d.bounced = false; d.flash = 5;
            shake = 10; hitstop = 9; zoom = Mathf.Max(zoom, .035f); sflash = Mathf.Max(sflash, 2);
        } else {
            d.state = St.Hurt; d.flash = 4; d.stun = (int)m.stun; d.vx = dir * m.kb; d.hurtKind = m.lvl == 'h' ? 1 : 0; hitstop = 5; shake = 3;
        }
        Spark(cx, cy, "hit"); Sfx.Play(m.snd == 'h' ? "bigPunch" : "littleKick", .8f);
        if (crit) {
            Spark(cx, cy, "hit"); shake = Mathf.Max(shake, 12); hitstop += 4; sflash = 4; zoom = Mathf.Max(zoom, .05f);
            fx.Add(new Fx { k = "txt", x = cx, y = cy - 30, life = 40, s = "CRITICAL!" });
        }
        if (d.hp <= 0) Sfx.Play("final", .8f);
    }

    public void Shoot(Fighter a) { proj.Add(new Proj { x = a.x + a.facing * 50, y = a.y - 62, vx = a.facing * 7, o = a }); Sfx.Play("littleKick", .7f); }

    static bool Down(Fighter d) { return d.state == St.Fall || d.state == St.Lie || d.state == St.GetUp || d.state == St.Dead; }

    void UpdProj() {
        foreach (Proj p in proj) {
            p.x += p.vx; p.t++;
            if (p.t % 2 == 0) fx.Add(new Fx { k = "ember", x = p.x, y = p.y + (Random.value - .5f) * 14, vx = -Mathf.Sign(p.vx) * (1 + Random.value * 2), vy = (Random.value - .5f) * 2, life = 14, h = Fighter.HueOf(p.o.ch.id) });
            Fighter d = p.o == f[0] ? f[1] : f[0];
            if (Down(d) || d.inv > 0) continue;
            float h = d.crouch ? 72 : 118;
            if (Mathf.Abs(p.x - d.x) > 30 || p.y < d.y - h || p.y > d.y + 8) continue;
            p.dead = true; int dir = p.vx > 0 ? 1 : -1; bool blocked = d.state == St.Free && d.block && d.move == null && !d.Air;
            if (blocked) {
                d.hp = Mathf.Max(1, d.hp - 1); d.state = St.Hurt; d.stun = 12; d.vx = dir * 3; d.blockedHit = true; Spark(p.x, p.y, "block"); Sfx.Play("littleKick", .4f);
            } else {
                float dmg = 10 * p.o.ch.dmg; d.hp = Mathf.Max(0, d.hp - dmg); lastDmg = Mathf.RoundToInt(dmg);
                p.o.combo = d.state == St.Hurt ? p.o.combo + 1 : 1; p.o.comboT = 100;
                p.o.power = Mathf.Min(100, p.o.power + 6); d.power = Mathf.Min(100, d.power + 6); d.facing = -dir; d.move = null;
                d.state = St.Fall; d.y = Mathf.Min(d.y, d.gy - 1); d.vy = -6; d.vx = dir * 3.5f; d.bounced = false; d.flash = 5;
                shake = 8; hitstop = 7; Spark(p.x, p.y, "hit"); Sfx.Play("bigPunch", .8f); if (d.hp <= 0) Sfx.Play("final", .8f);
            }
        }
        proj.RemoveAll(p => p.dead || p.t >= 120 || p.x <= camx - 60 || p.x >= camx + W + 60);
    }

    // ---------------------------------------------------------------- update
    public override void Update() {
        if (pause != null) { UpdatePause(); return; }
        if (In.Hit(KeyCode.P, KeyCode.Escape)) { pause = new PauseMenu(); Sfx.Move(); return; }
        if (slowmo > 0) { slowmo--; if ((Game.frame & 1) != 0) return; }
        Fighter p1 = f[0], p2 = f[1]; pt++;
        if (shake > 0) shake *= .8f; zoom *= .86f; if (sflash > 0) sflash--;
        if (hitstop > 0) { hitstop--; return; }
        if (phase == "intro") {
            if (pt > 8 && pt < 110 && In.Hit(KeyCode.Return, KeyCode.Space, KeyCode.F, KeyCode.Comma)) pt = 110;
            if (pt == 1) banner = "ROUND " + round;
            if (pt == 70) { banner = "FIGHT!"; Sfx.Play("you", .6f); }
            if (pt == 120) { phase = "fight"; banner = ""; }
        }
        Inp i1 = new Inp(), i2 = new Inp();
        if (phase == "fight") {
            i1 = c0.Read(p1);
            i2 = c1a != null ? c1a.Read(p2, p1) : (c1h != null ? c1h.Read(p2) : new Inp());
            if (S.mode != "train" && ++tick % 60 == 0 && timer > 0) timer--;
        }
        p1.UpdateF(i1, p2, this); p2.UpdateF(i2, p1, this);
        if (!p1.Air && !p2.Air && (p1.state == St.Free || p1.state == St.Hurt) && (p2.state == St.Free || p2.state == St.Hurt)) {
            float dx = p2.x - p1.x, min = 46;
            if (Mathf.Abs(dx) < min) { float push = (min - Mathf.Abs(dx)) / 2, s = dx >= 0 ? 1 : -1; p1.x -= s * push; p2.x += s * push; }
        }
        TryHit(p1, p2); TryHit(p2, p1); UpdProj();
        float lo = Mathf.Max(36, camx + 36), hi = Mathf.Min(st.World - 36, camx + W - 36);
        foreach (Fighter ff in f) ff.x = Mathf.Max(lo, Mathf.Min(hi, ff.x));
        float mx = (p1.x + p2.x) / 2, target = Mathf.Max(0, Mathf.Min(st.World - W, mx - W / 2));
        camx += (target - camx) * .12f;
        foreach (Fx e in fx) { e.t++; if (e.k == "ember") e.vy += .35f; if (e.vx != 0) { e.x += e.vx; e.y += e.vy; } }
        fx.RemoveAll(e => e.t >= e.life);
        if (S.mode == "train") {
            if (p2.state == St.Dead || (p2.hp <= 0 && p2.state == St.Free)) { p2.hp = 100; p2.state = St.Free; }
            if (p1.hp <= 0) p1.hp = 100;
            if (p2.state == St.Free && p2.hp < 100 && p2.age % 240 == 0) p2.hp = 100;
            p1.power = Mathf.Max(p1.power, 60);
        }
        if (phase == "fight" && S.mode != "train") {
            if (p1.hp <= 0 || p2.hp <= 0 || timer <= 0) {
                phase = "ko"; pt = 0;
                if (timer <= 0 && p1.hp > 0 && p2.hp > 0) { banner = "TIME UP"; winner = p1.hp > p2.hp ? p1 : p2.hp > p1.hp ? p2 : null; }
                else {
                    banner = "K.O."; winner = (p1.hp <= 0 && p2.hp <= 0) ? null : (p1.hp <= 0 ? p2 : p1); Sfx.Play("final", .7f);
                    zoom = .09f; sflash = 7; slowmo = 90; if (winner != null && winner.wins + 1 >= 2) sub = "FINISH HIM!";
                }
            }
        } else if (phase == "ko") {
            if (pt > 110) {
                phase = "end"; pt = 0;
                Fighter w = winner;
                if (w != null) {
                    w.wins++; w.state = St.Win; w.move = null; w.vx = 0; flawless = w.hp >= 100;
                    banner = flawless ? "FLAWLESS VICTORY" : w.ch.name + " WINS"; Sfx.Play("win", .8f);
                } else { p1.wins++; p2.wins++; banner = "DRAW"; }
                matchOver = p1.wins >= 2 || p2.wins >= 2;
                if (matchOver && p1.wins == p2.wins) matchOver = false;
            }
        } else if (phase == "end") {
            if (pt > 170) {
                if (matchOver) { Scenes.Result.winner = p1.wins >= 2 ? 0 : 1; Game.Go(Scenes.Result); }
                else NewRound();
            }
        }
    }

    void UpdatePause() {
        PauseMenu p = pause;
        if (In.Hit(KeyCode.W, KeyCode.UpArrow)) { p.sel = (p.sel + 2) % 3; Sfx.Move(); }
        if (In.Hit(KeyCode.S, KeyCode.DownArrow)) { p.sel = (p.sel + 1) % 3; Sfx.Move(); }
        if (In.Hit(KeyCode.P, KeyCode.Escape)) { pause = null; return; }
        if (In.Hit(KeyCode.Return, KeyCode.Space, KeyCode.F, KeyCode.Comma)) {
            Sfx.Ok();
            if (p.sel == 0) pause = null;
            else if (p.sel == 1) { Scenes.MoveList.back = Scenes.FightResume; Game.Go(Scenes.MoveList); }
            else { pause = null; Game.Go(Scenes.Title); }
        }
    }

    // ---------------------------------------------------------------- draw
    public override void Draw() {
        Fighter p1 = f[0], p2 = f[1]; int frame = Game.frame;
        Gfx.Fill(0, 0, W, H, Color.black);
        Gfx.Save();
        float sh = shake > .5f ? shake : 0;
        Gfx.Translate(sh != 0 ? (Random.value - .5f) * sh : 0, sh != 0 ? (Random.value - .5f) * sh : 0);
        float z1 = 1 + zoom;
        Gfx.Translate(W / 2, H * .72f); Gfx.Scale(z1, z1); Gfx.Translate(-W / 2, -H * .72f);
        float cx = Mathf.Round(camx);
        Gfx.Stage(st, cx, 0, W, H, 0, 0, W, H);
        Gfx.Translate(-cx, 0);
        foreach (Fighter ff in f) { Gfx.Save(); Gfx.GA = .13f; Gfx.Translate(0, ff.gy * 2 + 4); Gfx.Scale(1, -1); ff.Draw(); Gfx.Restore(); }
        bool a = p1.state == St.Fall || p1.state == St.Hurt;
        (a ? p1 : p2).Draw(); (a ? p2 : p1).Draw();
        foreach (Fx e in fx) DrawFx(e);
        foreach (Proj p in proj) {
            float hue = Fighter.HueOf(p.o.ch.id), dir = Mathf.Sign(p.vx), r = 15 + Mathf.Sin(p.t / 2f) * 3;
            for (int i = 1; i <= 7; i++) Gfx.Circ(p.x - dir * i * 9, p.y + Mathf.Sin((p.t - i) / 2f) * 2, r * (1 - i * .08f), Gfx.Hsl(hue, 1, .6f, (1 - i / 8f) * .5f));
            Gfx.Glow(p.x, p.y, r * 2.2f, Gfx.Hsl(hue, 1, .7f, .9f)); Gfx.Circ(p.x, p.y, r * .5f, Color.white);
        }
        Gfx.Restore();
        Ambient(); DrawHUD(); Overlays();
        if (banner != "") {
            if (lb != banner) { lb = banner; bt = frame; if (banner == "FIGHT!" || banner == "K.O.") { shake = 14; sflash = 3; } }
            float t = frame - bt, k = Mathf.Min(1f, t / 10f), e = 1 + 2.2f * Mathf.Pow(1 - k, 3) + Mathf.Sin(k * Mathf.PI) * .12f, big = banner.Length > 9 ? 40 : 66;
            Gfx.Save(); Gfx.GA = Mathf.Min(1f, t / 4f); Gfx.Translate(W / 2, H / 2 - 40); Gfx.Scale(e, e);
            Gfx.Glow(0, 0, 150, Gfx.Rgba(255, 42, 0, .25f)); Gfx.Gold(banner, 0, 0, big, 10); Gfx.Restore();
            if (phase == "ko" && sub != "" && pt > 25) Gfx.Txt(sub, W / 2, H / 2 + 24, 30 + Mathf.Sin(frame / 5f) * 2, Gfx.Hex("#ff3a2a"), 7);
            if (phase == "end" && winner != null) Gfx.Txt(winner.ch.title, W / 2, H / 2 + 4, 14, Gfx.Hex("#dddddd"), 4);
        }
        if (phase == "intro") {
            if (pt < 30) Gfx.Fill(0, 0, W, H, new Color(0, 0, 0, 1 - pt / 30f));
            if (pt > 12 && pt < 100) {
                float k = Mathf.Min(1f, (pt - 12) / 14f), o = (1 - k) * 220;
                Gfx.Gold(p1.ch.name, 130 - o, 120, 22, -1); Gfx.Gold(p2.ch.name, W - 130 + o, 120, 22, -1);
                Gfx.Txt("ENTER: SKIP", W / 2, 376, 11, Gfx.Hex("#999999"), 3);
            }
        }
        if (pause != null) {
            Gfx.Fill(0, 0, W, H, new Color(0, 0, 0, .7f)); Gfx.Gold("PAUSED", W / 2, 120, 44, 7);
            for (int i = 0; i < pause.items.Length; i++)
                Gfx.Txt((i == pause.sel ? "> " : "") + pause.items[i], W / 2, 190 + i * 38, i == pause.sel ? 28 : 24, Gfx.Hex(i == pause.sel ? "#ffd23a" : "#dddddd"), 5);
        }
    }

    void Ambient() {
        int frame = Game.frame;
        for (int i = 0; i < 3; i++) {
            float fx0 = ((frame * (.2f + i * .1f) + i * 260) % (W + 400)) - 200;
            Gfx.Glow(fx0, H - 40, 240, Gfx.Rgba(223, 230, 255, .09f));
        }
        if (motes == null) { motes = new float[36][]; for (int i = 0; i < 36; i++) motes[i] = new float[] { Random.value * W, Random.value * H, Random.value * 1.6f + .6f, Random.value * .4f + .1f }; }
        foreach (float[] m in motes) {
            m[1] -= m[3]; m[0] += Mathf.Sin((m[1] + m[2] * 40) / 30f) * .3f;
            if (m[1] < -4) { m[1] = H + 4; m[0] = Random.value * W; }
            Gfx.Fill(m[0], m[1], m[2], m[2], Gfx.Rgba(255, 225, 170, .35f));
        }
        Gfx.Tex(Gfx.Vig, 0, 0, W, H, Color.white);
    }

    void Overlays() {
        int frame = Game.frame;
        if (sflash > 0) Gfx.Fill(0, 0, W, H, new Color(1, 1, 1, Mathf.Min(.5f, sflash * .09f)));
        if (phase == "ko" || phase == "end") Gfx.Fill(0, 0, W, H, new Color(110 / 255f, 0, 0, .14f + .05f * Mathf.Sin(frame / 6f)));
        foreach (Fx e in fx) if (e.k == "call") {
            float k = e.t / e.life, w = 300 * Mathf.Min(1f, e.t / 6f), y = 112; bool left = e.side == 0; float d = left ? 1 : -1;
            Gfx.Save(); Gfx.GA = 1 - (k > .75f ? (k - .75f) * 4 : 0); Gfx.Translate(left ? 0 : W, 0);
            Color c = e.col;
            float x1 = d * (w + 8), lx = Mathf.Min(0, x1);
            Gfx.Fill(Mathf.Min(0, d * (w + 18)), y - 14, Mathf.Abs(d * (w + 18)), 28, c);
            Gfx.Fill(Mathf.Min(0, d * (w + 10)), y - 11, Mathf.Abs(d * (w + 10)), 22, new Color(0, 0, 0, .65f));
            Gfx.GoldA(e.s, d * 12, y + 1, 17, 4, left ? -1 : 1);
            Gfx.Restore();
        }
    }

    void DrawFx(Fx e) {
        float k = e.t / e.life;
        if (e.k == "ember") {
            Color c = e.h >= 0 ? Gfx.Hsl(e.h, 1, .65f, 1 - k) : Gfx.Rgba(255, 180 - k * 120, 60, 1 - k);
            Gfx.Fill(e.x, e.y, 3 * (1 - k) + 1, 3 * (1 - k) + 1, c);
        } else if (e.k == "ring") {
            Gfx.RingAt(e.x, e.y, 6 + k * 38, Gfx.Rgba(255, 225, 170, 1 - k));
        } else if (e.k == "crack") {
            Color c = Gfx.Rgba(20, 10, 10, .6f * (1 - k * k)); float px = e.x, py = e.y;
            for (int i = 0; i < e.px.Length; i++) {
                float nx = e.x + e.px[i] * Mathf.Min(1f, e.t / 6f), ny = e.y + e.py[i];
                Gfx.Line(px, py, nx, ny, 2, c); px = nx; py = ny;
            }
        } else if (e.k == "hit") {
            float r = 10 + k * 46;
            Gfx.Glow(e.x, e.y, r * 1.4f, Gfx.Rgba(255, 170, 60, .75f * (1 - k)));
            Color rc = Gfx.Rgba(255, 240, 200, 1 - k);
            for (int i = 0; i < 9; i++) {
                float ang = i * .698f + e.x * .13f, r1 = r * (1.1f + (i % 2) * .7f);
                Gfx.Line(e.x + Mathf.Cos(ang) * r * .5f, e.y + Mathf.Sin(ang) * r * .5f, e.x + Mathf.Cos(ang) * r1, e.y + Mathf.Sin(ang) * r1, 3 * (1 - k) + 1, rc);
            }
            Gfx.Fx(k < .3f ? "s0" : k < .6f ? "s1" : "s2", e.x, e.y, 2.4f + k, 1 - k * .6f, false);
        } else if (e.k == "txt") {
            Gfx.Gold(e.s, e.x, e.y - k * 20, 16, 4);
        } else if (e.k == "wave") {
            Gfx.Fx(k < .5f ? "w1" : "w2", e.x, e.y, .8f + k * 1.1f, 1 - k, true);
        } else if (e.k == "puff") {
            Gfx.Fx("dust", e.x, e.y, 1 + k * .5f, .95f - k, true);
        } else if (e.k == "block") {
            Gfx.RingAt(e.x, e.y, 8 + k * 22, Gfx.Rgba(160, 210, 255, 1 - k));
        } else if (e.k == "dust") {
            Gfx.Fill(e.x, e.y, 8 * (1 - k) + 2, 8 * (1 - k) + 2, Gfx.Rgba(210, 200, 190, .6f * (1 - k)));
        }
    }

    void DrawHUD() {
        Fighter p1 = f[0], p2 = f[1]; int frame = Game.frame; float bw = 252;
        Bar(18, p1, false); Bar(W - 18 - bw, p2, true);
        Gfx.Circ(W / 2, 26, 25, Gfx.Hex("#ffd23a")); Gfx.Circ(W / 2, 26, 22, Color.black);
        Gfx.Gold(S.mode == "train" ? "--" : Mathf.Max(0, timer).ToString(), W / 2, 27, 24, 4);
        for (int i = 0; i < 2; i++) {
            Fighter ff = f[i];
            if (ff.combo >= 2 && ff.comboT > 0) {
                float pop = 1 + Mathf.Max(0, ff.comboT - 90) * .04f, cxp = i == 1 ? W - 100 : 100;
                Gfx.Gold(ff.combo + " HITS", cxp, 150, 30 * pop, 6);
                Gfx.Txt("COMBO", cxp, 174, 12, Gfx.Hex("#ffd0a0"), 3);
            }
        }
        if (S.mode == "train") Gfx.Txt("LAST HIT: " + lastDmg, W / 2, 62, 12, Gfx.Hex("#ccddee"), 3);
    }

    void Bar(float x, Fighter ft, bool right) {
        float bw = 252, bh = 16, by = 16; int frame = Game.frame;
        Gfx.Fill(x - 3, by - 3, bw + 6, bh + 6, Color.black); Gfx.Fill(x, by, bw, bh, Gfx.Hex("#440000"));
        float lag = bw * ft.shown / 100, cur = bw * ft.hp / 100;
        Gfx.Fill(right ? x + bw - lag : x, by, lag, bh, Gfx.Hex("#e8700a"));
        Color col = Gfx.Hex(ft.hp > 50 ? "#35d14a" : ft.hp > 25 ? "#e8d000" : "#e03030");
        Color light = Gfx.Hex(ft.hp > 50 ? "#b6ff9a" : ft.hp > 25 ? "#fff58a" : "#ff9a8a");
        float gx = right ? x + bw - cur : x;
        Gfx.VGrad(gx, by, cur, bh / 2, light, col); Gfx.VGrad(gx, by + bh / 2, cur, bh / 2, col, new Color(col.r * .4f, col.g * .4f, col.b * .4f, 1));
        Gfx.Fill(gx, by, cur, 3, new Color(1, 1, 1, .3f));
        for (int i = 1; i < 10; i++) Gfx.Fill(x + bw * i / 10 - .5f, by, 1, bh, new Color(0, 0, 0, .35f));
        if (ft.hp <= 25 && frame % 24 < 12) Gfx.Fill(x, by, bw, bh, new Color(1, 0, 0, .28f));
        Color gold = Gfx.Hex("#ffd23a");
        Gfx.StrokeRect(x - 2, by - 2, bw + 4, bh + 4, 2, gold); Gfx.StrokeRect(x - 4.5f, by - 4.5f, bw + 9, bh + 9, 1, Gfx.Hex("#7a4a00")); Gfx.StrokeRect(x + .5f, by + .5f, bw - 1, bh - 1, 1, new Color(0, 0, 0, .6f));
        Gfx.GoldA(ft.ch.name, right ? x + bw : x, by + bh + 13, 14, 4, right ? 1 : -1);
        float pw = 110, px = right ? x + bw - pw : x, py = by + bh + 28;
        Gfx.Fill(px - 2, py - 2, pw + 4, 9, Color.black); Gfx.Fill(px, py, pw, 5, Gfx.Hex("#112233"));
        Color pc = ft.power >= Moves.COST ? Gfx.Hex(frame % 20 < 10 ? "#7fd0ff" : "#3a9cff") : Gfx.Hex("#2a5a9a");
        float w = pw * ft.power / 100; Gfx.Fill(right ? px + pw - w : px, py, w, 5, pc);
        float mx = px + (right ? pw - pw * Moves.COST / 100 : pw * Moves.COST / 100); Gfx.Fill(mx, py - 2, 1, 9, Color.white);
        if (ft.power >= Moves.COST && frame % 40 < 28) Gfx.Txt("SPECIAL READY", px + pw / 2, py + 17, 9, Gfx.Hex("#7fd0ff"), 3);
        for (int i = 0; i < 2; i++) {
            float cxw = right ? x + bw - 8 - i * 18 : x + 8 + i * 18, cyw = py + 20;
            Gfx.Circ(cxw, cyw, 7.5f, gold); Gfx.Circ(cxw, cyw, 6, i < ft.wins ? Gfx.Hex("#e8b020") : Gfx.Hex("#222222"));
        }
    }
}
