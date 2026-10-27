using System.Collections;
using UnityEngine;

namespace JamKit
{
    [RequireComponent(typeof(Health), typeof(BoxCollider2D))]
    public sealed class TrainingDummy : MonoBehaviour
    {
        [Min(0.1f)] public float resetAfter = 2;
        private Health health;
        private Collider2D shape;
        private SpriteRenderer[] visuals;
        private void Awake() { health = GetComponent<Health>(); shape = GetComponent<Collider2D>(); visuals = GetComponentsInChildren<SpriteRenderer>(); }
        private void OnEnable() => health.Died += ResetLater;
        private void OnDisable() => health.Died -= ResetLater;
        private void ResetLater() => StartCoroutine(ResetRoutine());
        private IEnumerator ResetRoutine()
        {
            shape.enabled = false;
            foreach (var image in visuals) image.enabled = false;
            yield return new WaitForSeconds(resetAfter);
            health.RestoreFull(); shape.enabled = true;
            foreach (var image in visuals) image.enabled = true;
        }
    }
}
