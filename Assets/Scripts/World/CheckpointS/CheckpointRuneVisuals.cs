using System.Collections;
using UnityEngine;

namespace World
{
    [DisallowMultipleComponent, RequireComponent(typeof(CheckpointInteractable))]
    public sealed class CheckpointRuneVisuals : MonoBehaviour
    {
        public SpriteRenderer[] glyphs;
        public CheckpointAudioSettings activationAudio;
        private AudioSource activationVoice;
        [Range(0f, 1f)] public float dormantOpacity = 0f;
        [Range(0f, 1f)] public float litOpacity = 1f;
        [Min(0.1f)] public float activationDuration = 2f;
        private bool revealing, refreshPending;

        private void OnEnable()
        {
            if(glyphs!=null)foreach(var glyph in glyphs)PixelPresentation.SetUnscaled(glyph,true);
            CheckpointService.SavedChanged += MarkDirty;
            refreshPending = true;
        }
        private void Start() { if (!revealing) RefreshSaved(); }
        private void MarkDirty() { refreshPending = true; }
        private void LateUpdate()
        {
            if (refreshPending && !revealing && !InGameCutscene.AnyPlaying) RefreshSaved();
        }
        private void RefreshSaved()
        {
            var checkpoint = GetComponent<CheckpointInteractable>();
            bool lit = CheckpointService.IsUnlocked(gameObject.scene.path, checkpoint.checkpointId);
            ApplyAll(lit ? 1f : 0f);
            refreshPending = false;
        }
        public void PrepareActivation()
        {
            StopActivationSound();
            StopAllCoroutines(); revealing = true; ApplyAll(0f);
        }
        public void BeginReveal()
        {
            PlayActivationSound();
            StopAllCoroutines(); revealing = true; StartCoroutine(Reveal());
        }
        private void PlayActivationSound()
        {
            var settings=activationAudio!=null?activationAudio:CheckpointAudioSettings.Default;
            if(settings==null||settings.activation==null)return;
            if(activationVoice==null){activationVoice=gameObject.AddComponent<AudioSource>();activationVoice.playOnAwake=false;activationVoice.spatialBlend=0;activationVoice.ignoreListenerPause=true;}
            activationVoice.Stop();activationVoice.clip=settings.activation;activationVoice.loop=false;
            var menuAudio=Resources.Load<MenuAudioSettings>("DefaultMenuAudio");
            activationVoice.volume=settings.volume*(menuAudio!=null?menuAudio.volume:1);
            activationVoice.pitch=settings.pitch;activationVoice.Play();
        }
        private void StopActivationSound(){if(activationVoice!=null)activationVoice.Stop();}
        private IEnumerator Reveal()
        {
            int count = glyphs == null ? 0 : glyphs.Length;
            float elapsed = 0f, duration = Mathf.Max(0.1f, activationDuration);
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                for (int i = 0; i < count; i++)
                {
                    float delay = count <= 1 ? 0f : duration * 0.45f * i / (count - 1);
                    float value = Mathf.Clamp01((elapsed - delay) / (duration * 0.55f));
                    Apply(i, Mathf.Floor(value * 8f) / 8f);
                }
                yield return null;
            }
            ApplyAll(1f); revealing = false;
        }
        public void CompleteActivation()
        {
            StopActivationSound();
            StopAllCoroutines(); revealing = false; RefreshSaved();
        }
        private void ApplyAll(float amount)
        {
            if (glyphs == null) return;
            for (int i = 0; i < glyphs.Length; i++) Apply(i, amount);
        }
        private void Apply(int index, float amount)
        {
            if (glyphs[index] == null) return;
            float brightness = Mathf.Lerp(0.35f, 1f, amount);
            PixelPresentation.SetPersistentColor(glyphs[index], new Color(brightness, brightness, brightness, Mathf.Lerp(dormantOpacity, litOpacity, amount)));
            glyphs[index].enabled = glyphs[index].color.a > 0f;
        }
        private void OnDisable()
        {
            StopActivationSound();
            if(glyphs!=null)foreach(var glyph in glyphs)PixelPresentation.SetUnscaled(glyph,false);
            CheckpointService.SavedChanged -= MarkDirty;
            StopAllCoroutines(); revealing = false;
        }
    }
}
