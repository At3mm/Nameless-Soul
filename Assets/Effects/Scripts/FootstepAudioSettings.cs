using UnityEngine;

[CreateAssetMenu(menuName="Game/Footstep Audio Settings")]
public sealed class FootstepAudioSettings : ScriptableObject
{
    public enum Surface { Dirt, Grass, Stone, Wood, Water, Poison }
    [System.Serializable] public class Set
    {
        public Surface surface;
        public AudioClip[] walk, run;
    }
    public Set[] surfaces;
    [Range(0,1)] public float volume=.35f;
    [Range(0,1)] public float enemyVolume=.4f;
    [Range(0,1)] public float poisonWaterBlend=.4f;
    public AudioClip[] poisonSplash;
    [Min(1)] public float audibleDistance=16;
    public AudioClip Find(Surface surface,bool running,int variation)
    {
        if(surfaces==null)return null;
        var set=System.Array.Find(surfaces,s=>s!=null && s.surface==surface);
        if(set==null)return null;
        var clips=running?set.run:set.walk;
        return clips!=null && clips.Length>0?clips[variation%clips.Length]:null;
    }
}
