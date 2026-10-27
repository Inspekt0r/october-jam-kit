using System;
using UnityEngine;

namespace JamKit
{
    public enum VisualRole { Background, Wall, Platform, Player, Enemy, Boss, Door, Checkpoint, Hazard }

    [CreateAssetMenu(menuName = "Jam Kit/Art Theme")]
    public sealed class ArtTheme : ScriptableObject
    {
        public Sprite fallback;
        public Sprite background, wall, platform, player, enemy, boss, door, checkpoint, hazard;
        public Sprite playerPortrait, enemyPortrait, healthFull, healthEmpty;
        public static event Action Changed;
        public Sprite For(VisualRole role)
        {
            switch (role)
            {
                case VisualRole.Background: return background;
                case VisualRole.Wall: return wall;
                case VisualRole.Platform: return platform;
                case VisualRole.Player: return player;
                case VisualRole.Enemy: return enemy;
                case VisualRole.Boss: return boss;
                case VisualRole.Door: return door;
                case VisualRole.Checkpoint: return checkpoint;
                case VisualRole.Hazard: return hazard;
                default: return null;
            }
        }
        private void OnValidate()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.delayCall += Notify;
#endif
        }
        private void Notify() { if (this != null) Changed?.Invoke(); }
    }
}
