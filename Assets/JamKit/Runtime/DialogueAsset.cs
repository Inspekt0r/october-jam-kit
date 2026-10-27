using System;
using UnityEngine;

namespace JamKit
{
    [Serializable]
    public struct DialogueLine
    {
        public string speaker;
        public Sprite portrait;
        [TextArea(2, 6)] public string text;
    }

    [CreateAssetMenu(menuName = "Jam Kit/Dialogue")]
    public sealed class DialogueAsset : ScriptableObject
    {
        public DialogueLine[] lines = Array.Empty<DialogueLine>();
    }
}
