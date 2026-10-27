using UnityEngine;

namespace JamKit
{
    public enum RoomTriggerKind { Hazard, Checkpoint, Finish }

    [RequireComponent(typeof(BoxCollider2D))]
    public sealed class RoomTrigger : MonoBehaviour
    {
        public RoomTriggerKind kind;
        public Transform respawnPoint;
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (GameApp.Instance == null || GameApp.Instance.State != GameState.Playing) return;
            var player = other.GetComponent<PlayerMotor>();
            if (player == null) return;
            switch (kind)
            {
                case RoomTriggerKind.Hazard: player.Respawn(); break;
                case RoomTriggerKind.Checkpoint: player.SetCheckpoint(respawnPoint != null ? (Vector2)respawnPoint.position : (Vector2)transform.position); break;
                case RoomTriggerKind.Finish: GameApp.Instance.Complete(); break;
            }
        }
    }
}
