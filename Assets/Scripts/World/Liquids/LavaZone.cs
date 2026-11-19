using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>Foot-position lava exposure, independent of animated tile collider settings.</summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Tilemap))]
public sealed class LavaZone : MonoBehaviour
{
    [Min(.1f)] public float duration = 5f;
    [Min(0f), Tooltip("Burn damage per second, as percent of maximum HP.")]
    public float maxHPPercentPerSecond = 4f;
    [Min(.05f)] public float refreshInterval = .2f;
    private Tilemap lava;
    private float nextScan;
    private readonly HashSet<CharacterDebuffs> exposed = new HashSet<CharacterDebuffs>();

    private void Awake()
    {
        lava = GetComponent<Tilemap>();
        // Animated map tiles default to no collision; use trigger cells for wading.
        foreach (var cell in lava.cellBounds.allPositionsWithin)
            if (lava.HasTile(cell)) lava.SetColliderType(cell, Tile.ColliderType.Grid);
        var collider = GetComponent<TilemapCollider2D>();
        if (collider != null) collider.ProcessTilemapChanges();
    }
    public bool IsExposing(CharacterHealth actor)
    {
        var affinity=actor!=null?actor.GetComponent<LavaAffinity>():null;
        var feet=actor!=null?actor.GetComponent<Collider2D>():null;
        Vector2 point=feet!=null?new Vector2(feet.bounds.center.x,feet.bounds.min.y):(actor!=null?(Vector2)actor.transform.position:Vector2.zero);
        var visual=actor!=null?actor.GetComponent<PlayerWaterRipples>():null;
        point+=visual!=null?visual.footOffset:WaterEffectDefaults.Current!=null?WaterEffectDefaults.Current.footOffset:new Vector2(0,.04f);
        return isActiveAndEnabled && lava != null && actor != null && !actor.isDead &&
            (affinity==null||!affinity.isActiveAndEnabled||!affinity.immuneToLavaBurn) &&
            actor.GetComponent<EnemyFlight>() == null && lava.HasTile(lava.WorldToCell(point));
    }
    private void Update()
    {
        if (Time.timeScale <= 0f || Time.time < nextScan) return;
        nextScan = Time.time + Mathf.Max(.05f, refreshInterval);
        exposed.RemoveWhere(d => d == null);
        foreach (var status in exposed)
            if (!IsExposing(status.GetComponent<CharacterHealth>()))
                status.SetExposure(CharacterDebuffs.Kind.Burning, this, false);
        exposed.RemoveWhere(d => !IsExposing(d.GetComponent<CharacterHealth>()));
        foreach (var actor in FindObjectsByType<CharacterHealth>(FindObjectsSortMode.None))
        {
            if (!IsExposing(actor)) continue;
            var status = actor.GetComponent<CharacterDebuffs>() ?? actor.gameObject.AddComponent<CharacterDebuffs>();
            exposed.Add(status);
            status.SetExposure(CharacterDebuffs.Kind.Burning, this, true);
            if (!status.IsBurning) status.ApplyBurn(duration, maxHPPercentPerSecond);
        }
    }
    private void OnDisable()
    {
        foreach (var status in exposed)
            if (status != null) status.SetExposure(CharacterDebuffs.Kind.Burning, this, false);
        exposed.Clear();
    }
}
