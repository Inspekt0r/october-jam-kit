using UnityEngine;
using UnityEngine.UI;

namespace JamKit
{
    public sealed class PlayerHud : MonoBehaviour
    {
        private GameApp app;
        private GameObject panel;
        private RectTransform pips;
        private Image portrait;
        private Text count;
        private Health health;
        private ArtTheme theme;
        public string HealthText => count != null ? count.text : "";

        public void Initialize(GameApp owner)
        {
            app = owner; theme = Resources.Load<ArtTheme>("ArtTheme");
            var root = new GameObject("Player status UI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            root.transform.SetParent(transform, false);
            root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            root.GetComponent<Canvas>().sortingOrder = 20;
            var scaler = root.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720); scaler.matchWidthOrHeight = 0.5f;
            var rect = MakeRect("Portrait and health", root.transform, new Vector2(220, -125), new Vector2(400, 80));
            rect.anchorMin = rect.anchorMax = new Vector2(0, 1); panel = rect.gameObject;
            portrait = MakeRect("Player portrait", rect, new Vector2(-170, 0), new Vector2(60, 60)).gameObject.AddComponent<Image>();
            portrait.preserveAspect = true; portrait.raycastTarget = false;
            pips = MakeRect("Health pips", rect, new Vector2(0, 10), new Vector2(280, 25));
            count = MakeRect("Health count", rect, new Vector2(10, -24), new Vector2(270, 28)).gameObject.AddComponent<Text>();
            count.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); count.fontSize = 19;
            count.color = Color.white; count.alignment = TextAnchor.MiddleLeft; count.raycastTarget = false;
            ArtTheme.Changed += Render;
        }

        private void LateUpdate()
        {
            if (app == null) return;
            var next = app.Player != null ? app.Player.GetComponent<Health>() : null;
            if (next != health)
            {
                if (health != null) health.Changed -= Render;
                health = next;
                if (health != null) health.Changed += Render;
                Render();
            }
            panel.SetActive(app.State == GameState.Playing && health != null);
        }

        private void Render()
        {
            if (this == null || pips == null || health == null) return;
            foreach (Transform child in pips) { child.gameObject.SetActive(false); Destroy(child.gameObject); }
            count.text = $"HP {health.Current}/{health.maxHealth}   /   J / LMB: attack";
            portrait.sprite = theme != null ? (theme.playerPortrait != null ? theme.playerPortrait : theme.player) : null;
            portrait.color = portrait.sprite != null ? Color.white : new Color(0.35f, 0.86f, 0.68f);
            for (int i = 0; i < Mathf.Min(health.maxHealth, 10); i++)
            {
                bool full = i < health.Current;
                var pip = MakeRect("Health " + (i + 1), pips, new Vector2(-115 + i * 26, 0), new Vector2(21, 21)).gameObject.AddComponent<Image>();
                pip.sprite = theme != null ? (full ? theme.healthFull : theme.healthEmpty) : null;
                pip.color = pip.sprite != null ? Color.white : (full ? new Color(0.35f, 0.86f, 0.68f) : new Color(0.23f, 0.28f, 0.34f));
                pip.preserveAspect = true; pip.raycastTarget = false;
            }
        }

        private static RectTransform MakeRect(string name, Transform parent, Vector2 position, Vector2 size)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>(); rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f); rect.anchoredPosition = position; rect.sizeDelta = size; return rect;
        }
        private void OnDestroy()
        {
            ArtTheme.Changed -= Render;
            if (health != null) health.Changed -= Render;
        }
    }
}
