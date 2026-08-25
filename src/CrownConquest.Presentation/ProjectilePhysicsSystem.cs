using System;
using CrownConquest.Domain.Common;

namespace CrownConquest.Presentation;

/// <summary>
/// Types of ballistic projectiles.
/// </summary>
public enum ProjectileType
{
    Arrow,
    CatapultBoulder,
    BallistaBolt,
    Javelin
}

/// <summary>
/// Active projectile in flight with ballistic trajectory parameters.
/// </summary>
public struct ActiveProjectile
{
    public uint Id;
    public ProjectileType Type;
    public Vector2D Origin;
    public Vector2D Target;
    public float ApexHeight;
    public int TotalTicks;
    public int CurrentTick;
    public bool IsActive;
    public bool IsImpacted;

    public readonly float Progress => TotalTicks > 0 ? Math.Clamp((float)CurrentTick / TotalTicks, 0f, 1f) : 1f;

    public readonly Vector2D GroundPosition
    {
        get
        {
            float t = Progress;
            return new Vector2D(
                Origin.X + ((Target.X - Origin.X) * t),
                Origin.Y + ((Target.Y - Origin.Y) * t));
        }
    }

    public readonly float ArcHeight
    {
        get
        {
            float t = Progress;
            return 4.0f * ApexHeight * t * (1.0f - t);
        }
    }

    public readonly Vector2D VisualPosition
    {
        get
        {
            var ground = GroundPosition;
            return new Vector2D(ground.X, ground.Y - ArcHeight);
        }
    }

    public readonly float ShadowScale
    {
        get
        {
            float heightRatio = ApexHeight > 0f ? Math.Clamp(ArcHeight / ApexHeight, 0f, 1f) : 0f;
            return 1.0f - (heightRatio * 0.4f); // Shrinks by up to 40% at peak height
        }
    }

    public readonly float ShadowAlpha
    {
        get
        {
            float heightRatio = ApexHeight > 0f ? Math.Clamp(ArcHeight / ApexHeight, 0f, 1f) : 0f;
            return 0.7f - (heightRatio * 0.35f); // Softens at peak height
        }
    }

    public readonly float RotationAngle
    {
        get
        {
            float dx = Target.X - Origin.X;
            float dy = Target.Y - Origin.Y;
            float t = Progress;
            // Derivative of Z(t) = 4 * H * (1 - 2t)
            float dzDt = 4.0f * ApexHeight * (1.0f - (2.0f * t));
            float visualDy = dy - dzDt;
            return MathF.Atan2(visualDy, dx);
        }
    }
}

/// <summary>
/// Simulates 2.5D ballistic projectile flight physics with parabolic trajectory arcs,
/// ground shadow scaling, and impact detection.
/// </summary>
public sealed class ProjectilePhysicsSystem
{
    private readonly ActiveProjectile[] _projectiles;
    private readonly int _capacity;
    private uint _nextId;
    private int _activeCount;

    public int ActiveCount => _activeCount;

    public ProjectilePhysicsSystem(int capacity = 256)
    {
        _capacity = capacity;
        _projectiles = new ActiveProjectile[capacity];
        _nextId = 1;
        _activeCount = 0;
    }

    /// <summary>
    /// Spawns a projectile with ballistic trajectory.
    /// </summary>
    public uint SpawnProjectile(
        ProjectileType type,
        Vector2D origin,
        Vector2D target,
        int flightTicks,
        float apexHeight = 60f)
    {
        for (int i = 0; i < _capacity; i++)
        {
            if (!_projectiles[i].IsActive)
            {
                uint id = _nextId++;
                _projectiles[i] = new ActiveProjectile
                {
                    Id = id,
                    Type = type,
                    Origin = origin,
                    Target = target,
                    ApexHeight = apexHeight,
                    TotalTicks = Math.Max(1, flightTicks),
                    CurrentTick = 0,
                    IsActive = true,
                    IsImpacted = false
                };
                _activeCount++;
                return id;
            }
        }

        return 0; // Pool exhausted
    }

    /// <summary>
    /// Advances physics simulation by one tick.
    /// </summary>
    /// <param name="onImpact">Optional callback triggered when a projectile impacts target.</param>
    public void Tick(Action<ActiveProjectile>? onImpact = null)
    {
        for (int i = 0; i < _capacity; i++)
        {
            if (!_projectiles[i].IsActive) continue;

            _projectiles[i].CurrentTick++;

            if (_projectiles[i].CurrentTick >= _projectiles[i].TotalTicks)
            {
                _projectiles[i].IsImpacted = true;
                _projectiles[i].IsActive = false;
                _activeCount--;
                onImpact?.Invoke(_projectiles[i]);
            }
        }
    }

    public bool TryGetProjectile(int index, out ActiveProjectile projectile)
    {
        int activeSeen = 0;
        for (int i = 0; i < _capacity; i++)
        {
            if (_projectiles[i].IsActive)
            {
                if (activeSeen == index)
                {
                    projectile = _projectiles[i];
                    return true;
                }
                activeSeen++;
            }
        }

        projectile = default;
        return false;
    }

    public void Clear()
    {
        for (int i = 0; i < _capacity; i++)
        {
            _projectiles[i].IsActive = false;
        }
        _activeCount = 0;
    }
}
