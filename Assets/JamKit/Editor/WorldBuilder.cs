using System;
using System.Collections.Generic;
using System.IO;
using JamKit;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace JamKitEditor
{
    public static class WorldBuilder
    {
        private const string Folder = "Assets/JamKit/WorldDemo";
        private const string DefinitionPath = "Assets/Resources/WorldDefinition.asset";
        private static Sprite sprite;

        [MenuItem("Jam Kit/Add connected world demo")]
        public static void Upgrade()
        {
            sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/JamKit/Generated/Square.png");
            if (sprite == null) throw new InvalidOperationException("Generate the starter project first.");
            Directory.CreateDirectory(Folder + "/Scenes");
            AssetDatabase.Refresh();
            var world = AssetDatabase.LoadAssetAtPath<WorldDefinition>(DefinitionPath);
            if (world == null)
            {
                world = ScriptableObject.CreateInstance<WorldDefinition>();
                world.rooms = new[] {
                    Info("hub", "WorldHub", "Crossroads", new Vector2(0, 0)),
                    Info("gallery", "WorldGallery", "Gallery", new Vector2(220, 0)),
                    Info("shaft", "WorldShaft", "Upper Passage", new Vector2(110, 145))
                };
                world.links = new[] {
                    new RoomLink { from = "hub", to = "gallery" },
                    new RoomLink { from = "gallery", to = "shaft" },
                    new RoomLink { from = "shaft", to = "hub" }
                };
                AssetDatabase.CreateAsset(world, DefinitionPath);
            }
            CreateRoom("hub", "WorldHub", "shaft", "gallery", new Color(0.22f, 0.32f, 0.4f));
            CreateRoom("gallery", "WorldGallery", "hub", "shaft", new Color(0.32f, 0.26f, 0.4f));
            CreateRoom("shaft", "WorldShaft", "gallery", "hub", new Color(0.3f, 0.37f, 0.27f));

            string prefabPath = "Assets/Resources/JamKitApp.prefab";
            var appRoot = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                var app = appRoot.GetComponent<GameApp>();
                if (app.playScene == "Sandbox") app.playScene = "WorldHub";
                var title = app.view.play.GetComponentInChildren<Text>(true);
                if (title != null && title.text == "Play sandbox") title.text = "Explore world";
                PrefabUtility.SaveAsPrefabAsset(appRoot, prefabPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(appRoot); }
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            foreach (var sceneName in new[] { "WorldHub", "WorldGallery", "WorldShaft" })
            {
                string path = Folder + "/Scenes/" + sceneName + ".unity";
                var existing = scenes.Find(scene => scene.path == path);
                if (existing == null) scenes.Add(new EditorBuildSettingsScene(path, true));
                else existing.enabled = true;
            }
            EditorBuildSettings.scenes = scenes.ToArray();
            AssetDatabase.SaveAssets();
            EditorSceneManager.OpenScene("Assets/JamKit/Generated/Scenes/MainMenu.unity");
            Debug.Log("JAMKIT_WORLD_READY");
        }

        private static RoomInfo Info(string id, string scene, string title, Vector2 position) =>
            new RoomInfo { id = id, sceneName = scene, title = title, mapBounds = new Rect(position, new Vector2(180, 100)) };

        private static void CreateRoom(string id, string sceneName, string westDestination, string eastDestination, Color ground)
        {
            string path = Folder + "/Scenes/" + sceneName + ".unity";
            if (File.Exists(path)) return; // Preserve room edits on repeated upgrades.
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var camera = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener), typeof(CameraFollow));
            camera.tag = "MainCamera"; camera.transform.position = new Vector3(0, 1, -10);
            var cam = camera.GetComponent<Camera>(); cam.orthographic = true; cam.orthographicSize = 5.5f;
            cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(0.045f, 0.065f, 0.095f);
            var follow = camera.GetComponent<CameraFollow>();
            follow.min = new Vector2(-3, 1); follow.max = new Vector2(3, 1); follow.offset = Vector2.up;
            Shape("Floor", new Vector2(0, -2), new Vector2(28, 1), ground, true);
            Shape("West wall", new Vector2(-14, 2), new Vector2(1, 10), ground, true);
            Shape("East wall", new Vector2(14, 2), new Vector2(1, 10), ground, true);
            Shape("Platform A", new Vector2(-4, 0.4f), new Vector2(3, 0.5f), ground, true);
            Shape("Platform B", new Vector2(4, 1.7f), new Vector2(3, 0.5f), ground, true);
            var room = new GameObject("Room Settings").AddComponent<WorldRoom>();
            room.roomId = id;
            room.playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/JamKit/Generated/Player.prefab");
            room.defaultSpawn = Point("Default spawn", room.transform, new Vector2(0, -0.85f));
            room.entrances = new[] {
                new RoomEntrance { id = "west", point = Point("West entrance", room.transform, new Vector2(-10, -0.85f)) },
                new RoomEntrance { id = "east", point = Point("East entrance", room.transform, new Vector2(10, -0.85f)) }
            };
            Door("West exit", -12.5f, westDestination, "east");
            Door("East exit", 12.5f, eastDestination, "west");
            var checkpoint = Shape("Checkpoint", new Vector2(0, -0.6f), new Vector2(0.35f, 1.7f), new Color(0.35f, 0.86f, 0.68f), false);
            checkpoint.GetComponent<BoxCollider2D>().isTrigger = true;
            var trigger = checkpoint.AddComponent<RoomTrigger>(); trigger.kind = RoomTriggerKind.Checkpoint;
            trigger.respawnPoint = room.defaultSpawn;
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), path);
        }

        private static void Door(string name, float x, string destination, string entrance)
        {
            var go = Shape(name, new Vector2(x, 1), new Vector2(0.6f, 5), new Color(0.25f, 0.64f, 0.85f, 0.6f), false);
            go.GetComponent<BoxCollider2D>().isTrigger = true;
            var door = go.AddComponent<RoomDoor>(); door.destinationRoom = destination; door.destinationEntrance = entrance;
        }
        private static Transform Point(string name, Transform parent, Vector2 position)
        {
            var point = new GameObject(name).transform; point.SetParent(parent); point.position = position; return point;
        }
        private static GameObject Shape(string name, Vector2 pos, Vector2 size, Color color, bool solid)
        {
            var go = new GameObject(name, typeof(SpriteRenderer), typeof(BoxCollider2D));
            go.transform.position = pos; go.transform.localScale = new Vector3(size.x, size.y, 1);
            var renderer = go.GetComponent<SpriteRenderer>(); renderer.sprite = sprite; renderer.color = color;
            go.GetComponent<BoxCollider2D>().size = Vector2.one;
            if (solid) go.layer = 6;
            return go;
        }
    }
}
