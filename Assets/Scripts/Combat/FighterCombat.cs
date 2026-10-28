using UnityEngine;

namespace MysticKombat
{
    // One per fighter: reads that player's keys, runs the move state machine,
    // and tells the Fighter how to look. P1 always stands left and faces right,
    // P2 the opposite (there is no jumping, so they can never cross).
    public class FighterCombat
    {
        static readonly Color HitFlash = new Color(1f, 0.35f, 0.30f, 1f);
        static readonly Color BlockFlash = new Color(0.50f, 0.80f, 1f, 1f);
        static readonly Color BlockTint = new Color(0.62f, 0.76f, 1f, 1f);
        static readonly Color ChargeTint = new Color(1f, 0.92f, 0.55f, 1f);
        static readonly Color KoTint = new Color(0.50f, 0.50f, 0.50f, 1f);

        public readonly int Player;                 // 0 = P1 (left), 1 = P2 (right)
        public readonly Fighter Body;
        public readonly PlayerHud Hud;
        public FighterCombat Opponent { get; private set; }
        public MoveState State { get; private set; }
        public bool InputEnabled = true;

        readonly BattleScene arena;
        float stateT, knockVel, flashT, pendingDamage;
        bool hitDone, spawned, hitPending;
        Color flashColor = Color.white;

        public float X { get { return Body.X; } }
        public float Dir { get { return Player == 0 ? 1f : -1f; } }     // +1 = facing right

        public FighterCombat(int player, Fighter body, PlayerHud hud, BattleScene arena)
        {
            Player = player;
            Body = body;
            Hud = hud;
            this.arena = arena;
        }

        public void SetOpponent(FighterCombat o) { Opponent = o; }

        public void Tick(float dt)
        {
            stateT += dt;
            if (flashT > 0f) flashT -= dt;

            if (knockVel != 0f)
            {
                SetX(X + knockVel * dt);
                knockVel = Mathf.MoveTowards(knockVel, 0f, 3000f * dt);
            }

            switch (State)
            {
                case MoveState.Idle:
                case MoveState.Walk:
                case MoveState.Block:
                    if (InputEnabled) FreeControl(dt); else State = MoveState.Idle;
                    break;
                case MoveState.Punch:
                    UpdateStrike(Moves.PunchStartup, Moves.PunchActive, Moves.PunchTotal, Moves.PunchReach, Moves.PunchDamage);
                    break;
                case MoveState.Kick:
                    UpdateStrike(Moves.KickStartup, Moves.KickActive, Moves.KickTotal, Moves.KickReach, Moves.KickDamage);
                    break;
                case MoveState.Special:
                    UpdateSpecial();
                    break;
                case MoveState.Hit:
                    if (stateT >= Moves.HitStun) State = MoveState.Idle;
                    break;
            }

            ApplyPose();
        }

        void FreeControl(float dt)
        {
            if (GameInput.Held(Player, Ctl.Block)) { State = MoveState.Block; return; }

            if (GameInput.Pressed(Player, Ctl.Special) && Hud.Resource >= Moves.SpecialCost) { Begin(MoveState.Special); return; }
            if (GameInput.Pressed(Player, Ctl.Punch)) { Begin(MoveState.Punch); return; }
            if (GameInput.Pressed(Player, Ctl.Kick)) { Begin(MoveState.Kick); return; }

            float move = (GameInput.Held(Player, Ctl.Right) ? 1f : 0f) - (GameInput.Held(Player, Ctl.Left) ? 1f : 0f);
            if (move != 0f)
            {
                SetX(X + move * Moves.WalkSpeed * dt);
                State = MoveState.Walk;
            }
            else
            {
                State = MoveState.Idle;
            }
        }

        void Begin(MoveState s)
        {
            State = s;
            stateT = 0f;
            hitDone = false;
            spawned = false;
        }

        void UpdateStrike(float startup, float active, float total, float reach, float damage)
        {
            // Only flags the hit; BattleScene resolves both fighters' hits together so
            // a simultaneous exchange trades instead of always favouring P1.
            if (!hitDone && stateT >= startup && stateT <= startup + active + 0.05f)
            {
                var o = Opponent;
                if (o.State != MoveState.KO && Mathf.Abs(o.X - X) <= 2f * Moves.BodyHalfWidth + reach)
                {
                    hitDone = true;
                    hitPending = true;
                    pendingDamage = damage;
                }
            }
            if (stateT >= total) State = MoveState.Idle;
        }

        void UpdateSpecial()
        {
            if (!spawned && stateT >= Moves.SpecialStartup)
            {
                spawned = true;
                if (Hud.Resource >= Moves.SpecialCost)       // checked again: getting hit mid-cast cancels it for free
                {
                    Hud.SetResource(Hud.Resource - Moves.SpecialCost);
                    arena.Shots.Spawn(this, Player == 0 ? ProjectileKind.Fireball : ProjectileKind.Shuriken);
                }
            }
            if (stateT >= Moves.SpecialTotal) State = MoveState.Idle;
        }

        public bool ConsumeHit(out float damage)
        {
            damage = pendingDamage;
            bool h = hitPending;
            hitPending = false;
            return h;
        }

        public void GainResource(float amount) { Hud.SetResource(Hud.Resource + amount); }

        // fromDir: +1 if the attack came travelling to the right, -1 to the left.
        public void TakeHit(float damage, float fromDir)
        {
            if (State == MoveState.KO) return;

            bool blocked = State == MoveState.Block;
            float dmg = blocked ? damage * (1f - Moves.BlockReduction) : damage;
            Hud.SetHealth(Hud.Health - dmg);

            flashT = 0.12f;
            flashColor = blocked ? BlockFlash : HitFlash;
            knockVel = fromDir * (blocked ? 250f : 600f);
            if (!blocked) Begin(MoveState.Hit);             // blocking isn't interrupted
        }

        public void Knockout()
        {
            Begin(MoveState.KO);
        }

        public void ResetRound(float startX)
        {
            State = MoveState.Idle;
            stateT = 0f;
            knockVel = 0f;
            flashT = 0f;
            hitPending = false;
            Body.X = startX;
            Hud.ResetForRound();
            ApplyPose();
        }

        // Stay inside the screen and never walk into / through the other fighter.
        void SetX(float x)
        {
            float limit = arena.WalkLimit;
            x = Mathf.Clamp(x, -limit, limit);
            float gap = 2f * Moves.BodyHalfWidth;
            if (Opponent != null)
                x = Player == 0 ? Mathf.Min(x, Opponent.X - gap) : Mathf.Max(x, Opponent.X + gap);
            Body.X = x;
        }

        // Works for both the dummy and real art.
        void ApplyPose()
        {
            float lunge = 0f, fall = 0f;
            int strike = 0;
            Color tint = Color.white;

            switch (State)
            {
                case MoveState.Punch:
                    if (stateT >= Moves.PunchStartup && stateT <= Moves.PunchStartup + Moves.PunchActive + 0.06f) { strike = 1; lunge = 18f; }
                    break;
                case MoveState.Kick:
                    if (stateT >= Moves.KickStartup && stateT <= Moves.KickStartup + Moves.KickActive + 0.06f) { strike = 2; lunge = 26f; }
                    break;
                case MoveState.Special:
                    if (stateT < Moves.SpecialStartup) { tint = ChargeTint; lunge = -12f; }
                    else lunge = 14f;
                    break;
                case MoveState.Block:
                    tint = BlockTint;
                    break;
                case MoveState.Hit:
                    lunge = -14f;
                    break;
                case MoveState.KO:
                    tint = KoTint;
                    fall = 80f;
                    break;
            }

            if (flashT > 0f) tint = flashColor;
            Body.SetPose(lunge, strike, tint, fall);
        }
    }
}
