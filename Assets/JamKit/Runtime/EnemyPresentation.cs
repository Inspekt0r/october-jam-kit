using System.Collections;
using UnityEngine;

namespace JamKit
{
    [RequireComponent(typeof(AudioSource))]
    public sealed class EnemyPresentation : MonoBehaviour
    {
        [HideInInspector] public EnemyDefinition definition;
        public SpriteStyle style;
        public Animator animator;
        private AudioSource source;
        private SpriteRenderer image;
        private Sprite originalOverride;
        private EnemyState state;
        private float stepAt;
        private Coroutine hurt;
        private DamageFlash flash;
        private Color baseColor;

        private void Awake()
        {
            var brain = GetComponent<EnemyBrain>();
            flash = GetComponent<DamageFlash>() ?? gameObject.AddComponent<DamageFlash>();
            if (brain != null && brain.definition != null) definition = brain.definition;
            source = GetComponent<AudioSource>(); image = style != null ? style.GetComponent<SpriteRenderer>() : GetComponentInChildren<SpriteRenderer>();
            if (image != null) baseColor = image.color;
            if (style != null) originalOverride = style.overrideSprite;
            if (animator != null && definition != null && definition.controller != null) animator.runtimeAnimatorController = definition.controller;
        }
        private void OnEnable() { GameSettings.Changed += Volume; Volume(); }
        private void OnDisable()
        {
            GameSettings.Changed -= Volume;
            if (hurt != null) { StopCoroutine(hurt); hurt = null; }
            flash.Cancel();
        }
        private void Volume() { if (source != null) source.volume = GameSettings.Current.effects; }
        public void Face(float direction) { if (image != null) image.flipX = direction < 0; }
        public void Walking(bool moving)
        {
            if (!moving || definition == null || Time.time < stepAt || state == EnemyState.Dead) return;
            stepAt = Time.time + definition.footstepInterval; Play(definition.footstepSound);
        }
        public void Alert() { if (definition != null) Play(definition.alertSound); }
        public void SetState(EnemyState next)
        {
            state = next;
            if (definition == null || image == null) return;
            if (next == EnemyState.Dead && hurt != null) { StopCoroutine(hurt); hurt = null; }
            if (next == EnemyState.Dead) flash.Cancel();
            ApplyVisual();
            if (next == EnemyState.Windup) Play(definition.windupSound);
            if (next == EnemyState.Attack) Play(definition.attackSound);
            if (next == EnemyState.Dead) { source.Stop(); Play(definition.deathSound); }
        }
        private void ApplyVisual()
        {
            Sprite sprite = definition.idleSprite; string animation = definition.idleState;
            switch (state)
            {
                case EnemyState.Patrol: case EnemyState.Chase: sprite = definition.walkSprite; animation = definition.walkState; break;
                case EnemyState.Windup: sprite = definition.windupSprite; animation = definition.windupState; break;
                case EnemyState.Attack: sprite = definition.attackSprite; animation = definition.attackState; break;
                case EnemyState.Recovery: animation = definition.recoveryState; break;
                case EnemyState.Dead: sprite = definition.deathSprite; animation = definition.deathState; break;
            }
            if (style != null) { style.overrideSprite = sprite != null ? sprite : originalOverride; style.Apply(); }
            else { if (sprite != null) image.sprite = sprite; image.color = baseColor; }
            if (definition.useStateTint)
            {
                if (state == EnemyState.Windup) image.color *= new Color(1, 1, 0.3f);
                if (state == EnemyState.Attack) image.color *= new Color(1, 0.35f, 0.35f);
                if (state == EnemyState.Dead) image.color *= new Color(0.45f, 0.45f, 0.45f);
            }
            if (hurt == null) PlayState(animation);
        }
        private void PlayState(string name)
        {
            if (animator == null || animator.runtimeAnimatorController == null || string.IsNullOrEmpty(name)) return;
            string layer = animator.GetLayerName(0) + ".";
            int hash = Animator.StringToHash(name.StartsWith(layer) ? name : layer + name);
            if (animator.HasState(0, hash)) animator.CrossFade(hash, 0.04f, 0);
        }
        public void Hurt()
        {
            if (definition == null || state == EnemyState.Dead) return;
            Play(definition.hurtSound);
            if (hurt != null) StopCoroutine(hurt);
            hurt = StartCoroutine(HurtRoutine());
        }
        private IEnumerator HurtRoutine()
        {
            if (definition.flashOnHurt) flash.Play(image, definition.hurtDuration, definition.hurtBlinkInterval);
            PlayState(definition.hurtState);
            yield return new WaitForSeconds(definition.hurtDuration);
            hurt = null;
            if (state != EnemyState.Dead) ApplyVisual();
        }
        private void Play(AudioClip clip)
        {
            if (clip != null && source != null && definition != null) source.PlayOneShot(clip, definition.volume);
        }
    }
}
