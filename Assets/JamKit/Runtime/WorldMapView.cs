using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace JamKit
{
    public sealed class WorldMapView : MonoBehaviour
    {
        public int VisibleRoomCount { get; private set; }
        private GameApp app;
        private GameObject panel;
        private RectTransform content;
        private Image fade;
        private Button close;
        private Text status;
        private Font font;

        public void Initialize(GameApp owner)
        {
            app = owner;
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var canvasObject = new GameObject("World Map UI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 30;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);
            scaler.matchWidthOrHeight = 0.5f;
            panel = Box("World map", canvas.transform, Vector2.zero, Vector2.zero, new Color(0.035f, 0.055f, 0.08f, 0.98f)).gameObject;
            var full = (RectTransform)panel.transform;
            full.anchorMin = Vector2.zero; full.anchorMax = Vector2.one;
            full.offsetMin = full.offsetMax = Vector2.zero;
            Label(panel.transform, "WORLD MAP", new Vector2(0, 270), new Vector2(900, 60), 38);
            status = Label(panel.transform, "", new Vector2(0, 220), new Vector2(1000, 40), 18);
            content = CreateRect("Discovered rooms", panel.transform, new Vector2(0, -5), new Vector2(960, 390));
            Label(panel.transform, "Green: current room   /   White dot: you   /   Tab or Esc: close", new Vector2(0, -235), new Vector2(1050, 35), 18);
            var buttonImage = Box("Close map", panel.transform, new Vector2(0, -285), new Vector2(260, 44), Color.white);
            close = buttonImage.gameObject.AddComponent<Button>();
            buttonImage.raycastTarget = true;
            close.targetGraphic = buttonImage;
            var colors = close.colors; colors.normalColor = new Color(0.13f, 0.23f, 0.29f);
            colors.selectedColor = colors.highlightedColor = new Color(0.15f, 0.43f, 0.38f); close.colors = colors;
            Label(buttonImage.transform, "Close map", Vector2.zero, new Vector2(250, 44), 20);
            close.onClick.AddListener(app.Resume);
            fade = Box("Transition fade", canvas.transform, Vector2.zero, Vector2.zero, Color.clear);
            var fadeRect = fade.rectTransform;
            fadeRect.anchorMin = Vector2.zero; fadeRect.anchorMax = Vector2.one;
            fadeRect.offsetMin = fadeRect.offsetMax = Vector2.zero;
            fade.raycastTarget = false;
            panel.SetActive(false);
        }

        public void Show(bool visible)
        {
            if (panel == null) return;
            panel.SetActive(visible);
            if (!visible) return;
            Render();
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(close.gameObject);
        }

        private void Render()
        {
            foreach (Transform child in content) { child.gameObject.SetActive(false); Destroy(child.gameObject); }
            VisibleRoomCount = 0;
            bool hasBounds = false;
            var bounds = new Rect();
            if (app.World != null)
            {
                foreach (var room in app.World.rooms)
                {
                    if (!app.Progress.Visited(room.id)) continue;
                    VisibleRoomCount++;
                    bounds = !hasBounds ? room.mapBounds : Rect.MinMaxRect(
                        Mathf.Min(bounds.xMin, room.mapBounds.xMin), Mathf.Min(bounds.yMin, room.mapBounds.yMin),
                        Mathf.Max(bounds.xMax, room.mapBounds.xMax), Mathf.Max(bounds.yMax, room.mapBounds.yMax));
                    hasBounds = true;
                }
            }
            status.text = VisibleRoomCount == 0 ? "No rooms explored yet." : $"Explored: {VisibleRoomCount}   /   {app.World.Find(app.CurrentRoom != null ? app.CurrentRoom.roomId : "")?.title ?? "Outside world"}";
            if (!hasBounds) return;
            float scale = Mathf.Min(1.7f, 920 / Mathf.Max(1, bounds.width), 360 / Mathf.Max(1, bounds.height));
            foreach (var link in app.World.links)
            {
                var from = app.World.Find(link.from); var to = app.World.Find(link.to);
                if (from == null || to == null || !app.Progress.Visited(from.id) || !app.Progress.Visited(to.id)) continue;
                Vector2 a = (from.mapBounds.center - bounds.center) * scale;
                Vector2 b = (to.mapBounds.center - bounds.center) * scale;
                var line = Box("Explored connection", content, (a + b) / 2, new Vector2(Vector2.Distance(a, b), 5), new Color(0.3f, 0.48f, 0.53f));
                line.rectTransform.localEulerAngles = new Vector3(0, 0, Mathf.Atan2(b.y - a.y, b.x - a.x) * Mathf.Rad2Deg);
            }
            foreach (var room in app.World.rooms)
            {
                if (!app.Progress.Visited(room.id)) continue;
                bool current = app.CurrentRoom != null && room.id == app.CurrentRoom.roomId;
                var size = room.mapBounds.size * scale;
                var card = Box(room.id, content, (room.mapBounds.center - bounds.center) * scale, size,
                    current ? new Color(0.16f, 0.44f, 0.37f) : new Color(0.15f, 0.24f, 0.32f));
                if (current)
                {
                    var outline = card.gameObject.AddComponent<Outline>();
                    outline.effectColor = new Color(0.35f, 0.86f, 0.68f); outline.effectDistance = new Vector2(3, 3);
                }
                Label(card.transform, room.title, new Vector2(0, size.y * 0.18f), size, Mathf.Clamp((int)(size.y * 0.16f), 12, 24));
                if (current && app.Player != null)
                {
                    var area = app.CurrentRoom.worldBounds;
                    var pos = app.Player.transform.position;
                    var dot = new Vector2(Mathf.InverseLerp(area.xMin, area.xMax, pos.x) - 0.5f,
                        Mathf.InverseLerp(area.yMin, area.yMax, pos.y) - 0.5f);
                    Box("Player position", card.transform, Vector2.Scale(dot, size * 0.9f), new Vector2(11, 11), Color.white);
                }
            }
        }

        public IEnumerator Fade(float target, float seconds)
        {
            float from = fade.color.a;
            for (float time = 0; time < seconds; time += Time.unscaledDeltaTime)
            {
                fade.color = new Color(0, 0, 0, Mathf.Lerp(from, target, time / seconds));
                yield return null;
            }
            fade.color = new Color(0, 0, 0, target);
        }

        private static RectTransform CreateRect(string name, Transform parent, Vector2 position, Vector2 size)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false); rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position; rect.sizeDelta = size;
            return rect;
        }
        private static Image Box(string name, Transform parent, Vector2 position, Vector2 size, Color color)
        {
            var image = CreateRect(name, parent, position, size).gameObject.AddComponent<Image>();
            image.color = color; image.raycastTarget = false;
            return image;
        }
        private Text Label(Transform parent, string text, Vector2 position, Vector2 size, int fontSize)
        {
            var label = CreateRect(text, parent, position, size).gameObject.AddComponent<Text>();
            label.font = font; label.text = text; label.fontSize = fontSize; label.alignment = TextAnchor.MiddleCenter;
            label.color = new Color(0.88f, 0.94f, 0.96f); label.raycastTarget = false;
            return label;
        }
    }
}
