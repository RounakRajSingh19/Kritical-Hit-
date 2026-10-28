// All damage / timing / distance values live here.

using UnityEngine;

namespace MysticKombat
{
    public static class Moves
    {
        // damage
        public const float PunchDamage = 10f;
        public const float KickDamage = 20f;
        public const float FireballDamage = 25f;
        public const float ShurikenDamage = 15f;
        public const float BlockReduction = 0.5f;      // blocking takes 50% less from everything

        // resource
        public const float SpecialCost = 20f;
        public const float ResourcePerHit = 10f;       // gained by punch / kick when it CONNECTS (blocked hits count)

        // timings (seconds): hit lands during [startup, startup + active], move ends at total
        public const float PunchStartup = 0.08f, PunchActive = 0.10f, PunchTotal = 0.30f;
        public const float KickStartup = 0.16f, KickActive = 0.12f, KickTotal = 0.45f;
        public const float SpecialStartup = 0.25f, SpecialTotal = 0.55f;
        public const float HitStun = 0.25f;

        // distances (canvas units, 1920x1080 reference)
        public const float WalkSpeed = 420f;
        public const float BodyHalfWidth = 80f;        // half the width of a fighter's hurt box
        public const float PunchReach = 110f;          // how far past the body the move reaches
        public const float KickReach = 140f;
        public const float FireballSpeed = 700f;
        public const float ShurikenSpeed = 1100f;
        public const float ProjectileHeight = 250f;    // above the feet
    }
}
