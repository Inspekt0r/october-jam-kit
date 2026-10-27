using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace JamKit
{
    public enum GameState { Menu, Playing, Paused, Settings, Complete, Loading, Map, Transition, Dialogue }

    public sealed class GameApp : MonoBehaviour
    {
        public static GameApp Instance { get; private set; }
        public GameView view;
        public string menuScene = "MainMenu";
        public string playScene = "Sandbox";
        public bool pauseOnFocusLoss = true;
        public GameState State { get; private set; }
        public PlayerMotor Player { get; private set; }
        public WorldRoom CurrentRoom { get; private set; }
        public WorldDefinition World { get; private set; }
        public WorldProgress Progress { get; private set; }
        public WorldMapView MapView { get; private set; }
        public PlayerHud StatusHud { get; private set; }
        public DialogueView Dialogues { get; private set; }
        private readonly Dictionary<string, Vector2> checkpoints = new Dictionary<string, Vector2>();
        private readonly HashSet<string> invalidDestinations = new HashSet<string>();
        private float doorReadyAt;
        private GameState settingsReturn;
        private bool loading;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (Instance == null) Instantiate(Resources.Load<GameObject>("JamKitApp"));
        }

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            GameSettings.Load();
            World = Resources.Load<WorldDefinition>("WorldDefinition");
            Progress = new WorldProgress();
            Progress.Load();
            SceneManager.sceneLoaded += SceneLoaded;
        }

        private void Start()
        {
            view.Bind(this);
            MapView = gameObject.AddComponent<WorldMapView>();
            MapView.Initialize(this);
            StatusHud = gameObject.AddComponent<PlayerHud>();
            StatusHud.Initialize(this);
            Dialogues = gameObject.AddComponent<DialogueView>();
            Dialogues.Initialize(this);
            RefreshScene();
            if (System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "--jam-smoke") >= 0)
                gameObject.AddComponent<SmokeRunner>();
            if (System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "--world-smoke") >= 0)
                gameObject.AddComponent<WorldSmokeRunner>();
            if (System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "--combat-smoke") >= 0)
                gameObject.AddComponent<CombatSmokeRunner>();
            if (System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "--enemy-smoke") >= 0)
                gameObject.AddComponent<EnemySmokeRunner>();
            if (System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "--dialogue-smoke") >= 0)
                gameObject.AddComponent<DialogueSmokeRunner>();
        }

        private void Update()
        {
            if (loading) return;
            if (GameInput.MapPressed && (State == GameState.Playing || State == GameState.Map))
            {
                SetState(State == GameState.Map ? GameState.Playing : GameState.Map);
                return;
            }
            if (GameInput.Back)
            {
                if (State == GameState.Map) Resume();
                else if (State == GameState.Settings) CloseSettings();
                else if (State == GameState.Playing) SetState(GameState.Paused);
                else if (State == GameState.Paused) Resume();
            }
            if (State == GameState.Playing && GameInput.Restart) Restart();
        }

        private void SceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (!loading) RefreshScene();
        }

        private void RefreshScene(string entrance = null)
        {
            CurrentRoom = FindFirstObjectByType<WorldRoom>();
            if (CurrentRoom != null)
            {
                if (Player == null) Player = FindFirstObjectByType<PlayerMotor>();
                if (Player == null && CurrentRoom.playerPrefab != null)
                    Player = Instantiate(CurrentRoom.playerPrefab).GetComponent<PlayerMotor>();
                if (Player != null)
                {
                    DontDestroyOnLoad(Player.gameObject);
                    Vector2 spawn = CurrentRoom.Spawn(entrance);
                    Player.EnterRoom(spawn, checkpoints.TryGetValue(CurrentRoom.roomId, out var checkpoint) ? checkpoint : spawn);
                    var camera = FindFirstObjectByType<CameraFollow>();
                    if (camera != null) { camera.target = Player.transform; camera.Snap(); }
                }
                Progress.Visit(CurrentRoom.roomId);
            }
            else Player = FindFirstObjectByType<PlayerMotor>();
            view.SetRoomTitle(World?.Find(CurrentRoom != null ? CurrentRoom.roomId : "")?.title ?? "SANDBOX");
            if (!loading) SetState(Player != null ? GameState.Playing : GameState.Menu);
        }

        public void SetState(GameState state)
        {
            if (state != GameState.Dialogue) Dialogues?.CancelForStateChange();
            State = state;
            Time.timeScale = state == GameState.Playing || state == GameState.Menu ? 1 : 0;
            view.Show(state);
            MapView?.Show(state == GameState.Map);
        }

        public void Play() { if (!loading) StartCoroutine(Load(playScene)); }
        public void Restart()
        {
            if (loading) return;
            if (CurrentRoom != null) checkpoints.Remove(CurrentRoom.roomId);
            StartCoroutine(Load(SceneManager.GetActiveScene().name, false));
        }
        public void MainMenu() { if (!loading) StartCoroutine(Load(menuScene)); }
        public void Resume() => SetState(GameState.Playing);
        public void Complete() { if (State == GameState.Playing) SetState(GameState.Complete); }
        public void OpenSettings() { settingsReturn = State; SetState(GameState.Settings); }
        public void CloseSettings() { GameSettings.Save(); SetState(settingsReturn); }
        public void Quit() { GameSettings.Save(); Application.Quit(); }

        public void RememberCheckpoint(Vector2 position)
        {
            if (CurrentRoom != null) checkpoints[CurrentRoom.roomId] = position;
        }

        public void Travel(string roomId, string entrance)
        {
            if (loading || State != GameState.Playing || Time.unscaledTime < doorReadyAt) return;
            var room = World != null ? World.Find(roomId) : null;
            if (room == null || !Application.CanStreamedLevelBeLoaded(room.sceneName))
            {
                if (invalidDestinations.Add(roomId ?? ""))
                    Debug.LogError($"Cannot travel to room '{roomId}': check WorldDefinition and Scene List.");
                return;
            }
            StartCoroutine(TravelRoutine(room.sceneName, entrance));
        }

        private IEnumerator TravelRoutine(string scene, string entrance)
        {
            loading = true;
            SetState(GameState.Transition);
            if (Player != null) Player.GetComponent<Rigidbody2D>().linearVelocity = Vector2.zero;
            yield return MapView.Fade(1, 0.12f);
            yield return SceneManager.LoadSceneAsync(scene);
            RefreshScene(entrance);
            yield return MapView.Fade(0, 0.12f);
            doorReadyAt = Time.unscaledTime + 0.35f;
            loading = false;
            SetState(GameState.Playing);
        }

        private IEnumerator Load(string scene, bool resetSession = true)
        {
            if (!Application.CanStreamedLevelBeLoaded(scene))
            {
                Debug.LogError($"Scene '{scene}' is missing from Scene List.");
                yield break;
            }
            loading = true;
            SetState(GameState.Loading);
            if (Player != null && CurrentRoom != null) Destroy(Player.gameObject);
            Player = null;
            CurrentRoom = null;
            if (resetSession) { checkpoints.Clear(); Dialogues?.ResetSession(); }
            Time.timeScale = 1;
            yield return SceneManager.LoadSceneAsync(scene);
            loading = false;
            RefreshScene();
            doorReadyAt = Time.unscaledTime + 0.35f;
        }

        private void OnApplicationFocus(bool focused)
        {
            if (!focused && pauseOnFocusLoss && State == GameState.Playing) SetState(GameState.Paused);
        }

        private void OnApplicationQuit() => GameSettings.Save();
        private void OnDestroy()
        {
            if (Instance != this) return;
            SceneManager.sceneLoaded -= SceneLoaded;
            Instance = null;
            Time.timeScale = 1;
        }
    }
}
