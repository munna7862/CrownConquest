using CrownConquest.Domain.Entities;

namespace CrownConquest.Domain.World;

/// <summary>
/// Authoritative mutable runtime state tracking an accepted or ongoing campaign mission.
/// </summary>
public sealed class MissionRuntimeState
{
    public MissionDefinition Definition { get; }
    public string MissionId => Definition.Id;
    public MissionType Type => Definition.Type;

    public MissionStatus Status { get; set; } = MissionStatus.Inactive;
    public int StartTick { get; set; }
    public int ElapsedTicks { get; set; }
    public int CurrentProgress { get; set; }
    public int TargetQuantity => Definition.TargetQuantity;
    public int CompletedTick { get; set; }
    public int FailedTick { get; set; }
    public string? FailureReason { get; set; }

    public StrategicArmyId? AssignedArmyId { get; set; }
    public int? AssignedHeroEntityId { get; set; }

    public bool IsActive => Status == MissionStatus.Active;
    public bool IsCompleted => Status == MissionStatus.Completed;
    public bool IsFailed => Status == MissionStatus.Failed;
    public bool IsExpired => Status == MissionStatus.Expired;
    public bool IsTerminal => IsCompleted || IsFailed || IsExpired;

    public float ProgressFraction
    {
        get
        {
            if (TargetQuantity <= 0) return 0f;
            return System.Math.Clamp((float)CurrentProgress / TargetQuantity, 0f, 1f);
        }
    }

    public MissionRuntimeState(MissionDefinition definition)
    {
        Definition = definition;
    }

    public void Start(int currentTick, StrategicArmyId? armyId = null, int? heroEntityId = null)
    {
        Status = MissionStatus.Active;
        StartTick = currentTick;
        ElapsedTicks = 0;
        CurrentProgress = 0;
        AssignedArmyId = armyId;
        AssignedHeroEntityId = heroEntityId;
    }

    public void Complete(int currentTick)
    {
        Status = MissionStatus.Completed;
        CompletedTick = currentTick;
        CurrentProgress = TargetQuantity;
    }

    public void Fail(string reason, int currentTick)
    {
        Status = MissionStatus.Failed;
        FailureReason = reason;
        FailedTick = currentTick;
    }

    public void Expire(int currentTick)
    {
        Status = MissionStatus.Expired;
        FailureReason = "Mission time limit expired.";
        FailedTick = currentTick;
    }
}
