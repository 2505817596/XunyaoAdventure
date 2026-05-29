using LeanClr.Mathematics;

namespace LeanClr.Battle;

public sealed partial class BattleWorld
{
    private void SpawnPlayerUnits()
    {
        _playerUnitIds.Clear();
        _playerHomePositions.Clear();
        SpawnUnits(BattleTeam.TeamA, _setup.PlayerUnits, _playerUnitIds);
    }

    internal void StartEnemyWave(int waveIndex)
    {
        _enemyUnitIds.Clear();
        SpawnUnits(BattleTeam.TeamB, _setup.EnemyWaves[waveIndex].Units, _enemyUnitIds);
    }

    private void SpawnUnits(BattleTeam team, IReadOnlyList<BattleUnitSpawn> spawns, List<int> collector)
    {
        for (int i = 0; i < spawns.Count; i++)
        {
            BattleUnitSpawn spawn = spawns[i];
            int id = _nextUnitId++;
            BattleUnit unit = new(
                id,
                team,
                new BattleUnitSpec(
                    spawn.Spec.MaxHp,
                    spawn.Spec.MoveSpeed,
                    spawn.Spec.AttackRange,
                    spawn.Spec.AttackInterval,
                    spawn.Spec.PhysicalAttack,
                    spawn.Spec.UltimateDamage,
                    spawn.Spec.UltimateRadius,
                    spawn.Spec.UltimateEnergyCost,
                    spawn.Spec.EnergyRegenPerSecond,
                    spawn.Spec.UltimateCooldown,
                    spawn.Spec.Radius,
                    spawn.Spec.AggroRange,
                    spawn.Spec.ControlMode,
                    spawn.Spec.UnitTags,
                    spawn.Spec.PrimaryAttribute,
                    spawn.Spec.Level,
                    spawn.Spec.MagicPower,
                    spawn.Spec.PhysicalArmor,
                    spawn.Spec.MagicResist,
                    spawn.Spec.PhysicalCrit,
                    spawn.Spec.HpRegenPerSecond,
                    spawn.Spec.InterruptThreshold),
                spawn.SkillProfileId,
                spawn.Position);
            unit.SetFacing(team == BattleTeam.TeamB ? FixVec2.Left : FixVec2.Right);

            _units.Add(id, unit);
            _unitOrder.Add(id);
            collector.Add(id);
            if (team == BattleTeam.TeamA)
            {
                _playerHomePositions.Add(id, spawn.Position);
            }

            _events.Add(BattleEvent.UnitSpawned(id, spawn.Position, unit.Hp, team));
        }
    }

    public void StartBattle()
    {
        if (_battleStarted)
        {
            return;
        }

        _battleStarted = true;
        _events.Clear();
        _events.Add(BattleEvent.BattleStarted());

        SpawnPlayerUnits();
        _waveController.StartFirstWave();
        TriggerBattleStartPassives();
    }

    public bool TryGetUnit(int unitId, out BattleUnit unit) => _units.TryGetValue(unitId, out unit);

    public bool IssueCommand(int unitId, BattleCommand command)
    {
        if (!_battleStarted || _battleEnded)
        {
            return false;
        }

        if (!_units.TryGetValue(unitId, out BattleUnit unit) || !unit.IsAlive)
        {
            return false;
        }

        if (unit.Team != BattleTeam.TeamA)
        {
            return false;
        }

        if (command.Type != BattleCommandType.CastUltimate)
        {
            return false;
        }

        if (!unit.CanCastUltimate())
        {
            _events.Add(BattleEvent.UltimateRejected(unitId, command.TargetUnitId, unit.Team));
            return false;
        }

        BattleUnit? target = ResolveSkillTarget(unit, command.TargetUnitId);
        if (target == null)
        {
            _events.Add(BattleEvent.UltimateRejected(unitId, command.TargetUnitId, unit.Team));
            return false;
        }

        _skillCastController.BeginUltimate(unit, target);
        return true;
    }

    public void Tick(Fix32 deltaTime)
    {
        if (_battleEnded || !_battleStarted)
        {
            return;
        }

        TickCount++;

        if (_waveController.IsBetweenWaves)
        {
            _waveController.ProcessBetweenWaves(deltaTime);
            return;
        }

        if (!_waveController.IsWaveActive)
        {
            return;
        }

        TickProjectiles(deltaTime);
        FlushPendingTriggeredEffects();
        _statusTickController.TickAuraPassives(deltaTime);
        FlushPendingTriggeredEffects();

        for (int i = 0; i < _unitOrder.Count; i++)
        {
            BattleUnit unit = _units[_unitOrder[i]];
            if (!unit.IsAlive)
            {
                continue;
            }

            unit.ReduceCooldown(deltaTime);
            TickUnitBuffs(unit, deltaTime);
            FlushPendingTriggeredEffects();
            if (unit.IsStunned)
            {
                continue;
            }

            _skillCastController.ProcessUnit(unit, deltaTime);
        }

        _unitLifecycleController.CleanupDeadUnits();
        _unitLifecycleController.FlushPendingSummons();
        _waveController.EvaluateBattleState();
    }

    private void TickProjectiles(Fix32 deltaTime)
    {
        if (_projectiles.Count == 0)
        {
            return;
        }

        _projectilesToRemove.Clear();
        for (int i = 0; i < _projectiles.Count; i++)
        {
            BattleProjectile projectile = _projectiles[i];
            if (!_units.TryGetValue(projectile.SourceUnitId, out BattleUnit source) || !source.IsAlive)
            {
                _projectilesToRemove.Add(projectile.Id);
                continue;
            }

            bool hasLivingTarget = _units.TryGetValue(projectile.TargetUnitId, out BattleUnit target) && target.IsAlive;
            if (projectile.MotionType == BattleProjectileMotionType.Homing)
            {
                if (!hasLivingTarget)
                {
                    _projectilesToRemove.Add(projectile.Id);
                    continue;
                }

                projectile.SetDestination(target.Position);
            }

            bool arrived = projectile.Move(deltaTime);
            _events.Add(BattleEvent.ProjectileMoved(projectile.Id, projectile.TargetUnitId, projectile.Position, projectile.Team));
            if (!arrived)
            {
                continue;
            }

            _events.Add(BattleEvent.ProjectileHit(projectile.Id, projectile.TargetUnitId, projectile.Position, projectile.Team));
            if (projectile.ImpactType == BattleProjectileImpactType.AreaAtDestination)
            {
                _skillCastController.ExecuteResolvedSkillHitAtPosition(source, projectile.Position, projectile.Hit);
            }
            else if (hasLivingTarget)
            {
                _skillCastController.ExecuteResolvedSkillHit(source, target, projectile.Hit);
            }

            _projectilesToRemove.Add(projectile.Id);
            FlushPendingTriggeredEffects();
        }

        if (_projectilesToRemove.Count == 0)
        {
            return;
        }

        for (int i = _projectiles.Count - 1; i >= 0; i--)
        {
            if (_projectilesToRemove.Contains(_projectiles[i].Id))
            {
                _projectiles.RemoveAt(i);
            }
        }

        _projectilesToRemove.Clear();
    }

    private void SpawnProjectile(BattleUnit source, BattleUnit target, BattleSkillHit hit)
    {
        int id = _nextProjectileId++;
        BattleProjectile projectile = new(
            id,
            source.Id,
            target.Id,
            source.Team,
            source.Position,
            target.Position,
            hit.ProjectileSpeed,
            hit.ProjectileMotionType,
            hit.ProjectileImpactType,
            hit);
        _projectiles.Add(projectile);
        _events.Add(BattleEvent.ProjectileSpawned(id, source.Id, source.Position, source.Team));
    }

    private void FlushPendingTriggeredEffects()
        => _damageResolver.FlushPendingTriggeredEffects();

    private void TriggerBattleStartPassives()
        => _passiveTriggerResolver.TriggerBattleStartPassives();
}
