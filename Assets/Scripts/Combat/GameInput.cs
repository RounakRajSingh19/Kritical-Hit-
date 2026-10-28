// Default keys:
//   P1:  A / D  walk     F  punch     G  kick     H  block     T  special (fireball)
//   P2:  LEFT / RIGHT    ,  punch     .  kick     /  block     ;  special (shuriken)

using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace MysticKombat
{
    static class GameInput
    {
#if ENABLE_INPUT_SYSTEM
        static readonly Key[][] Map =
        {
            new[] { Key.A, Key.D, Key.F, Key.G, Key.H, Key.T },
            new[] { Key.LeftArrow, Key.RightArrow, Key.Comma, Key.Period, Key.Slash, Key.Semicolon },
        };

        public static bool Pressed(int player, Ctl c)
        {
            var k = Keyboard.current;
            return k != null && k[Map[player][(int)c]].wasPressedThisFrame;
        }

        public static bool Held(int player, Ctl c)
        {
            var k = Keyboard.current;
            return k != null && k[Map[player][(int)c]].isPressed;
        }
#elif ENABLE_LEGACY_INPUT_MANAGER
        static readonly KeyCode[][] Map =
        {
            new[] { KeyCode.A, KeyCode.D, KeyCode.F, KeyCode.G, KeyCode.H, KeyCode.T },
            new[] { KeyCode.LeftArrow, KeyCode.RightArrow, KeyCode.Comma, KeyCode.Period, KeyCode.Slash, KeyCode.Semicolon },
        };

        public static bool Pressed(int player, Ctl c) { return Input.GetKeyDown(Map[player][(int)c]); }
        public static bool Held(int player, Ctl c) { return Input.GetKey(Map[player][(int)c]); }
#else
        public static bool Pressed(int player, Ctl c) { return false; }
        public static bool Held(int player, Ctl c) { return false; }
#endif
    }
}
