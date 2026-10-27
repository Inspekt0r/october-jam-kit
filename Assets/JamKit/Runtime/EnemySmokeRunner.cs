using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace JamKit
{
    public sealed class EnemySmokeRunner : MonoBehaviour
    {
        private GameApp app;
        private Keyboard keyboard;
        private Mouse mouse;
        private int checks, failures;
        private float started;
        private bool hadSave;
        private string saved, output;
        private IEnumerator Start()
        {
            app = GameApp.Instance; app.pauseOnFocusLoss = false; Application.runInBackground = true; started = Time.realtimeSinceStartup;
            output = Path.Combine(Application.dataPath, "..", "QA"); Directory.CreateDirectory(output);
            hadSave = PlayerPrefs.HasKey(WorldProgress.StorageKey); saved = PlayerPrefs.GetString(WorldProgress.StorageKey);
            keyboard = InputSystem.AddDevice<Keyboard>(); mouse = InputSystem.AddDevice<Mouse>();
            app.playScene = "EnemyDemo"; app.Play(); yield return Room("EnemyDemo");
            var enemy = GameObject.Find("Patrol enemy").GetComponent<EnemyBrain>();
            var player = app.Player; var health = player.GetComponent<Health>();
            float startX = enemy.transform.position.x; yield return new WaitForSecondsRealtime(0.5f);
            Check(Mathf.Abs(enemy.transform.position.x - startX) > 0.3f && enemy.State == EnemyState.Patrol, "Enemy walks on patrol");
            player.EnterRoom((Vector2)enemy.transform.position + Vector2.right * 3, player.Checkpoint);
            var wall = new GameObject("QA sight wall", typeof(BoxCollider2D)); wall.layer = 6;
            wall.transform.position = enemy.transform.position + Vector3.right * 1.5f; wall.GetComponent<BoxCollider2D>().size = new Vector2(0.25f, 4);
            Physics2D.SyncTransforms(); yield return new WaitForSecondsRealtime(0.1f);
            Check(!enemy.SeesPlayer, "Ground wall blocks player detection");
            Destroy(wall); yield return null; yield return new WaitForSecondsRealtime(0.1f);
            Check(enemy.SeesPlayer && enemy.State == EnemyState.Chase, "Enemy detects and pursues visible player");
            player.EnterRoom((Vector2)enemy.transform.position + Vector2.right * 1.2f, player.Checkpoint);
            yield return State(enemy, EnemyState.Windup);
            int hp = health.Current;
            yield return new WaitForSecondsRealtime(0.12f);
            Check(health.Current == hp, "Windup warns before causing damage");
            Check(enemy.presentation.animator.GetCurrentAnimatorStateInfo(0).IsName("Base Layer.Windup"), "Animator enters requested windup state");
            app.SetState(GameState.Map); var enemyPosition = enemy.transform.position;
            yield return new WaitForSecondsRealtime(0.2f);
            Check(enemy.State == EnemyState.Windup && Vector3.Distance(enemyPosition, enemy.transform.position) < 0.001f && health.Current == hp, "Map freezes enemy movement and attack timer");
            app.Resume(); player.EnterRoom((Vector2)enemy.transform.position + Vector2.left * 2.5f, player.Checkpoint);
            yield return State(enemy, EnemyState.Recovery);
            Check(health.Current == hp, "Player can evade a committed swing");
            player.EnterRoom(new Vector2(-10, -0.85f), player.Checkpoint);
            yield return new WaitForSecondsRealtime(0.85f);
            player.EnterRoom((Vector2)enemy.transform.position + Vector2.right * 1.2f, player.Checkpoint);
            yield return State(enemy, EnemyState.Windup);
            health.RestoreFull(); health.invulnerabilitySeconds = 0; hp = health.Current;
            yield return State(enemy, EnemyState.Recovery);
            Check(health.Current == hp - enemy.definition.damage, "Enemy deals damage once per active swing without relying on invulnerability");
            health.invulnerabilitySeconds = 0.7f;

            var profile = Instantiate(enemy.definition); profile.detectionRange = 0; profile.patrolDistance = 0;
            enemy.definition = profile; enemy.presentation.definition = profile;
            player.EnterRoom(new Vector2(-10, -0.85f), player.Checkpoint); health.RestoreFull();
            yield return new WaitForSecondsRealtime(1.5f);
            Check(!enemy.SeesPlayer && enemy.State == EnemyState.Idle, "Enemy loses player and returns to idle");

            var edgeProfile = Instantiate(profile); edgeProfile.patrolDistance = 5; edgeProfile.patrolSpeed = 2;
            var edge = Instantiate(enemy.gameObject, new Vector3(30, -0.9f, 0), Quaternion.identity).GetComponent<EnemyBrain>();
            edge.definition = edgeProfile; edge.presentation.definition = edgeProfile; edge.presentation.animator.runtimeAnimatorController = null;
            var floor = new GameObject("QA ledge floor", typeof(BoxCollider2D)); floor.layer = 6; floor.transform.position = new Vector2(30, -2);
            floor.GetComponent<BoxCollider2D>().size = new Vector2(4, 1); Physics2D.SyncTransforms();
            float minY = 0; bool turned = false;
            for (float end = Time.realtimeSinceStartup + 2; Time.realtimeSinceStartup < end;)
            {
                minY = Mathf.Min(minY, edge.transform.position.y); turned |= edge.Facing < 0; yield return null;
            }
            Check(turned && minY > -1.1f, "Patrol reverses at ledge without falling, optional Animator may be absent");
            Destroy(edge.gameObject); Destroy(floor);

            var wallProfile = Instantiate(edgeProfile); wallProfile.patrolDistance = 3;
            var walker = Instantiate(enemy.gameObject, new Vector3(-9, -0.9f, 0), Quaternion.identity).GetComponent<EnemyBrain>();
            walker.definition = wallProfile; walker.presentation.definition = wallProfile;
            wall = new GameObject("QA patrol wall", typeof(BoxCollider2D)); wall.layer = 6; wall.transform.position = new Vector2(-7.5f, -0.4f);
            wall.GetComponent<BoxCollider2D>().size = new Vector2(0.3f, 3); Physics2D.SyncTransforms();
            yield return new WaitForSecondsRealtime(0.8f);
            Check(walker.Facing < 0 && walker.transform.position.x < -7.9f, "Patrol turns before a wall");
            Destroy(walker.gameObject); Destroy(wall);

            var enemyHealth = enemy.GetComponent<Health>(); enemyHealth.RestoreFull();
            health.invulnerabilitySeconds = 0; health.RestoreFull(); health.TakeDamage(1);
            var playerFlash = player.GetComponent<DamageFlash>();
            Check(playerFlash != null && playerFlash.IsPlaying, "Player damage starts feedback");
            yield return null;
            app.SetState(GameState.Map); yield return null;
            bool pausedVisibility = player.avatar.forceRenderingOff;
            yield return new WaitForSecondsRealtime(0.15f);
            Check(playerFlash.IsPlaying && player.avatar.forceRenderingOff == pausedVisibility, "Map freezes damage flash");
            app.Resume();
            yield return Blink(player.avatar, playerFlash, "Player blinks and restores visible sprite");
            health.TakeDamage(1); health.TakeDamage(99);
            Check(!playerFlash.IsPlaying && !player.avatar.forceRenderingOff && health.Current == health.maxHealth, "Respawn cancels damage flash");
            health.invulnerabilitySeconds = 0.7f;
            enemyHealth.TakeDamage(1);
            var enemyFlash = enemy.GetComponent<DamageFlash>();
            Check(enemyFlash != null && enemyFlash.IsPlaying, "Enemy damage starts feedback");
            yield return new WaitForSecondsRealtime(0.07f);
            Check(enemy.presentation.animator.GetCurrentAnimatorStateInfo(0).IsName("Base Layer.Hurt"), "Enemy plays Hurt while receiving damage");
            yield return Blink(enemy.presentation.style.GetComponent<SpriteRenderer>(), enemyFlash, "Enemy blinks and restores visible sprite");
            yield return null;
            Check(enemy.presentation.animator.GetCurrentAnimatorStateInfo(0).IsName("Base Layer.Idle"), "Enemy returns from Hurt to behavior animation");
            enemyHealth.RestoreFull();
            player.EnterRoom((Vector2)enemy.transform.position + Vector2.left * 1.25f, player.Checkpoint);
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.D)); yield return null; yield return null;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); yield return null;
            app.SetState(GameState.Paused); yield return null;
            InputSystem.QueueStateEvent(mouse, new MouseState { buttons = 1 }); yield return null;
            app.view.resume.onClick.Invoke(); yield return new WaitForSecondsRealtime(0.2f);
            Check(enemyHealth.Current == 3 && !player.GetComponent<PlayerAttack>().IsAttacking, "Clicking Resume does not leak into a melee attack");
            InputSystem.QueueStateEvent(mouse, new MouseState()); yield return null; yield return null;
            InputSystem.QueueStateEvent(mouse, new MouseState { buttons = 1 }); yield return new WaitForSecondsRealtime(0.2f);
            Check(enemyHealth.Current == 2, "Left mouse button attacks enemy");
            InputSystem.QueueStateEvent(mouse, new MouseState()); yield return new WaitForSecondsRealtime(0.2f);
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.J)); yield return new WaitForSecondsRealtime(0.2f);
            Check(enemyHealth.Current == 1, "J remains an additional attack binding");
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); yield return new WaitForSecondsRealtime(0.2f);
            if (!Application.isBatchMode)
            {
                yield return new WaitForEndOfFrame(); ScreenCapture.CaptureScreenshot(Path.Combine(output, "enemy-example.png"));
                yield return new WaitForSecondsRealtime(0.1f);
            }
            enemy.presentation.Hurt();
            player.GetComponent<PlayerAttack>().TryAttack(); yield return new WaitForSecondsRealtime(0.2f);
            Check(enemy.State == EnemyState.Dead && !enemy.GetComponent<Collider2D>().enabled && !enemy.GetComponent<Rigidbody2D>().simulated, "Death disables movement, collision and attacks immediately");
            Check(!enemyFlash.IsPlaying && !enemy.presentation.style.GetComponent<SpriteRenderer>().forceRenderingOff, "Death clears flash so death animation stays visible");
            Check(enemy.presentation.animator.GetCurrentAnimatorStateInfo(0).IsName("Base Layer.Death"), "Death animation state plays before cleanup");
            yield return new WaitForSecondsRealtime(profile.deathDuration + 0.1f);
            Check(enemy == null, "Dead enemy is removed after presentation duration");
            Finish();
        }
        private IEnumerator Room(string id)
        {
            float end = Time.realtimeSinceStartup + 15;
            while ((UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != id || app.State != GameState.Playing) && Time.realtimeSinceStartup < end) yield return null;
            Check(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == id && app.State == GameState.Playing, "Reach " + id);
        }
        private IEnumerator Blink(SpriteRenderer image, DamageFlash flash, string label)
        {
            bool hidden = false, visible = false;
            Color color = image.color;
            for (float end = Time.realtimeSinceStartup + 0.45f; Time.realtimeSinceStartup < end;)
            {
                hidden |= image.forceRenderingOff; visible |= !image.forceRenderingOff;
                yield return null;
            }
            Check(hidden && visible && !flash.IsPlaying && !image.forceRenderingOff && image.color == color, label);
        }
        private IEnumerator State(EnemyBrain enemy, EnemyState state)
        {
            float end = Time.realtimeSinceStartup + 3;
            while (enemy != null && enemy.State != state && Time.realtimeSinceStartup < end) yield return null;
            Check(enemy != null && enemy.State == state, "Enemy phase " + state);
        }
        private void Check(bool passed, string label) { checks++; if (!passed) failures++; Debug.Log("ENEMY_CHECK " + (passed ? "PASS " : "FAIL ") + label); }
        private void Update() { if (started > 0 && Time.realtimeSinceStartup - started > 90) { Check(false, "Enemy test timeout"); Finish(); } }
        private void Finish()
        {
            enabled = false; StopAllCoroutines(); if (keyboard != null) InputSystem.RemoveDevice(keyboard); if (mouse != null) InputSystem.RemoveDevice(mouse);
            if (hadSave) PlayerPrefs.SetString(WorldProgress.StorageKey, saved); else PlayerPrefs.DeleteKey(WorldProgress.StorageKey);
            PlayerPrefs.Save(); app.Progress.Load(); string result = $"{checks} checks, {failures} failures";
            File.WriteAllText(Path.Combine(output, "enemy-result.txt"), result); Debug.Log("ENEMY_SMOKE " + result); Application.Quit(failures == 0 ? 0 : 1);
        }
    }
}
