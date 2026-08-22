using System.Text.Json;
using CrownConquest.Data.Models;
using CrownConquest.Domain.Common;

namespace CrownConquest.Data.Loaders;

/// <summary>
/// Loads and validates data-driven game definitions from JSON.
/// </summary>
public static class DataLoader
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    public static Result<List<UnitDefinition>> LoadUnitsFromJson(string json)
    {
        try
        {
            var units = JsonSerializer.Deserialize<List<UnitDefinition>>(json, JsonOptions);
            if (units == null || units.Count == 0)
            {
                return Result<List<UnitDefinition>>.Failure(new GameError("EMPTY_DATA", "Units data definition is empty."));
            }

            foreach (var unit in units)
            {
                if (string.IsNullOrWhiteSpace(unit.Id))
                {
                    return Result<List<UnitDefinition>>.Failure(new GameError("INVALID_UNIT_ID", "Unit ID cannot be empty."));
                }
                if (unit.MaxHealth <= 0 || unit.AttackDamage < 0)
                {
                    return Result<List<UnitDefinition>>.Failure(new GameError("INVALID_STATS", $"Invalid stats for unit {unit.Id}."));
                }
            }

            return Result<List<UnitDefinition>>.Success(units);
        }
        catch (Exception ex)
        {
            return Result<List<UnitDefinition>>.Failure(new GameError("JSON_PARSE_ERROR", ex.Message));
        }
    }

    public static Result<List<ProgressionCurveDefinition>> LoadProgressionCurvesFromJson(string json)
    {
        try
        {
            var curves = JsonSerializer.Deserialize<List<ProgressionCurveDefinition>>(json, JsonOptions);
            if (curves == null || curves.Count == 0)
            {
                return Result<List<ProgressionCurveDefinition>>.Failure(new GameError("EMPTY_DATA", "XP curves definition is empty."));
            }

            foreach (var curve in curves)
            {
                if (curve.LevelXpThresholds == null || curve.LevelXpThresholds.Length == 0)
                {
                    return Result<List<ProgressionCurveDefinition>>.Failure(new GameError("INVALID_CURVE", $"Curve {curve.Id} has no thresholds."));
                }

                // Verify monotonic increasing thresholds
                for (int i = 1; i < curve.LevelXpThresholds.Length; i++)
                {
                    if (curve.LevelXpThresholds[i] <= curve.LevelXpThresholds[i - 1])
                    {
                        return Result<List<ProgressionCurveDefinition>>.Failure(
                            new GameError("NON_MONOTONIC_THRESHOLDS", $"Curve {curve.Id} thresholds are not strictly increasing."));
                    }
                }
            }

            return Result<List<ProgressionCurveDefinition>>.Success(curves);
        }
        catch (Exception ex)
        {
            return Result<List<ProgressionCurveDefinition>>.Failure(new GameError("JSON_PARSE_ERROR", ex.Message));
        }
    }

    public static Result<List<UnitDefinition>> LoadUnitsFromFile(string filePath)
    {
        if (!File.Exists(filePath))
        {
            return Result<List<UnitDefinition>>.Failure(new GameError("FILE_NOT_FOUND", $"Definition file not found: {filePath}"));
        }
        string json = File.ReadAllText(filePath);
        return LoadUnitsFromJson(json);
    }

    public static Result<List<ProgressionCurveDefinition>> LoadProgressionCurvesFromFile(string filePath)
    {
        if (!File.Exists(filePath))
        {
            return Result<List<ProgressionCurveDefinition>>.Failure(new GameError("FILE_NOT_FOUND", $"Definition file not found: {filePath}"));
        }
        string json = File.ReadAllText(filePath);
        return LoadProgressionCurvesFromJson(json);
    }
}
