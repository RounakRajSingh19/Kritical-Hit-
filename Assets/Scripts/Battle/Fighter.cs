using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MysticKombat
{
    // If Assets/Resources/<spriteName>.png exists it is used (scaled to Fighter.Height,
    // aspect kept, art should face RIGHT); otherwise a blocky dummy in the given colour.
    // FighterCombat drives it through X / SetPose.
    public class Fighter
    {
        public const float Height = 420f;        // standing height in canvas units
        public const float GroundY = 140f;       // feet distance from the bottom of the screen

        public RectTransform Rt { get; private set; }
        public bool FacingRight { get; private set; }
        public bool UsesCustomArt { get; private set; }

        RectTransform body;                       // child of Rt that gets lunges / falls
        readonly List<Graphic> graphics = new List<Graphic>();
        readonly List<Color> baseColors = new List<Color>();
        GameObject punchLimb, kickLimb;           // dummy only: shown while a strike is active
        int shownStrike;

        public float X
        {
            get { return Rt.anchoredPosition.x; }
            set { var p = Rt.anchoredPosition; p.x = value; Rt.anchoredPosition = p; }
        }

        public void Face(bool right)
        {
            FacingRight = right;
            Rt.localScale = new Vector3(right ? 1f : -1f, 1f, 1f);
        }

        // lunge: sideways nudge in the facing direction; strike: 0 none, 1 punch, 2 kick;
        // tint: multiplies the colours; fallAngle: degrees, tips the body backwards (K.O.).
        public void SetPose(float lunge, int strike, Color tint, float fallAngle)
        {
            body.anchoredPosition = new Vector2(lunge, 0f);
            body.localEulerAngles = new Vector3(0f, 0f, fallAngle);

            if (strike != shownStrike)
            {
                shownStrike = strike;
                if (punchLimb != null) punchLimb.SetActive(strike == 1);
                if (kickLimb != null) kickLimb.SetActive(strike == 2);
            }

            for (int i = 0; i < graphics.Count; i++) graphics[i].color = baseColors[i] * tint;
        }

        public static Fighter Create(Transform parent, string name, string spriteName, Color tint, float x, bool facingRight)
        {
            var f = new Fighter();
            f.Rt = BattleScene.NewRect(name, parent);
            f.Rt.anchorMin = f.Rt.anchorMax = new Vector2(0.5f, 0f);
            f.Rt.pivot = new Vector2(0.5f, 0f);               // feet are the origin
            f.Rt.anchoredPosition = new Vector2(x, GroundY);

            var tex = Resources.Load<Texture2D>(spriteName);
            f.Rt.sizeDelta = tex != null ? new Vector2(Height * tex.width / tex.height, Height) : new Vector2(240f, Height);

            f.body = BattleScene.NewRect("Body", f.Rt);
            f.body.pivot = new Vector2(0.5f, 0f);
            f.body.anchorMin = Vector2.zero;
            f.body.anchorMax = Vector2.one;
            f.body.offsetMin = Vector2.zero;
            f.body.offsetMax = Vector2.zero;

            if (tex != null)
            {
                f.UsesCustomArt = true;
                var art = BattleScene.NewRect("Art", f.body);
                art.anchorMin = Vector2.zero;
                art.anchorMax = Vector2.one;
                art.offsetMin = Vector2.zero;
                art.offsetMax = Vector2.zero;
                var img = art.gameObject.AddComponent<RawImage>();
                img.texture = tex;
                img.raycastTarget = false;
                f.Track(img);
            }
            else
            {
                f.BuildDummy(tint);
            }

            f.Face(facingRight);
            return f;
        }

        void Track(Graphic g)
        {
            graphics.Add(g);
            baseColors.Add(g.color);
        }

        void BuildDummy(Color c)
        {
            Color dark = c * 0.55f;  dark.a = 1f;
            Color mid = c * 0.8f;    mid.a = 1f;
            Color light = Color.Lerp(c, Color.white, 0.35f);

            Box("LegL", -34f, 0f, 58f, 190f, dark);
            Box("LegR", 34f, 0f, 58f, 190f, dark);
            Box("ArmL", -98f, 185f, 40f, 150f, mid);
            Box("ArmR", 98f, 185f, 40f, 150f, mid);
            Box("Torso", 0f, 180f, 150f, 160f, c);
            Box("Head", 0f, 335f, 85f, 85f, light);
            Box("Eye", 20f, 375f, 26f, 14f, Color.white);          // marks which way it faces

            // limbs that stick out while punching / kicking (hidden otherwise)
            punchLimb = Box("PunchArm", 130f, 262f, 110f, 38f, light).gameObject;
            kickLimb = Box("KickLeg", 140f, 110f, 160f, 46f, light).gameObject;
            punchLimb.SetActive(false);
            kickLimb.SetActive(false);
        }

        // Rect standing on the bottom-centre of the body; x is its centre, bottomY its base.
        Image Box(string name, float x, float bottomY, float w, float h, Color c)
        {
            var rt = BattleScene.NewRect(name, body);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.sizeDelta = new Vector2(w, h);
            rt.anchoredPosition = new Vector2(x, bottomY);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = c;
            img.raycastTarget = false;
            Track(img);
            return img;
        }
    }
}
