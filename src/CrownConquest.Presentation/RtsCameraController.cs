using System;
using CrownConquest.Domain.Common;
using CrownConquest.Domain.Simulation;

namespace CrownConquest.Presentation;

/// <summary>
/// Controls RTS viewport panning, zooming, coordinate projections, and map bounds clamping.
/// Decoupled from Godot node APIs so it can run headlessly and in unit tests.
/// </summary>
public sealed class RtsCameraController
{
    private Vector2D _position;
    private float _zoom;
    private readonly BattlefieldBounds _bounds;

    public float PanSpeed { get; set; } = 30.0f;
    public float MinZoom { get; set; } = 0.5f;
    public float MaxZoom { get; set; } = 3.0f;
    public float ZoomStep { get; set; } = 0.2f;

    public Vector2D Position => _position;
    public float Zoom => _zoom;
    public BattlefieldBounds Bounds => _bounds;

    public RtsCameraController(
        Vector2D? initialPosition = null,
        float initialZoom = 1.0f,
        BattlefieldBounds? bounds = null)
    {
        _bounds = bounds ?? BattlefieldBounds.Default;
        _position = initialPosition ?? new Vector2D(_bounds.Width * 0.5f, _bounds.Height * 0.5f);
        _zoom = Math.Clamp(initialZoom, MinZoom, MaxZoom);
    }

    public void Pan(Vector2D direction, float deltaTime)
    {
        if (direction.LengthSquared > 0f)
        {
            var normalized = direction.Normalized();
            var movement = normalized * (PanSpeed * deltaTime / _zoom);
            _position = _bounds.Clamp(_position + movement);
        }
    }

    public void AdjustZoom(float zoomDelta)
    {
        _zoom = Math.Clamp(_zoom + zoomDelta, MinZoom, MaxZoom);
    }

    public void ZoomIn() => AdjustZoom(ZoomStep);
    public void ZoomOut() => AdjustZoom(-ZoomStep);

    public void SetPosition(Vector2D targetPos)
    {
        _position = _bounds.Clamp(targetPos);
    }

    /// <summary>
    /// Converts screen viewport pixel coordinates to world coordinates.
    /// </summary>
    public Vector2D ScreenToWorld(Vector2D screenPos, Vector2D viewportSize)
    {
        var screenCenter = viewportSize * 0.5f;
        var offsetFromCenter = screenPos - screenCenter;
        var worldOffset = offsetFromCenter / _zoom;
        return _position + worldOffset;
    }

    /// <summary>
    /// Converts world coordinates to screen viewport pixel coordinates.
    /// </summary>
    public Vector2D WorldToScreen(Vector2D worldPos, Vector2D viewportSize)
    {
        var screenCenter = viewportSize * 0.5f;
        var worldOffset = worldPos - _position;
        var screenOffset = worldOffset * _zoom;
        return screenCenter + screenOffset;
    }
}
