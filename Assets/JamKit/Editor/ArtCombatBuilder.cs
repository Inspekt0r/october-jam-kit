using System;
using System.IO;
using JamKit;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace JamKitEditor
{
    public static class ArtCombatBuilder
    {
        private const string Models = "Assets/JamKit/Models";
        private const string Hero = "Assets/JamKit/Generated/Player.prefab";
        private static ArtTheme theme;

        [MenuItem("Jam Kit/Add art and player combat")]
        public static void Upgrade()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Directory.CreateDirectory(Models); AssetDatabase.Refresh();
            var tags = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            var damageLayer = tags.FindProperty("layers").GetArrayElementAtIndex(8);
            if (string.IsNullOrEmpty(damageLayer.stringValue)) { damageLayer.stringValue = "Damageable"; tags.ApplyModifiedPropertiesWithoutUndo(); }
            theme = AssetDatabase.LoadAssetAtPath<ArtTheme>("Assets/Resources/ArtTheme.asset");
            if (theme == null)
            {
                theme = ScriptableObject.CreateInstance<ArtTheme>();
                theme.fallback = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/JamKit/Generated/Square.png");
                AssetDatabase.CreateAsset(theme, "Assets/Resources/ArtTheme.asset");
            }
            var hero = PrefabUtility.LoadPrefabContents(Hero);
            try { SetupPlayer(hero); PrefabUtility.SaveAsPrefabAsset(hero, Hero); }
            finally { PrefabUtility.UnloadPrefabContents(hero); }
            SaveModel("Background", VisualRole.Background, new Vector2(32, 18), new Color(0.055f, 0.075f, 0.12f), false);
            SaveModel("Wall", VisualRole.Wall, new Vector2(2, 3), new Color(0.25f, 0.33f, 0.4f), true);
            SaveModel("Platform", VisualRole.Platform, new Vector2(3, 0.5f), new Color(0.3f, 0.4f, 0.47f), true);
            SaveModel("EnemyVisual", VisualRole.Enemy, new Vector2(0.8f, 1.2f), new Color(0.95f, 0.45f, 0.35f), false);
            SaveModel("BossVisual", VisualRole.Boss, new Vector2(1.8f, 2.4f), new Color(0.65f, 0.35f, 0.85f), false);
            MakeDoor(); MakeDummy(); MakeDamageZone(); MakeRoomTemplate();
            var template = PrefabUtility.LoadPrefabContents(Models + "/RoomTemplate.prefab");
            try
            {
                var platform = template.transform.Find("Platform");
                if (platform != null && Mathf.Approximately(platform.localPosition.y, 1.5f))
                {
                    platform.localPosition = new Vector3(4, 0.4f, 0);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(platform);
                    PrefabUtility.SaveAsPrefabAsset(template, Models + "/RoomTemplate.prefab");
                }
            }
            finally { PrefabUtility.UnloadPrefabContents(template); }
            foreach (var scene in EditorBuildSettings.scenes)
            {
                if (!scene.enabled) continue;
                EditorSceneManager.OpenScene(scene.path, OpenSceneMode.Single);
                bool gameplay = UnityEngine.Object.FindFirstObjectByType<WorldRoom>() != null || UnityEngine.Object.FindFirstObjectByType<PlayerMotor>() != null;
                if (!gameplay) continue;
                foreach (var player in UnityEngine.Object.FindObjectsByType<PlayerMotor>(FindObjectsSortMode.None)) SetupPlayer(player.gameObject);
                foreach (var image in UnityEngine.Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None))
                {
                    if (image.GetComponent<SpriteStyle>() != null || image.transform.parent?.GetComponent<PlayerMotor>() != null || image.GetComponent<PlayerMotor>() != null) continue;
                    var go = image.gameObject;
                    VisualRole role = go.GetComponent<RoomDoor>() != null ? VisualRole.Door :
                        go.GetComponent<RoomTrigger>() != null ? (go.GetComponent<RoomTrigger>().kind == RoomTriggerKind.Checkpoint ? VisualRole.Checkpoint : VisualRole.Hazard) :
                        go.name.Contains("Platform") || go.name.Contains("Step") || go.name.Contains("Bridge") || go.name.Contains("Landing") ? VisualRole.Platform : VisualRole.Wall;
                    Skin(go, role, Vector2.one, image.color);
                }
                if (GameObject.Find("Room Background") == null)
                {
                    var background = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Models + "/Background.prefab"));
                    background.name = "Room Background"; background.transform.position = new Vector3(0, 3, 0);
                    var follow = UnityEngine.Object.FindFirstObjectByType<CameraFollow>();
                    if (follow != null)
                    {
                        var cam = follow.GetComponent<Camera>();
                        var style = background.GetComponentInChildren<SpriteStyle>();
                        style.size = new Vector2(follow.max.x - follow.min.x + cam.orthographicSize * 4 + 4,
                            follow.max.y - follow.min.y + cam.orthographicSize * 2 + 4);
                        background.transform.position = (follow.min + follow.max) / 2;
                        style.Apply(); PrefabUtility.RecordPrefabInstancePropertyModifications(style);
                    }
                    PrefabUtility.RecordPrefabInstancePropertyModifications(background);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(background.transform);
                }
                if (SceneManager.GetActiveScene().name == "WorldGallery")
                {
                    Place("Training target", "TrainingDummy", new Vector2(6, -0.7f));
                    Place("Damage practice zone", "DamageZone", new Vector2(8.5f, -1.25f));
                }
                EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            }
            AssetDatabase.SaveAssets();
            EditorSceneManager.OpenScene("Assets/JamKit/Generated/Scenes/MainMenu.unity");
            Debug.Log("JAMKIT_ART_COMBAT_READY");
        }

        private static void SetupPlayer(GameObject go)
        {
            Skin(go, VisualRole.Player, Vector2.one, new Color(0.35f, 0.86f, 0.68f));
            go.GetComponent<PlayerMotor>().avatar = go.transform.Find("Visual").GetComponent<SpriteRenderer>();
            if (go.GetComponent<Health>() == null)
            {
                var health = go.AddComponent<Health>(); health.maxHealth = 5; health.invulnerabilitySeconds = 0.7f;
            }
            if (go.GetComponent<PlayerVitals>() == null) go.AddComponent<PlayerVitals>();
            if (go.GetComponent<PlayerAttack>() == null) go.AddComponent<PlayerAttack>().attackSprite = theme.fallback;
        }

        private static void Skin(GameObject root, VisualRole role, Vector2 size, Color placeholder)
        {
            var original = root.GetComponent<SpriteRenderer>();
            var child = root.transform.Find("Visual");
            if (child == null)
            {
                child = new GameObject("Visual", typeof(SpriteRenderer)).transform; child.SetParent(root.transform, false);
                if (original != null) original.enabled = false;
            }
            var style = child.GetComponent<SpriteStyle>();
            if (style != null) return;
            style = child.gameObject.AddComponent<SpriteStyle>(); style.theme = theme; style.role = role;
            style.size = size; style.placeholderColor = placeholder; style.sortingOrder = role == VisualRole.Background ? -100 : (role == VisualRole.Player ? 10 : 0);
            if (original != null && original.sprite != null)
                style.size = new Vector2(original.localBounds.size.x, original.localBounds.size.y);
            if (original != null && original.sprite != null && original.sprite != theme.fallback)
            {
                style.overrideSprite = original.sprite; style.tint = original.color; style.sortingOrder = original.sortingOrder;
                child.GetComponent<SpriteRenderer>().sortingLayerID = original.sortingLayerID;
            }
            style.Apply();
        }

        private static void SaveModel(string name, VisualRole role, Vector2 size, Color color, bool solid)
        {
            if (File.Exists(Models + "/" + name + ".prefab")) return;
            var root = new GameObject(name); Skin(root, role, size, color);
            if (solid) { root.layer = 6; root.AddComponent<BoxCollider2D>().size = size; }
            Save(root, name);
        }

        private static void MakeDoor()
        {
            if (File.Exists(Models + "/Door.prefab")) return;
            var root = new GameObject("Door", typeof(BoxCollider2D), typeof(RoomDoor));
            root.GetComponent<BoxCollider2D>().size = new Vector2(0.6f, 5); root.GetComponent<BoxCollider2D>().isTrigger = true;
            Skin(root, VisualRole.Door, new Vector2(0.6f, 5), new Color(0.25f, 0.65f, 0.9f, 0.7f)); Save(root, "Door");
        }

        private static void MakeDummy()
        {
            if (File.Exists(Models + "/TrainingDummy.prefab")) return;
            var root = new GameObject("TrainingDummy", typeof(BoxCollider2D), typeof(Health), typeof(TrainingDummy));
            root.layer = 8; root.GetComponent<BoxCollider2D>().size = new Vector2(0.8f, 1.6f); root.GetComponent<BoxCollider2D>().isTrigger = true;
            root.GetComponent<Health>().maxHealth = 3;
            Skin(root, VisualRole.Enemy, new Vector2(0.8f, 1.6f), new Color(0.95f, 0.45f, 0.35f)); Save(root, "TrainingDummy");
        }

        private static void MakeDamageZone()
        {
            if (File.Exists(Models + "/DamageZone.prefab")) return;
            var root = new GameObject("DamageZone", typeof(BoxCollider2D), typeof(DamageZone));
            root.GetComponent<BoxCollider2D>().size = new Vector2(1.5f, 0.5f); root.GetComponent<BoxCollider2D>().isTrigger = true;
            Skin(root, VisualRole.Hazard, new Vector2(1.5f, 0.5f), new Color(0.95f, 0.2f, 0.25f)); Save(root, "DamageZone");
        }

        private static void MakeRoomTemplate()
        {
            if (File.Exists(Models + "/RoomTemplate.prefab")) return;
            var root = new GameObject("RoomTemplate", typeof(WorldRoom)); var room = root.GetComponent<WorldRoom>();
            room.roomId = "new_room"; room.playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(Hero);
            room.defaultSpawn = Point(root.transform, "Default spawn", new Vector2(0, -0.85f));
            room.entrances = new[] {
                new RoomEntrance { id = "west", point = Point(root.transform, "West entrance", new Vector2(-10, -0.85f)) },
                new RoomEntrance { id = "east", point = Point(root.transform, "East entrance", new Vector2(10, -0.85f)) }
            };
            ChildModel(root.transform, "Background", new Vector2(0, 3));
            var floor = new GameObject("Floor", typeof(BoxCollider2D)); floor.transform.SetParent(root.transform, false); floor.transform.localPosition = new Vector2(0, -2);
            floor.layer = 6; floor.GetComponent<BoxCollider2D>().size = new Vector2(28, 1);
            Skin(floor, VisualRole.Wall, new Vector2(28, 1), new Color(0.25f, 0.33f, 0.4f));
            ChildModel(root.transform, "Platform", new Vector2(4, 0.4f));
            ChildModel(root.transform, "Door", new Vector2(-12.5f, 1)).name = "West exit";
            ChildModel(root.transform, "Door", new Vector2(12.5f, 1)).name = "East exit";
            var camera = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener), typeof(CameraFollow)); camera.transform.SetParent(root.transform);
            camera.tag = "MainCamera"; camera.transform.localPosition = new Vector3(0, 1, -10);
            camera.GetComponent<Camera>().orthographic = true; camera.GetComponent<Camera>().orthographicSize = 5.5f;
            camera.GetComponent<Camera>().clearFlags = CameraClearFlags.SolidColor;
            var follow = camera.GetComponent<CameraFollow>(); follow.min = new Vector2(-3, 1); follow.max = new Vector2(3, 1);
            Save(root, "RoomTemplate");
        }

        private static Transform Point(Transform parent, string name, Vector2 pos)
        {
            var point = new GameObject(name).transform; point.SetParent(parent, false); point.localPosition = pos; return point;
        }
        private static GameObject ChildModel(Transform parent, string model, Vector2 position)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Models + "/" + model + ".prefab"));
            go.transform.SetParent(parent, false); go.transform.localPosition = position;
            PrefabUtility.RecordPrefabInstancePropertyModifications(go.transform); return go;
        }
        private static void Place(string name, string model, Vector2 position)
        {
            if (GameObject.Find(name) != null) return;
            var go = ChildModel(null, model, position); go.name = name;
            PrefabUtility.RecordPrefabInstancePropertyModifications(go);
        }
        private static void Save(GameObject root, string name)
        {
            foreach (var style in root.GetComponentsInChildren<SpriteStyle>()) style.Apply();
            PrefabUtility.SaveAsPrefabAsset(root, Models + "/" + name + ".prefab"); UnityEngine.Object.DestroyImmediate(root);
        }

        [MenuItem("Jam Kit/Create empty world room")]
        public static void CreateRoom()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            string path = EditorUtility.SaveFilePanelInProject("New world room", "MyRoom", "unity", "Choose a scene name.");
            if (string.IsNullOrEmpty(path)) return;
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Models + "/RoomTemplate.prefab");
            if (prefab == null) throw new InvalidOperationException("Run Add art and player combat first.");
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            root.GetComponent<WorldRoom>().roomId = Path.GetFileNameWithoutExtension(path).ToLowerInvariant();
            PrefabUtility.RecordPrefabInstancePropertyModifications(root.GetComponent<WorldRoom>());
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), path); Selection.activeGameObject = root;
        }
    }
}
