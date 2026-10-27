using UnityEngine;

namespace JamKit
{
    [RequireComponent(typeof(Health), typeof(PlayerMotor))]
    public sealed class PlayerVitals : MonoBehaviour
    {
        [Header("Damage feedback")]
        public bool flashOnHurt = true;
        [Min(0.02f)] public float hurtDuration = 0.35f;
        [Min(0.02f)] public float hurtBlinkInterval = 0.06f;
        public Animator hurtAnimator;
        public string hurtTrigger = "Hurt";
        private Health health;
        private PlayerMotor motor;
        private DamageFlash flash;
        private int previousHealth;
        private void Awake()
        {
            health = GetComponent<Health>(); motor = GetComponent<PlayerMotor>();
            flash = GetComponent<DamageFlash>() ?? gameObject.AddComponent<DamageFlash>();
        }
        private void OnEnable() { previousHealth = health.Current > 0 ? health.Current : Mathf.Max(1, health.maxHealth); health.Died += Die; health.Changed += HealthChanged; }
        private void OnDisable() { health.Died -= Die; health.Changed -= HealthChanged; flash.Cancel(); }
        private void HealthChanged()
        {
            bool damaged = health.Current > 0 && health.Current < previousHealth;
            if (health.Current >= previousHealth || health.Current == 0) flash.Cancel();
            previousHealth = health.Current;
            if (!damaged) return;
            if (flashOnHurt) flash.Play(motor.avatar, hurtDuration, hurtBlinkInterval);
            if (hurtAnimator == null || string.IsNullOrEmpty(hurtTrigger) || hurtAnimator.runtimeAnimatorController == null) return;
            foreach (var parameter in hurtAnimator.parameters)
                if (parameter.type == AnimatorControllerParameterType.Trigger && parameter.name == hurtTrigger)
                { hurtAnimator.SetTrigger(hurtTrigger); break; }
        }
        private void Die() { flash.Cancel(); motor.Respawn(); }
    }
}
