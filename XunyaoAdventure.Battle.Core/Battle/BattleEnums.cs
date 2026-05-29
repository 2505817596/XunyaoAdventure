namespace LeanClr.Battle;

public enum BattleTeam
{
    Neutral = 0,
    TeamA = 1,
    TeamB = 2,
}

public enum BattleControlMode
{
    Manual = 0,
    AutoCombat = 1,
}

public enum BattlePrimaryAttribute
{
    Strength = 0,
    Intelligence = 1,
    Agility = 2,
}

[System.Flags]
public enum BattleUnitTag
{
    None = 0,
    Hero = 1 << 0,
    Summon = 1 << 1,
    Illusion = 1 << 2,
    Boss = 1 << 3,
    Front = 1 << 4,
    Middle = 1 << 5,
    Back = 1 << 6,
    Tank = 1 << 7,
    Warrior = 1 << 8,
    Assassin = 1 << 9,
    Mage = 1 << 10,
    Support = 1 << 11,
}

public enum BattleCommandType
{
    None = 0,
    CastUltimate = 1,
}

public enum BattleCombatEventType
{
    None = 0,
    BuffApplied = 1,
    BuffExpired = 2,
    BuffDispelled = 3,
    DamageDealt = 4,
    DamageTaken = 5,
    Healed = 6,
    UnitDied = 7,
}

public enum BattleEventType
{
    BattleStarted = 0,
    WaveStarted = 1,
    WaveEnded = 2,
    UnitSpawned = 3,
    UnitMoved = 4,
    UnitAttack = 5,
    UnitDamaged = 6,
    UnitDied = 7,
    UltimateCast = 8,
    UltimateRejected = 9,
    BattleEnded = 10,
    BuffApplied = 11,
    BuffExpired = 12,
    UltimateHit = 13,
    UltimateRecovered = 14,
    UnitHealed = 15,
    BuffTriggered = 16,
    BuffDispelled = 17,
    ProjectileSpawned = 18,
    ProjectileMoved = 19,
    ProjectileHit = 20,
    UnitRevived = 21,
    UnitEnergyChanged = 22,
}

