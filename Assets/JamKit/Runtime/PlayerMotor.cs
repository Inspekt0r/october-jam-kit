using UnityEngine;

namespace JamKit
{
    [RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
    public sealed class PlayerMotor : MonoBehaviour
    {
        [Header("Movement")]
        [Min(0)] public float speed = 7;
        [Min(0)] public float acceleration = 55;
        [Min(0)] public float jumpSpeed = 13;
        [Range(0, 1)] public float jumpCut = 0.45f;
        [Min(0)] public float coyoteTime = 0.12f;
        [Min(0)] public float jumpBuffer = 0.12f;
        public LayerMask groundMask = 1 << 6;
        public float deathHeight = -12;
        public bool Grounded { get; private set; }
        public float Facing { get; private set; } = 1;
        public SpriteRenderer avatar;
        public Vector2 Checkpoint { get; private set; }
        public AudioSource effects;
        public AudioClip jumpSound;
        public AudioClip respawnSound;
        private Rigidbody2D body;
        private BoxCollider2D shape;
        private float groundedAt = float.NegativeInfinity;
        private float jumpAt = float.NegativeInfinity;
        private bool cutJump;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            shape = GetComponent<BoxCollider2D>();
            Checkpoint = transform.position;
        }

        private void Update()
        {
            if (GameApp.Instance == null || GameApp.Instance.State != GameState.Playing) return;
            if (GameInput.Move != 0) Facing = Mathf.Sign(GameInput.Move);
            if (avatar != null) avatar.flipX = Facing < 0;
            if (GameInput.JumpPressed) jumpAt = Time.time;
            if (GameInput.JumpReleased) cutJump = true;
            if (transform.position.y < deathHeight) Respawn();
        }

        private void FixedUpdate()
        {
            if (GameApp.Instance == null || GameApp.Instance.State != GameState.Playing) return;
            var bounds = shape.bounds;
            Grounded = body.linearVelocity.y <= 0.1f && Physics2D.BoxCast(
                new Vector2(bounds.center.x, bounds.min.y + 0.06f),
                new Vector2(bounds.size.x * 0.85f, 0.06f), 0, Vector2.down, 0.13f, groundMask);
            if (Grounded) groundedAt = Time.time;
            var velocity = body.linearVelocity;
            velocity.x = Mathf.MoveTowards(velocity.x, GameInput.Move * speed, acceleration * Time.fixedDeltaTime);
            if (Time.time - jumpAt <= jumpBuffer && Time.time - groundedAt <= coyoteTime)
            {
                velocity.y = jumpSpeed;
                jumpAt = groundedAt = float.NegativeInfinity;
                Grounded = false;
                cutJump = !GameInput.JumpHeld;
                PlaySound(jumpSound);
            }
            if (cutJump && velocity.y > 0) velocity.y *= jumpCut;
            cutJump = false;
            velocity.y = Mathf.Max(velocity.y, -24);
            body.linearVelocity = velocity;
        }

        public void SetCheckpoint(Vector2 position)
        {
            Checkpoint = position;
            GameApp.Instance?.RememberCheckpoint(position);
        }

        public void CancelBufferedInput()
        {
            jumpAt = float.NegativeInfinity; cutJump = false;
            if (body != null) body.linearVelocity = Vector2.zero;
            GetComponent<PlayerAttack>()?.Cancel();
        }

        public void EnterRoom(Vector2 position, Vector2 checkpoint)
        {
            Checkpoint = checkpoint;
            Teleport(position);
        }

        public void Respawn()
        {
            GetComponent<Health>()?.RestoreFull(true);
            Teleport(Checkpoint);
            PlaySound(respawnSound);
        }

        private void Teleport(Vector2 position)
        {
            GetComponent<PlayerAttack>()?.Cancel();
            body.position = position;
            transform.position = position;
            body.linearVelocity = Vector2.zero;
            jumpAt = groundedAt = float.NegativeInfinity;
            cutJump = false;
            Physics2D.SyncTransforms();
            FindFirstObjectByType<CameraFollow>()?.Snap();
        }

        private void PlaySound(AudioClip clip)
        {
            if (effects != null && clip != null) effects.PlayOneShot(clip, GameSettings.Current.effects);
        }
    }
}
