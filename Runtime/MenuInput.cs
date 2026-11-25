using System;
using UnityEngine;
#if ATTHREE_INPUT_SYSTEM && ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Atthree.Menus
{
    /// <summary>
    /// Back and pause buttons for both input systems: Escape, gamepad B / East (back) and Start (pause).
    /// Replace <see cref="BackPressed"/> / <see cref="PausePressed"/> to use your own input actions.
    /// </summary>
    public static class MenuInput
    {
        /// <summary>Returns true on the frame "back" is pressed. Default: Escape or gamepad B.</summary>
        public static Func<bool> BackPressed = DefaultBack;

        /// <summary>Returns true on the frame "pause" is pressed. Default: Escape or gamepad Start.</summary>
        public static Func<bool> PausePressed = DefaultPause;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            BackPressed = DefaultBack;
            PausePressed = DefaultPause;
        }

        public static bool DefaultBack()
        {
#if ATTHREE_INPUT_SYSTEM && ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) return true;
            if (Gamepad.current != null && Gamepad.current.buttonEast.wasPressedThisFrame) return true;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.JoystickButton1)) return true;
#endif
            return false;
        }

        public static bool DefaultPause()
        {
#if ATTHREE_INPUT_SYSTEM && ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) return true;
            if (Gamepad.current != null && Gamepad.current.startButton.wasPressedThisFrame) return true;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.JoystickButton7)) return true;
#endif
            return false;
        }
    }
}
