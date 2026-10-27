using System;
using UnityEngine;

namespace JamKit
{
    [Serializable]
    public sealed class RoomEntrance
    {
        public string id;
        public Transform point;
    }

    public sealed class WorldRoom : MonoBehaviour
    {
        public string roomId;
        public GameObject playerPrefab;
        public Transform defaultSpawn;
        public RoomEntrance[] entrances = Array.Empty<RoomEntrance>();
        public Rect worldBounds = new Rect(-13, -2.5f, 26, 8);

        public Vector2 Spawn(string entrance)
        {
            var entry = Array.Find(entrances, item => item.id == entrance && item.point != null);
            if (entry != null) return entry.point.position;
            if (!string.IsNullOrEmpty(entrance)) Debug.LogError($"Room {roomId}: entrance '{entrance}' is missing.", this);
            return defaultSpawn != null ? (Vector2)defaultSpawn.position : Vector2.zero;
        }
    }
}
