using System;
using System.Collections.Generic;
using CrownConquest.Domain.Common;

namespace CrownConquest.Domain.AI;

public enum AiBuildStepType
{
    ConstructBuilding,
    TrainUnits,
    ResearchTech
}

public sealed class AiBuildStep
{
    public AiBuildStepType StepType { get; }
    public string TargetIdentifier { get; }
    public int TargetCount { get; }
    public bool IsCompleted { get; internal set; }

    public AiBuildStep(AiBuildStepType stepType, string targetIdentifier, int targetCount = 1)
    {
        StepType = stepType;
        TargetIdentifier = targetIdentifier;
        TargetCount = targetCount;
        IsCompleted = false;
    }
}

/// <summary>
/// Authoritative AI build order sequence guiding economic foundation and military expansion.
/// </summary>
public sealed class AiBuildOrderPlan
{
    private readonly List<AiBuildStep> _steps = new(32);
    private int _currentStepIndex;

    public IReadOnlyList<AiBuildStep> Steps => _steps;
    public int CurrentStepIndex => _currentStepIndex;
    public bool IsPlanFinished => _currentStepIndex >= _steps.Count;

    public AiBuildStep? CurrentStep => _currentStepIndex < _steps.Count ? _steps[_currentStepIndex] : null;

    public void AddStep(AiBuildStep step)
    {
        ArgumentNullException.ThrowIfNull(step);
        _steps.Add(step);
    }

    public void AdvanceStep()
    {
        if (_currentStepIndex < _steps.Count)
        {
            _steps[_currentStepIndex].IsCompleted = true;
            _currentStepIndex++;
        }
    }

    public void Reset()
    {
        _currentStepIndex = 0;
        for (int i = 0; i < _steps.Count; i++)
        {
            _steps[i].IsCompleted = false;
        }
    }

    /// <summary>
    /// Creates the standard balanced RTS build order template.
    /// </summary>
    public static AiBuildOrderPlan CreateStandardPlan()
    {
        var plan = new AiBuildOrderPlan();
        plan.AddStep(new AiBuildStep(AiBuildStepType.TrainUnits, "worker", 3));
        plan.AddStep(new AiBuildStep(AiBuildStepType.ConstructBuilding, "farm", 1));
        plan.AddStep(new AiBuildStep(AiBuildStepType.ConstructBuilding, "house", 1));
        plan.AddStep(new AiBuildStep(AiBuildStepType.ConstructBuilding, "barracks", 1));
        plan.AddStep(new AiBuildStep(AiBuildStepType.TrainUnits, "spearman", 3));
        plan.AddStep(new AiBuildStep(AiBuildStepType.ConstructBuilding, "house", 1));
        plan.AddStep(new AiBuildStep(AiBuildStepType.ConstructBuilding, "archery_range", 1));
        plan.AddStep(new AiBuildStep(AiBuildStepType.TrainUnits, "archer", 3));
        plan.AddStep(new AiBuildStep(AiBuildStepType.ConstructBuilding, "stable", 1));
        plan.AddStep(new AiBuildStep(AiBuildStepType.TrainUnits, "cavalry", 2));
        plan.AddStep(new AiBuildStep(AiBuildStepType.ConstructBuilding, "siege_workshop", 1));
        plan.AddStep(new AiBuildStep(AiBuildStepType.TrainUnits, "battering_ram", 1));
        plan.AddStep(new AiBuildStep(AiBuildStepType.TrainUnits, "catapult", 1));
        return plan;
    }
}
