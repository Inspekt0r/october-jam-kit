using System.Collections.Generic;
using System.IO;
using JamKit;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace JamKitEditor
{
    public static class DialogueBuilder
    {
        private const string Folder = "Assets/JamKit/Story";
        [MenuItem("Jam Kit/Add dialogue examples")]
        public static void Upgrade()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Directory.CreateDirectory(Folder); AssetDatabase.Refresh();
            var intro = Dialogue("Arrival", new[] {
                Line("Рассказчик", "За дверью стих шум. Впереди — незнакомый путь. Это пример сюжетного сообщения: замени его своей историей."),
                Line("Рассказчик", "Такие окна можно показывать при входе в комнату, у найденной записки или перед важной встречей.") });
            var conversation = Dialogue("GuideConversation", new[] {
                Line("Смотритель", "Ты здесь впервые? Не спеши. Сначала осмотрись."),
                Line("Путник", "Что ждёт меня дальше?"),
                Line("Смотритель", "Пока это только заготовка разговора. Ты можешь менять имена, портреты и реплики, а потом построить вокруг них свою историю.") });
            string npcPath = Folder + "/NPC.prefab", triggerPath = Folder + "/StoryTrigger.prefab";
            if (!File.Exists(npcPath))
            {
                var npc = new GameObject("NPC", typeof(DialogueActor)); npc.GetComponent<DialogueActor>().dialogue = conversation;
                var visual = new GameObject("Visual", typeof(SpriteRenderer), typeof(SpriteStyle)); visual.transform.SetParent(npc.transform, false);
                var style = visual.GetComponent<SpriteStyle>(); style.overrideSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/JamKit/Generated/Square.png");
                style.tint = new Color(0.4f, 0.65f, 1); style.size = new Vector2(0.8f, 1.2f); style.Apply();
                PrefabUtility.SaveAsPrefabAsset(npc, npcPath); Object.DestroyImmediate(npc);
            }
            if (!File.Exists(triggerPath))
            {
                var trigger = new GameObject("Story trigger", typeof(BoxCollider2D), typeof(DialogueActor));
                var area = trigger.GetComponent<BoxCollider2D>(); area.isTrigger = true; area.size = new Vector2(2, 4);
                var actor = trigger.GetComponent<DialogueActor>(); actor.dialogue = intro; actor.automatic = true; actor.oncePerSession = true; actor.storyArea = area;
                PrefabUtility.SaveAsPrefabAsset(trigger, triggerPath); Object.DestroyImmediate(trigger);
            }
            string scene = Folder + "/DialogueDemo.unity";
            if (!File.Exists(scene))
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                var camera = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener), typeof(CameraFollow));
                camera.tag = "MainCamera"; camera.transform.position = new Vector3(0, 1, -10);
                camera.GetComponent<Camera>().orthographic = true; camera.GetComponent<Camera>().orthographicSize = 5.5f;
                camera.GetComponent<Camera>().backgroundColor = new Color(0.05f, 0.07f, 0.1f);
                var floor = new GameObject("Floor", typeof(BoxCollider2D), typeof(SpriteRenderer)); floor.layer = 6; floor.transform.position = new Vector2(0, -2);
                floor.GetComponent<BoxCollider2D>().size = new Vector2(28, 1);
                var image = floor.GetComponent<SpriteRenderer>(); image.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/JamKit/Generated/Square.png");
                image.drawMode = SpriteDrawMode.Sliced; image.size = new Vector2(28, 1); image.color = new Color(0.25f, 0.33f, 0.4f);
                var player = Place("Assets/JamKit/Generated/Player.prefab", new Vector2(-9, -0.9f));
                camera.GetComponent<CameraFollow>().target = player.transform; camera.GetComponent<CameraFollow>().min = new Vector2(-4, 1); camera.GetComponent<CameraFollow>().max = new Vector2(4, 1);
                Place(npcPath, new Vector2(0, -0.9f)); Place(triggerPath, new Vector2(-4, -0.5f));
                EditorSceneManager.SaveScene(player.scene, scene);
            }
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            if (!scenes.Exists(item => item.path == scene)) { scenes.Add(new EditorBuildSettingsScene(scene, true)); EditorBuildSettings.scenes = scenes.ToArray(); }
            AssetDatabase.SaveAssets(); EditorSceneManager.OpenScene("Assets/JamKit/Generated/Scenes/MainMenu.unity"); Debug.Log("JAMKIT_DIALOGUE_READY");
        }
        private static DialogueLine Line(string speaker, string text) => new DialogueLine { speaker = speaker, text = text };
        private static DialogueAsset Dialogue(string name, DialogueLine[] lines)
        {
            string path = Folder + "/" + name + ".asset";
            var asset = AssetDatabase.LoadAssetAtPath<DialogueAsset>(path);
            if (asset != null) return asset;
            asset = ScriptableObject.CreateInstance<DialogueAsset>(); asset.lines = lines; AssetDatabase.CreateAsset(asset, path); return asset;
        }
        private static GameObject Place(string path, Vector2 position)
        {
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path));
            instance.transform.position = position; PrefabUtility.RecordPrefabInstancePropertyModifications(instance.transform); return instance;
        }
    }
}
