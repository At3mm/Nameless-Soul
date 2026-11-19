public sealed class SlimeCombat : EnemyCombat
{
    public const string QuickAttack = "NormalAttack";
    public const string HeavySlash = "HeavyAttack";

    // The quick strike closes distance. The heavy slash stays planted during its wind-up.
    protected override bool ShouldLunge(SkillData skill) => skill.skillName == QuickAttack;
}
