using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace JamKit
{
    public sealed class WorldSmokeRunner : MonoBehaviour
    {
        private GameApp app;
        private Keyboard keyboard;
        private int checks, failures;
        private bool hadSave;
        private string saved;
        private string folder;
        private float started;

        private IEnumerator Start()
        {
            app = GameApp.Instance; app.pauseOnFocusLoss = false;
            Application.runInBackground = true;
            started = Time.realtimeSinceStartup;
            folder = Path.Combine(Application.dataPath, "..", "QA"); Directory.CreateDirectory(folder);
            hadSave = PlayerPrefs.HasKey(WorldProgress.StorageKey); saved = PlayerPrefs.GetString(WorldProgress.StorageKey);
            PlayerPrefs.DeleteKey(WorldProgress.StorageKey); app.Progress.Load();
            keyboard = InputSystem.AddDevice<Keyboard>();
            yield return null;
            app.Play();
            yield return WaitRoom("hub");
            // Let native window focus/device synchronization settle before injecting the first key.
            yield return new WaitForSecondsRealtime(0.3f);
            var player = app.Player;
            Check(player != null, "Player spawned from room prefab");
            if (player == null) { Finish(); yield break; }
            player.speed = 8.25f;
            Check(app.Progress.Visited("hub") && !app.Progress.Visited("gallery"), "Unvisited rooms remain hidden");
            yield return PressKey(Key.Tab);
            Check(app.State == GameState.Map && Time.timeScale == 0, "Tab opens map and pauses world");
            Check(app.MapView.VisibleRoomCount == 1, "Map shows only the explored room");
            Vector3 paused = player.transform.position;
            yield return new WaitForSecondsRealtime(0.12f);
            Check(Vector3.Distance(paused, player.transform.position) < 0.001f, "Map freezes player physics");
            yield return Capture("map-one-room.png");
            yield return PressKey(Key.Tab);
            Check(app.State == GameState.Playing, "Tab closes map");
            player.SetCheckpoint(new Vector2(2, -0.85f));
            yield return Door(12.5f, "gallery");
            Check(app.Player == player && Mathf.Approximately(player.speed, 8.25f), "Same player and tuning survive transition");
            Check(player.transform.position.x < -9, "Arrival at matching west entrance");
            Check(app.Progress.Visited("gallery"), "Destination room discovered");
            yield return null;
            Check(app.CurrentRoom.roomId == "gallery", "Arrival does not bounce back through door");
            var immediateBody = player.GetComponent<Rigidbody2D>();
            immediateBody.position = new Vector2(-12.5f, -0.85f);
            immediateBody.transform.position = immediateBody.position;
            yield return WaitRoom("hub");
            Check(app.CurrentRoom.roomId == "hub", "Exit entered during cooldown retries automatically");
            Check(player.transform.position.x > 9, "Backtracking returns to east entrance");
            Check(Vector2.Distance(player.Checkpoint, new Vector2(2, -0.85f)) < 0.01f, "Room checkpoint remembered after return");
            var body = player.GetComponent<Rigidbody2D>(); body.position = new Vector2(8, -15);
            yield return new WaitForSecondsRealtime(0.1f);
            Check(Mathf.Abs(player.transform.position.x - 2) < 0.1f, "Death uses this room's remembered checkpoint");
            yield return Door(-12.5f, "shaft");
            yield return Door(-12.5f, "gallery");
            yield return Door(-12.5f, "hub");
            var loaded = new WorldProgress(); loaded.Load();
            Check(loaded.Visited("hub") && loaded.Visited("gallery") && loaded.Visited("shaft"), "Exploration is written to save");
            yield return PressKey(Key.Tab);
            Check(app.MapView.VisibleRoomCount == 3, "Map reveals all three visited rooms");
            yield return Capture("world-map.png");
            yield return PressKey(Key.Escape);
            Check(app.State == GameState.Playing && Time.timeScale == 1, "Esc closes map and resumes");
            Check(FindObjectsByType<PlayerMotor>(FindObjectsSortMode.None).Length == 1, "No duplicate player after repeated travel");
            app.MainMenu();
            float deadline = Time.realtimeSinceStartup + 10;
            while (app.State != GameState.Menu && Time.realtimeSinceStartup < deadline) yield return null;
            Check(app.State == GameState.Menu && app.Player == null && FindObjectsByType<PlayerMotor>(FindObjectsSortMode.None).Length == 0, "Menu cleans up persistent player");
            app.Play(); yield return WaitRoom("hub");
            Check(app.Progress.Visited("shaft"), "Exploration survives starting another session");
            Finish();
        }

        private IEnumerator Door(float x, string target)
        {
            yield return new WaitForSecondsRealtime(0.4f);
            var body = app.Player.GetComponent<Rigidbody2D>();
            body.position = new Vector2(x, -0.85f); body.transform.position = body.position; body.linearVelocity = Vector2.zero;
            yield return WaitRoom(target);
        }

        private IEnumerator WaitRoom(string id)
        {
            float deadline = Time.realtimeSinceStartup + 15;
            while ((app.CurrentRoom == null || app.CurrentRoom.roomId != id || app.State != GameState.Playing) && Time.realtimeSinceStartup < deadline) yield return null;
            Check(app.CurrentRoom != null && app.CurrentRoom.roomId == id && app.State == GameState.Playing, "Travel to " + id);
        }

        private IEnumerator PressKey(Key key)
        {
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(key));
            yield return null; yield return null;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            yield return null;
        }

        private IEnumerator Capture(string file)
        {
            if (Application.isBatchMode) yield break;
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(Path.Combine(folder, file));
            yield return new WaitForSecondsRealtime(0.1f);
        }

        private void Check(bool passed, string label)
        {
            checks++; if (!passed) failures++;
            Debug.Log("WORLD_CHECK " + (passed ? "PASS " : "FAIL ") + label);
        }

        private void Update()
        {
            if (started > 0 && Time.realtimeSinceStartup - started > 90)
            { Check(false, "World test timeout"); Finish(); }
        }

        private void Finish()
        {
            enabled = false; StopAllCoroutines();
            if (keyboard != null) InputSystem.RemoveDevice(keyboard);
            if (hadSave) PlayerPrefs.SetString(WorldProgress.StorageKey, saved);
            else PlayerPrefs.DeleteKey(WorldProgress.StorageKey);
            PlayerPrefs.Save(); app.Progress.Load();
            string result = $"{checks} checks, {failures} failures";
            File.WriteAllText(Path.Combine(folder, "world-result.txt"), result);
            Debug.Log("WORLD_SMOKE " + result); Application.Quit(failures == 0 ? 0 : 1);
        }
    }
}
