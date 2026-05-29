using LeanClr.Mathematics;

namespace LeanClr.Battle;

internal sealed class BattleSkillEffectExecutor
{
    private readonly BattleWorld _world;
    private readonly BattleTargetSelector _targetSelector;

    public BattleSkillEffectExecutor(BattleWorld world)
    {
        _world = world;
        _targetSelector = new BattleTargetSelector(world);
    }

    public void Execute(BattleUnit source, BattleUnit primaryTarget, BattleSkillSpec skill)
        => Execute(source, primaryTarget, skill.Effects, skill.Type);

    public void Execute(BattleUnit source, BattleUnit primaryTarget, BattleSkillHit hit)
        => Execute(source, primaryTarget, hit.Effects, hit.Skill.Type);

    public void ExecuteAtPosition(BattleUnit source, FixVec2 center, BattleSkillHit hit)
        => ExecuteAtPosition(source, center, hit.Effects, hit.Skill.Type);

    public void ExecuteEffects(BattleUnit source, BattleUnit primaryTarget, BattleSkillEffectSpec[] effects)
        => Execute(source, primaryTarget, effects);

    public void ExecuteEffectsAtPosition(BattleUnit source, FixVec2 center, BattleSkillEffectSpec[] effects)
        => ExecuteAtPosition(source, center, effects);

    private void Execute(BattleUnit source, BattleUnit primaryTarget, BattleSkillEffectSpec[] effects)
        => Execute(source, primaryTarget, effects, BattleSkillType.Passive);

    private void Execute(BattleUnit source, BattleUnit primaryTarget, BattleSkillEffectSpec[] effects, BattleSkillType skillType)
    {
        for (int i = 0; i < effects.Length; i++)
        {
            ExecuteEffect(source, primaryTarget, effects[i], skillType);
        }
    }

    private void ExecuteAtPosition(BattleUnit source, FixVec2 center, BattleSkillEffectSpec[] effects)
        => ExecuteAtPosition(source, center, effects, BattleSkillType.Passive);

    private void ExecuteAtPosition(BattleUnit source, FixVec2 center, BattleSkillEffectSpec[] effects, BattleSkillType skillType)
    {
        for (int i = 0; i < effects.Length; i++)
        {
            ExecuteEffectAtPosition(source, center, effects[i], skillType);
        }
    }

    private void ExecuteEffect(BattleUnit source, BattleUnit primaryTarget, BattleSkillEffectSpec effect)
        => ExecuteEffect(source, primaryTarget, effect, BattleSkillType.Passive);

    private void ExecuteEffect(BattleUnit source, BattleUnit primaryTarget, BattleSkillEffectSpec effect, BattleSkillType skillType)
    {
        if (effect.Type == BattleSkillEffectType.Summon)
        {
            _world.ApplySummon(source, primaryTarget, effect);
            return;
        }

        _targetSelector.ForEachSelectedUnit(source, primaryTarget, effect, unit => ApplyEffectToUnit(source, unit, effect, skillType));
    }

    private void ExecuteEffectAtPosition(BattleUnit source, FixVec2 center, BattleSkillEffectSpec effect)
        => ExecuteEffectAtPosition(source, center, effect, BattleSkillType.Passive);

    private void ExecuteEffectAtPosition(BattleUnit source, FixVec2 center, BattleSkillEffectSpec effect, BattleSkillType skillType)
    {
        if (effect.Type == BattleSkillEffectType.Summon)
        {
            _world.ApplySummonAtPosition(source, center, effect);
            return;
        }

        _targetSelector.ForEachSelectedUnitAtPosition(source, center, effect, unit => ApplyEffectToUnit(source, unit, effect, skillType));
    }

    private void ApplyEffectToUnit(BattleUnit source, BattleUnit target, BattleSkillEffectSpec effect, BattleSkillType skillType)
    {
        switch (effect.Type)
        {
            case BattleSkillEffectType.Damage:
                _world.ApplyDamage(source, target, effect.ResolveAmount(source, target), effect.DamageType, skillType, effect.Tags);
                break;
            case BattleSkillEffectType.ApplyStatus:
            case BattleSkillEffectType.Revive:
                if (effect.BuffType == BattleBuffType.None)
                {
                    return;
                }

                BattleBuff? buff = BattleBuffFactory.Create(effect, source.Id);
                if (buff != null)
                {
                    _world.ApplyBuff(source, target, buff);
                }
                break;
            case BattleSkillEffectType.Heal:
                _world.ApplyHeal(source, target, effect.ResolveAmount(source, target));
                break;
            case BattleSkillEffectType.Dispel:
                _world.ApplyDispel(source, target, effect.ResolveDispelCount(), effect.ResolveDispelMode());
                break;
            case BattleSkillEffectType.ModifyEnergy:
                _world.ApplyEnergy(source, target, effect.ResolveAmount(source, target));
                break;
            case BattleSkillEffectType.Displace:
                _world.ApplyDisplace(source, target, effect.ResolveAmount(source, target));
                break;
            case BattleSkillEffectType.Execute:
                ExecuteUnit(source, target, effect, skillType);
                break;
            case BattleSkillEffectType.Interrupt:
                _world.ApplyInterrupt(source, target);
                break;
        }
    }

    private void ExecuteUnit(BattleUnit source, BattleUnit target, BattleSkillEffectSpec effect, BattleSkillType skillType)
    {
        if (!source.IsAlive || !target.IsAlive)
        {
            return;
        }

        Fix32 threshold = effect.ResolveAmount(source, target);
        if (threshold <= Fix32.Zero || target.HpRatio > threshold)
        {
            return;
        }

        _world.ApplyDamage(source, target, target.Hp, effect.DamageType, skillType, effect.Tags);
    }

}
