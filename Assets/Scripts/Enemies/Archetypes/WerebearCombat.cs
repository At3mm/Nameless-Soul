public sealed class WerebearCombat : EnemyCombat
{
    public const string ClawSwipe = "Skill1";

    // Only the quick swipe closes distance. The combo and ground slam stay planted.
    protected override bool ShouldLunge(SkillData skill) => skill.skillName == ClawSwipe;
}
