using LeanClr.Mathematics;

namespace LeanClr.Battle;

internal sealed class BattleProjectile
{
    public BattleProjectile(
        int id,
        int sourceUnitId,
        int targetUnitId,
        BattleTeam team,
        FixVec2 position,
        FixVec2 destination,
        Fix32 speed,
        BattleProjectileMotionType motionType,
        BattleProjectileImpactType impactType,
        BattleSkillHit hit)
    {
        Id = id;
        SourceUnitId = sourceUnitId;
        TargetUnitId = targetUnitId;
        Team = team;
        Position = position;
        Destination = destination;
        Speed = speed;
        MotionType = motionType;
        ImpactType = impactType;
        Hit = hit;
    }

    public int Id { get; }
    public int SourceUnitId { get; }
    public int TargetUnitId { get; }
    public BattleTeam Team { get; }
    public FixVec2 Position { get; private set; }
    public FixVec2 Destination { get; private set; }
    public Fix32 Speed { get; }
    public BattleProjectileMotionType MotionType { get; }
    public BattleProjectileImpactType ImpactType { get; }
    public BattleSkillHit Hit { get; }

    public void SetDestination(FixVec2 destination)
    {
        Destination = destination;
    }

    public bool Move(Fix32 deltaTime)
    {
        FixVec2 delta = Destination - Position;
        Fix32 distance = delta.Magnitude;
        if (distance <= FixMath.Epsilon)
        {
            Position = Destination;
            return true;
        }

        Fix32 step = Speed * deltaTime;
        if (step >= distance)
        {
            Position = Destination;
            return true;
        }

        Position += delta / distance * step;
        return false;
    }
}
