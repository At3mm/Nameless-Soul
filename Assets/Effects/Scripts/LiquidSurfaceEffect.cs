using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>Uses shared default animation materials without adding gameplay colliders or hazards.</summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(TilemapRenderer))]
public sealed class LiquidSurfaceEffect : MonoBehaviour
{
    public enum SurfaceKind { Auto, Water, Poison }
    public SurfaceKind kind;
    public bool useDefaultMaterial=true;
    public static void Ensure(GameObject target,SurfaceKind type=SurfaceKind.Auto)
    {
        if(!Application.isPlaying || target.GetComponent<TilemapRenderer>()==null) return;
        var effect=target.GetComponent<LiquidSurfaceEffect>() ?? target.AddComponent<LiquidSurfaceEffect>();
        if(type!=SurfaceKind.Auto) effect.kind=type;
        effect.ApplyDefaults();
    }
    private void OnEnable() { ApplyDefaults(); }
    private void OnValidate() { if(Application.isPlaying) ApplyDefaults(); }
    public void ApplyDefaults()
    {
        // Lava retains its own pixel artwork/material, even with shared wading logic.
        if (GetComponent<LavaZone>() != null) return;
        if(!useDefaultMaterial) return;
        var defaults=LiquidSurfaceDefaults.Current;
        if(defaults==null) return;
        bool poison=kind==SurfaceKind.Poison || (kind==SurfaceKind.Auto && (GetComponent<PoisonZone>()!=null || name=="Poison"));
        var material=poison ? defaults.poisonMaterial : defaults.waterMaterial;
        if(material!=null) GetComponent<TilemapRenderer>().sharedMaterial=material;
    }
}
