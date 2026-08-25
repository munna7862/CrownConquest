using System;
using CrownConquest.Domain.Shipping;

namespace CrownConquest.Presentation;

/// <summary>
/// Playable & headless scenario orchestrating the end-to-end release pipeline,
/// clean-machine verification, performance certification, smoke automation,
/// checksum validation, and UI presentation formatting for Sprint 15.
/// </summary>
public sealed class ReleaseCandidateScenario
{
    private readonly ReleaseCandidatePresenter _presenter = new();

    public ReleaseCertificationReport? LatestReport { get; private set; }
    public ReleaseCandidateSummaryViewModel? LatestSummaryVm { get; private set; }

    public bool RunReleaseCertification(string version = "1.0.0", string platform = "win-x64")
    {
        // 1. Execute release pipeline
        LatestReport = ReleasePipelineEngine.ExecuteReleasePipeline(version, platform);

        // 2. Format presentation view model
        LatestSummaryVm = _presenter.PresentCertification(LatestReport);

        // 3. Return overall readiness
        return LatestReport.IsApprovedForRelease;
    }
}
