using UnityEngine;

[CreateAssetMenu(menuName="Game/Player Death Drop Settings")]
public sealed class PlayerDeathDropSettings : ScriptableObject
{
    public Sprite icon;
    [Min(.2f)] public float interactionRadius=1.5f;
    [Min(.05f)] public float iconWorldHeight=.6f;
    [Min(0)] public float bobHeight=.07f;
    public string sortingLayer="Characters";
    public int sortingOrder=15;
    private static PlayerDeathDropSettings cached;
    public static PlayerDeathDropSettings Current
    {
        get{if(cached==null){cached=Resources.Load<PlayerDeathDropSettings>("DefaultPlayerDeathDrop");if(cached==null){cached=CreateInstance<PlayerDeathDropSettings>();cached.hideFlags=HideFlags.HideAndDontSave;cached.icon=World.LifeSeedPickup.Icon;}}return cached;}
    }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetCache(){cached=null;}
}
