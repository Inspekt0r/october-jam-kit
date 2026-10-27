using UnityEngine;

namespace JamKit
{
    [RequireComponent(typeof(AudioSource))]
    public sealed class MusicChannel : MonoBehaviour
    {
        private AudioSource source;
        private void Awake() => source = GetComponent<AudioSource>();
        private void OnEnable() { GameSettings.Changed += Apply; Apply(); }
        private void OnDisable() => GameSettings.Changed -= Apply;
        private void Apply() => source.volume = GameSettings.Current.music;
    }
}
