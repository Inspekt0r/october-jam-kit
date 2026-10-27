using UnityEngine;
using UnityEngine.InputSystem;

namespace JamKit
{
    // Keyboard.current uses physical keys, so WASD also works with a Russian layout.
    public static class GameInput
    {
        private static bool waitForNeutral;
        private static int suppressedFrame = -1;
        public static void SuppressActions() { waitForNeutral = true; suppressedFrame = Time.frameCount; }
        private static bool ActionsBlocked
        {
            get
            {
                if (Time.frameCount == suppressedFrame) return true;
                if (!waitForNeutral) return false;
                var keys = Keyboard.current;
                if ((keys != null && (keys.spaceKey.isPressed || keys.wKey.isPressed || keys.upArrowKey.isPressed ||
                    keys.jKey.isPressed || keys.eKey.isPressed || keys.enterKey.isPressed || keys.escapeKey.isPressed)) ||
                    (Mouse.current != null && Mouse.current.leftButton.isPressed)) return true;
                waitForNeutral = false; return false;
            }
        }
        public static float Move
        {
            get
            {
                var keys = Keyboard.current;
                if (keys == null) return 0;
                return (keys.dKey.isPressed || keys.rightArrowKey.isPressed ? 1 : 0)
                    - (keys.aKey.isPressed || keys.leftArrowKey.isPressed ? 1 : 0);
            }
        }

        public static bool JumpPressed => !ActionsBlocked && Keyboard.current != null &&
            (Keyboard.current.spaceKey.wasPressedThisFrame || Keyboard.current.wKey.wasPressedThisFrame || Keyboard.current.upArrowKey.wasPressedThisFrame);
        public static bool JumpHeld => Keyboard.current != null &&
            (Keyboard.current.spaceKey.isPressed || Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed);
        public static bool JumpReleased => Keyboard.current != null &&
            !Keyboard.current.spaceKey.isPressed && !Keyboard.current.wKey.isPressed && !Keyboard.current.upArrowKey.isPressed &&
            (Keyboard.current.spaceKey.wasReleasedThisFrame || Keyboard.current.wKey.wasReleasedThisFrame || Keyboard.current.upArrowKey.wasReleasedThisFrame);
        public static bool Back => !ActionsBlocked && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
        public static bool Restart => !ActionsBlocked && Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame;
        public static bool MapPressed => !ActionsBlocked && Keyboard.current != null && Keyboard.current.tabKey.wasPressedThisFrame;
        public static bool InteractPressed => !ActionsBlocked && Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame;
        public static bool AttackPressed => !ActionsBlocked && ((Keyboard.current != null && Keyboard.current.jKey.wasPressedThisFrame) ||
            (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame));
        public static bool AttackHeld => (Keyboard.current != null && Keyboard.current.jKey.isPressed) ||
            (Mouse.current != null && Mouse.current.leftButton.isPressed);
    }
}
