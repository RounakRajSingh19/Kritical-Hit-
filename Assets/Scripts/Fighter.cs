using UnityEngine;
public struct Inp { public float dir; public bool up, down, block, punch, kick, special; }
public enum St { Free, Attack, Hurt, Fall, Lie, Win }
public class Fighter : MonoBehaviour {
  public CharDef ch; public Fighter foe; public int facing = 1; public float gy, hp = 100, power = 30, x, y, vx, vy, sx0, sx1;
  public St st; public bool block, crouch; int t, stun, atk; bool hit; string mv; SpriteRenderer sr; float age;
  const float G = .55f, JUMP = -12.5f;
  public void Init(CharDef c, float groundY, float startX, int face, float minX, float maxX) {
    ch = c; gy = groundY; x = startX; y = groundY; facing = face; sx0 = minX; sx1 = maxX; hp = 100; power = 30; st = St.Free;
    sr = gameObject.AddComponent<SpriteRenderer>(); sr.sortingOrder = 5;
  }
  public bool Air => y < gy - .5f;
  public void Tick(Inp i) {
    age++; power = Mathf.Min(100, power + .035f);
    if (foe) facing = foe.x >= x ? 1 : -1;
    block = false; crouch = false;
    switch (st) {
      case St.Free:
        if (!Air) { crouch = i.down; block = i.block;
          vx = (crouch || block) ? 0 : i.dir * ch.speed;
          if (i.up && !crouch) vy = JUMP;
          if (i.special && power >= 30) { power -= 30; Begin("dash", 54); }
          else if (i.punch) Begin(i.down ? "low" : (i.up ? "upper" : "punch"), i.down ? 30 : 20);
          else if (i.kick) Begin("kick", 28);
        } else if (i.punch || i.kick) Begin(i.kick ? "jk" : "jp", 99);
        break;
      case St.Attack: t++; Attack(); break;
      case St.Hurt: vx *= .82f; if (--stun <= 0) st = St.Free; break;
      case St.Fall: if (y >= gy && vy >= 0) { y = gy; vx = 0; vy = 0; st = St.Lie; stun = hp <= 0 ? 9999 : 42; Game.I.Shake(.12f); Game.I.Play("littleKick"); } break;
      case St.Lie: if (--stun <= 0) { st = St.Free; } break;
    }
    x = Mathf.Clamp(x + vx, sx0, sx1);
    if (vy != 0 || Air) { vy += G; y += vy; if (y >= gy) { y = gy; vy = 0; if (st == St.Free) { vx = 0; if (st == St.Attack) st = St.Free; } if (mv == "jp" || mv == "jk") { st = St.Free; } } }
    if (st == St.Attack && !Air && (mv == "jp" || mv == "jk")) st = St.Free;
    Draw();
  }
  void Begin(string m, int dur) { st = St.Attack; mv = m; t = 0; atk = dur; hit = false; }
  // hit window / box in px: reach, damage, knockdown
  void Attack() {
    float reach = 70, d = 6, kb = 3; bool kd = false; int s = 5, e = 8; float top = 104, bot = 84;
    switch (mv) {
      case "kick": reach = 38; d = 9; s = 8; e = 12; kb = 4; break;
      case "upper": reach = 30; d = 12; s = 8; e = 14; kd = true; break;
      case "low": reach = 26; d = 7; s = 7; e = 11; bot = 0; top = 26; break;
      case "jp": reach = 58; d = 7; s = 3; e = 999; break;
      case "jk": reach = 50; d = 10; s = 3; e = 999; kb = 4; break;
      case "dash": reach = 48; d = 15; s = 12; e = 34; kd = true; vx = (t >= 11 && t < 34) ? 6.5f * facing : 0; break;
    }
    if (!hit && t >= s && t <= e && foe && foe.st != St.Fall && foe.st != St.Lie) {
      float dx = (foe.x - x) * facing; float fy = foe.gy - foe.y;
      bool inRange = dx > -10 && dx < reach + 20 && Mathf.Abs(foe.y - y) < 60;
      if (mv == "low" && foe.Air) inRange = false;
      if (inRange) { hit = true; foe.Hurt(d * ch.dmg, kb * facing, kd, this); power = Mathf.Min(100, power + 8); }
    }
    if (atk < 99 && t >= atk) { st = St.Free; vx = 0; }
  }
  public void Hurt(float dmg, float kb, bool kd, Fighter by) {
    if (block && !Air && Mathf.Sign(by.x - x) == facing) { hp = Mathf.Max(1, hp - dmg * .08f); st = St.Hurt; stun = 9; vx = kb * .7f; Game.I.Play("littleKick"); return; }
    hp = Mathf.Max(0, hp - dmg); power = Mathf.Min(100, power + 6); Game.I.Shake(.04f); Game.I.Play(dmg > 9 ? "bigPunch" : "littleKick");
    if (kd || hp <= 0 || Air) { st = St.Fall; vy = -8; vx = kb; } else { st = St.Hurt; stun = 16; vx = kb; }
  }
  void Draw() {
    Sprite s;
    switch (st) {
      case St.Attack: { int n = mv == "kick" ? 1 : 0; bool second = t > atk * .25f;
        s = mv == "punch" ? ch.S(ch.punch, second ? 1 : 0) : mv == "kick" ? ch.S(ch.kick, t < 7 ? 0 : 1) : mv == "upper" ? ch.S(ch.up, t < 8 ? 0 : t < 14 ? 1 : 2)
          : mv == "low" ? (t < 5 ? ch.S(ch.crouch, 1) : ch.S(ch.kick, 0)) : mv == "jp" ? ch.S(ch.up, 3) : mv == "jk" ? ch.S(ch.jk, 0)
          : mv == "dash" ? (t < 8 ? ch.S(ch.up, 0) : t < 12 ? ch.S(ch.up, 1) : ch.S(ch.up, 3)) : ch.S(ch.idle, 0); break; }
      case St.Hurt: s = ch.S(ch.hurt, stun > 8 ? 0 : 1); break;
      case St.Fall: s = ch.S(ch.fall, vy < -2 ? 0 : vy < 3 ? 1 : 2); break;
      case St.Lie: s = ch.S(ch.fall, 3); break;
      case St.Win: s = ch.S(ch.taunt, (int)(age / 8) % ch.taunt.Length); break;
      default:
        if (Air) s = ch.S(ch.jump, vy < -4 ? 0 : vy < 4 ? 1 : 2);
        else if (crouch || block) s = ch.S(ch.crouch, 0);
        else if (Mathf.Abs(vx) > .1f) s = ch.S(ch.walk, (int)(age / 6) % ch.walk.Length);
        else s = ch.S(ch.idle, (int)(age / 12) % ch.idle.Length);
        break;
    }
    sr.sprite = s; transform.position = Data.P(x, y);
    transform.localScale = new Vector3(ch.sc * facing, ch.sc, 1);
  }
  public void Win() { st = St.Win; vx = 0; }
  public bool Dead => hp <= 0 && st == St.Lie;
}
