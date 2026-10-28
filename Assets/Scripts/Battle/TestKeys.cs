using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace MysticKombat
{
    // Digits 1..9 then 0 map to index 0..9.
    static class TestKeys
    {
#if ENABLE_INPUT_SYSTEM
        static readonly Key[] Digits =
        {
            Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4, Key.Digit5,
            Key.Digit6, Key.Digit7, Key.Digit8, Key.Digit9, Key.Digit0
        };
#endif
        public static bool Down(int index)
        {
#if ENABLE_INPUT_SYSTEM
            var k = Keyboard.current;
            return k != null && k[Digits[index]].wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(index < 9 ? KeyCode.Alpha1 + index : KeyCode.Alpha0);
#else
            return false;
#endif
        }
    }
}
