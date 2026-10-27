using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;

namespace JamKit
{
    public sealed class DialogueSmokeRunner : MonoBehaviour
    {
        private GameApp app;
        private Keyboard keyboard;
        private int checks, failures;
        private float started;
        private IEnumerator Start()
        {
            app = GameApp.Instance; app.pauseOnFocusLoss = false; Application.runInBackground = true;
            started = Time.realtimeSinceStartup; keyboard = InputSystem.AddDevice<Keyboard>();
            app.playScene = "DialogueDemo"; app.Play();
            while (SceneManager.GetActiveScene().name != "DialogueDemo" || app.State != GameState.Playing) yield return null;
            var player = app.Player; var view = app.Dialogues;
            var actors = FindObjectsByType<DialogueActor>(FindObjectsSortMode.None);
            DialogueActor npc = null, story = null;
            foreach (var actor in actors) { if (actor.automatic) story = actor; else npc = actor; }
            Check(npc != null && story != null && !view.IsOpen, "Dialogue demo has NPC and story area");
            Check(!npc.Interact(), "Distant NPC cannot be used");
            player.EnterRoom(story.transform.position, player.Checkpoint); yield return null; yield return null;
            Check(view.IsOpen && app.State == GameState.Dialogue && Time.timeScale == 0, "Story area opens paused dialogue");
            Check(view.DisplayedText == story.dialogue.lines[0].text && view.LineIndex == 0, "First story line displayed");
            Vector3 position = player.transform.position; int hp = player.GetComponent<Health>().Current;
            yield return Press(Key.Tab); yield return Press(Key.D);
            Check(app.State == GameState.Dialogue && player.transform.position == position && !player.GetComponent<Health>().TakeDamage(1) && player.GetComponent<Health>().Current == hp, "Dialogue blocks map, movement and damage");
            yield return Press(Key.Enter);
            Check(view.LineIndex == 1, "Enter advances one line");
            if (!Application.isBatchMode)
            {
                yield return new WaitForEndOfFrame();
                Directory.CreateDirectory(Path.Combine(Application.dataPath, "..", "QA"));
                ScreenCapture.CaptureScreenshot(Path.Combine(Application.dataPath, "..", "QA", "dialogue-example.png"));
                yield return new WaitForSecondsRealtime(0.1f);
            }
            yield return Press(Key.Space);
            Check(!view.IsOpen && app.State == GameState.Playing && !story.Available, "Last line completes once-per-session story");
            Check(player.GetComponent<Rigidbody2D>().linearVelocity.y <= 0.1f, "Closing dialogue with Space does not jump");
            yield return new WaitForSecondsRealtime(0.1f);
            Check(!view.IsOpen, "Story does not repeat while still inside area");
            player.EnterRoom((Vector2)npc.transform.position + Vector2.left, player.Checkpoint); yield return null; yield return null;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.E)); yield return null; yield return null;
            Check(view.IsOpen && view.LineIndex == 0 && view.DisplayedText == npc.dialogue.lines[0].text, "E opens NPC without skipping opening line");
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); yield return null; yield return null;
            yield return Press(Key.Escape);
            Check(!view.IsOpen && app.State == GameState.Playing && !view.Completed(npc.dialogue), "Esc cancels without pausing or marking complete");
            yield return Press(Key.E);
            Check(view.IsOpen && view.LineIndex == 0, "Cancelled conversation can restart");
            var empty = ScriptableObject.CreateInstance<DialogueAsset>();
            var invalid = new GameObject("Empty dialogue", typeof(DialogueActor)).GetComponent<DialogueActor>(); invalid.dialogue = empty;
            Check(!view.Begin(invalid), "Empty or concurrent dialogue is rejected"); Destroy(invalid.gameObject); Destroy(empty);
            var nextButton = app.transform.Find("Dialogue UI/Dialogue overlay/Dialogue box/Next").GetComponent<UnityEngine.UI.Button>();
            nextButton.onClick.Invoke(); yield return null;
            Check(view.LineIndex == 1 && !string.IsNullOrEmpty(nextButton.GetComponentInChildren<UnityEngine.UI.Text>().text), "Mouse button advances conversation and has a visible label");
            for (int i = 1; i < npc.dialogue.lines.Length; i++) yield return Press(Key.Enter);
            Check(!view.IsOpen && view.Completed(npc.dialogue) && npc.Available, "Repeatable NPC remains available after completion");
            yield return Press(Key.E); app.Restart();
            yield return null;
            while (app.State == GameState.Loading) yield return null;
            Check(!view.IsOpen && Time.timeScale == 1 && app.State == GameState.Playing, "Scene restart closes dialogue and restores gameplay");
            DialogueActor reloadedStory = null;
            foreach (var actor in FindObjectsByType<DialogueActor>(FindObjectsSortMode.None)) if (actor.automatic) reloadedStory = actor;
            Check(reloadedStory != null && !reloadedStory.Available, "Story completion survives room restart");
            app.MainMenu(); yield return null;
            while (app.State == GameState.Loading) yield return null;
            Check(app.State == GameState.Menu && !view.IsOpen, "Main menu has no stale dialogue overlay");
            Finish();
        }
        private IEnumerator Press(Key key)
        {
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(key)); yield return null; yield return null;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); yield return null; yield return null;
        }
        private void Check(bool passed, string label) { checks++; if (!passed) failures++; Debug.Log("DIALOGUE_CHECK " + (passed ? "PASS " : "FAIL ") + label); }
        private void Update() { if (started > 0 && Time.realtimeSinceStartup - started > 60) { Check(false, "Dialogue test timeout"); Finish(); } }
        private void Finish()
        {
            enabled = false; StopAllCoroutines(); if (keyboard != null) InputSystem.RemoveDevice(keyboard);
            string output = Path.Combine(Application.dataPath, "..", "QA"); Directory.CreateDirectory(output);
            string result = $"{checks} checks, {failures} failures"; File.WriteAllText(Path.Combine(output, "dialogue-result.txt"), result);
            Debug.Log("DIALOGUE_SMOKE " + result); Application.Quit(failures == 0 ? 0 : 1);
        }
    }
}
