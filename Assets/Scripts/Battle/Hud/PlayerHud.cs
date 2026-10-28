using UnityEngine;
using UnityEngine.UI;

namespace MysticKombat
{
    public class PlayerHud
    {
        public const float MaxHealth = 100f;
        public const float MaxResource = 100f;
        public const int RoundsToWin = 2;               // number of diamonds

        // layout (canvas units, reference 1920x1080)
        const float MarginX = 60f, MarginTop = 40f;
        const float BarWidth = 760f;
        const float HealthHeight = 46f;
        const float ResourceHeight = 14f;
        const float SlantAmount = 0.28f;                 // ~15 degree slant on the short edges
        const float DiamondSide = 15f;                   // side of the tilted square
        const float DiamondSpan = DiamondSide * 1.41421356f;   // its width/height once tilted 45 degrees
        const float BubbleGap = 10f;

        static readonly Color Green  = new Color(0.20f, 0.92f, 0.27f, 1f);
        static readonly Color Yellow = new Color(1.00f, 0.86f, 0.12f, 1f);
        static readonly Color Red    = new Color(0.96f, 0.15f, 0.12f, 1f);
        static readonly Color Blue   = new Color(0.20f, 0.55f, 1.00f, 1f);
        static readonly Color FrameBg = new Color(0.02f, 0.02f, 0.04f, 0.85f);
        static readonly Color FrameLine = new Color(0.78f, 0.80f, 0.88f, 0.9f);
        static readonly Color ResLine = new Color(0.45f, 0.62f, 0.90f, 0.9f);
        static readonly Color RimOff = new Color(0.40f, 0.45f, 0.58f, 1f);
        static readonly Color RimOn = new Color(1f, 0.96f, 0.72f, 1f);
        static readonly Color CoreOff = new Color(0.04f, 0.05f, 0.09f, 0.95f);
        static readonly Color CoreOn = new Color(1f, 0.86f, 0.38f, 1f);

        readonly bool mirrored;                          // false = P1 (left), true = P2 (right)
        RectTransform healthFill, resourceFill;
        Image healthImg;
        readonly RectTransform[] bubbleRt = new RectTransform[RoundsToWin];
        readonly Image[] rim = new Image[RoundsToWin];
        readonly Image[] core = new Image[RoundsToWin];
        readonly float[] lit = new float[RoundsToWin];
        readonly float[] pop = new float[RoundsToWin];
        readonly bool[] wasLit = new bool[RoundsToWin];

        float health = MaxHealth, shownHealth = MaxHealth;
        float resource = 0f, shownResource = 0f;                     // resource starts empty each round
        int roundsWon;
        Color shownColor = Green;

        public float Health => health;
        public float Resource => resource;
        public int RoundsWon => roundsWon;

        public void SetHealth(float value) { health = Mathf.Clamp(value, 0f, MaxHealth); }
        public void SetResource(float value) { resource = Mathf.Clamp(value, 0f, MaxResource); }
        public void SetRoundsWon(int count) { roundsWon = Mathf.Clamp(count, 0, RoundsToWin); }

        // 100-70 green, 70-30 yellow, below 30 red
        static Color BandColor(float h)
        {
            if (h >= 70f) return Green;
            if (h >= 30f) return Yellow;
            return Red;
        }

        public PlayerHud(Transform parent, bool mirrored)
        {
            this.mirrored = mirrored;
            float s = mirrored ? -1f : 1f;                              // +1 = grows to the right, -1 = to the left
            float slant = s * SlantAmount;                              // P1 leans one way, P2 the mirror of it
            var corner = new Vector2(mirrored ? 1f : 0f, 1f);           // top-left for P1, top-right for P2

            var holder = BattleScene.NewRect(mirrored ? "Hud_P2" : "Hud_P1", parent);
            holder.anchorMin = corner;
            holder.anchorMax = corner;
            holder.pivot = corner;
            holder.anchoredPosition = new Vector2(s * MarginX, -MarginTop);
            holder.sizeDelta = new Vector2(BarWidth, 130f);

            var hFrame = NewCornerRect("HealthFrame", holder, corner, 0f, 0f, BarWidth, HealthHeight);
            var hBg = hFrame.gameObject.AddComponent<Image>();
            hBg.color = FrameBg;
            hBg.raycastTarget = false;
            var hLine = hFrame.gameObject.AddComponent<Outline>();
            hLine.effectColor = FrameLine;
            hLine.effectDistance = new Vector2(3, -3);
            hLine.useGraphicAlpha = false;
            hFrame.gameObject.AddComponent<SlantEffect>().amount = slant;

            var hInner = BattleScene.NewRect("Inner", hFrame);
            hInner.anchorMin = Vector2.zero;
            hInner.anchorMax = Vector2.one;
            hInner.offsetMin = new Vector2(4, 4);
            hInner.offsetMax = new Vector2(-4, -4);

            healthFill = BattleScene.NewRect("HealthFill", hInner);
            healthImg = healthFill.gameObject.AddComponent<Image>();
            healthImg.sprite = HudSprites.EdgeFade();
            healthImg.color = Green;
            healthImg.raycastTarget = false;
            healthFill.gameObject.AddComponent<SlantEffect>().amount = slant;

            float resY = HealthHeight + 8f;
            var rFrame = NewCornerRect("ResourceFrame", holder, corner, 0f, resY, BarWidth, ResourceHeight);
            var rBg = rFrame.gameObject.AddComponent<Image>();
            rBg.color = FrameBg;
            rBg.raycastTarget = false;
            var rLine = rFrame.gameObject.AddComponent<Outline>();
            rLine.effectColor = ResLine;
            rLine.effectDistance = new Vector2(2, -2);
            rLine.useGraphicAlpha = false;
            rFrame.gameObject.AddComponent<SlantEffect>().amount = slant;

            var rInner = BattleScene.NewRect("Inner", rFrame);
            rInner.anchorMin = Vector2.zero;
            rInner.anchorMax = Vector2.one;
            rInner.offsetMin = new Vector2(2, 2);
            rInner.offsetMax = new Vector2(-2, -2);

            resourceFill = BattleScene.NewRect("ResourceFill", rInner);
            var rImg = resourceFill.gameObject.AddComponent<Image>();
            rImg.color = Blue;
            rImg.raycastTarget = false;
            resourceFill.gameObject.AddComponent<SlantEffect>().amount = slant;

            // round-win diamonds, starting at the outer edge
            float bubY = resY + ResourceHeight + 12f;
            for (int i = 0; i < RoundsToWin; i++)
            {
                var b = BattleScene.NewRect("Diamond" + (i + 1), holder);
                b.anchorMin = corner;
                b.anchorMax = corner;
                b.pivot = new Vector2(0.5f, 0.5f);
                b.sizeDelta = new Vector2(DiamondSpan, DiamondSpan);
                b.anchoredPosition = new Vector2(s * (i * (DiamondSpan + BubbleGap) + DiamondSpan * 0.5f),
                                                 -(bubY + DiamondSpan * 0.5f));
                bubbleRt[i] = b;

                rim[i] = NewDiamond("Rim", b, DiamondSide);
                core[i] = NewDiamond("Core", b, DiamondSide - 6f);
            }

            ApplyBars();
            ApplyBubbles(0f);
        }

        static RectTransform NewCornerRect(string name, Transform parent, Vector2 corner, float x, float yDown, float w, float h)
        {
            var rt = BattleScene.NewRect(name, parent);
            rt.anchorMin = corner;
            rt.anchorMax = corner;
            rt.pivot = corner;
            rt.sizeDelta = new Vector2(w, h);
            rt.anchoredPosition = new Vector2(x, -yDown);
            return rt;
        }

        // A plain square turned 45 degrees.
        static Image NewDiamond(string name, Transform parent, float side)
        {
            var rt = BattleScene.NewRect(name, parent);
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(side, side);
            rt.anchoredPosition = Vector2.zero;
            rt.localEulerAngles = new Vector3(0f, 0f, 45f);
            var img = rt.gameObject.AddComponent<Image>();
            img.raycastTarget = false;
            return img;
        }

        public void Tick(float dt)
        {
            shownHealth = Mathf.MoveTowards(shownHealth, health, 90f * dt);
            shownResource = Mathf.MoveTowards(shownResource, resource, 120f * dt);

            // colour eases between bands as the bar drains past 70 and 30
            shownColor = Color.Lerp(shownColor, BandColor(shownHealth), 1f - Mathf.Exp(-12f * dt));
            ApplyBars();
            ApplyBubbles(dt);
        }

        void ApplyBars()
        {
            healthImg.color = shownColor;
            SetFill(healthFill, shownHealth / MaxHealth);
            SetFill(resourceFill, shownResource / MaxResource);
        }

        // The fill hugs the outer screen edge and drains towards the centre.
        void SetFill(RectTransform rt, float f)
        {
            f = Mathf.Clamp01(f);
            if (!mirrored)
            {
                rt.anchorMin = new Vector2(0f, 0f);
                rt.anchorMax = new Vector2(f, 1f);
            }
            else
            {
                rt.anchorMin = new Vector2(1f - f, 0f);
                rt.anchorMax = new Vector2(1f, 1f);
            }
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        void ApplyBubbles(float dt)
        {
            for (int i = 0; i < RoundsToWin; i++)
            {
                bool on = i < roundsWon;
                if (on && !wasLit[i]) pop[i] = 1f;          // pop animation when a round is won
                wasLit[i] = on;

                lit[i] = Mathf.MoveTowards(lit[i], on ? 1f : 0f, dt * 5f);
                pop[i] = Mathf.MoveTowards(pop[i], 0f, dt * 3f);
                float e = lit[i] * lit[i] * (3f - 2f * lit[i]);

                rim[i].color = Color.Lerp(RimOff, RimOn, e);
                core[i].color = Color.Lerp(CoreOff, CoreOn, e);

                float sc = 1f + 0.45f * pop[i];
                bubbleRt[i].localScale = new Vector3(sc, sc, 1f);
            }
        }

        // Full health, empty resource, no rounds won - call at the start of a new match.
        public void ResetAll()
        {
            health = MaxHealth;
            resource = 0f;
            roundsWon = 0;
        }

        // Full health, empty resource, rounds won are kept - call at the start of each round.
        public void ResetForRound()
        {
            health = MaxHealth;
            resource = 0f;
        }
    }
}
