using System.IO;
using System.Collections.Generic;
using JamKit;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace JamKitEditor
{
    public static class EnemyBuilder
    {
        private const string Folder = "Assets/JamKit/Enemies";
        private const string PrefabPath = "Assets/JamKit/Models/Enemy.prefab";
        [MenuItem("Jam Kit/Add melee enemy")]
        public static void Upgrade()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Directory.CreateDirectory(Folder + "/Animations"); AssetDatabase.Refresh();
            var definition = AssetDatabase.LoadAssetAtPath<EnemyDefinition>(Folder + "/BasicEnemy.asset");
            if (definition == null)
            {
                definition = ScriptableObject.CreateInstance<EnemyDefinition>();
                definition.controller = CreateController();
                AssetDatabase.CreateAsset(definition, Folder + "/BasicEnemy.asset");
            }
            if (!File.Exists(PrefabPath))
            {
                var root = new GameObject("Enemy", typeof(Rigidbody2D), typeof(BoxCollider2D), typeof(Health), typeof(AudioSource));
                root.layer = 8;
                var rigid = root.GetComponent<Rigidbody2D>(); rigid.gravityScale = 3; rigid.constraints = RigidbodyConstraints2D.FreezeRotation;
                rigid.interpolation = RigidbodyInterpolation2D.Interpolate; rigid.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
                root.GetComponent<BoxCollider2D>().size = new Vector2(0.8f, 1.2f);
                root.GetComponent<Health>().maxHealth = definition.maxHealth;
                var audio = root.GetComponent<AudioSource>(); audio.playOnAwake = false; audio.spatialBlend = 0;
                var visual = new GameObject("Visual", typeof(SpriteRenderer), typeof(SpriteStyle), typeof(Animator)); visual.transform.SetParent(root.transform, false);
                var style = visual.GetComponent<SpriteStyle>(); style.theme = AssetDatabase.LoadAssetAtPath<ArtTheme>("Assets/Resources/ArtTheme.asset");
                style.role = VisualRole.Enemy; style.size = new Vector2(0.8f, 1.2f); style.placeholderColor = new Color(0.95f, 0.45f, 0.35f); style.Apply();
                var animator = visual.GetComponent<Animator>(); animator.runtimeAnimatorController = definition.controller; animator.applyRootMotion = false;
                var presentation = root.AddComponent<EnemyPresentation>(); presentation.definition = definition; presentation.style = style; presentation.animator = animator;
                var brain = root.AddComponent<EnemyBrain>(); brain.definition = definition; brain.presentation = presentation;
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath); Object.DestroyImmediate(root);
            }
            string demo = Folder + "/EnemyDemo.unity";
            if (!File.Exists(demo))
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                var camera = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener), typeof(CameraFollow));
                camera.tag = "MainCamera"; camera.transform.position = new Vector3(0, 1, -10);
                camera.GetComponent<Camera>().orthographic = true; camera.GetComponent<Camera>().orthographicSize = 5.5f;
                camera.GetComponent<Camera>().backgroundColor = new Color(0.05f, 0.07f, 0.1f);
                var floor = new GameObject("Floor", typeof(BoxCollider2D), typeof(SpriteRenderer)); floor.layer = 6;
                floor.transform.position = new Vector2(0, -2); floor.GetComponent<BoxCollider2D>().size = new Vector2(28, 1);
                var image = floor.GetComponent<SpriteRenderer>(); image.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/JamKit/Generated/Square.png");
                image.drawMode = SpriteDrawMode.Sliced; image.size = new Vector2(28, 1); image.color = new Color(0.25f, 0.33f, 0.4f);
                var player = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/JamKit/Generated/Player.prefab"));
                player.transform.position = new Vector2(-10, -0.9f); PrefabUtility.RecordPrefabInstancePropertyModifications(player.transform);
                camera.GetComponent<CameraFollow>().target = player.transform; camera.GetComponent<CameraFollow>().min = new Vector2(-4, 1); camera.GetComponent<CameraFollow>().max = new Vector2(4, 1);
                var enemy = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath));
                enemy.name = "Patrol enemy"; enemy.transform.position = new Vector2(4, -0.9f);
                PrefabUtility.RecordPrefabInstancePropertyModifications(enemy); PrefabUtility.RecordPrefabInstancePropertyModifications(enemy.transform);
                EditorSceneManager.SaveScene(enemy.scene, demo);
            }
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            if (!scenes.Exists(scene => scene.path == demo)) { scenes.Add(new EditorBuildSettingsScene(demo, true)); EditorBuildSettings.scenes = scenes.ToArray(); }
            AssetDatabase.SaveAssets(); EditorSceneManager.OpenScene("Assets/JamKit/Generated/Scenes/MainMenu.unity");
            Debug.Log("JAMKIT_ENEMY_READY");
        }

        private static RuntimeAnimatorController CreateController()
        {
            string path = Folder + "/BasicEnemy.controller";
            var existing = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(path); if (existing != null) return existing;
            var controller = AnimatorController.CreateAnimatorControllerAtPath(path);
            var machine = controller.layers[0].stateMachine;
            foreach (var name in new[] { "Idle", "Walk", "Windup", "Attack", "Recovery", "Hurt", "Death" })
            {
                var clip = new AnimationClip { name = name };
                clip.SetCurve("", typeof(Transform), "m_LocalPosition.z", AnimationCurve.Constant(0, 0.5f, 0));
                var settings = AnimationUtility.GetAnimationClipSettings(clip); settings.loopTime = name == "Idle" || name == "Walk";
                AnimationUtility.SetAnimationClipSettings(clip, settings);
                AssetDatabase.CreateAsset(clip, Folder + "/Animations/" + name + ".anim");
                var state = machine.AddState(name); state.motion = clip;
                if (name == "Idle") machine.defaultState = state;
            }
            return controller;
        }
    }
}
