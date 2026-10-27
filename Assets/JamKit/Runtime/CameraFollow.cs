using UnityEngine;

namespace JamKit
{
    public sealed class CameraFollow : MonoBehaviour
    {
        public Transform target;
        public Vector2 offset = new Vector2(0, 1);
        public Vector2 min = new Vector2(-5, -2);
        public Vector2 max = new Vector2(22, 9);
        [Min(0.01f)] public float smoothTime = 0.18f;
        private Vector3 velocity;

        private Vector3 Desired()
        {
            var p = target.position + (Vector3)offset;
            return new Vector3(Mathf.Clamp(p.x, min.x, max.x), Mathf.Clamp(p.y, min.y, max.y), -10);
        }

        private void Start() => Snap();
        private void LateUpdate()
        {
            if (target != null) transform.position = Vector3.SmoothDamp(transform.position, Desired(), ref velocity, smoothTime);
        }
        public void Snap()
        {
            if (target == null) return;
            velocity = Vector3.zero;
            transform.position = Desired();
        }
    }
}
