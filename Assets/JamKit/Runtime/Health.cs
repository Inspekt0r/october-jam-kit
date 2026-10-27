using System;
using UnityEngine;

namespace JamKit
{
    public sealed class Health : MonoBehaviour
    {
        [Min(1)] public int maxHealth = 5;
        [Min(0)] public float invulnerabilitySeconds;
        public int Current { get; private set; }
        public event Action Changed;
        public event Action Died;
        private float invulnerableUntil;
        private void Awake() => Current = Mathf.Max(1, maxHealth);

        public bool TakeDamage(int damage)
        {
            if (damage <= 0 || Current <= 0 || Time.time < invulnerableUntil) return false;
            if (GameApp.Instance != null && GameApp.Instance.State != GameState.Playing) return false;
            Current = Mathf.Max(0, Current - damage);
            invulnerableUntil = Time.time + invulnerabilitySeconds;
            Changed?.Invoke();
            if (Current == 0) Died?.Invoke();
            return true;
        }

        public void RestoreFull(bool protect = false)
        {
            Current = Mathf.Max(1, maxHealth);
            invulnerableUntil = protect ? Time.time + invulnerabilitySeconds : 0;
            Changed?.Invoke();
        }
    }
}
