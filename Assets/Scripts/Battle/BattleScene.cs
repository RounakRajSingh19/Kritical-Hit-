// Battle screen: arena background (Resources/arena.jpg, dark fallback), two fighters
// (Resources/fighter1.png / fighter2.png, dummy figures if missing), two mirrored HUDs and the
// round / fight logic. Created by TitleMenu when 1 PLAYER / 2 PLAYERS is chosen.

using UnityEngine;
using UnityEngine.UI;

namespace MysticKombat
{
    public class BattleScene : MonoBehaviour
    {
        // Set to false to hide the on-screen test-key hint and disable the test keys.
        const bool ShowTestKeys = true;

        public static BattleScene Active { get; private set; }

        public PlayerHud P1 { get; private set; }
        public PlayerHud P2 { get; private set; }
        public Fighter F1 { get; private set; }
        public Fighter F2 { get; private set; }
        public FighterCombat C1 { get; private set; }
        public FighterCombat C2 { get; private set; }
        public Projectiles Shots { get; private set; }
        public bool TwoPlayers { get; private set; }

        // how far from the centre a fighter may walk (keeps them on screen)
        public float WalkLimit
        {
            get { return rootRt != null ? Mathf.Max(400f, rootRt.rect.width * 0.5f - 110f) : 800f; }
        }

        Font font;
        System.Action onExit;
        Image fade;
        RawImage arenaImg;
        RectTransform rootRt;
        Text koTitle, koSub;
        bool matchOver;          // true while a round has ended and we wait for ENTER
        bool fightOver;          // true when somebody has won RoundsToWin rounds (the whole fight)
        float fightOverT;        // seconds since the fight ended
        const float ReturnToMenuDelay = 3.5f;   // how long the winner banner stays before going back to the menu
        float clock, lastAspect;

        public static BattleScene Begin(Font font, bool twoPlayers, System.Action onExit)
        {
            if (Active != null) return Active;
            var go = new GameObject("MysticKombat_Battle");
            var scene = go.AddComponent<BattleScene>();
            scene.font = font;
            scene.TwoPlayers = twoPlayers;
            scene.onExit = onExit;
            scene.BuildUI();
            Active = scene;
            return scene;
        }

        internal static RectTransform NewRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go.GetComponent<RectTransform>();
        }

        void BuildUI()
        {
            var canvasGo = new GameObject("BattleCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            var root = canvasGo.GetComponent<RectTransform>();
            rootRt = root;

            // arena background (Assets/Resources/arena.jpg), cover-fit
            var bg = NewRect("Arena", root);
            Stretch(bg);
            arenaImg = bg.gameObject.AddComponent<RawImage>();
            arenaImg.raycastTarget = false;
            var arenaTex = Resources.Load<Texture2D>("arena");
            if (arenaTex != null)
            {
                arenaImg.texture = arenaTex;
                arenaImg.color = Color.white;
                UpdateArenaCover();
            }
            else
            {
                arenaImg.color = new Color(0.07f, 0.06f, 0.14f, 1f);
            }

            // fighters (drawn above the arena, below the HUD)
            F1 = Fighter.Create(root, "Fighter_P1", "fighter1", new Color(0.25f, 0.65f, 1f, 1f), -380f, true);
            F2 = Fighter.Create(root, "Fighter_P2", "fighter2", new Color(1f, 0.35f, 0.55f, 1f), 380f, false);

            // projectile layer (above fighters, below HUD)
            var shotsRt = NewRect("Projectiles", root);
            Stretch(shotsRt);
            Shots = new Projectiles(shotsRt, this);

            P1 = new PlayerHud(root, false);
            P2 = new PlayerHud(root, true);

            C1 = new FighterCombat(0, F1, P1, this);
            C2 = new FighterCombat(1, F2, P2, this);
            C1.SetOpponent(C2);
            C2.SetOpponent(C1);

            koTitle = NewBannerText("KoTitle", root, 170, 90f, new Color(1f, 0.85f, 0.30f, 1f));
            koSub = NewBannerText("KoSub", root, 44, -60f, new Color(0.85f, 0.95f, 1f, 1f));
            koTitle.gameObject.SetActive(false);
            koSub.gameObject.SetActive(false);

            if (ShowTestKeys)
            {
                var rt = NewRect("TestKeysHint", root);
                rt.anchorMin = new Vector2(0.5f, 0f);
                rt.anchorMax = new Vector2(0.5f, 0f);
                rt.pivot = new Vector2(0.5f, 0f);
                rt.sizeDelta = new Vector2(1800, 40);
                rt.anchoredPosition = new Vector2(0, 24);
                var t = rt.gameObject.AddComponent<Text>();
                t.font = font;
                t.fontSize = 24;
                t.alignment = TextAnchor.MiddleCenter;
                t.color = new Color(0.8f, 0.9f, 1f, 0.55f);
                t.supportRichText = false;
                t.raycastTarget = false;
                t.horizontalOverflow = HorizontalWrapMode.Overflow;
                t.text = "TEST KEYS    P1:  1/2 health   3/4 resource   5 round won        " +
                         "P2:  6/7 health   8/9 resource   0 round won        ESC  pause";
            }

            // fade-in from black (last = on top)
            var fadeRt = NewRect("Fade", root);
            Stretch(fadeRt);
            fade = fadeRt.gameObject.AddComponent<Image>();
            fade.color = Color.black;
            fade.raycastTarget = false;
        }

        Text NewBannerText(string name, Transform parent, int size, float y, Color c)
        {
            var rt = NewRect(name, parent);
            rt.sizeDelta = new Vector2(1600, 260);
            rt.anchoredPosition = new Vector2(0, y);
            var t = rt.gameObject.AddComponent<Text>();
            t.font = font;
            t.fontSize = size;
            t.fontStyle = FontStyle.Bold;
            t.alignment = TextAnchor.MiddleCenter;
            t.color = c;
            t.supportRichText = false;
            t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            var sh = rt.gameObject.AddComponent<Shadow>();
            sh.effectColor = new Color(0f, 0f, 0f, 0.9f);
            sh.effectDistance = new Vector2(6, -6);
            return t;
        }

        // Fill the screen with the arena picture without stretching it.
        void UpdateArenaCover()
        {
            if (arenaImg == null || arenaImg.texture == null) return;
            float screenAspect = (float)Screen.width / Mathf.Max(1, Screen.height);
            lastAspect = screenAspect;
            float texAspect = (float)arenaImg.texture.width / arenaImg.texture.height;
            if (screenAspect > texAspect)
            {
                float h = texAspect / screenAspect;
                arenaImg.uvRect = new Rect(0f, (1f - h) * 0.5f, 1f, h);
            }
            else
            {
                float w = screenAspect / texAspect;
                arenaImg.uvRect = new Rect((1f - w) * 0.5f, 0f, w, 1f);
            }
        }

        static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        void Update()
        {
            if (TitleMenu.IsPaused) return;             // pause menu is open: freeze the battle

            float dt = Time.unscaledDeltaTime;
            clock += dt;

            if (!Mathf.Approximately(lastAspect, (float)Screen.width / Mathf.Max(1, Screen.height)))
                UpdateArenaCover();

            C1.InputEnabled = !matchOver;
            C2.InputEnabled = !matchOver;
            C1.Tick(dt);
            C2.Tick(dt);
            if (!matchOver)
            {
                ResolveHits();
                Shots.Tick(dt);
                CheckKnockout();
            }
            else if (fightOver)
            {
                // fight is decided: let the last diamond / banner show, then back to the title screen
                fightOverT += dt;
                if (fightOverT >= ReturnToMenuDelay || MenuInput.Confirm())
                {
                    Exit();
                    return;
                }
            }
            else if (MenuInput.Confirm())
            {
                RestartRound();
            }

            P1.Tick(dt);
            P2.Tick(dt);

            fade.color = new Color(0f, 0f, 0f, 1f - Mathf.Clamp01(clock / 0.4f));

            if (ShowTestKeys)
            {
                HandleTestKeys(P1, 0);
                HandleTestKeys(P2, 5);
            }

            // Esc is handled by the pause menu in TitleMenu (its EXIT button leaves the battle).
        }

        // Punch / kick hits of both fighters are applied together, so a simultaneous exchange trades.
        void ResolveHits()
        {
            float d1, d2;
            bool h1 = C1.ConsumeHit(out d1);
            bool h2 = C2.ConsumeHit(out d2);
            if (h1) { C2.TakeHit(d1, C1.Dir); C1.GainResource(Moves.ResourcePerHit); }
            if (h2) { C1.TakeHit(d2, C2.Dir); C2.GainResource(Moves.ResourcePerHit); }
        }

        // A round ends when someone hits 0 health. The winner gets a diamond; a double K.O.
        // gives both a diamond. First to RoundsToWin (2) rounds wins the whole fight.
        void CheckKnockout()
        {
            bool d1 = P1.Health <= 0f, d2 = P2.Health <= 0f;
            if (!d1 && !d2) return;

            matchOver = true;
            if (d1) C1.Knockout();
            if (d2) C2.Knockout();
            Shots.Clear();

            if (!d1) P1.SetRoundsWon(P1.RoundsWon + 1);
            if (!d2) P2.SetRoundsWon(P2.RoundsWon + 1);
            if (d1 && d2)
            {
                P1.SetRoundsWon(P1.RoundsWon + 1);
                P2.SetRoundsWon(P2.RoundsWon + 1);
            }

            bool p1Fight = P1.RoundsWon >= PlayerHud.RoundsToWin;
            bool p2Fight = P2.RoundsWon >= PlayerHud.RoundsToWin;
            fightOver = p1Fight || p2Fight;

            if (fightOver)
            {
                koTitle.text = (p1Fight && p2Fight) ? "DRAW" : "K.O.";
                koSub.text = ((p1Fight && p2Fight) ? "THE FIGHT IS A DRAW" : (p1Fight ? "PLAYER 1 WINS THE FIGHT" : "PLAYER 2 WINS THE FIGHT"))
                             + "     -     returning to menu...";
            }
            else
            {
                koTitle.text = (d1 && d2) ? "DRAW" : "K.O.";
                koSub.text = ((d1 && d2) ? "ROUND DRAWN" : (d1 ? "PLAYER 2 WINS THE ROUND" : "PLAYER 1 WINS THE ROUND"))
                             + "     -     ENTER  for the next round";
            }
            koTitle.gameObject.SetActive(true);
            koSub.gameObject.SetActive(true);
        }

        // New round: full health, empty resource, back to the start spots (rounds won are kept).
        void RestartRound()
        {
            matchOver = false;
            Shots.Clear();
            C1.ResetRound(-380f);
            C2.ResetRound(380f);
            koTitle.gameObject.SetActive(false);
            koSub.gameObject.SetActive(false);
        }

        // keys: base+0 health -10, +1 health +10, +2 resource -10, +3 resource +10, +4 cycle rounds won
        static void HandleTestKeys(PlayerHud p, int baseIndex)
        {
            if (TestKeys.Down(baseIndex + 0)) p.SetHealth(p.Health - 10f);
            if (TestKeys.Down(baseIndex + 1)) p.SetHealth(p.Health + 10f);
            if (TestKeys.Down(baseIndex + 2)) p.SetResource(p.Resource - 10f);
            if (TestKeys.Down(baseIndex + 3)) p.SetResource(p.Resource + 10f);
            if (TestKeys.Down(baseIndex + 4)) p.SetRoundsWon((p.RoundsWon + 1) % (PlayerHud.RoundsToWin + 1));
        }

        void Exit()
        {
            Active = null;
            var cb = onExit;
            Destroy(gameObject);
            if (cb != null) cb();
        }

        void OnDestroy()
        {
            if (Active == this) Active = null;
        }
    }
}
