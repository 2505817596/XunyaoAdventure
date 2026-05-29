using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace LeanClr.Battle;

internal static class BattleSkillCapabilitySnapshot
{
    public static string Build()
    {
        StringBuilder sb = new();
        AppendLine(sb, "# Battle Skill Capability Snapshot");
        AppendLine(sb, "");
        AppendLine(sb, "This file is generated from `BattleSkillProfiles.All`.");
        AppendLine(sb, "");

        BattleSkillProfile[] profiles = GetProfiles();
        AppendLine(sb, $"profiles={profiles.Length}");

        for (int i = 0; i < profiles.Length; i++)
        {
            AppendProfile(sb, profiles[i]);
        }

        AppendLine(sb, "");
        AppendLine(sb, "capabilities:");
        AppendLine(sb, $"  effect_types: {JoinEnumNames(GetEffectTypes(profiles))}");
        AppendLine(sb, $"  targets: {JoinEnumNames(GetTargets(profiles), includeZero: true)}");
        AppendLine(sb, $"  trigger_conditions: {JoinEnumNames(GetTriggers(profiles))}");
        AppendLine(sb, $"  buffs: {JoinEnumNames(GetBuffs(profiles))}");
        AppendLine(sb, $"  damage_types: {JoinEnumNames(GetDamageTypes(profiles))}");
        AppendLine(sb, $"  tags: {JoinEnumNames(GetTags(profiles))}");
        AppendLine(sb, $"  formulas: {GetFormulaCount(profiles)}");

        return sb.ToString();
    }

    public static BattleConfigValidationResult ValidateAgainst(string expectedSnapshot)
    {
        string actual = Build().Replace("\r\n", "\n");
        string expected = (expectedSnapshot ?? string.Empty).Replace("\r\n", "\n");
        if (!string.Equals(actual, expected, StringComparison.Ordinal))
        {
            return BattleConfigValidationResult.Fail("Battle skill capability snapshot mismatch.");
        }

        return BattleConfigValidationResult.Success();
    }

    public static string WriteGoldenFile()
    {
        string snapshotPath = GetGoldenFilePath();
        File.WriteAllText(snapshotPath, Build(), new UTF8Encoding(false));
        return snapshotPath;
    }

    public static string GetGoldenFilePath()
    {
        string snapshotPath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Battle", "Skill", "BattleSkillCapabilitySnapshot.generated.txt");
        return Path.GetFullPath(snapshotPath);
    }

    private static BattleSkillProfile[] GetProfiles()
        => BattleSkillProfiles.All as BattleSkillProfile[] ?? new List<BattleSkillProfile>(BattleSkillProfiles.All).ToArray();

    private static void AppendProfile(StringBuilder sb, BattleSkillProfile profile)
    {
        AppendLine(sb, $"profile {profile.Id}:");
        AppendLine(sb, $"  unit_tags: {FormatUnitTags(profile.UnitTags)}");
        AppendLine(sb, $"  basic_attack: {SummarizeEffects(profile.BasicAttackEffects)}");
        AppendLine(sb, $"  ultimate: {SummarizeEffects(profile.UltimateEffects)}");
        if (profile.UltimateTimelineEvents.Length > 0)
        {
            AppendLine(sb, $"  ultimate_timeline: {SummarizeTimeline(profile.UltimateTimelineEvents)}");
        }
        if (profile.AutoSkillTemplates.Length > 0)
        {
            AppendLine(sb, $"  auto_skills: {SummarizeAutoSkills(profile.AutoSkillTemplates)}");
        }
        if (profile.DeathEffects.Length > 0)
        {
            AppendLine(sb, $"  death_effects: {SummarizeEffects(profile.DeathEffects)}");
        }
        if (profile.PassiveTriggers.Length > 0)
        {
            AppendLine(sb, $"  passive_triggers: {SummarizeTriggers(profile.PassiveTriggers)}");
        }
    }

    private static string SummarizeEffects(BattleSkillEffectTemplate[] effects)
    {
        List<string> parts = new();
        for (int i = 0; i < effects.Length; i++)
        {
            BattleSkillEffectTemplate effect = effects[i];
            parts.Add($"{effect.Type}/{effect.Target}/{effect.TargetTeam}/{effect.DamageType}/{effect.BuffType}/{FormatSkillTags(effect.Tags)}");
        }

        return string.Join(", ", parts);
    }

    private static string SummarizeTimeline(BattleSkillTimelineEventTemplate[] events)
    {
        List<string> parts = new();
        for (int i = 0; i < events.Length; i++)
        {
            BattleSkillTimelineEventTemplate timeline = events[i];
            parts.Add($"{timeline.Time.Raw}:{timeline.ProjectileMotionType}/{timeline.ProjectileImpactType}/{timeline.ProjectileTargetMode}x{timeline.ProjectileTargetCount}/{timeline.Effects.Length}");
        }

        return string.Join(", ", parts);
    }

    private static string SummarizeAutoSkills(BattleSkillConfigTemplate[] skills)
    {
        List<string> parts = new();
        for (int i = 0; i < skills.Length; i++)
        {
            BattleSkillConfigTemplate skill = skills[i];
            parts.Add($"{skill.Slot}/{skill.Type}/{skill.TargetTeam}/{skill.Effects.Length}/{skill.TimelineEvents.Length}");
        }

        return string.Join(", ", parts);
    }

    private static string SummarizeTriggers(BattlePassiveTriggerTemplate[] triggers)
    {
        List<string> parts = new();
        for (int i = 0; i < triggers.Length; i++)
        {
            BattlePassiveTriggerTemplate trigger = triggers[i];
            parts.Add($"{trigger.Condition}:{trigger.Threshold.Raw}/{trigger.Effects.Length}/{trigger.ConsumeOnTrigger}");
        }

        return string.Join(", ", parts);
    }

    private static HashSet<BattleSkillEffectType> GetEffectTypes(BattleSkillProfile[] profiles)
    {
        HashSet<BattleSkillEffectType> result = new();
        for (int i = 0; i < profiles.Length; i++)
        {
            CollectEffectTypes(result, profiles[i].BasicAttackEffects);
            CollectEffectTypes(result, profiles[i].UltimateEffects);
            CollectEffectTypes(result, profiles[i].DeathEffects);
            CollectEffectTypes(result, profiles[i].PassiveTriggers);
            CollectEffectTypes(result, profiles[i].AutoSkillTemplates);
            CollectEffectTypes(result, profiles[i].UltimateTimelineEvents);
        }
        return result;
    }

    private static HashSet<BattleSkillEffectTarget> GetTargets(BattleSkillProfile[] profiles)
    {
        HashSet<BattleSkillEffectTarget> result = new();
        for (int i = 0; i < profiles.Length; i++)
        {
            CollectTargets(result, profiles[i].BasicAttackEffects);
            CollectTargets(result, profiles[i].UltimateEffects);
            CollectTargets(result, profiles[i].DeathEffects);
            CollectTargets(result, profiles[i].PassiveTriggers);
            CollectTargets(result, profiles[i].AutoSkillTemplates);
            CollectTargets(result, profiles[i].UltimateTimelineEvents);
        }
        return result;
    }

    private static HashSet<BattlePassiveTriggerCondition> GetTriggers(BattleSkillProfile[] profiles)
    {
        HashSet<BattlePassiveTriggerCondition> result = new();
        for (int i = 0; i < profiles.Length; i++)
        {
            for (int j = 0; j < profiles[i].PassiveTriggers.Length; j++)
            {
                result.Add(profiles[i].PassiveTriggers[j].Condition);
            }
        }
        return result;
    }

    private static HashSet<BattleBuffType> GetBuffs(BattleSkillProfile[] profiles)
    {
        HashSet<BattleBuffType> result = new();
        for (int i = 0; i < profiles.Length; i++)
        {
            CollectBuffs(result, profiles[i].BasicAttackEffects);
            CollectBuffs(result, profiles[i].UltimateEffects);
            CollectBuffs(result, profiles[i].DeathEffects);
            CollectBuffs(result, profiles[i].PassiveTriggers);
            CollectBuffs(result, profiles[i].AutoSkillTemplates);
            CollectBuffs(result, profiles[i].UltimateTimelineEvents);
        }
        return result;
    }

    private static HashSet<BattleDamageType> GetDamageTypes(BattleSkillProfile[] profiles)
    {
        HashSet<BattleDamageType> result = new();
        for (int i = 0; i < profiles.Length; i++)
        {
            CollectDamageTypes(result, profiles[i].BasicAttackEffects);
            CollectDamageTypes(result, profiles[i].UltimateEffects);
            CollectDamageTypes(result, profiles[i].DeathEffects);
            CollectDamageTypes(result, profiles[i].PassiveTriggers);
            CollectDamageTypes(result, profiles[i].AutoSkillTemplates);
            CollectDamageTypes(result, profiles[i].UltimateTimelineEvents);
        }
        return result;
    }

    private static HashSet<BattleSkillTag> GetTags(BattleSkillProfile[] profiles)
    {
        HashSet<BattleSkillTag> result = new();
        for (int i = 0; i < profiles.Length; i++)
        {
            CollectSkillTags(result, profiles[i].BasicAttackEffects);
            CollectSkillTags(result, profiles[i].UltimateEffects);
            CollectSkillTags(result, profiles[i].DeathEffects);
            CollectSkillTags(result, profiles[i].PassiveTriggers);
            CollectSkillTags(result, profiles[i].AutoSkillTemplates);
            CollectSkillTags(result, profiles[i].UltimateTimelineEvents);
        }
        return result;
    }

    private static int GetFormulaCount(BattleSkillProfile[] profiles)
    {
        int count = 0;
        for (int i = 0; i < profiles.Length; i++)
        {
            count += CountFormulas(profiles[i].BasicAttackEffects);
            count += CountFormulas(profiles[i].UltimateEffects);
            count += CountFormulas(profiles[i].DeathEffects);
            count += CountFormulas(profiles[i].PassiveTriggers);
            count += CountFormulas(profiles[i].AutoSkillTemplates);
            count += CountFormulas(profiles[i].UltimateTimelineEvents);
        }

        return count;
    }

    private static void CollectEffectTypes(HashSet<BattleSkillEffectType> set, BattleSkillEffectTemplate[] effects)
    {
        for (int i = 0; i < effects.Length; i++)
        {
            set.Add(effects[i].Type);
        }
    }

    private static void CollectEffectTypes(HashSet<BattleSkillEffectType> set, BattlePassiveTriggerTemplate[] triggers)
    {
        for (int i = 0; i < triggers.Length; i++)
        {
            CollectEffectTypes(set, triggers[i].Effects);
        }
    }

    private static void CollectEffectTypes(HashSet<BattleSkillEffectType> set, BattleSkillConfigTemplate[] skills)
    {
        for (int i = 0; i < skills.Length; i++)
        {
            CollectEffectTypes(set, skills[i].Effects);
        }
    }

    private static void CollectEffectTypes(HashSet<BattleSkillEffectType> set, BattleSkillTimelineEventTemplate[] events)
    {
        for (int i = 0; i < events.Length; i++)
        {
            CollectEffectTypes(set, events[i].Effects);
        }
    }

    private static void CollectTargets(HashSet<BattleSkillEffectTarget> set, BattleSkillEffectTemplate[] effects)
    {
        for (int i = 0; i < effects.Length; i++)
        {
            set.Add(effects[i].Target);
        }
    }

    private static void CollectTargets(HashSet<BattleSkillEffectTarget> set, BattlePassiveTriggerTemplate[] triggers)
    {
        for (int i = 0; i < triggers.Length; i++)
        {
            CollectTargets(set, triggers[i].Effects);
        }
    }

    private static void CollectTargets(HashSet<BattleSkillEffectTarget> set, BattleSkillConfigTemplate[] skills)
    {
        for (int i = 0; i < skills.Length; i++)
        {
            CollectTargets(set, skills[i].Effects);
        }
    }

    private static void CollectTargets(HashSet<BattleSkillEffectTarget> set, BattleSkillTimelineEventTemplate[] events)
    {
        for (int i = 0; i < events.Length; i++)
        {
            CollectTargets(set, events[i].Effects);
        }
    }

    private static void CollectBuffs(HashSet<BattleBuffType> set, BattleSkillEffectTemplate[] effects)
    {
        for (int i = 0; i < effects.Length; i++)
        {
            if (effects[i].BuffType != BattleBuffType.None)
            {
                set.Add(effects[i].BuffType);
            }
        }
    }

    private static void CollectBuffs(HashSet<BattleBuffType> set, BattlePassiveTriggerTemplate[] triggers)
    {
        for (int i = 0; i < triggers.Length; i++)
        {
            CollectBuffs(set, triggers[i].Effects);
        }
    }

    private static void CollectBuffs(HashSet<BattleBuffType> set, BattleSkillConfigTemplate[] skills)
    {
        for (int i = 0; i < skills.Length; i++)
        {
            CollectBuffs(set, skills[i].Effects);
        }
    }

    private static void CollectBuffs(HashSet<BattleBuffType> set, BattleSkillTimelineEventTemplate[] events)
    {
        for (int i = 0; i < events.Length; i++)
        {
            CollectBuffs(set, events[i].Effects);
        }
    }

    private static void CollectDamageTypes(HashSet<BattleDamageType> set, BattleSkillEffectTemplate[] effects)
    {
        for (int i = 0; i < effects.Length; i++)
        {
            set.Add(effects[i].DamageType);
        }
    }

    private static void CollectDamageTypes(HashSet<BattleDamageType> set, BattlePassiveTriggerTemplate[] triggers)
    {
        for (int i = 0; i < triggers.Length; i++)
        {
            CollectDamageTypes(set, triggers[i].Effects);
        }
    }

    private static void CollectDamageTypes(HashSet<BattleDamageType> set, BattleSkillConfigTemplate[] skills)
    {
        for (int i = 0; i < skills.Length; i++)
        {
            CollectDamageTypes(set, skills[i].Effects);
        }
    }

    private static void CollectDamageTypes(HashSet<BattleDamageType> set, BattleSkillTimelineEventTemplate[] events)
    {
        for (int i = 0; i < events.Length; i++)
        {
            CollectDamageTypes(set, events[i].Effects);
        }
    }

    private static void CollectSkillTags(HashSet<BattleSkillTag> set, BattleSkillEffectTemplate[] effects)
    {
        for (int i = 0; i < effects.Length; i++)
        {
            AddTagBits(set, effects[i].Tags);
        }
    }

    private static void CollectSkillTags(HashSet<BattleSkillTag> set, BattlePassiveTriggerTemplate[] triggers)
    {
        for (int i = 0; i < triggers.Length; i++)
        {
            CollectSkillTags(set, triggers[i].Effects);
        }
    }

    private static void CollectSkillTags(HashSet<BattleSkillTag> set, BattleSkillConfigTemplate[] skills)
    {
        for (int i = 0; i < skills.Length; i++)
        {
            CollectSkillTags(set, skills[i].Effects);
        }
    }

    private static void CollectSkillTags(HashSet<BattleSkillTag> set, BattleSkillTimelineEventTemplate[] events)
    {
        for (int i = 0; i < events.Length; i++)
        {
            CollectSkillTags(set, events[i].Effects);
        }
    }

    private static void AddTagBits(HashSet<BattleSkillTag> set, BattleSkillTag tags)
    {
        BattleSkillTag[] flags =
        {
            BattleSkillTag.Damage,
            BattleSkillTag.Heal,
            BattleSkillTag.Buff,
            BattleSkillTag.Debuff,
            BattleSkillTag.Control,
            BattleSkillTag.Shield,
            BattleSkillTag.Dispel,
            BattleSkillTag.Energy,
            BattleSkillTag.Displace,
            BattleSkillTag.Revive,
            BattleSkillTag.Summon,
            BattleSkillTag.Periodic,
            BattleSkillTag.Immunity,
            BattleSkillTag.NoLifeSteal,
            BattleSkillTag.NoReflection,
        };

        for (int i = 0; i < flags.Length; i++)
        {
            if ((tags & flags[i]) == flags[i])
            {
                set.Add(flags[i]);
            }
        }
    }

    private static int CountFormulas(BattleSkillEffectTemplate[] effects)
    {
        int count = 0;
        for (int i = 0; i < effects.Length; i++)
        {
            if (!effects[i].AmountFormula.IsEmpty) count++;
            if (!effects[i].RadiusFormula.IsEmpty) count++;
            if (!effects[i].StatusDurationFormula.IsEmpty) count++;
        }
        return count;
    }

    private static int CountFormulas(BattlePassiveTriggerTemplate[] triggers)
    {
        int count = 0;
        for (int i = 0; i < triggers.Length; i++)
        {
            count += CountFormulas(triggers[i].Effects);
        }
        return count;
    }

    private static int CountFormulas(BattleSkillConfigTemplate[] skills)
    {
        int count = 0;
        for (int i = 0; i < skills.Length; i++)
        {
            count += CountFormulas(skills[i].Effects);
            count += CountFormulas(skills[i].TimelineEvents);
        }
        return count;
    }

    private static int CountFormulas(BattleSkillTimelineEventTemplate[] events)
    {
        int count = 0;
        for (int i = 0; i < events.Length; i++)
        {
            count += CountFormulas(events[i].Effects);
        }
        return count;
    }

    private static string FormatSkillTags(BattleSkillTag tags)
    {
        if (tags == BattleSkillTag.None)
        {
            return "None";
        }

        List<string> values = new();
        AddTag(values, tags, BattleSkillTag.Damage, "Damage");
        AddTag(values, tags, BattleSkillTag.Heal, "Heal");
        AddTag(values, tags, BattleSkillTag.Buff, "Buff");
        AddTag(values, tags, BattleSkillTag.Debuff, "Debuff");
        AddTag(values, tags, BattleSkillTag.Control, "Control");
        AddTag(values, tags, BattleSkillTag.Shield, "Shield");
        AddTag(values, tags, BattleSkillTag.Dispel, "Dispel");
        AddTag(values, tags, BattleSkillTag.Energy, "Energy");
        AddTag(values, tags, BattleSkillTag.Displace, "Displace");
        AddTag(values, tags, BattleSkillTag.Revive, "Revive");
        AddTag(values, tags, BattleSkillTag.Summon, "Summon");
        AddTag(values, tags, BattleSkillTag.Periodic, "Periodic");
        AddTag(values, tags, BattleSkillTag.Immunity, "Immunity");
        AddTag(values, tags, BattleSkillTag.NoLifeSteal, "NoLifeSteal");
        AddTag(values, tags, BattleSkillTag.NoReflection, "NoReflection");
        return string.Join("|", values);
    }

    private static string FormatUnitTags(BattleUnitTag tags)
    {
        if (tags == BattleUnitTag.None)
        {
            return "None";
        }

        List<string> values = new();
        AddTag(values, tags, BattleUnitTag.Hero, "Hero");
        AddTag(values, tags, BattleUnitTag.Summon, "Summon");
        AddTag(values, tags, BattleUnitTag.Illusion, "Illusion");
        AddTag(values, tags, BattleUnitTag.Boss, "Boss");
        AddTag(values, tags, BattleUnitTag.Front, "Front");
        AddTag(values, tags, BattleUnitTag.Middle, "Middle");
        AddTag(values, tags, BattleUnitTag.Back, "Back");
        AddTag(values, tags, BattleUnitTag.Tank, "Tank");
        AddTag(values, tags, BattleUnitTag.Warrior, "Warrior");
        AddTag(values, tags, BattleUnitTag.Assassin, "Assassin");
        AddTag(values, tags, BattleUnitTag.Mage, "Mage");
        AddTag(values, tags, BattleUnitTag.Support, "Support");
        return string.Join("|", values);
    }

    private static void AddTag(List<string> values, BattleSkillTag tags, BattleSkillTag flag, string name)
    {
        if ((tags & flag) == flag)
        {
            values.Add(name);
        }
    }

    private static void AddTag(List<string> values, BattleUnitTag tags, BattleUnitTag flag, string name)
    {
        if ((tags & flag) == flag)
        {
            values.Add(name);
        }
    }

    private static string JoinEnumNames<T>(IEnumerable<T> values, bool includeZero = false) where T : struct, Enum
    {
        List<string> items = new();
        foreach (T value in values)
        {
            if (includeZero || Convert.ToInt64(value) != 0)
            {
                items.Add(value.ToString());
            }
        }

        items.Sort(StringComparer.Ordinal);
        return items.Count == 0 ? "-" : string.Join(", ", items);
    }

    private static void AppendLine(StringBuilder sb, string value)
        => sb.AppendLine(value);
}
