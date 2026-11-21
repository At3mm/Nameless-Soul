using System.Collections.Generic;
using UnityEngine;

/// <summary>Stable non-final boss encounter IDs used only to recognize victories in older saves.</summary>
public sealed class BossDefeatCatalog : ScriptableObject
{
    public List<string> bossEncounterIds=new List<string>();
}
