using UnityEngine;

[CreateAssetMenu(menuName="Game/Quests/Quest Item")]
public sealed class QuestItemDefinition : GameItemDefinition
{
    public static QuestItemDefinition Find(string id)
    {
        foreach(var item in Resources.LoadAll<QuestItemDefinition>("QuestItems"))if(item.itemId==id)return item;
        return null;
    }
}
