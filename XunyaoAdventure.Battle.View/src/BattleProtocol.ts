export const BattleWireMagic = 0x314c5442;
export const BattleWireVersion = 10;
export const CommandHeaderSize = 32;
export const CommandRecordSize = 12;
export const EventHeaderSize = 48;
export const EventRecordSize = 40;
export const UnitStateRecordSize = 24;

export enum BattleTeam {
  Neutral = 0,
  TeamA = 1,
  TeamB = 2,
}

export enum BattleCommandType {
  None = 0,
  CastUltimate = 1,
}

export enum BattleEventType {
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

export enum BattleBuffType {
  None = 0,
  Stun = 1,
  PhysicalArmorUp = 2,
  Shield = 3,
  Thorns = 4,
  LifeSteal = 5,
  Rage = 6,
  SecondWind = 7,
  DeathBurst = 8,
  Slow = 9,
  Silence = 10,
  PhysicalAttackUp = 11,
  AttackSpeedUp = 12,
  EnergyRegenUp = 13,
  MagicResistUp = 15,
  PhysicalAttackDown = 16,
  AttackSpeedDown = 17,
  EnergyRegenDown = 18,
  PhysicalArmorDown = 19,
  MagicResistDown = 20,
  Taunt = 21,
  Revive = 22,
  SummonLife = 23,
  DamageOverTime = 24,
  HealOverTime = 25,
  ControlImmune = 26,
  SilenceImmune = 27,
  DisplaceImmune = 28,
  Uninterruptible = 29,
  DamageImmune = 30,
  PhysicalImmune = 31,
  MagicalImmune = 32,
  PureImmune = 33,
  HealingAmplify = 34,
  HealingReduce = 35,
  DamageTakenAmplify = 36,
  DamageTakenReduce = 37,
  PhysicalDamageTakenAmplify = 38,
  PhysicalDamageTakenReduce = 39,
  MagicalDamageTakenAmplify = 40,
  MagicalDamageTakenReduce = 41,
  PureDamageTakenAmplify = 42,
  PureDamageTakenReduce = 43,
}

export type BattleCommandRecord = {
  unitId: number;
  type: BattleCommandType;
  targetUnitId?: number;
};

export type BattleEventRecord = {
  type: BattleEventType;
  unitId: number;
  targetUnitId: number;
  team: BattleTeam;
  waveIndex: number;
  amountRaw: number;
  remainingHpRaw: number;
  xRaw: number;
  yRaw: number;
  buffType: BattleBuffType;
};

export type BattleFrameOutput = {
  frameNo: number;
  battleEnded: boolean;
  winner: BattleTeam;
  currentWaveIndex: number;
  alivePlayerCount: number;
  aliveEnemyCount: number;
  stateHash: number;
  events: BattleEventRecord[];
  unitStates: BattleUnitFrameState[];
};

export type BattleUnitFrameState = {
  unitId: number;
  team: BattleTeam;
  hpRaw: number;
  maxHpRaw: number;
  energyRaw: number;
  isAlive: boolean;
};

export function encodeCommandBatch(
  frameNo: number,
  deltaRaw: number,
  commands: BattleCommandRecord[],
) {
  const buffer = new ArrayBuffer(
    CommandHeaderSize + commands.length * CommandRecordSize,
  );
  const view = new DataView(buffer);
  let offset = 0;

  offset = writeUint32(view, offset, BattleWireMagic);
  offset = writeInt32(view, offset, BattleWireVersion);
  offset = writeInt32(view, offset, frameNo);
  offset = writeInt32(view, offset, deltaRaw);
  offset = writeInt32(view, offset, commands.length);
  offset = writeInt32(view, offset, buffer.byteLength);
  offset = writeInt32(view, offset, 0);
  offset = writeInt32(view, offset, 0);

  for (const command of commands) {
    offset = writeInt32(view, offset, command.unitId);
    offset = writeInt32(view, offset, command.type);
    offset = writeInt32(view, offset, command.targetUnitId ?? 0);
  }

  return new Uint8Array(buffer);
}

export function decodeEventBatch(bytes: Uint8Array): BattleFrameOutput {
  const view = new DataView(bytes.buffer, bytes.byteOffset, bytes.byteLength);
  let offset = 0;

  const magic = readUint32(view, offset);
  offset += 4;
  if (magic !== BattleWireMagic) {
    throw new Error(`Invalid battle event magic: ${magic}`);
  }

  const version = readInt32(view, offset);
  offset += 4;
  if (version !== BattleWireVersion) {
    throw new Error(`Unsupported battle event version: ${version}`);
  }

  const frameNo = readInt32(view, offset);
  offset += 4;
  const battleEnded = readInt32(view, offset) !== 0;
  offset += 4;
  const winner = readInt32(view, offset) as BattleTeam;
  offset += 4;
  const currentWaveIndex = readInt32(view, offset);
  offset += 4;
  const eventCount = readInt32(view, offset);
  offset += 4;
  const declaredSize = readInt32(view, offset);
  offset += 4;
  const alivePlayerCount = readInt32(view, offset);
  offset += 4;
  const aliveEnemyCount = readInt32(view, offset);
  offset += 4;
  const stateHash = readInt32(view, offset);
  offset += 4;
  const unitStateCount = readInt32(view, offset);
  offset += 4;

  if (declaredSize !== bytes.byteLength) {
    throw new Error(
      `Battle event size mismatch: declared=${declaredSize} actual=${bytes.byteLength}`,
    );
  }

  const events: BattleEventRecord[] = [];
  for (let i = 0; i < eventCount; i++) {
    events.push({
      type: readInt32(view, offset) as BattleEventType,
      unitId: readInt32(view, offset + 4),
      targetUnitId: readInt32(view, offset + 8),
      team: readInt32(view, offset + 12) as BattleTeam,
      waveIndex: readInt32(view, offset + 16),
      amountRaw: readInt32(view, offset + 20),
      remainingHpRaw: readInt32(view, offset + 24),
      xRaw: readInt32(view, offset + 28),
      yRaw: readInt32(view, offset + 32),
      buffType: readInt32(view, offset + 36) as BattleBuffType,
    });
    offset += EventRecordSize;
  }

  const unitStates: BattleUnitFrameState[] = [];
  for (let i = 0; i < unitStateCount; i++) {
    unitStates.push({
      unitId: readInt32(view, offset),
      team: readInt32(view, offset + 4) as BattleTeam,
      hpRaw: readInt32(view, offset + 8),
      maxHpRaw: readInt32(view, offset + 12),
      energyRaw: readInt32(view, offset + 16),
      isAlive: readInt32(view, offset + 20) !== 0,
    });
    offset += UnitStateRecordSize;
  }

  return {
    frameNo,
    battleEnded,
    winner,
    currentWaveIndex,
    alivePlayerCount,
    aliveEnemyCount,
    stateHash,
    events,
    unitStates,
  };
}

export function fixRaw(value: number) {
  return Math.round(value * 65536);
}

export function fixToNumber(raw: number) {
  return raw / 65536;
}

function writeInt32(view: DataView, offset: number, value: number) {
  view.setInt32(offset, value, true);
  return offset + 4;
}

function writeUint32(view: DataView, offset: number, value: number) {
  view.setUint32(offset, value, true);
  return offset + 4;
}

function readInt32(view: DataView, offset: number) {
  return view.getInt32(offset, true);
}

function readUint32(view: DataView, offset: number) {
  return view.getUint32(offset, true);
}
