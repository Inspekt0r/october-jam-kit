using System.Collections.Generic;
using UnityEngine;

namespace JamKit
{
    // NPC interaction or a story area. Does not require a physics event to detect the player.
    public sealed class DialogueActor : MonoBehaviour
    {
        internal static readonly List<DialogueActor> Active = new List<DialogueActor>();
        public DialogueAsset dialogue;
        public string prompt = "E — поговорить";
        [Min(0.1f)] public float interactionDistance = 2;
        public LayerMask obstructionMask = 1 << 6;
        public bool automatic;
        public BoxCollider2D storyArea;
        public bool oncePerSession;
        private bool entered;

        public bool Available => dialogue != null && dialogue.lines != null && dialogue.lines.Length > 0 &&
            (!oncePerSession || GameApp.Instance == null || GameApp.Instance.Dialogues == null || !GameApp.Instance.Dialogues.Completed(dialogue));

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetActors() => Active.Clear();
        private void OnEnable() { if (!Active.Contains(this)) Active.Add(this); }
        private void OnDisable() => Active.Remove(this);
        public bool InReach(PlayerMotor player)
        {
            return player != null && Vector2.Distance(player.transform.position, transform.position) <= interactionDistance &&
                !Physics2D.Linecast(player.GetComponent<Collider2D>().bounds.center, transform.position, obstructionMask);
        }
        public bool Interact()
        {
            var app = GameApp.Instance;
            return app != null && app.Dialogues != null && Available && !automatic && InReach(app.Player) && app.Dialogues.Begin(this);
        }
        private void Update()
        {
            if (!automatic || storyArea == null) return;
            var app = GameApp.Instance;
            if (app == null || app.Player == null || app.State != GameState.Playing) return;
            bool inside = storyArea.OverlapPoint(app.Player.GetComponent<Collider2D>().bounds.center);
            if (!inside) { entered = false; return; }
            if (!entered && Available && app.Dialogues != null && app.Dialogues.Begin(this)) entered = true;
        }
        private void OnDrawGizmosSelected()
        {
            if (automatic) return;
            Gizmos.color = Color.cyan; Gizmos.DrawWireSphere(transform.position, interactionDistance);
        }
    }
}
