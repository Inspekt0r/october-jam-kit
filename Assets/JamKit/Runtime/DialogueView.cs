using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace JamKit
{
    public sealed class DialogueView : MonoBehaviour
    {
        private GameApp app;
        private GameObject panel;
        private Text speaker, body, counter, hint;
        private Image portrait;
        private DialogueAsset current;
        private readonly HashSet<DialogueAsset> completed = new HashSet<DialogueAsset>();
        private int line, lastActionFrame;
        private bool waitForRelease;
        public bool IsOpen => current != null;
        public string DisplayedText => body != null ? body.text : "";
        public int LineIndex => line;
        public bool Completed(DialogueAsset asset) => completed.Contains(asset);

        public void Initialize(GameApp owner)
        {
            app = owner;
            var root = new GameObject("Dialogue UI", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            root.transform.SetParent(transform, false);
            root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay; root.GetComponent<Canvas>().sortingOrder = 60;
            var scaler = root.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720); scaler.matchWidthOrHeight = 0.5f;
            panel = Rect("Dialogue overlay", root.transform, Vector2.zero, Vector2.zero).gameObject;
            var full = panel.GetComponent<RectTransform>(); full.anchorMin = Vector2.zero; full.anchorMax = Vector2.one; full.sizeDelta = Vector2.zero;
            panel.AddComponent<Image>().color = new Color(0, 0, 0, 0.3f);
            var box = Rect("Dialogue box", panel.transform, new Vector2(0, 155), new Vector2(1120, 270));
            box.anchorMin = box.anchorMax = new Vector2(0.5f, 0);
            box.gameObject.AddComponent<Image>().color = new Color(0.06f, 0.09f, 0.14f, 0.98f);
            portrait = Rect("Portrait", box, new Vector2(-450, 20), new Vector2(145, 160)).gameObject.AddComponent<Image>();
            portrait.preserveAspect = true; portrait.raycastTarget = false;
            speaker = Label("Speaker", box, new Vector2(65, 96), new Vector2(820, 36), 25);
            speaker.color = new Color(0.4f, 0.9f, 0.76f);
            body = Label("Dialogue text", box, new Vector2(65, 9), new Vector2(820, 128), 23);
            body.alignment = TextAnchor.UpperLeft;
            counter = Label("Progress", box, new Vector2(-340, -104), new Vector2(370, 28), 16);
            Button("Next", box, new Vector2(275, -100), new Vector2(205, 42), "Далее · Enter", Next);
            Button("Close", box, new Vector2(460, -100), new Vector2(145, 42), "Закрыть · Esc", Close);
            hint = Label("Interaction prompt", root.transform, new Vector2(0, 65), new Vector2(760, 40), 22);
            hint.rectTransform.anchorMin = hint.rectTransform.anchorMax = new Vector2(0.5f, 0); hint.alignment = TextAnchor.MiddleCenter;
            panel.SetActive(false); hint.gameObject.SetActive(false);
        }

        private void Update()
        {
            if (app == null) return;
            if (IsOpen)
            {
                if (app.State != GameState.Dialogue) { CancelForStateChange(); return; }
                var keys = Keyboard.current;
                if (keys == null) return;
                if (waitForRelease)
                {
                    if (!keys.eKey.isPressed && !keys.enterKey.isPressed && !keys.spaceKey.isPressed && !keys.escapeKey.isPressed) waitForRelease = false;
                    return;
                }
                if (keys.escapeKey.wasPressedThisFrame) Close();
                else if (keys.eKey.wasPressedThisFrame || keys.enterKey.wasPressedThisFrame || keys.spaceKey.wasPressedThisFrame) Next();
                return;
            }
            DialogueActor nearest = null; float distance = float.PositiveInfinity;
            if (app.State == GameState.Playing && app.Player != null)
                foreach (var actor in DialogueActor.Active)
                {
                    if (actor == null || actor.automatic || !actor.Available || !actor.InReach(app.Player)) continue;
                    float next = Vector2.SqrMagnitude(actor.transform.position - app.Player.transform.position);
                    if (next < distance) { distance = next; nearest = actor; }
                }
            hint.gameObject.SetActive(nearest != null);
            if (nearest == null) return;
            hint.text = nearest.prompt;
            if (GameInput.InteractPressed) nearest.Interact();
        }

        public bool Begin(DialogueActor actor)
        {
            if (actor == null || !actor.Available || app.State != GameState.Playing || IsOpen) return false;
            current = actor.dialogue; line = 0; waitForRelease = true; lastActionFrame = Time.frameCount;
            app.Player?.CancelBufferedInput(); GameInput.SuppressActions();
            app.SetState(GameState.Dialogue); hint.gameObject.SetActive(false); panel.SetActive(true); Render(); return true;
        }
        private void Render()
        {
            var entry = current.lines[line]; speaker.text = entry.speaker ?? ""; body.text = entry.text ?? "";
            portrait.sprite = entry.portrait; portrait.gameObject.SetActive(entry.portrait != null);
            counter.text = $"{line + 1} / {current.lines.Length}    Space / E — далее";
        }
        public void Next()
        {
            if (!IsOpen || app.State != GameState.Dialogue || lastActionFrame == Time.frameCount) return;
            lastActionFrame = Time.frameCount;
            if (line + 1 < current.lines.Length) { line++; Render(); }
            else { completed.Add(current); Close(); }
        }
        public void Close()
        {
            if (!IsOpen) return;
            bool resume = app.State == GameState.Dialogue;
            CancelForStateChange();
            if (resume) app.SetState(GameState.Playing);
        }
        public void CancelForStateChange()
        {
            if (current != null) { GameInput.SuppressActions(); app.Player?.CancelBufferedInput(); }
            current = null;
            if (panel != null) panel.SetActive(false);
            if (hint != null) hint.gameObject.SetActive(false);
        }
        public void ResetSession() { CancelForStateChange(); completed.Clear(); }

        private static RectTransform Rect(string name, Transform parent, Vector2 position, Vector2 size)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>(); rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f); rect.anchoredPosition = position; rect.sizeDelta = size; return rect;
        }
        private static Text Label(string name, Transform parent, Vector2 position, Vector2 size, int fontSize)
        {
            var text = Rect(name, parent, position, size).gameObject.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); text.fontSize = fontSize; text.color = Color.white;
            text.alignment = TextAnchor.MiddleLeft; text.supportRichText = false; text.raycastTarget = false; return text;
        }
        private static void Button(string name, Transform parent, Vector2 position, Vector2 size, string title, UnityEngine.Events.UnityAction action)
        {
            var rect = Rect(name, parent, position, size); var image = rect.gameObject.AddComponent<Image>(); image.color = new Color(0.16f, 0.25f, 0.32f);
            var button = rect.gameObject.AddComponent<UnityEngine.UI.Button>(); button.targetGraphic = image;
            button.navigation = new Navigation { mode = Navigation.Mode.None }; button.onClick.AddListener(action);
            var text = Label("Label", rect, Vector2.zero, size, 18); text.text = title; text.alignment = TextAnchor.MiddleCenter;
        }
    }
}
