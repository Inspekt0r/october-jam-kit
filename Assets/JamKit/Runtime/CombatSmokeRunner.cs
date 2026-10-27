using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace JamKit
{
    public sealed class CombatSmokeRunner : MonoBehaviour
    {
        private GameApp app;
        private Keyboard keyboard;
        private int checks, failures;
        private float started;
        private string output, saved;
        private bool hadSave;

        private IEnumerator Start()
        {
            app = GameApp.Instance; app.pauseOnFocusLoss = false; Application.runInBackground = true;
            started = Time.realtimeSinceStartup; output = Path.Combine(Application.dataPath, "..", "QA"); Directory.CreateDirectory(output);
            hadSave = PlayerPrefs.HasKey(WorldProgress.StorageKey); saved = PlayerPrefs.GetString(WorldProgress.StorageKey);
            keyboard = InputSystem.AddDevice<Keyboard>();
            app.Play(); yield return Room("hub"); yield return new WaitForSecondsRealtime(0.4f);
            app.Travel("gallery", "west"); yield return Room("gallery"); yield return new WaitForSecondsRealtime(0.4f);
            var player = app.Player; var health = player.GetComponent<Health>(); var attack = player.GetComponent<PlayerAttack>();
            var target = GameObject.Find("Training target").GetComponent<Health>();
            Check(health != null && attack != null && health.Current == 5, "Hero starts with health and melee attack");
            player.EnterRoom(new Vector2(4.8f, -0.85f), player.Checkpoint);
            // Teleport cancels input until released, just like a room transition.
            yield return null; yield return null;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.J)); yield return null; yield return null;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); yield return new WaitForSecondsRealtime(0.18f);
            Check(target.Current == 2, "J hit damages target once across active frames");
            Check(!attack.TryAttack(), "Attack cooldown blocks immediate repeat");
            yield return new WaitForSecondsRealtime(0.25f); attack.TryAttack(); yield return new WaitForSecondsRealtime(0.2f);
            Check(target.Current == 1, "Second swing applies configured damage");
            yield return new WaitForSecondsRealtime(0.2f); attack.TryAttack(); yield return new WaitForSecondsRealtime(0.2f);
            Check(target.Current == 0 && !target.GetComponent<Collider2D>().enabled, "Training target defeats and disables collision");
            yield return new WaitForSecondsRealtime(2.1f);
            Check(target.Current == 3 && target.GetComponent<Collider2D>().enabled, "Training target resets without scene reload");
            player.EnterRoom(new Vector2(7.3f, -0.85f), player.Checkpoint);
            attack.TryAttack(); yield return new WaitForSecondsRealtime(0.2f);
            Check(target.Current == 3, "Attack does not hit behind hero");
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.A)); yield return null; yield return null;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); yield return new WaitForSecondsRealtime(0.25f);
            var wall = new GameObject("QA blocking wall", typeof(BoxCollider2D)); wall.layer = 6;
            wall.transform.position = new Vector2(6.7f, -0.4f); wall.GetComponent<BoxCollider2D>().size = new Vector2(0.2f, 3);
            Physics2D.SyncTransforms(); attack.TryAttack(); yield return new WaitForSecondsRealtime(0.2f);
            Check(target.Current == 3, "Wall blocks melee damage");
            Destroy(wall); yield return null; yield return new WaitForSecondsRealtime(0.2f);
            attack.TryAttack(); yield return new WaitForSecondsRealtime(0.2f);
            Check(target.Current == 2 && player.Facing < 0, "Left-facing attack hits after wall removed");
            Check(health.TakeDamage(1) && health.Current == 4, "Player takes health damage");
            Check(!health.TakeDamage(1) && health.Current == 4, "Invulnerability prevents repeated contact damage");
            yield return null;
            Check(app.StatusHud.HealthText.StartsWith("HP 4/5"), "HUD reflects current health count");
            if (!Application.isBatchMode)
            {
                yield return new WaitForEndOfFrame(); ScreenCapture.CaptureScreenshot(Path.Combine(output, "combat-and-health.png"));
                yield return new WaitForSecondsRealtime(0.1f);
            }
            app.SetState(GameState.Map);
            Check(!attack.TryAttack() && !health.TakeDamage(1), "Map blocks attacks and damage"); app.Resume();
            yield return new WaitForSecondsRealtime(0.8f); health.TakeDamage(99);
            Check(health.Current == health.maxHealth && Vector2.Distance(player.transform.position, player.Checkpoint) < 0.05f, "Death respawns with restored health");
            var style = target.GetComponentInChildren<SpriteStyle>();
            var colliderSize = target.GetComponent<Collider2D>().bounds.size;
            var texture = new Texture2D(32, 16); var testSprite = Sprite.Create(texture, new Rect(0, 0, 32, 16), new Vector2(0.5f, 0.5f), 16);
            style.overrideSprite = testSprite; style.Apply(); yield return null;
            var rendered = style.GetComponent<SpriteRenderer>().bounds.size;
            Check(Vector2.Distance(rendered, style.size) < 0.01f && Vector3.Distance(colliderSize, target.GetComponent<Collider2D>().bounds.size) < 0.01f, "Sprite replacement fits visual without changing collider");
            yield return new WaitForSecondsRealtime(0.8f); health.TakeDamage(1);
            app.Travel("hub", "east"); yield return Room("hub");
            Check(app.Player == player && app.Player.GetComponent<Health>().Current == 4, "Health survives room transition");
            Finish();
        }

        private IEnumerator Room(string id)
        {
            float deadline = Time.realtimeSinceStartup + 15;
            while ((app.CurrentRoom == null || app.CurrentRoom.roomId != id || app.State != GameState.Playing) && Time.realtimeSinceStartup < deadline) yield return null;
            Check(app.CurrentRoom != null && app.CurrentRoom.roomId == id, "Reach " + id);
        }
        private void Check(bool passed, string label)
        {
            checks++; if (!passed) failures++; Debug.Log("COMBAT_CHECK " + (passed ? "PASS " : "FAIL ") + label);
        }
        private void Update()
        {
            if (started > 0 && Time.realtimeSinceStartup - started > 90) { Check(false, "Combat test timeout"); Finish(); }
        }
        private void Finish()
        {
            enabled = false; StopAllCoroutines(); if (keyboard != null) InputSystem.RemoveDevice(keyboard);
            if (hadSave) PlayerPrefs.SetString(WorldProgress.StorageKey, saved); else PlayerPrefs.DeleteKey(WorldProgress.StorageKey);
            PlayerPrefs.Save(); app.Progress.Load(); string result = $"{checks} checks, {failures} failures";
            File.WriteAllText(Path.Combine(output, "combat-result.txt"), result); Debug.Log("COMBAT_SMOKE " + result); Application.Quit(failures == 0 ? 0 : 1);
        }
    }
}
