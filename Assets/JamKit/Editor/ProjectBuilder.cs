using System;
using System.IO;
using JamKit;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace JamKitEditor
{
    public static class ProjectBuilder
    {
        private const string Root = "Assets/JamKit/Generated";
        private static Font font;
        private static Sprite square;
        private static readonly Color Ink = new Color(0.055f, 0.075f, 0.11f);
        private static readonly Color Accent = new Color(0.35f, 0.86f, 0.68f);

        // Explicit menu action: rebuilding never runs automatically over user-authored scenes.
        [MenuItem("Jam Kit/Generate starter scenes (overwrites Generated)")]
        public static void Generate()
        {
            Directory.CreateDirectory(Root);
            Directory.CreateDirectory("Assets/Resources");
            Directory.CreateDirectory(Root + "/Scenes");
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var texture = new Texture2D(16, 16);
            var pixels = new Color[256];
            Array.Fill(pixels, Color.white);
            texture.SetPixels(pixels);
            texture.Apply();
            File.WriteAllBytes(Root + "/Square.png", texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(Root + "/Square.png");
            var importer = (TextureImporter)AssetImporter.GetAtPath(Root + "/Square.png");
            importer.textureType = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = 16;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
            square = AssetDatabase.LoadAssetAtPath<Sprite>(Root + "/Square.png");
            WriteTone("Jump", 620, 0.09f);
            WriteTone("Respawn", 180, 0.17f);
            AssetDatabase.Refresh();
            Configure();
            MakeApp();
            MakeMenu();
            MakeRoom();
            EditorBuildSettings.scenes = new[] {
                new EditorBuildSettingsScene(Root + "/Scenes/MainMenu.unity", true),
                new EditorBuildSettingsScene(Root + "/Scenes/Sandbox.unity", true)
            };
            AssetDatabase.SaveAssets();
            EditorSceneManager.OpenScene(Root + "/Scenes/MainMenu.unity");
            Debug.Log("JAMKIT_GENERATED");
        }

        private static void Configure()
        {
            PlayerSettings.companyName = "JamKit";
            PlayerSettings.productName = "October Jam Kit";
            PlayerSettings.defaultScreenWidth = 1280;
            PlayerSettings.defaultScreenHeight = 720;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.runInBackground = false;
            PlayerSettings.colorSpace = ColorSpace.Linear;
            EditorSettings.serializationMode = SerializationMode.ForceText;
            EditorSettings.defaultBehaviorMode = EditorBehaviorMode.Mode2D;
            var tags = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            tags.FindProperty("layers").GetArrayElementAtIndex(6).stringValue = "Ground";
            tags.FindProperty("layers").GetArrayElementAtIndex(7).stringValue = "Player";
            tags.ApplyModifiedPropertiesWithoutUndo();
            var playerSettings = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0]);
            var input = playerSettings.FindProperty("activeInputHandler");
            if (input != null) { input.intValue = 1; playerSettings.ApplyModifiedPropertiesWithoutUndo(); }
        }

        private static void MakeApp()
        {
            var root = new GameObject("JamKitApp");
            var app = root.AddComponent<GameApp>();
            var view = root.AddComponent<GameView>();
            app.view = view;
            var events = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            events.transform.SetParent(root.transform);
            events.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
            var music = root.AddComponent<AudioSource>();
            // Keep the music channel silent until a real track is assigned.
            music.clip = null;
            music.loop = true;
            music.playOnAwake = true;
            root.AddComponent<MusicChannel>();

            var dimCanvas = Canvas("World brightness", root.transform, -10);
            view.brightnessOverlay = Fill("Dimmer", dimCanvas.transform, Color.clear).AddComponent<Image>();
            view.brightnessOverlay.raycastTarget = false;
            var canvas = Canvas("Interface", root.transform, 10);
            view.menu = Panel("Menu", canvas.transform, "JAM KIT", "2D platformer foundation / Unity 6.3");
            view.play = Button(view.menu, "Play sandbox", 90);
            view.menuSettings = Button(view.menu, "Settings", 25);
            view.quit = Button(view.menu, "Quit", -40);
            Text(view.menu.transform, "WASD / arrows - move     Space - jump\nEsc - pause     R - restart room", 0, -190, 520, 70, 18);

            view.pause = Panel("Pause", canvas.transform, "PAUSED", "Take a breath.");
            view.resume = Button(view.pause, "Resume", 100);
            view.restart = Button(view.pause, "Restart room", 35);
            view.pauseSettings = Button(view.pause, "Settings", -30);
            view.toMenu = Button(view.pause, "Main menu", -95);

            view.complete = Panel("Complete", canvas.transform, "TEST COMPLETE", "This room is a technical playground.");
            view.again = Button(view.complete, "Play again", 50);
            view.completeMenu = Button(view.complete, "Main menu", -15);
            view.loading = Panel("Loading", canvas.transform, "LOADING", "Preparing the room...");

            view.settings = Panel("Settings", canvas.transform, "SETTINGS", "Changes apply immediately / Esc - back");
            view.master = Slider(view.settings, "Master volume", 110, 0, 1);
            view.music = Slider(view.settings, "Music volume", 45, 0, 1);
            view.effects = Slider(view.settings, "Effects volume", -20, 0, 1);
            view.brightness = Slider(view.settings, "World brightness", -85, 0.35f, 1);
            view.fullscreen = Toggle(view.settings, "Fullscreen", -145);
            view.highQuality = Toggle(view.settings, "High quality (60 fps / MSAA)", -190);
            view.back = Button(view.settings, "Back", -255, -140, 250);
            view.reset = Button(view.settings, "Reset defaults", -255, 140, 250);

            view.hud = new GameObject("HUD", typeof(RectTransform));
            view.hud.transform.SetParent(canvas.transform, false);
            var hudRect = (RectTransform)view.hud.transform;
            hudRect.anchorMin = new Vector2(0, 1); hudRect.anchorMax = new Vector2(0, 1);
            hudRect.anchoredPosition = new Vector2(340, -48);
            Text(view.hud.transform, "SANDBOX   /   Space: jump   Esc: pause   R: restart\nGreen: checkpoint   Red: hazard   Gold: finish", 0, 0, 640, 70, 18);
            PrefabUtility.SaveAsPrefabAsset(root, "Assets/Resources/JamKitApp.prefab");
            UnityEngine.Object.DestroyImmediate(root);
        }

        private static void MakeMenu()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            Camera("Menu Camera");
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), Root + "/Scenes/MainMenu.unity");
        }

        private static void MakeRoom()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var camera = Camera("Main Camera");
            var ground = new Color(0.22f, 0.30f, 0.39f);
            Box("Floor A", new Vector2(0, -2), new Vector2(14, 1), ground);
            Box("Floor B", new Vector2(18, -2), new Vector2(14, 1), ground);
            Box("Step A", new Vector2(2, 0), new Vector2(3, 0.5f), ground);
            Box("Step B", new Vector2(6, 1.5f), new Vector2(3, 0.5f), ground);
            Box("Bridge", new Vector2(10, 2.5f), new Vector2(3, 0.5f), ground);
            Box("Landing", new Vector2(15, 2), new Vector2(4, 0.5f), ground);
            Box("Left boundary", new Vector2(-7.5f, 3), new Vector2(1, 12), ground);
            Box("Right boundary", new Vector2(25.5f, 3), new Vector2(1, 12), ground);
            var hazard = Trigger("Hazard", new Vector2(18, -1.25f), new Vector2(2, 0.5f), new Color(0.95f, 0.30f, 0.30f), RoomTriggerKind.Hazard);
            var checkpoint = Trigger("Checkpoint", new Vector2(14, 3), new Vector2(0.4f, 1.5f), Accent, RoomTriggerKind.Checkpoint);
            checkpoint.respawnPoint = new GameObject("Respawn point").transform;
            checkpoint.respawnPoint.SetParent(checkpoint.transform);
            checkpoint.respawnPoint.position = new Vector3(14, 3, 0);
            Trigger("Finish", new Vector2(23, -0.5f), new Vector2(0.7f, 2), new Color(1, 0.78f, 0.35f), RoomTriggerKind.Finish);

            var player = Box("Player", new Vector2(-4, 0), new Vector2(0.8f, 1.2f), Accent, false);
            player.layer = 7;
            var rigid = player.AddComponent<Rigidbody2D>();
            rigid.gravityScale = 3;
            rigid.constraints = RigidbodyConstraints2D.FreezeRotation;
            rigid.interpolation = RigidbodyInterpolation2D.Interpolate;
            rigid.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            var motor = player.AddComponent<PlayerMotor>();
            motor.effects = player.AddComponent<AudioSource>();
            motor.effects.playOnAwake = false;
            motor.jumpSound = Clip("Jump"); motor.respawnSound = Clip("Respawn");
            PrefabUtility.SaveAsPrefabAsset(player, Root + "/Player.prefab");
            var follow = camera.gameObject.AddComponent<CameraFollow>();
            follow.target = player.transform;
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), Root + "/Scenes/Sandbox.unity");
        }

        private static RoomTrigger Trigger(string name, Vector2 pos, Vector2 size, Color color, RoomTriggerKind kind)
        {
            var go = Box(name, pos, size, color, false);
            go.GetComponent<BoxCollider2D>().isTrigger = true;
            var trigger = go.AddComponent<RoomTrigger>(); trigger.kind = kind;
            return trigger;
        }

        private static GameObject Box(string name, Vector2 pos, Vector2 size, Color color, bool ground = true)
        {
            var go = new GameObject(name, typeof(SpriteRenderer), typeof(BoxCollider2D));
            go.transform.position = pos;
            go.transform.localScale = new Vector3(size.x, size.y, 1);
            go.GetComponent<SpriteRenderer>().sprite = square;
            go.GetComponent<SpriteRenderer>().color = color;
            go.GetComponent<BoxCollider2D>().size = Vector2.one;
            if (ground) go.layer = 6;
            return go;
        }

        private static Camera Camera(string name)
        {
            var go = new GameObject(name, typeof(Camera), typeof(AudioListener));
            go.tag = "MainCamera";
            go.transform.position = new Vector3(0, 1, -10);
            var camera = go.GetComponent<Camera>();
            camera.orthographic = true; camera.orthographicSize = 5.5f;
            camera.backgroundColor = Ink; camera.clearFlags = CameraClearFlags.SolidColor;
            return camera;
        }

        private static Canvas Canvas(string name, Transform parent, int order)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            go.transform.SetParent(parent, false);
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = order;
            var scale = go.GetComponent<CanvasScaler>();
            scale.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scale.referenceResolution = new Vector2(1280, 720); scale.matchWidthOrHeight = 0.5f;
            return canvas;
        }

        private static GameObject Fill(string name, Transform parent, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            return go;
        }

        private static GameObject Panel(string name, Transform parent, string title, string subtitle)
        {
            var panel = Fill(name, parent, Ink);
            panel.AddComponent<Image>().color = new Color(Ink.r, Ink.g, Ink.b, 0.97f);
            Text(panel.transform, title, 0, 255, 600, 65, 44, Accent);
            Text(panel.transform, subtitle, 0, 202, 600, 40, 18);
            return panel;
        }

        private static RectTransform Rect(string name, Transform parent, float x, float y, float w, float h)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(w, h); rect.anchoredPosition = new Vector2(x, y);
            return rect;
        }

        private static Text Text(Transform parent, string value, float x, float y, float w, float h, int size, Color? color = null)
        {
            var text = Rect(value.Split('\n')[0], parent, x, y, w, h).gameObject.AddComponent<Text>();
            text.font = font; text.text = value; text.fontSize = size;
            text.color = color ?? new Color(0.86f, 0.90f, 0.94f);
            text.alignment = TextAnchor.MiddleCenter; text.raycastTarget = false;
            return text;
        }

        private static Button Button(GameObject panel, string label, float y, float x = 0, float width = 540)
        {
            var rect = Rect(label, panel.transform, x, y, width, 50);
            var image = rect.gameObject.AddComponent<Image>(); image.color = Color.white;
            var button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = image;
            var colors = button.colors; colors.normalColor = new Color(0.13f, 0.20f, 0.27f);
            colors.highlightedColor = colors.selectedColor = new Color(0.15f, 0.43f, 0.38f);
            colors.pressedColor = new Color(0.08f, 0.3f, 0.25f); button.colors = colors;
            Text(rect, label, 0, 0, width, 50, 22);
            return button;
        }

        private static Slider Slider(GameObject panel, string label, float y, float min, float max)
        {
            Text(panel.transform, label, -140, y, 270, 38, 19);
            var rect = Rect(label + " slider", panel.transform, 155, y, 250, 30);
            rect.gameObject.AddComponent<Image>().color = new Color(0.16f, 0.23f, 0.30f);
            var slider = rect.gameObject.AddComponent<Slider>();
            var area = Fill("Handle area", rect, Color.clear);
            var areaRect = (RectTransform)area.transform;
            areaRect.offsetMin = new Vector2(9, 0); areaRect.offsetMax = new Vector2(-9, 0);
            var handle = Rect("Handle", area.transform, 0, 0, 18, 4);
            var image = handle.gameObject.AddComponent<Image>(); image.color = Color.white;
            slider.handleRect = handle; slider.targetGraphic = image;
            slider.minValue = min; slider.maxValue = max;
            var colors = slider.colors; colors.normalColor = Accent;
            colors.highlightedColor = colors.selectedColor = Color.white; slider.colors = colors;
            return slider;
        }

        private static Toggle Toggle(GameObject panel, string label, float y)
        {
            var rect = Rect(label, panel.transform, 0, y, 540, 36);
            var toggle = rect.gameObject.AddComponent<Toggle>();
            var bg = Rect("Box", rect, -245, 0, 28, 28).gameObject.AddComponent<Image>();
            bg.color = Color.white;
            var mark = Rect("Check", bg.transform, 0, 0, 16, 16).gameObject.AddComponent<Image>(); mark.color = Accent;
            toggle.targetGraphic = bg; toggle.graphic = mark;
            var colors = toggle.colors; colors.normalColor = new Color(0.2f, 0.28f, 0.35f);
            colors.highlightedColor = colors.selectedColor = new Color(0.15f, 0.43f, 0.38f); toggle.colors = colors;
            Text(rect, label, 25, 0, 470, 36, 19);
            return toggle;
        }

        private static AudioClip Clip(string name) => AssetDatabase.LoadAssetAtPath<AudioClip>(Root + "/" + name + ".wav");

        private static void WriteTone(string name, float frequency, float seconds)
        {
            const int rate = 22050;
            int samples = (int)(rate * seconds);
            using (var stream = File.Create(Root + "/" + name + ".wav"))
            using (var writer = new BinaryWriter(stream))
            {
                writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF")); writer.Write(36 + samples * 2);
                writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt ")); writer.Write(16);
                writer.Write((short)1); writer.Write((short)1); writer.Write(rate); writer.Write(rate * 2);
                writer.Write((short)2); writer.Write((short)16);
                writer.Write(System.Text.Encoding.ASCII.GetBytes("data")); writer.Write(samples * 2);
                for (int i = 0; i < samples; i++)
                {
                    float t = (float)i / rate;
                    float envelope = Mathf.Min(t * 40, 1) * Mathf.Min((seconds - t) * 12, 1);
                    writer.Write((short)(Mathf.Sin(t * frequency * 2 * Mathf.PI) * envelope * 1800));
                }
            }
        }

        [MenuItem("Jam Kit/Build Windows")]
        public static void BuildWindows() => Build(BuildTarget.StandaloneWindows64, "Builds/Windows/JamKit.exe");
        [MenuItem("Jam Kit/Build Linux")]
        public static void BuildLinux() => Build(BuildTarget.StandaloneLinux64, "Builds/Linux/JamKit.x86_64");

        [MenuItem("Jam Kit/Build both platforms")]
        public static void BuildAll()
        {
            BuildWindows();
            BuildLinux();
        }

        private static void Build(BuildTarget target, string path)
        {
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Standalone, target))
                throw new InvalidOperationException("Install build support for " + target + " through Unity Hub.");
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                scenes = Array.ConvertAll(Array.FindAll(EditorBuildSettings.scenes, scene => scene.enabled), scene => scene.path),
                locationPathName = path, target = target, options = BuildOptions.Development
            });
            if (report.summary.result != BuildResult.Succeeded) throw new Exception("Build failed: " + report.summary.result);
            Debug.Log("JAMKIT_BUILD_OK " + path);
        }
    }
}
