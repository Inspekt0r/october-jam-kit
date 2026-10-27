using System;
using UnityEngine;

namespace JamKit
{
    [Serializable]
    public sealed class RoomInfo
    {
        public string id;
        public string sceneName;
        public string title;
        public Rect mapBounds = new Rect(0, 0, 180, 100);
    }

    [Serializable]
    public sealed class RoomLink
    {
        public string from;
        public string to;
    }

    [CreateAssetMenu(menuName = "Jam Kit/World Definition")]
    public sealed class WorldDefinition : ScriptableObject
    {
        public RoomInfo[] rooms = Array.Empty<RoomInfo>();
        public RoomLink[] links = Array.Empty<RoomLink>();
        public RoomInfo Find(string id) => Array.Find(rooms, room => room.id == id);
    }
}
