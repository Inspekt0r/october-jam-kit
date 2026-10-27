using UnityEngine;

namespace JamKit
{
    // Visibility overlay: leaves sprite, color, Animator and collider untouched.
    [DisallowMultipleComponent]
    public sealed class DamageFlash : MonoBehaviour
    {
        private SpriteRenderer image;
        private bool originalHidden;
        private float started, duration, interval;
        public bool IsPlaying => image != null;

        public void Play(SpriteRenderer target, float seconds, float blinkInterval)
        {
            Cancel();
            if (target == null || seconds <= 0) return;
            image = target; originalHidden = image.forceRenderingOff;
            started = Time.time; duration = seconds; interval = Mathf.Max(0.02f, blinkInterval);
            image.forceRenderingOff = true;
        }

        private void LateUpdate()
        {
            if (image == null) return;
            float elapsed = Time.time - started;
            if (elapsed >= duration) { Cancel(); return; }
            image.forceRenderingOff = originalHidden || Mathf.FloorToInt(elapsed / interval) % 2 == 0;
        }

        public void Cancel()
        {
            if (image != null) image.forceRenderingOff = originalHidden;
            image = null;
        }
        private void OnDisable() => Cancel();
    }
}
