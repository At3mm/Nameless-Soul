using UnityEngine;

/// <summary>Bounded shared SFX voices. Uses the game's existing AudioListener master volume.</summary>
public sealed class CombatSFX : MonoBehaviour
{
    static CombatSFX instance;
    static CombatAudioSettings settings;
    readonly AudioSource[] voices = new AudioSource[10];
    readonly AudioSource[] breakVoices = new AudioSource[2];
    int nextBreakVoice;
    readonly float[] lastPlayed = new float[19];
    int hitVariation;
    int enemySwingVariation;
    int swingVariation, heavyVariation;
    int heavyHitVariation;
    bool paused;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { instance = null; settings = null; }

    public static void Hit(Vector3 position, bool heavy = false, bool enemyAttack = false) { Play(heavy ? 15 : 0, position, false, enemyAttack); }
    public static void Parry(Vector3 position) { Play(1, position); }
    public static void ParryReady(Vector3 position) { Play(16, position); }
    public static void EnemyBroken(Vector3 position) { Play(18, position); }
    public static void Dash(Vector3 position) { Play(2, position); }
    public static void Buff(CharacterBuffs.Kind kind, Vector3 position) { if ((int)kind >= 0 && (int)kind < 5) Play(3 + (int)kind, position); }
    public static void Debuff(CharacterDebuffs.Kind kind, Vector3 position) { if ((int)kind >= 0 && (int)kind < 3) Play(8 + (int)kind, position); }
    public static void Heal(Vector3 position) { Play(11, position, false); }
    public static void Cleanse(Vector3 position) { Play(11, position, true); }
    public static void EnemyAttackSwing(Vector3 position) { Play(17,position,false,true); }
    public static void AttackSwing(Vector3 position) { Play(12, position); }
    public static void Skill2Cast(Vector3 position) { Play(13, position); }
    public static void Skill2Swing(Vector3 position) { Play(14, position); }

    static void Play(int id, Vector3 position, bool cleanse = false, bool enemy = false)
    {
        if (!Application.isPlaying || Time.timeScale <= 0f) return;
        if (settings == null)
            settings = Resources.Load<CombatAudioSettings>("DefaultCombatAudio");
        if (settings == null || settings.volume <= 0f) return;
        if (instance == null)
        {
            var go = new GameObject("Combat SFX");
            instance = go.AddComponent<CombatSFX>();
            DontDestroyOnLoad(go);
        }
        instance.Emit(id, position, cleanse, enemy);
    }

    void Awake()
    {
        for (int i = 0; i < lastPlayed.Length; i++) lastPlayed[i] = -100f;
        for (int i = 0; i < voices.Length; i++)
        {
            voices[i] = gameObject.AddComponent<AudioSource>();
            voices[i].playOnAwake = false;
            voices[i].spatialBlend = 0f;
        }
        // Reserve critical feedback so crowded hit/swing voices cannot discard it.
        for (int i = 0; i < breakVoices.Length; i++)
        {
            breakVoices[i] = gameObject.AddComponent<AudioSource>();
            breakVoices[i].playOnAwake = false;
            breakVoices[i].spatialBlend = 0f;
            breakVoices[i].priority = 0;
        }
    }

    void Emit(int id, Vector3 position, bool cleanse, bool enemy)
    {
        float cooldown = id == 0 || id == 15 ? .035f : id == 1 ? .06f : id >= 12 ? .06f : .15f;
        if (id != 18 && Time.time - lastPlayed[id] < cooldown) return;
        float gain = 1f;
        var camera = Camera.main;
        if (camera != null)
        {
            float distance = Vector2.Distance(position, camera.transform.position);
            gain = 1f - Mathf.Clamp01((distance - 5f) / Mathf.Max(1f, settings.audibleDistance - 5f));
        }
        if (gain <= 0f) return;
        AudioClip clip;
        float level;
        if (id == 0) { clip = settings.hits != null && settings.hits.Length > 0 ? settings.hits[hitVariation % settings.hits.Length] : null; level = settings.hitVolume; }
        else if (id == 1) { clip = settings.parry; level = settings.parryVolume; }
        else if (id == 2) { clip = settings.dash; level = settings.dashVolume; }
        else if (id < 8) { clip = settings.buffs != null && settings.buffs.Length > id - 3 ? settings.buffs[id - 3] : null; level = settings.statusVolume; }
        else if (id < 11) { clip = settings.debuffs != null && settings.debuffs.Length > id - 8 ? settings.debuffs[id - 8] : null; level = settings.statusVolume; }
        else if (id == 11) { clip = cleanse ? settings.cleanse : settings.heal; level = settings.statusVolume; }
        else if (id == 12) { clip = Variation(settings.attackSwingVariations, settings.attackSwing, swingVariation); level = settings.swingVolume; }
        else if (id == 13) { clip = settings.skill2Cast; level = settings.skillVolume; }
        else if (id == 14) { clip = Variation(settings.skill2SwingVariations, settings.skill2Swing, heavyVariation); level = settings.skillVolume; }
        else if (id == 15) { clip = Variation(settings.heavyHits, Variation(settings.hits, null, heavyHitVariation), heavyHitVariation); level = settings.hitVolume; }
        else if(id==17) { clip=Variation(settings.attackSwingVariations,settings.attackSwing,enemySwingVariation);level=settings.swingVolume; }
        else if(id==18) { clip=settings.enemyBreak;level=settings.enemyBreakVolume; }
        else { clip = settings.parryReady; level = settings.parryReadyVolume; }
        if (clip == null) return;
        if (id == 18)
        {
            var voice = breakVoices[nextBreakVoice];
            nextBreakVoice = (nextBreakVoice + 1) % breakVoices.Length;
            voice.Stop();
            voice.clip = clip;
            voice.volume = settings.volume * level * gain;
            voice.pitch = 1f;
            voice.Play();
            lastPlayed[id] = Time.time;
            return;
        }
        if(enemy){int active=0;foreach(var source in voices)if(source.isPlaying)active++;if(active>=4)return;}
        foreach (var voice in voices)
        {
            if (voice.isPlaying) continue;
            voice.clip = clip;
            voice.volume = settings.volume * level * gain * (enemy?settings.enemyVolume:1f);
            voice.pitch = id == 0 ? 1f + (hitVariation % 3 - 1) * .035f :
                id == 12 ? 1f + (swingVariation % 3 - 1) * .025f :
                id == 14 ? 1f + (heavyVariation % 3 - 1) * .02f : 1f;
            voice.Play();
            lastPlayed[id] = Time.time;
            if (id == 0) hitVariation++;
            if (id == 12) swingVariation++;
            if (id == 14) heavyVariation++;
            if (id == 15) heavyHitVariation++;
            if (id == 17) enemySwingVariation++;
            return;
        }
    }

    static AudioClip Variation(AudioClip[] clips, AudioClip fallback, int index)
    {
        return clips != null && clips.Length > 0 && clips[index % clips.Length] != null ? clips[index % clips.Length] : fallback;
    }

    void Update()
    {
        bool pause = Time.timeScale <= 0f;
        if (pause == paused) return;
        paused = pause;
        foreach (var voice in voices) { if (pause) voice.Pause(); else voice.UnPause(); }
        foreach (var voice in breakVoices) { if (pause) voice.Pause(); else voice.UnPause(); }
    }
}
