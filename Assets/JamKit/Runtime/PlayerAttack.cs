using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace JamKit
{
    [RequireComponent(typeof(PlayerMotor))]
    public sealed class PlayerAttack : MonoBehaviour
    {
        [Min(1)] public int damage = 1;
        [Min(0.01f)] public float reach = 1.35f;
        [Min(0.01f)] public float height = 1.1f;
        [Min(0)] public float windup = 0.06f;
        [Min(0.01f)] public float activeTime = 0.1f;
        [Min(0.01f)] public float cooldown = 0.35f;
        public LayerMask targetMask = 1 << 8;
        public LayerMask wallMask = 1 << 6;
        public Sprite attackSprite;
        private PlayerMotor motor;
        private BoxCollider2D body;
        private SpriteRenderer visual;
        private Coroutine attack;
        private float readyAt;
        private bool waitForRelease = true;
        public bool IsAttacking => attack != null;

        private void Awake()
        {
            motor = GetComponent<PlayerMotor>(); body = GetComponent<BoxCollider2D>();
            var child = new GameObject("Melee effect", typeof(SpriteRenderer)); child.transform.SetParent(transform, false);
            visual = child.GetComponent<SpriteRenderer>(); visual.sprite = attackSprite;
            visual.color = new Color(0.75f, 1, 0.9f, 0.65f); visual.sortingOrder = 25; visual.enabled = false;
        }

        private void Update()
        {
            if (GameApp.Instance == null || GameApp.Instance.State != GameState.Playing) { Cancel(); return; }
            if (waitForRelease)
            {
                if (GameInput.AttackHeld) return;
                waitForRelease = false;
            }
            if (GameInput.AttackPressed) TryAttack();
        }

        public bool TryAttack()
        {
            if (attack != null || Time.time < readyAt || GameApp.Instance == null || GameApp.Instance.State != GameState.Playing) return false;
            readyAt = Time.time + Mathf.Max(cooldown, windup + activeTime);
            attack = StartCoroutine(Swing());
            return true;
        }

        private Vector2 Center(float facing) => (Vector2)body.bounds.center + Vector2.right * facing * (body.bounds.extents.x + reach / 2);

        private IEnumerator Swing()
        {
            float facing = motor.Facing;
            yield return new WaitForSeconds(windup);
            var hit = new HashSet<Health>();
            for (float elapsed = 0; elapsed < activeTime; elapsed += Time.deltaTime)
            {
                if (GameApp.Instance.State != GameState.Playing) break;
                Vector2 center = Center(facing);
                if (visual.sprite != null)
                {
                    visual.enabled = true; visual.transform.position = center;
                    var spriteSize = visual.sprite.bounds.size; var parentScale = transform.lossyScale;
                    visual.transform.localScale = new Vector3(reach / Mathf.Max(0.001f, Mathf.Abs(parentScale.x) * spriteSize.x),
                        height / Mathf.Max(0.001f, Mathf.Abs(parentScale.y) * spriteSize.y), 1);
                    visual.flipX = facing < 0;
                }
                foreach (var collider in Physics2D.OverlapBoxAll(center, new Vector2(reach, height), 0, targetMask))
                {
                    var target = collider.GetComponentInParent<Health>();
                    if (target == null || target.gameObject == gameObject || hit.Contains(target)) continue;
                    Vector2 nearest = collider.ClosestPoint(body.bounds.center);
                    if (Physics2D.Linecast(body.bounds.center, nearest, wallMask)) continue;
                    hit.Add(target); target.TakeDamage(damage);
                }
                yield return null;
            }
            visual.enabled = false; attack = null;
        }

        public void Cancel()
        {
            waitForRelease = true;
            if (attack != null) StopCoroutine(attack);
            attack = null;
            if (visual != null) visual.enabled = false;
        }
        private void OnDisable() => Cancel();
    }
}
