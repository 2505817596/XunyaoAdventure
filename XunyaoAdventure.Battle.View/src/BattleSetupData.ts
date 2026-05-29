export enum BattlePrimaryAttribute {
  Strength = 0,
  Intelligence = 1,
  Agility = 2,
}

export type BattleUnitSetupRow = {
  maxHp: number;
  moveSpeed: number;
  attackRange: number;
  attackInterval: number;
  physicalAttack: number;
  ultimateDamage: number;
  ultimateRadius: number;
  ultimateEnergyCost: number;
  energyRegenPerSecond: number;
  ultimateCooldown: number;
  radius: number;
  aggroRange: number;
  controlMode: number;
  skillProfileId: number;
  profileName: string;
  roleName: string;
  primaryAttribute: BattlePrimaryAttribute;
  level: number;
  magicPower: number;
  physicalArmor: number;
  magicResist: number;
  physicalCrit: number;
  hpRegenPerSecond: number;
  interruptThreshold: number;
  xRaw: number;
  yRaw: number;
};

export type BattleUnitDefinitionRow = Omit<BattleUnitSetupRow, "xRaw" | "yRaw">;

export type BattleWaveSetupRow = {
  units: BattleUnitSetupRow[];
};

export const BattleSkillProfileNames: Record<number, string> = {
  0: "Default",
  1: "NoStun",
  2: "LongStun",
  3: "PhysicalArmorUp",
  4: "Shield",
  5: "Thorns",
  6: "LifeSteal",
  7: "Dispel",
  8: "Rage",
  9: "SecondWind",
  10: "DeathBurst",
  11: "SlowMage",
  12: "TripleStrike",
  13: "EnergySupport",
  14: "TauntTank",
  15: "PullMage",
  16: "RevivePriest",
  17: "Summoner",
  18: "DeathSummon",
  19: "BurningMage",
  20: "LastStandTank",
  21: "BattleStartCleric",
  22: "KillFighter",
  23: "DamageTakenGuard",
  24: "DamageDealtStriker",
  25: "AllyDeathGuard",
  26: "EnemyDeathCleric",
  27: "SkillStartSage",
  28: "SkillFinishSage",
  29: "HealReceivedPriest",
  30: "HealGrantedCleric",
  31: "SkillHitArcanist",
  32: "AuraBanner",
  33: "WarDrumAura",
  34: "FrostCurseAura",
  35: "GuardianAura",
  36: "LinePiercer",
  37: "FanBreaker",
  38: "ChainLightningMage",
  39: "BacklineAssassin",
  40: "MidlineSilencer",
  41: "FrontlineProtector",
  42: "MultiShotArcher",
  43: "ScatterVolley",
  44: "ArcaneExecutioner",
};
