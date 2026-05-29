namespace LeanClr.Battle;

[System.Flags]
public enum BattleSkillTag
{
    None = 0,
    Damage = 1 << 0,
    Heal = 1 << 1,
    Buff = 1 << 2,
    Debuff = 1 << 3,
    Control = 1 << 4,
    Shield = 1 << 5,
    Dispel = 1 << 6,
    Energy = 1 << 7,
    Displace = 1 << 8,
    Revive = 1 << 9,
    Summon = 1 << 10,
    Periodic = 1 << 11,
    Immunity = 1 << 12,
    NoLifeSteal = 1 << 13,
    NoReflection = 1 << 14,
}

public enum BattleSkillType
{
    None = 0,
    BasicAttack = 1,
    Ultimate = 2,
    AutoSkill = 3,
    Passive = 4,
}

public enum BattleSkillSlot
{
    BasicAttack = 0,
    Ultimate = 1,
    AutoSkill1 = 2,
    AutoSkill2 = 3,
    Passive = 4,
}

public enum BattleSkillRuntimePhase
{
    None = 0,
    Windup = 1,
    Recover = 2,
    Finished = 3,
}

public enum BattleSkillEffectType
{
    None = 0,
    Damage = 1,
    ApplyStatus = 2,
    Heal = 3,
    Dispel = 4,
    ModifyEnergy = 5,
    Displace = 6,
    Revive = 7,
    Summon = 8,
    Execute = 9,
    Interrupt = 10,
}

public enum BattleDispelMode
{
    Any = 0,
    BuffOnly = 1,
    DebuffOnly = 2,
}

public enum BattleSkillEffectTarget
{
    PrimaryTarget = 0,
    Self = 1,
    EnemyUnitsInRadius = 2,
    AllyUnitsInRadius = 3,
    AllEnemyUnits = 4,
    AllAllyUnits = 5,
    LowestHpEnemy = 6,
    LowestHpAlly = 7,
    RandomEnemy = 8,
    RandomAlly = 9,
    NearestEnemy = 10,
    NearestAlly = 11,
    FrontEnemy = 12,
    FrontAlly = 13,
    EnemyUnitsInLine = 14,
    AllyUnitsInLine = 15,
    EnemyUnitsInFan = 16,
    AllyUnitsInFan = 17,
    ChainedEnemies = 18,
    ChainedAllies = 19,
    BackEnemy = 20,
    BackAlly = 21,
    MiddleEnemy = 22,
    MiddleAlly = 23,
    FrontEnemyRow = 24,
    FrontAllyRow = 25,
    BackEnemyRow = 26,
    BackAllyRow = 27,
    MiddleEnemyRow = 28,
    MiddleAllyRow = 29,
}

public enum BattleSkillEffectConditionType
{
    None = 0,
    SourceHpBelow = 1,
    SourceHpAbove = 2,
    TargetHpBelow = 3,
    TargetHpAbove = 4,
    SourceHasBuff = 5,
    TargetHasBuff = 6,
    SourceMissingBuff = 7,
    TargetMissingBuff = 8,
    TargetIsControlled = 9,
    TargetIsNotControlled = 10,
    SourceHasUnitTag = 11,
    TargetHasUnitTag = 12,
    SourceMissingUnitTag = 13,
    TargetMissingUnitTag = 14,
}

public enum BattleSkillTargetTeam
{
    Enemy = 0,
    Ally = 1,
    Self = 2,
}

public enum BattleSkillValueSource
{
    Constant = 0,
    PhysicalAttack = 1,
    UltimateDamage = 2,
    UltimateRadius = 3,
    SourceMaxHp = 4,
    SourceCurrentHp = 5,
    SourceMissingHp = 6,
    SourceHpRatio = 7,
    SourceEnergy = 8,
    TargetMaxHp = 9,
    TargetCurrentHp = 10,
    TargetMissingHp = 11,
    TargetHpRatio = 12,
    TargetEnergy = 13,
    SourcePhysicalArmor = 14,
    SourceMagicResist = 15,
    TargetPhysicalArmor = 16,
    TargetMagicResist = 17,
    SourceMagicPower = 18,
    SourcePhysicalCrit = 19,
    TargetPhysicalCrit = 20,
}

public enum BattleFormulaOp
{
    Add = 0,
    Multiply = 1,
}

public enum BattleDamageType
{
    Physical = 0,
    Magical = 1,
    Pure = 2,
    Healing = 3,
    Shield = 4,
    All = 5,
}

public enum BattleProjectileMotionType
{
    None = 0,
    Homing = 1,
    Linear = 2,
}

public enum BattleProjectileImpactType
{
    PrimaryTarget = 0,
    AreaAtDestination = 1,
}

public enum BattleProjectileTargetMode
{
    PrimaryTarget = 0,
    NearestEnemies = 1,
    RandomEnemies = 2,
    LowestHpEnemies = 3,
    FrontEnemyRow = 4,
    BackEnemyRow = 5,
    NearestAllies = 6,
    RandomAllies = 7,
    LowestHpAllies = 8,
}
