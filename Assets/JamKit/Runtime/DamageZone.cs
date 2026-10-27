using UnityEngine;

namespace JamKit
{
    [RequireComponent(typeof(BoxCollider2D))]
    public sealed class DamageZone : MonoBehaviour
    {
        [Min(1)] public int damage = 1;
        private void OnTriggerStay2D(Collider2D other)
        {
            if (other.GetComponentInParent<PlayerMotor>() != null)
                other.GetComponentInParent<Health>()?.TakeDamage(damage);
        }
    }
}
