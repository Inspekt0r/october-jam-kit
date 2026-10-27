using UnityEngine;

namespace JamKit
{
    [RequireComponent(typeof(BoxCollider2D))]
    public sealed class RoomDoor : MonoBehaviour
    {
        public string destinationRoom;
        public string destinationEntrance;
        private void OnTriggerStay2D(Collider2D other)
        {
            var player = other.GetComponent<PlayerMotor>();
            if (player != null && GameApp.Instance != null)
                GameApp.Instance.Travel(destinationRoom, destinationEntrance);
        }
    }
}
