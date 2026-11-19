using UnityEngine;
[CreateAssetMenu(menuName="Game/Status HUD Settings")]
public sealed class StatusHUDSettings : ScriptableObject
{
    public Sprite[] buffs=new Sprite[5];
    public Sprite[] debuffs=new Sprite[3];
}
