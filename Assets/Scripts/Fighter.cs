using System.Collections.Generic;
using UnityEngine;

public struct Inp {
    public bool l, r, u, d, b, p, k, t, s1, s2, s3;
    public static Inp Mk(string s) {
        Inp i = new Inp();
        foreach (char c in s) {
            switch (c) {
                case 'l': i.l = true; break; case 'r': i.r = true; break; case 'u': i.u = true; break; case 'd': i.d = true; break;
                case 'b': i.b = true; break; case 'p': i.p = true; break; case 'k': i.k = true; break; case 't': i.t = true; break;
                case '1': i.s1 = true; break; case '2': i.s2 = true; break; case '3': i.s3 = true; break;
            }
        }
        return i;
    }
    public void Or(Inp o) {
        l |= o.l; r |= o.r; u |= o.u; d |= o.d; b |= o.b; p |= o.p; k |= o.k; t |= o.t; s1 |= o.s1; s2 |= o.s2; s3 |= o.s3;
    }
}

public delegate float VxFn(int t);
public delegate int AnimFn(Fighter f, int t);

public class Move {
    public bool air, thr, kd, cost; public int dur, hs, he; public float[] box;
    public float dmg, stun, bstun, kb, launch; public char lvl, snd; public VxFn vx; public AnimFn anim;
}

public static class Moves {
    public const float COST = 30f;
    public static Dictionary<string, Move> M = new Dictionary<string, Move>();
    static Moves() {
        M["punch"] = new Move { dur = 20, hs = 5, he = 8, box = new float[] { 14, 84, 70, 104 }, dmg = 6, stun = 16, bstun = 9, kb = 3.2f, lvl = 'h', snd = 'l', anim = (f, t) => f.Fm("punch", t < 5 ? 0 : 1) };
        M["kick"] = new Move { dur = 28, hs = 8, he = 12, box = new float[] { 16, 108, 38, 74 }, dmg = 9, stun = 18, bstun = 11, kb = 4, lvl = 'm', snd = 'h', anim = (f, t) => f.Fm("kick", t < 7 ? 0 : 1) };
        M["upper"] = new Move { dur = 38, hs = 8, he = 14, box = new float[] { 8, 62, 30, 128 }, dmg = 12, bstun = 14, kb = 2, lvl = 'm', kd = true, launch = -9, snd = 'h', anim = (f, t) => f.Fm("up", t < 8 ? 0 : t < 14 ? 1 : 2) };
        M["low"] = new Move { dur = 30, hs = 7, he = 11, box = new float[] { 14, 92, 0, 26 }, dmg = 7, stun = 20, bstun = 10, kb = 3, lvl = 'l', snd = 'l', anim = (f, t) => t < 5 ? f.Fm("crouch", 1) : f.Fm("kick", 0) };
        M["jp"] = new Move { air = true, dur = 99, hs = 3, he = 999, box = new float[] { 10, 78, 58, 98 }, dmg = 7, stun = 16, bstun = 9, kb = 3, lvl = 'h', snd = 'l', anim = (f, t) => f.Fm("up", 3) };
        M["jk"] = new Move { air = true, dur = 99, hs = 3, he = 999, box = new float[] { 10, 98, 22, 70 }, dmg = 10, stun = 20, bstun = 12, kb = 4, lvl = 'm', snd = 'h', anim = (f, t) => f.Fm("jk", 0) };
        M["dash"] = new Move { dur = 54, hs = 12, he = 34, box = new float[] { 8, 80, 48, 112 }, dmg = 15, bstun = 16, kb = 4, kd = true, launch = -8, cost = true, lvl = 'm', snd = 'h',
            vx = t => (t >= 11 && t < 34) ? 6.5f : 0f, anim = (f, t) => t < 8 ? f.Fm("up", 0) : t < 12 ? f.Fm("up", 1) : f.Fm("up", 3) };
        M["slide"] = new Move { dur = 48, hs = 8, he = 32, box = new float[] { 10, 104, 26, 66 }, dmg = 12, bstun = 14, kb = 4, kd = true, launch = -7, cost = true, lvl = 'm', snd = 'h',
            vx = t => (t >= 6 && t < 34) ? 7.5f : 0f, anim = (f, t) => t < 6 ? f.Fm("crouch", 1) : f.Fm("jk", 0) };
        M["throw"] = new Move { dur = 64, thr = true, dmg = 14, anim = (f, t) => f.Fm("throw", t < 8 ? 0 : t < 18 ? 1 : 2) };
        M["blast"] = new Move { dur = 40, cost = true, anim = (f, t) => f.Fm("throw", t < 8 ? 0 : t < 24 ? 1 : 2) };
        M["taunt"] = new Move { dur = 56, anim = (f, t) => f.Fm("taunt", (t / 8) % 4) };
    }
}

public enum St { Free, Hurt, Fall, Lie, GetUp, Taunt, Win, Dead }

public class Trail { public float x, y, a; public Fr f; public int face; }

public class Fighter {
    public const float GACC = 0.55f, JUMPV = -12.5f;
    public CharDef ch; public int side; public Texture2D sheet; public int wins;
    public float x, y, gy, vx, vy, hp, shown, power;
    public int facing, mt, stun, hurtKind, timer, inv, age, combo, comboT, walk, landT, flash;
    public St state; public Move move; public string mname; public bool moveHit, crouch, block, blockedHit, bounced;
    List<Trail> trail = new List<Trail>();

    public Fighter(CharDef c, int side, Texture2D sheet) { ch = c; this.side = side; this.sheet = sheet; wins = 0; }
    public int Fm(string n, int i) { return ch.Fm(n, i); }
    public bool Air { get { return y < gy - 0.5f; } }

    public void Reset(float px, float pgy, int face) {
        x = px; y = pgy; gy = pgy; vx = 0; vy = 0; facing = face; hp = 100; shown = 100; power = 30; state = St.Free; move = null; mt = 0;
        moveHit = false; crouch = false; block = false; stun = 0; hurtKind = 1; timer = 0; inv = 0; age = 0; combo = 0; comboT = 0; walk = 0; landT = 0;
    }
    void Start(string name) { move = Moves.M[name]; mname = name; mt = 0; moveHit = false; }

    static float Rnd() { return Random.value; }

    public void UpdateF(Inp inp, Fighter opp, FightScene G) {
        age++; if (inv > 0) inv--; if (flash > 0) flash--; UpdTrail();
        if (comboT > 0 && --comboT == 0) combo = 0;
        power = Mathf.Min(100, power + 0.035f);
        shown += (hp - shown) * 0.05f; if (shown - hp < .2f) shown = hp;
        switch (state) {
            case St.Free: FreeF(inp, opp, G); break;
            case St.Hurt: vx *= .82f; if (--stun <= 0) { state = St.Free; blockedHit = false; } break;
            case St.Fall:
                if (y >= gy && vy >= 0) {
                    if (!bounced && vy > 4) {
                        bounced = true; y = gy - 1; vy = -2.2f; vx *= .55f; G.shake = 10; G.zoom = Mathf.Max(G.zoom, .03f); G.Dust(x, gy);
                        foreach (int sg in new int[] { -1, 1 }) {
                            Fx e = new Fx { k = "crack", x = x, y = gy + 3, life = 55 };
                            e.px = new float[4]; e.py = new float[4];
                            for (int i = 1; i <= 4; i++) { e.px[i - 1] = sg * i * 14; e.py[i - 1] = (Rnd() - .5f) * 5; }
                            G.fx.Add(e);
                        }
                        Sfx.Play("littleKick", .6f); break;
                    }
                    y = gy; vx = 0; vy = 0; state = St.Lie; timer = hp <= 0 ? 9999 : 42; G.shake = 7; G.Dust(x, gy);
                    G.fx.Add(new Fx { k = "wave", x = x, y = gy + 4, life = 18 }); Sfx.Play("littleKick", .6f);
                }
                break;
            case St.Lie: if (--timer <= 0) { state = St.GetUp; timer = 22; inv = 16; } if (hp <= 0) state = St.Dead; break;
            case St.GetUp: if (--timer <= 0) state = St.Free; break;
            case St.Taunt: if (--timer <= 0) state = St.Free; break;
        }
        x += vx;
        if (state == St.Fall || (state == St.Free && Air) || vy < 0) {
            vy += GACC; y += vy;
            if (y >= gy) {
                y = gy;
                if (state == St.Free) { vy = 0; vx = 0; if (move != null && move.air) move = null; landT = 4; G.Dust(x, gy); }
            }
        }
    }

    void UpdTrail() {
        Move mv = move;
        bool fast = (mv != null && (mv.vx != null || mname == "kick" || mname == "jk" || mname == "upper")) || (state == St.Fall && Mathf.Abs(vx) > 2.5f);
        if (fast && age % 2 == 0) trail.Add(new Trail { x = x, y = y, f = ch.F(FrameIdx()), face = facing, a = .55f });
        foreach (Trail o in trail) o.a -= .055f;
        trail.RemoveAll(o => o.a <= 0);
    }

    void FreeF(Inp inp, Fighter opp, FightScene G) {
        crouch = false; block = false; walk = 0;
        bool air = Air;
        if (move != null) {
            Move m = move; mt++;
            if (m.vx != null) vx = facing * m.vx(mt); else if (!air) vx *= .7f;
            if (mname == "blast" && mt == 16) G.Shoot(this);
            if (m.thr && mt == 10) {
                float d = Mathf.Abs(opp.x - x);
                if (d < 86 && !opp.Air && (opp.state == St.Free || opp.state == St.Hurt) && opp.inv <= 0) G.DoThrow(this, opp);
            }
            if (mt >= m.dur) { move = null; vx = 0; }
            return;
        }
        if (air) { if (inp.p) Start("jp"); else if (inp.k) Start("jk"); return; }
        if (landT > 0) { landT--; vx = 0; if (!(inp.p || inp.k)) return; }
        facing = opp.x > x ? 1 : -1;
        float dist = Mathf.Abs(opp.x - x);
        if (inp.s1 && power >= Moves.COST) { power -= Moves.COST; Start("dash"); SpecialFx(G); return; }
        if (inp.s3 && power >= Moves.COST) { power -= Moves.COST; Start("blast"); SpecialFx(G); return; }
        if (inp.s2 && power >= Moves.COST) { power -= Moves.COST; Start("slide"); SpecialFx(G); return; }
        if (inp.s1 || inp.s2) Sfx.Beep(180f, .12f, "sawtooth", .04f);
        if (inp.b) {
            block = true; crouch = inp.d; vx = 0;
            if (inp.p && dist < 86) { Start("throw"); block = false; }
            return;
        }
        if (inp.t) { state = St.Taunt; timer = 56; power = Mathf.Min(100, power + 8); vx = 0; return; }
        if (inp.u) { vy = JUMPV; y -= 1; G.Dust(x, gy); vx = (inp.r ? 1 : inp.l ? -1 : 0) * 3.0f; return; }
        if (inp.d) {
            crouch = true; vx = 0;
            if (inp.p) Start("upper"); else if (inp.k) Start("low");
            return;
        }
        if (inp.p) { Start("punch"); vx = 0; return; }
        if (inp.k) { Start("kick"); vx = 0; return; }
        int dir = inp.r ? 1 : inp.l ? -1 : 0;
        bool back = dir != 0 && dir != facing;
        if (dir != 0 && age % 16 == 0) G.fx.Add(new Fx { k = "dust", x = x - facing * 12, y = gy - 2, vx = -dir * .6f, vy = -.5f, life = 16 });
        vx = dir * ch.speed * (back ? .75f : 1f); walk = dir != 0 ? (back ? -1 : 1) : 0;
    }
    void SpecialFx(FightScene G) {
        G.fx.Add(new Fx { k = "wave", x = x, y = gy + 4, life = 18 }); Sfx.Play("bigPunch", .5f); G.Callout(this);
    }

    public int FrameIdx() {
        switch (state) {
            case St.Hurt: return blockedHit ? (crouch ? Fm("crouch", 1) : Fm("taunt", 0)) : Fm("hurt", hurtKind);
            case St.Fall: return vy < -2 ? Fm("fall", 0) : vy < 3 ? Fm("fall", 1) : Fm("fall", 2);
            case St.Lie: case St.Dead: return Fm("fall", 3);
            case St.GetUp: return timer > 11 ? Fm("get", 0) : Fm("get", 1);
            case St.Taunt: return Fm("taunt", ((56 - timer) / 8) % 4);
            case St.Win: return Fm("taunt", (age / 9) % 4);
        }
        if (move != null) return move.anim(this, mt);
        if (Air) return vy < -4 ? ch.air[0] : vy < 4 ? ch.air[1] : ch.air[2];
        if (landT > 0) return Fm("crouch", 1);
        if (block) return crouch ? Fm("crouch", 1) : Fm("taunt", 0);
        if (crouch) return Fm("crouch", 1);
        if (walk != 0) {
            int[] w = ch.walk; int i = (age / 6) % w.Length;
            return w[walk > 0 ? i : w.Length - 1 - i];
        }
        return Fm("idle", (age / 9) % 4);
    }

    public static float HueOf(string id) { int a = 0; foreach (char c in id) a += c; return (a * 37) % 360; }

    public void Draw() {
        Fr f = ch.F(FrameIdx()); float scale = ch.Sc;
        // shadow
        float sh = Mathf.Max(.4f, 1 - (gy - y) / 220f);
        Gfx.Tex(Gfx.Circle, x - 34 * sh, gy + 2 - 7 * sh, 68 * sh, 14 * sh, new Color(0, 0, 0, .35f));
        DrawJuiced(f, scale);
    }

    void DrawJuiced(Fr f, float s) {
        St st = state; int t = age; float sx = 1, sy = 1, dx = 0, dy = 0, rot = 0;
        if (FightScene.I.hitstop > 0 && (st == St.Hurt || st == St.Fall)) dx += (Rnd() - .5f) * 5;
        if (move != null) {
            float k = Mathf.Sin(Mathf.Min(1f, (float)mt / move.dur) * Mathf.PI);
            dx += 8 * k; sx = 1 + .05f * k; sy = 1 - .025f * k;
            if (move.air) rot = vy < 0 ? -.07f : .09f;
        } else if (st == St.Free && !Air) {
            if (walk != 0) { dy = -Mathf.Abs(Mathf.Sin(t / 3.2f)) * 3; rot = walk * .035f; }
            else if (crouch || landT > 0) { sy = .95f; sx = 1.04f; }
            else { float b = Mathf.Sin(t / 12f); sy = 1 + b * .015f; sx = 1 - b * .008f; }
        } else if (st == St.Free) { sy = 1 + Mathf.Min(.07f, Mathf.Abs(vy) * .008f); sx = 2 - sy; }
        else if (st == St.Hurt) { dx += Mathf.Sin(stun * 1.7f) * 2.5f; rot = -.05f; }
        else if (st == St.Win || st == St.Taunt) dy = -Mathf.Abs(Mathf.Sin(t / 9f)) * 3;
        Color col = Gfx.Hsl(HueOf(ch.id), 1f, .6f, 1f);
        Texture2D tinted = null;
        if (trail.Count > 0 || (move != null && move.cost)) tinted = Gfx.Tinted(sheet, col);
        foreach (Trail o in trail) Gfx.Frame(tinted, o.f, o.x, o.y, o.face, s, o.a * .55f, Color.white);
        if (move != null && move.cost) Gfx.Frame(tinted, f, x + facing * dx, y + dy, facing, s * 1.07f, .35f + .15f * Mathf.Sin(t), Color.white);
        Gfx.Save(); Gfx.Translate(Mathf.Round(x), Mathf.Round(y + dy)); if (facing < 0) Gfx.Scale(-1, 1);
        Gfx.Translate(dx, 0); Gfx.Rotate(rot); Gfx.Scale(sx, sy);
        float dw = f.w * s, dh = f.h * s, ox = Mathf.Round(-f.px * s);
        Gfx.Img(sheet, f.x, f.y, f.w, f.h, ox, -dh, dw, dh);
        if (flash > 0) Gfx.Img(Gfx.Tinted(sheet, Color.white), f.x, f.y, f.w, f.h, ox, -dh, dw, dh, Mathf.Min(1f, flash / 4f) * .85f, Color.white);
        Gfx.Restore();
    }
}

// ------------------------------------------------------------------ controllers
public class KeyMap { public KeyCode l, r, u, d, p, k, b, t; }

public class Human {
    struct Hh { public bool d, f, b; }
    KeyMap[] m; List<Hh> hist = new List<Hh>();
    public Human(params KeyMap[] maps) { m = maps; }
    static bool Get(Hh h, char c) { return c == 'd' ? h.d : c == 'f' ? h.f : h.b; }
    bool Seq(char a, char b) {
        int ia = -1;
        for (int i = 0; i < hist.Count; i++) {
            if (ia < 0 && Get(hist[i], a)) ia = i;
            if (ia >= 0 && Get(hist[i], b) && (i > ia || a == 'd')) return true;
        }
        return false;
    }
    public Inp Read(Fighter f) {
        Inp inp = new Inp();
        foreach (KeyMap mm in m) {
            inp.l |= In.Down(mm.l); inp.r |= In.Down(mm.r); inp.u |= In.Down(mm.u); inp.d |= In.Down(mm.d); inp.b |= In.Down(mm.b);
            inp.p |= In.Hit(mm.p); inp.k |= In.Hit(mm.k); inp.t |= In.Hit(mm.t);
        }
        bool fwd = f.facing > 0 ? inp.r : inp.l, back = f.facing > 0 ? inp.l : inp.r;
        Hh h; h.d = inp.d; h.f = fwd; h.b = back; hist.Add(h); if (hist.Count > 22) hist.RemoveAt(0);
        if (inp.p && f.power >= Moves.COST && Seq('d', 'f') && !inp.b) { inp.s1 = true; inp.p = false; hist.Clear(); }
        else if (inp.k && f.power >= Moves.COST && Seq('d', 'f') && !inp.b) { inp.s3 = true; inp.k = false; hist.Clear(); }
        else if (inp.k && f.power >= Moves.COST && Seq('b', 'f')) { inp.s2 = true; inp.k = false; hist.Clear(); }
        return inp;
    }
}

public class CpuAI {
    int lv, think; float block, aggr; int timer; Inp hold; int holdT; Inp fire; bool hasFire;
    static float R() { return Random.value; }
    public CpuAI(int level) {
        lv = level;
        if (level == 0) { think = 26; block = .18f; aggr = .45f; }
        else if (level == 1) { think = 14; block = .42f; aggr = .7f; }
        else { think = 8; block = .68f; aggr = .95f; }
    }
    void SetHold(string k, float t) { hold = Inp.Mk(k); holdT = (int)t; }
    void Fire(string k) { fire = Inp.Mk(k); hasFire = true; }
    public Inp Read(Fighter me, Fighter opp) {
        Inp inp = new Inp(); timer--;
        float dist = Mathf.Abs(opp.x - me.x);
        if (opp.move != null && !opp.move.air && me.state == St.Free && me.move == null && dist < 150 && holdT <= 0 && R() < block * .12f) {
            hold = Inp.Mk("b"); hold.d = opp.move.lvl == 'l' || R() < .3f; holdT = 18 + (int)(R() * 14);
        }
        if (timer <= 0 && me.state == St.Free && me.move == null) {
            timer = think + (int)(R() * 6);
            string toward = opp.x > me.x ? "r" : "l", away = toward == "r" ? "l" : "r";
            float r = R(); hasFire = false; bool pw = me.power >= Moves.COST;
            if (holdT > 0) { }
            else if (opp.y < opp.gy - 40 && dist < 110 && r < .6f * aggr + .15f) Fire("dp");
            else if (dist > 210) {
                SetHold(toward, 24 + R() * 20);
                if (r < .12f && pw) Fire("1"); else if (r < .22f) SetHold(toward + "u", 4);
            } else if (dist > 130) {
                if (r < .25f && pw) Fire("1");
                else if (r < .35f && pw) Fire("3");
                else if (r < .4f && pw) Fire("2");
                else if (r < .5f) Fire("k");
                else if (r < .6f && lv > 0) SetHold(toward + "u", 4);
                else SetHold(toward, 14 + R() * 14);
            } else if (dist < 96) {
                if (R() > aggr) SetHold(away, 14 + R() * 12);
                else if (r < .3f) Fire("p");
                else if (r < .5f) Fire("k");
                else if (r < .62f) Fire("dp");
                else if (r < .74f) Fire("dk");
                else if (r < .88f) Fire("bp");
                else SetHold("b", 16);
            } else {
                if (r < .5f) Fire("k"); else if (r < .75f) Fire("p"); else SetHold(toward, 10);
            }
        }
        if (holdT > 0) { holdT--; inp.Or(hold); }
        if (hasFire) { inp.Or(fire); hasFire = false; }
        return inp;
    }
}
