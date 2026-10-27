using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace JamKit
{
    public sealed class GameView : MonoBehaviour
    {
        public GameObject menu, pause, settings, complete, loading, hud;
        public Button play, menuSettings, quit, resume, restart, pauseSettings, toMenu, again, completeMenu, back, reset;
        public Slider master, music, effects, brightness;
        public Toggle fullscreen, highQuality;
        public Image brightnessOverlay;
        private GameApp app;

        private void Update()
        {
            var keys = Keyboard.current;
            if (keys != null && keys.spaceKey.wasPressedThisFrame && EventSystem.current != null &&
                EventSystem.current.currentSelectedGameObject != null)
                ExecuteEvents.Execute(EventSystem.current.currentSelectedGameObject,
                    new BaseEventData(EventSystem.current), ExecuteEvents.submitHandler);
            if (keys == null || !keys.tabKey.wasPressedThisFrame || EventSystem.current == null) return;
            GameObject panel = null;
            switch (app.State)
            {
                case GameState.Menu: panel = menu; break;
                case GameState.Paused: panel = pause; break;
                case GameState.Settings: panel = settings; break;
                case GameState.Complete: panel = complete; break;
            }
            if (panel == null) return;
            var controls = System.Array.FindAll(panel.GetComponentsInChildren<Selectable>(), control => control.IsInteractable());
            if (controls.Length == 0) return;
            int current = System.Array.FindIndex(controls, control => control.gameObject == EventSystem.current.currentSelectedGameObject);
            int step = keys.leftShiftKey.isPressed || keys.rightShiftKey.isPressed ? -1 : 1;
            int next = current < 0 ? 0 : (current + step + controls.Length) % controls.Length;
            EventSystem.current.SetSelectedGameObject(controls[next].gameObject);
        }

        public void Bind(GameApp owner)
        {
            app = owner;
            play.onClick.AddListener(app.Play);
            menuSettings.onClick.AddListener(app.OpenSettings);
            quit.onClick.AddListener(app.Quit);
            resume.onClick.AddListener(app.Resume);
            restart.onClick.AddListener(app.Restart);
            pauseSettings.onClick.AddListener(app.OpenSettings);
            toMenu.onClick.AddListener(app.MainMenu);
            again.onClick.AddListener(app.Play);
            completeMenu.onClick.AddListener(app.MainMenu);
            back.onClick.AddListener(app.CloseSettings);
            reset.onClick.AddListener(() => { GameSettings.Reset(); SyncControls(); });
            master.onValueChanged.AddListener(value => { GameSettings.Current.master = value; GameSettings.Apply(); });
            music.onValueChanged.AddListener(value => { GameSettings.Current.music = value; GameSettings.Apply(); });
            effects.onValueChanged.AddListener(value => { GameSettings.Current.effects = value; GameSettings.Apply(); });
            brightness.onValueChanged.AddListener(value => { GameSettings.Current.brightness = value; GameSettings.Apply(); });
            fullscreen.onValueChanged.AddListener(value => { GameSettings.Current.fullscreen = value; GameSettings.Apply(); });
            highQuality.onValueChanged.AddListener(value => { GameSettings.Current.highQuality = value; GameSettings.Apply(); });
            GameSettings.Changed += UpdateBrightness;
            UpdateBrightness();
        }

        public void Show(GameState state)
        {
            menu.SetActive(state == GameState.Menu);
            pause.SetActive(state == GameState.Paused);
            settings.SetActive(state == GameState.Settings);
            complete.SetActive(state == GameState.Complete);
            loading.SetActive(state == GameState.Loading);
            hud.SetActive(state == GameState.Playing);
            if (state == GameState.Settings) SyncControls();
            GameObject selected = null;
            switch (state)
            {
                case GameState.Menu: selected = play.gameObject; break;
                case GameState.Paused: selected = resume.gameObject; break;
                case GameState.Settings: selected = master.gameObject; break;
                case GameState.Complete: selected = again.gameObject; break;
            }
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(selected);
        }

        private void SyncControls()
        {
            var data = GameSettings.Current;
            master.SetValueWithoutNotify(data.master);
            music.SetValueWithoutNotify(data.music);
            effects.SetValueWithoutNotify(data.effects);
            brightness.SetValueWithoutNotify(data.brightness);
            fullscreen.SetIsOnWithoutNotify(data.fullscreen);
            highQuality.SetIsOnWithoutNotify(data.highQuality);
        }

        public void SetRoomTitle(string title)
        {
            var label = hud.GetComponentInChildren<Text>(true);
            if (label != null) label.text = title + "   /   Space: jump   Esc: pause   R: restart\nTab: explored world map   /   Blue: room exit   Green: checkpoint";
        }

        private void UpdateBrightness() => brightnessOverlay.color = new Color(0, 0, 0, 1 - GameSettings.Current.brightness);
        private void OnDestroy() => GameSettings.Changed -= UpdateBrightness;
    }
}
