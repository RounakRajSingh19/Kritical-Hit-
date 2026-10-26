using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MysticKombat
{
    public class Projectiles
    {
        class Shot
        {
            public RectTransform rt, spinner;
            public FighterCombat owner;
            public float x, dir, speed, damage, radius, age;
        }

        static readonly Color Silver = new Color(0.86f, 0.89f, 0.96f, 1f);

        readonly Transform parent;
        readonly BattleScene arena;
        readonly List<Shot> shots = new List<Shot>();

        public Projectiles(Transform parent, BattleScene arena)
        {
            this.parent = parent;
            this.arena = arena;
        }

        public void Spawn(FighterCombat owner, ProjectileKind kind)
        {
            bool fire = kind == ProjectileKind.Fireball;
            var s = new Shot();
            s.owner = owner;
            s.dir = owner.Dir;
            s.x = owner.X + s.dir * (Moves.BodyHalfWidth + 30f);
            s.speed = fire ? Moves.FireballSpeed : Moves.ShurikenSpeed;
            s.damage = fire ? Moves.FireballDamage : Moves.ShurikenDamage;
            s.radius = fire ? 32f : 28f;

            s.rt = BattleScene.NewRect(fire ? "Fireball" : "Shuriken", parent);
            s.rt.anchorMin = s.rt.anchorMax = new Vector2(0.5f, 0f);
            s.rt.pivot = new Vector2(0.5f, 0.5f);
            s.rt.sizeDelta = new Vector2(64f, 64f);
            s.rt.anchoredPosition = new Vector2(s.x, Fighter.GroundY + Moves.ProjectileHeight);

            if (fire)
            {
                AddImage(s.rt, "Glow", HudSprites.Glow(), 170f, new Color(1f, 0.45f, 0.10f, 0.55f));
                AddImage(s.rt, "Body", HudSprites.Circle(), 64f, new Color(1f, 0.42f, 0.06f, 1f));
                AddImage(s.rt, "Core", HudSprites.Circle(), 34f, new Color(1f, 0.92f, 0.45f, 1f));
            }
            else
            {
                s.spinner = AddImage(s.rt, "Star", HudSprites.Star(), 60f, Silver).rectTransform;
            }

            shots.Add(s);
        }

        static Image AddImage(Transform parent, string name, Sprite sprite, float size, Color c)
        {
            var rt = BattleScene.NewRect(name, parent);
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(size, size);
            rt.anchoredPosition = Vector2.zero;
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.color = c;
            img.raycastTarget = false;
            return img;
        }

        public void Tick(float dt)
        {
            float offscreen = arena.WalkLimit + 400f;

            for (int i = shots.Count - 1; i >= 0; i--)
            {
                var s = shots[i];
                s.age += dt;
                s.x += s.dir * s.speed * dt;

                var p = s.rt.anchoredPosition;
                p.x = s.x;
                s.rt.anchoredPosition = p;

                if (s.spinner != null) s.spinner.localEulerAngles = new Vector3(0f, 0f, -s.dir * 1100f * s.age);
                else s.rt.localScale = Vector3.one * (1f + 0.08f * Mathf.Sin(s.age * 30f));

                var target = s.owner.Opponent;
                if (target.State != MoveState.KO && Mathf.Abs(target.X - s.x) <= Moves.BodyHalfWidth + s.radius)
                {
                    target.TakeHit(s.damage, s.dir);
                    Remove(i);
                    continue;
                }

                if (Mathf.Abs(s.x) > offscreen) Remove(i);
            }
        }

        void Remove(int i)
        {
            Object.Destroy(shots[i].rt.gameObject);
            shots.RemoveAt(i);
        }

        public void Clear()
        {
            for (int i = shots.Count - 1; i >= 0; i--) Remove(i);
        }
    }
}
