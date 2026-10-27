using UnityEngine;

namespace JamKit
{
    public enum EnemyState { Idle, Patrol, Chase, Windup, Attack, Recovery, Dead }

    [RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D), typeof(Health))]
    public sealed class EnemyBrain : MonoBehaviour
    {
        public EnemyDefinition definition;
        public EnemyPresentation presentation;
        public EnemyState State { get; private set; } = EnemyState.Idle;
        public float Facing { get; private set; } = 1;
        public bool SeesPlayer { get; private set; }
        private Rigidbody2D body;
        private BoxCollider2D shape;
        private Health health;
        private Vector2 home, lastSeen;
        private float stateAt, seenAt = float.NegativeInfinity;
        private int previousHealth;
        private bool hit;
        private bool initialized;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>(); shape = GetComponent<BoxCollider2D>(); health = GetComponent<Health>();
            home = transform.position;
            if (definition == null) { Debug.LogError("Assign EnemyDefinition before enabling the enemy.", this); enabled = false; return; }
            health.maxHealth = Mathf.Max(1, definition.maxHealth); health.RestoreFull(); previousHealth = health.Current;
        }
        private void OnEnable() { health.Died += Die; health.Changed += HealthChanged; }
        private void OnDisable()
        {
            if (health != null) { health.Died -= Die; health.Changed -= HealthChanged; }
            if (body != null && body.simulated) body.linearVelocity = Vector2.zero;
        }
        private void Start() { if (State != EnemyState.Dead) Change(EnemyState.Idle); }

        private void FixedUpdate()
        {
            if (State == EnemyState.Dead) return;
            if (GameApp.Instance == null || GameApp.Instance.State != GameState.Playing) { Move(0); return; }
            var player = GameApp.Instance.Player;
            bool hadSight = SeesPlayer;
            SeesPlayer = player != null && Vector2.Distance(body.position, player.transform.position) <= definition.detectionRange &&
                !Physics2D.Linecast(shape.bounds.center, player.GetComponent<Collider2D>().bounds.center, definition.groundMask);
            if (SeesPlayer)
            {
                lastSeen = player.transform.position; seenAt = Time.time;
                if (!hadSight) presentation?.Alert();
            }
            float elapsed = Time.time - stateAt;
            if (State == EnemyState.Windup)
            {
                Move(0); if (elapsed >= definition.windup) Change(EnemyState.Attack); return;
            }
            if (State == EnemyState.Attack)
            {
                Move(0); HitPlayer(player);
                if (elapsed >= definition.activeTime) Change(EnemyState.Recovery);
                return;
            }
            if (State == EnemyState.Recovery)
            {
                Move(0); if (elapsed >= definition.recovery) Change(EnemyState.Idle); return;
            }
            if (player != null && Time.time - seenAt <= definition.loseSightDelay)
            {
                if (Mathf.Abs(lastSeen.x - body.position.x) > 0.05f) Face(Mathf.Sign(lastSeen.x - body.position.x));
                float gap = Mathf.Abs(player.transform.position.x - body.position.x) - shape.bounds.extents.x - player.GetComponent<Collider2D>().bounds.extents.x;
                bool aligned = Mathf.Abs(player.transform.position.y - body.position.y) < definition.attackHeight * 0.75f;
                if (SeesPlayer && aligned && gap <= definition.attackReach * 0.85f)
                { Change(EnemyState.Windup); Move(0); return; }
                Change(EnemyState.Chase);
                Move(Mathf.Abs(lastSeen.x - body.position.x) > 0.15f && CanStep(Facing) ? Facing * definition.chaseSpeed : 0);
                return;
            }
            if (definition.patrolDistance <= 0.01f) { Change(EnemyState.Idle); Move(0); return; }
            if (body.position.x <= home.x - definition.patrolDistance) Face(1);
            else if (body.position.x >= home.x + definition.patrolDistance) Face(-1);
            if (!CanStep(Facing)) Face(-Facing);
            Change(EnemyState.Patrol); Move(CanStep(Facing) ? Facing * definition.patrolSpeed : 0);
        }

        private bool CanStep(float direction)
        {
            var bounds = shape.bounds;
            bool floor = Physics2D.Raycast(new Vector2(bounds.center.x + direction * (bounds.extents.x + 0.2f), bounds.min.y + 0.1f), Vector2.down, 0.4f, definition.groundMask);
            bool wall = Physics2D.BoxCast(bounds.center, bounds.size * 0.85f, 0, Vector2.right * direction, 0.2f, definition.groundMask);
            return floor && !wall;
        }
        private void Move(float speed)
        {
            body.linearVelocity = new Vector2(speed, body.linearVelocity.y);
            presentation?.Walking(Mathf.Abs(speed) > 0.05f);
        }
        private void Face(float direction) { Facing = direction; presentation?.Face(direction); }
        private Vector2 AttackCenter => (Vector2)shape.bounds.center + Vector2.right * Facing * (shape.bounds.extents.x + definition.attackReach / 2);
        private void HitPlayer(PlayerMotor player)
        {
            if (hit || player == null) return;
            foreach (var collider in Physics2D.OverlapBoxAll(AttackCenter, new Vector2(definition.attackReach, definition.attackHeight), 0, 1 << 7))
            {
                if (collider.GetComponentInParent<PlayerMotor>() != player) continue;
                if (Physics2D.Linecast(shape.bounds.center, collider.ClosestPoint(shape.bounds.center), definition.groundMask)) continue;
                var target = player.GetComponent<Health>();
                if (target != null && target.TakeDamage(definition.damage)) hit = true;
            }
        }
        private void Change(EnemyState next)
        {
            if (initialized && next == State) return;
            initialized = true;
            State = next; stateAt = Time.time;
            if (next == EnemyState.Attack) hit = false;
            presentation?.SetState(next);
        }
        private void HealthChanged()
        {
            if (health.Current > 0 && health.Current < previousHealth) presentation?.Hurt();
            previousHealth = health.Current;
        }
        private void Die()
        {
            SeesPlayer = false;
            Change(EnemyState.Dead); shape.enabled = false; body.linearVelocity = Vector2.zero; body.simulated = false;
            float soundTime = definition.deathSound != null ? definition.deathSound.length : 0;
            Destroy(gameObject, Mathf.Max(definition.deathDuration, soundTime));
        }
        private void OnDrawGizmosSelected()
        {
            if (definition == null) return;
            Gizmos.color = Color.cyan; Gizmos.DrawWireSphere(transform.position, definition.detectionRange);
            Vector3 origin = Application.isPlaying ? (Vector3)home : transform.position;
            Gizmos.color = Color.green; Gizmos.DrawLine(origin + Vector3.left * definition.patrolDistance, origin + Vector3.right * definition.patrolDistance);
            var collider = GetComponent<BoxCollider2D>(); if (collider == null) return;
            Gizmos.color = State == EnemyState.Attack ? Color.red : Color.yellow;
            var center = (Vector2)collider.bounds.center + Vector2.right * Facing * (collider.bounds.extents.x + definition.attackReach / 2);
            Gizmos.DrawWireCube(center, new Vector2(definition.attackReach, definition.attackHeight));
        }
    }
}
