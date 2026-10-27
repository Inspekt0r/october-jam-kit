using UnityEngine;

namespace JamKit
{
    [ExecuteAlways, RequireComponent(typeof(SpriteRenderer))]
    public sealed class SpriteStyle : MonoBehaviour
    {
        public ArtTheme theme;
        public VisualRole role;
        public Sprite overrideSprite;
        public Vector2 size = Vector2.one;
        public Color tint = Color.white;
        public Color placeholderColor = Color.white;
        public int sortingOrder;

        private void OnEnable() { ArtTheme.Changed += Apply; Apply(); }
        private void OnDisable() => ArtTheme.Changed -= Apply;
        private void OnValidate()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.delayCall += Apply;
#endif
        }

        public void Apply()
        {
            if (this == null) return;
            if (theme == null) theme = Resources.Load<ArtTheme>("ArtTheme");
            if (theme == null) return;
            var image = GetComponent<SpriteRenderer>();
            var chosen = overrideSprite != null ? overrideSprite : theme.For(role);
            image.sprite = chosen != null ? chosen : theme.fallback;
            image.color = chosen != null ? tint : placeholderColor;
            image.sortingOrder = sortingOrder;
            if (image.sprite == null) return;
            var bounds = image.sprite.bounds.size;
            transform.localScale = new Vector3(Mathf.Max(0.01f, size.x) / Mathf.Max(0.001f, bounds.x),
                Mathf.Max(0.01f, size.y) / Mathf.Max(0.001f, bounds.y), 1);
        }
    }
}
