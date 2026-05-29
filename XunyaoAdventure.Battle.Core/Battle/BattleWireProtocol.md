# Battle Wire Protocol

All fields are little-endian `int32`.

`Fix32` values are written as raw fixed-point values.

Battle wire version: `10`.

## Command Batch

Header size: 32 bytes.

| Offset | Field |
| --- | --- |
| 0 | magic `0x314C5442` |
| 4 | version |
| 8 | frameNo |
| 12 | deltaRaw |
| 16 | commandCount |
| 20 | totalByteSize |
| 24 | reserved |
| 28 | reserved |

Command record size: 12 bytes.

| Offset | Field |
| --- | --- |
| 0 | unitId |
| 4 | commandType |
| 8 | targetUnitId |

## Event Batch

Header size: 48 bytes.

| Offset | Field |
| --- | --- |
| 0 | magic `0x314C5442` |
| 4 | version |
| 8 | frameNo |
| 12 | battleEnded |
| 16 | winner |
| 20 | currentWaveIndex |
| 24 | eventCount |
| 28 | totalByteSize |
| 32 | alivePlayerCount |
| 36 | aliveEnemyCount |
| 40 | stateHash |
| 44 | unitStateCount |

Event record size: 40 bytes.

| Offset | Field |
| --- | --- |
| 0 | eventType |
| 4 | unitId |
| 8 | targetUnitId |
| 12 | team |
| 16 | waveIndex |
| 20 | amountRaw |
| 24 | remainingHpRaw |
| 28 | xRaw |
| 32 | yRaw |
| 36 | buffType |

Unit state record size: 24 bytes.

| Offset | Field |
| --- | --- |
| 0 | unitId |
| 4 | team |
| 8 | hpRaw |
| 12 | maxHpRaw |
| 16 | energyRaw |
| 20 | isAlive |

Known event ids:

- `20`: `ProjectileHit`
- `21`: `UnitRevived`
- `22`: `UnitEnergyChanged`

`UnitSpawned.amountRaw` carries the spawned unit's skill profile id for runtime summons.

Skill tags such as `Damage`, `Control`, `Periodic`, `NoLifeSteal`, and `NoReflection` are server-side config semantics. They do not add fields to the runtime wire format.

Known buff ids:

- `21`: `Taunt`
- `22`: `Revive`
- `23`: `SummonLife`
- `24`: `DamageOverTime`
- `25`: `HealOverTime`
- `26`: `ControlImmune`
- `27`: `SilenceImmune`
- `28`: `DisplaceImmune`
- `29`: `Uninterruptible`
- `30`: `DamageImmune`
- `31`: `PhysicalImmune`
- `32`: `MagicalImmune`
- `33`: `PureImmune`

## Setup Batch

Setup magic: `0x31455342`.

Setup version: `4`.

Header size: 32 bytes.

| Offset | Field |
| --- | --- |
| 0 | magic |
| 4 | version |
| 8 | playerUnitCount |
| 12 | enemyWaveCount |
| 16 | totalByteSize |
| 20 | reserved |
| 24 | reserved |
| 28 | reserved |

Spawn record size: 96 bytes.

| Offset | Field |
| --- | --- |
| 0 | maxHpRaw |
| 4 | moveSpeedRaw |
| 8 | attackRangeRaw |
| 12 | attackIntervalRaw |
| 16 | physicalAttackRaw |
| 20 | ultimateDamageRaw |
| 24 | ultimateRadiusRaw |
| 28 | ultimateEnergyCostRaw |
| 32 | energyRegenPerSecondRaw |
| 36 | ultimateCooldownRaw |
| 40 | radiusRaw |
| 44 | aggroRangeRaw |
| 48 | controlMode |
| 52 | xRaw |
| 56 | yRaw |
| 60 | skillProfileId |
| 64 | primaryAttribute |
| 68 | level |
| 72 | magicPowerRaw |
| 76 | physicalArmorRaw |
| 80 | magicResistRaw |
| 84 | physicalCritRaw |
| 88 | hpRegenPerSecondRaw |
| 92 | interruptThresholdRaw |

Known primary attribute ids:

- `0`: `Strength`
- `1`: `Intelligence`
- `2`: `Agility`

Setup body layout:

1. `playerUnitCount` spawn records.
2. For each enemy wave: one `int32` unit count, then that many spawn records.
