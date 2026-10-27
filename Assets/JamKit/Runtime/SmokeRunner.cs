using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace JamKit
{
    // Only runs when the built executable is explicitly launched with --jam-smoke.
    public sealed class SmokeRunner : MonoBehaviour
    {
        private int failures;
        private int checks;
        private string originalSettings;
        private bool hadSettings;
        private float started;
        private Keyboard keyboard;
        private string output;

        private IEnumerator Start()
        {
            Application.runInBackground = true;
            started = Time.realtimeSinceStartup;
            output = Path.Combine(Application.dataPath, "..", "QA");
            Directory.CreateDirectory(output);
            hadSettings = PlayerPrefs.HasKey(GameSettings.StorageKey);
            originalSettings = PlayerPrefs.GetString(GameSettings.StorageKey);
            keyboard = InputSystem.AddDevice<Keyboard>();
            var app = GameApp.Instance;
            app.playScene = "Sandbox"; // Keep the original platformer regression suite independent of the demo world.
            app.pauseOnFocusLoss = false;
            yield return null;
            Check(app.State == GameState.Menu && app.view.menu.activeSelf, "Initial menu");
            yield return Capture("menu.png");
            app.view.play.onClick.Invoke();
            yield return WaitForState(GameState.Playing);
            yield return new WaitForSecondsRealtime(0.5f);
            var player = app.Player;
            Check(player != null && player.Grounded, "Spawn and land on floor");
            if (player == null) { Finish(); yield break; }
            float x = player.transform.position.x;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.D));
            yield return new WaitForSecondsRealtime(0.35f);
            Check(player.transform.position.x > x + 0.5f, "Keyboard horizontal movement");
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            yield return new WaitForSecondsRealtime(0.2f);
            float y = player.transform.position.y;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Space));
            yield return new WaitForSecondsRealtime(0.16f);
            Check(player.transform.position.y > y + 0.5f, "Keyboard jump");
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            yield return new WaitForSecondsRealtime(0.6f);
            yield return Capture("sandbox.png");
            app.SetState(GameState.Paused);
            var position = player.transform.position;
            yield return new WaitForSecondsRealtime(0.15f);
            Check(Vector3.Distance(position, player.transform.position) < 0.001f && Time.timeScale == 0, "Pause freezes physics");
            app.view.pauseSettings.onClick.Invoke();
            Check(app.State == GameState.Settings, "Pause opens settings");
            app.view.master.value = 0.31f;
            app.view.brightness.value = 0.6f;
            app.view.highQuality.isOn = false;
            Check(Mathf.Abs(AudioListener.volume - 0.31f) < 0.01f && Application.targetFrameRate == 30, "Settings apply");
            yield return null;
            yield return Capture("settings.png");
            app.view.back.onClick.Invoke();
            GameSettings.Load();
            Check(app.State == GameState.Paused && Mathf.Abs(GameSettings.Current.master - 0.31f) < 0.01f, "Settings persist and return to pause");
            app.view.resume.onClick.Invoke();
            Check(app.State == GameState.Playing && Time.timeScale == 1, "Resume");
            var body = player.GetComponent<Rigidbody2D>();
            body.position = new Vector2(14, 3);
            body.linearVelocity = Vector2.zero;
            yield return new WaitForSecondsRealtime(0.15f);
            Check(Vector2.Distance(player.Checkpoint, new Vector2(14, 3)) < 0.1f, "Checkpoint trigger");
            body.position = new Vector2(18, -1.15f);
            body.linearVelocity = Vector2.zero;
            yield return new WaitForSecondsRealtime(0.1f);
            Check(player.transform.position.x < 15, "Hazard respawns at checkpoint");
            body.position = new Vector2(10, -15);
            yield return new WaitForSecondsRealtime(0.1f);
            Check(player.transform.position.x < 15 && player.transform.position.y > -5, "Fall recovery");
            body.position = new Vector2(23, -0.5f);
            body.linearVelocity = Vector2.zero;
            yield return new WaitForSecondsRealtime(0.15f);
            Check(app.State == GameState.Complete && app.view.complete.activeSelf, "Finish trigger");
            app.view.again.onClick.Invoke();
            yield return WaitForState(GameState.Playing);
            Check(app.Player != player && app.Player.Checkpoint.x == -4, "Restart creates fresh room");
            app.MainMenu();
            yield return WaitForState(GameState.Menu);
            Check(FindObjectsByType<GameApp>(FindObjectsSortMode.None).Length == 1, "Return to menu preserves one app");
            Finish();
        }

        private IEnumerator WaitForState(GameState state)
        {
            float deadline = Time.realtimeSinceStartup + 15;
            while (GameApp.Instance.State != state && Time.realtimeSinceStartup < deadline) yield return null;
            Check(GameApp.Instance.State == state, "Scene transition to " + state);
        }

        private void Check(bool passed, string description)
        {
            checks++;
            if (!passed) failures++;
            Debug.Log("JAMKIT_CHECK " + (passed ? "PASS " : "FAIL ") + description);
        }

        private IEnumerator Capture(string name)
        {
            if (Application.isBatchMode) yield break;
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(Path.Combine(output, name));
            yield return new WaitForSecondsRealtime(0.1f);
        }

        private void Update()
        {
            if (started > 0 && Time.realtimeSinceStartup - started > 90)
            { Check(false, "Smoke test timed out"); Finish(); }
        }

        private void Finish()
        {
            enabled = false;
            StopAllCoroutines();
            if (keyboard != null) InputSystem.RemoveDevice(keyboard);
            if (hadSettings) PlayerPrefs.SetString(GameSettings.StorageKey, originalSettings);
            else PlayerPrefs.DeleteKey(GameSettings.StorageKey);
            PlayerPrefs.Save();
            GameSettings.Load();
            string summary = $"{checks} checks, {failures} failures";
            File.WriteAllText(Path.Combine(output, "result.txt"), summary);
            Debug.Log("JAMKIT_SMOKE " + summary);
            // Prevent the application's quit hook from overwriting the restored settings.
            Application.Quit(failures == 0 ? 0 : 1);
        }
    }
}
