using System;

namespace CrownConquest.Presentation;

// ─────────────────────────────────────────────────
// Tutorial Step System & Objective Tracker
// ─────────────────────────────────────────────────

/// <summary>
/// State of a tutorial step.
/// </summary>
public enum TutorialStepState
{
    Locked,
    Active,
    Completed,
    Skipped
}

/// <summary>
/// Describes a single tutorial step with its objective and hint.
/// </summary>
public readonly record struct TutorialStepDescriptor(
    int StepIndex,
    string Title,
    string Objective,
    string HintText,
    TutorialStepState State);

/// <summary>
/// View model for the tutorial overlay showing current progress.
/// </summary>
public readonly record struct TutorialOverlayViewModel(
    bool IsActive,
    int CurrentStepIndex,
    int TotalSteps,
    int CompletedSteps,
    float ProgressPercentage,
    string CurrentObjective,
    string CurrentHint,
    bool ShowHint,
    bool IsComplete);

/// <summary>
/// Tutorial system that manages step progression, objective tracking,
/// and hint display. Entirely presentation-layer — no simulation mutations.
/// </summary>
public sealed class TutorialPresenter
{
    private readonly string[] _stepTitles;
    private readonly string[] _stepObjectives;
    private readonly string[] _stepHints;
    private readonly TutorialStepState[] _stepStates;
    private readonly int _totalSteps;
    private int _currentStepIndex;
    private bool _isActive;
    private bool _showHint;
    private int _completedCount;

    public int TotalSteps => _totalSteps;
    public int CurrentStepIndex => _currentStepIndex;
    public int CompletedSteps => _completedCount;
    public bool IsActive => _isActive;
    public bool IsComplete => _completedCount >= _totalSteps;
    public bool ShowHint => _showHint;

    public TutorialPresenter(string[] titles, string[] objectives, string[] hints)
    {
        ArgumentNullException.ThrowIfNull(titles);
        ArgumentNullException.ThrowIfNull(objectives);
        ArgumentNullException.ThrowIfNull(hints);

        if (titles.Length != objectives.Length || titles.Length != hints.Length)
            throw new ArgumentException("Tutorial arrays must have equal length.");

        _totalSteps = titles.Length;
        _stepTitles = titles;
        _stepObjectives = objectives;
        _stepHints = hints;
        _stepStates = new TutorialStepState[_totalSteps];
        _currentStepIndex = 0;
        _isActive = false;
        _showHint = false;
        _completedCount = 0;

        // Initialize: first step active, rest locked
        if (_totalSteps > 0)
        {
            _stepStates[0] = TutorialStepState.Active;
            for (int i = 1; i < _totalSteps; i++)
            {
                _stepStates[i] = TutorialStepState.Locked;
            }
        }
    }

    /// <summary>
    /// Starts the tutorial sequence.
    /// </summary>
    public void Start()
    {
        _isActive = true;
        _currentStepIndex = 0;
        _completedCount = 0;
        if (_totalSteps > 0)
        {
            _stepStates[0] = TutorialStepState.Active;
        }
    }

    /// <summary>
    /// Completes the current active step and advances to the next.
    /// </summary>
    public bool CompleteCurrentStep()
    {
        if (!_isActive || _currentStepIndex >= _totalSteps) return false;
        if (_stepStates[_currentStepIndex] != TutorialStepState.Active) return false;

        _stepStates[_currentStepIndex] = TutorialStepState.Completed;
        _completedCount++;
        _showHint = false;

        // Advance to next step
        _currentStepIndex++;
        if (_currentStepIndex < _totalSteps)
        {
            _stepStates[_currentStepIndex] = TutorialStepState.Active;
        }
        else
        {
            _isActive = false; // Tutorial complete
        }

        return true;
    }

    /// <summary>
    /// Skips the current step.
    /// </summary>
    public bool SkipCurrentStep()
    {
        if (!_isActive || _currentStepIndex >= _totalSteps) return false;

        _stepStates[_currentStepIndex] = TutorialStepState.Skipped;
        _completedCount++;
        _showHint = false;

        _currentStepIndex++;
        if (_currentStepIndex < _totalSteps)
        {
            _stepStates[_currentStepIndex] = TutorialStepState.Active;
        }
        else
        {
            _isActive = false;
        }

        return true;
    }

    /// <summary>
    /// Toggles hint visibility for the current step.
    /// </summary>
    public void ToggleHint() => _showHint = !_showHint;

    /// <summary>
    /// Gets the step descriptor for a specific index.
    /// </summary>
    public TutorialStepDescriptor GetStep(int index)
    {
        if (index < 0 || index >= _totalSteps)
            throw new ArgumentOutOfRangeException(nameof(index));

        return new TutorialStepDescriptor(
            StepIndex: index,
            Title: _stepTitles[index],
            Objective: _stepObjectives[index],
            HintText: _stepHints[index],
            State: _stepStates[index]);
    }

    /// <summary>
    /// Gets the tutorial overlay view model for HUD display.
    /// </summary>
    public TutorialOverlayViewModel GetOverlayViewModel()
    {
        if (!_isActive || _currentStepIndex >= _totalSteps)
        {
            return new TutorialOverlayViewModel(
                IsActive: false,
                CurrentStepIndex: _currentStepIndex,
                TotalSteps: _totalSteps,
                CompletedSteps: _completedCount,
                ProgressPercentage: _totalSteps > 0 ? (float)_completedCount / _totalSteps : 1.0f,
                CurrentObjective: "",
                CurrentHint: "",
                ShowHint: false,
                IsComplete: IsComplete);
        }

        return new TutorialOverlayViewModel(
            IsActive: true,
            CurrentStepIndex: _currentStepIndex,
            TotalSteps: _totalSteps,
            CompletedSteps: _completedCount,
            ProgressPercentage: (float)_completedCount / _totalSteps,
            CurrentObjective: _stepObjectives[_currentStepIndex],
            CurrentHint: _stepHints[_currentStepIndex],
            ShowHint: _showHint,
            IsComplete: false);
    }

    /// <summary>
    /// Cancels the tutorial.
    /// </summary>
    public void Cancel()
    {
        _isActive = false;
    }
}
