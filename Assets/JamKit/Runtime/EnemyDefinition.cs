using UnityEngine;

namespace JamKit
{
    [CreateAssetMenu(menuName = "Jam Kit/Enemy Definition")]
    public sealed class EnemyDefinition : ScriptableObject
    {
        [Header("Life and movement")]
        [Min(1)] public int maxHealth = 3;
        [Min(0)] public float patrolSpeed = 1.5f;
        [Min(0)] public float chaseSpeed = 2.5f;
        [Min(0)] public float patrolDistance = 3;
        [Min(0)] public float detectionRange = 5;
        [Min(0)] public float loseSightDelay = 1.2f;
        public LayerMask groundMask = 1 << 6;
        [Header("Melee")]
        [Min(1)] public int damage = 1;
        [Min(0.05f)] public float attackReach = 1.1f;
        [Min(0.05f)] public float attackHeight = 1.25f;
        [Min(0.02f)] public float windup = 0.45f;
        [Min(0.02f)] public float activeTime = 0.15f;
        [Min(0.02f)] public float recovery = 0.7f;
        [Header("Optional sprites")]
        public Sprite idleSprite, walkSprite, windupSprite, attackSprite, deathSprite;
        public bool useStateTint = true;
        [Header("Optional Animator states")]
        public RuntimeAnimatorController controller;
        public string idleState = "Idle", walkState = "Walk", windupState = "Windup", attackState = "Attack", recoveryState = "Recovery", deathState = "Death", hurtState = "Hurt";
        [Min(0.05f)] public float deathDuration = 0.8f;
        [Min(0.02f)] public float hurtDuration = 0.35f;
        public bool flashOnHurt = true;
        [Min(0.02f)] public float hurtBlinkInterval = 0.06f;
        [Header("Optional 2D sounds")]
        public AudioClip footstepSound, alertSound, windupSound, attackSound, hurtSound, deathSound;
        [Range(0, 1)] public float volume = 0.8f;
        [Min(0.05f)] public float footstepInterval = 0.4f;
    }
}
