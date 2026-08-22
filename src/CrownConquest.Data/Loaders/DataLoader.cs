using System;
using System.Collections.Generic;
using System.IO;
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

    public static Result<List<BuildingDefinition>> LoadBuildingsFromJson(string json)
    {
        try
        {
            var buildings = JsonSerializer.Deserialize<List<BuildingDefinition>>(json, JsonOptions);
            if (buildings == null || buildings.Count == 0)
            {
                return Result<List<BuildingDefinition>>.Failure(new GameError("EMPTY_DATA", "Buildings definition is empty."));
            }

            foreach (var b in buildings)
            {
                if (string.IsNullOrWhiteSpace(b.Id))
                {
                    return Result<List<BuildingDefinition>>.Failure(new GameError("INVALID_BUILDING_ID", "Building ID cannot be empty."));
                }
                if (b.MaxHealth <= 0 || b.GridWidth <= 0 || b.GridHeight <= 0 || b.BuildTimeTicks <= 0)
                {
                    return Result<List<BuildingDefinition>>.Failure(new GameError("INVALID_BUILDING_STATS", $"Invalid stats for building {b.Id}."));
                }
            }

            return Result<List<BuildingDefinition>>.Success(buildings);
        }
        catch (Exception ex)
        {
            return Result<List<BuildingDefinition>>.Failure(new GameError("JSON_PARSE_ERROR", ex.Message));
        }
    }

    public static Result<List<ResourceNodeDefinition>> LoadResourcesFromJson(string json)
    {
        try
        {
            var resources = JsonSerializer.Deserialize<List<ResourceNodeDefinition>>(json, JsonOptions);
            if (resources == null || resources.Count == 0)
            {
                return Result<List<ResourceNodeDefinition>>.Failure(new GameError("EMPTY_DATA", "Resources definition is empty."));
            }

            foreach (var r in resources)
            {
                if (string.IsNullOrWhiteSpace(r.Id))
                {
                    return Result<List<ResourceNodeDefinition>>.Failure(new GameError("INVALID_RESOURCE_ID", "Resource ID cannot be empty."));
                }
                if (r.MaxAmount <= 0 || r.HarvestRadius <= 0)
                {
                    return Result<List<ResourceNodeDefinition>>.Failure(new GameError("INVALID_RESOURCE_STATS", $"Invalid stats for resource {r.Id}."));
                }
            }

            return Result<List<ResourceNodeDefinition>>.Success(resources);
        }
        catch (Exception ex)
        {
            return Result<List<ResourceNodeDefinition>>.Failure(new GameError("JSON_PARSE_ERROR", ex.Message));
        }
    }

    public static Result<List<EraDefinition>> LoadErasFromJson(string json)
    {
        try
        {
            var eras = JsonSerializer.Deserialize<List<EraDefinition>>(json, JsonOptions);
            if (eras == null || eras.Count == 0)
            {
                return Result<List<EraDefinition>>.Failure(new GameError("EMPTY_DATA", "Eras definition is empty."));
            }

            foreach (var era in eras)
            {
                if (string.IsNullOrWhiteSpace(era.Id))
                {
                    return Result<List<EraDefinition>>.Failure(new GameError("INVALID_ERA_ID", "Era ID cannot be empty."));
                }
            }

            return Result<List<EraDefinition>>.Success(eras);
        }
        catch (Exception ex)
        {
            return Result<List<EraDefinition>>.Failure(new GameError("JSON_PARSE_ERROR", ex.Message));
        }
    }

    public static Result<List<TechnologyDefinitionModel>> LoadTechnologiesFromJson(string json)
    {
        try
        {
            var techs = JsonSerializer.Deserialize<List<TechnologyDefinitionModel>>(json, JsonOptions);
            if (techs == null || techs.Count == 0)
            {
                return Result<List<TechnologyDefinitionModel>>.Failure(new GameError("EMPTY_DATA", "Technologies definition is empty."));
            }

            foreach (var tech in techs)
            {
                if (string.IsNullOrWhiteSpace(tech.Id))
                {
                    return Result<List<TechnologyDefinitionModel>>.Failure(new GameError("INVALID_TECH_ID", "Technology ID cannot be empty."));
                }
                if (tech.ResearchDurationTicks <= 0)
                {
                    return Result<List<TechnologyDefinitionModel>>.Failure(new GameError("INVALID_TECH_DURATION", $"Invalid duration for tech {tech.Id}."));
                }
            }

            return Result<List<TechnologyDefinitionModel>>.Success(techs);
        }
        catch (Exception ex)
        {
            return Result<List<TechnologyDefinitionModel>>.Failure(new GameError("JSON_PARSE_ERROR", ex.Message));
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

    public static Result<List<BuildingDefinition>> LoadBuildingsFromFile(string filePath)
    {
        if (!File.Exists(filePath))
        {
            return Result<List<BuildingDefinition>>.Failure(new GameError("FILE_NOT_FOUND", $"Definition file not found: {filePath}"));
        }
        string json = File.ReadAllText(filePath);
        return LoadBuildingsFromJson(json);
    }

    public static Result<List<ResourceNodeDefinition>> LoadResourcesFromFile(string filePath)
    {
        if (!File.Exists(filePath))
        {
            return Result<List<ResourceNodeDefinition>>.Failure(new GameError("FILE_NOT_FOUND", $"Definition file not found: {filePath}"));
        }
        string json = File.ReadAllText(filePath);
        return LoadResourcesFromJson(json);
    }

    public static Result<List<EraDefinition>> LoadErasFromFile(string filePath)
    {
        if (!File.Exists(filePath))
        {
            return Result<List<EraDefinition>>.Failure(new GameError("FILE_NOT_FOUND", $"Definition file not found: {filePath}"));
        }
        string json = File.ReadAllText(filePath);
        return LoadErasFromJson(json);
    }

    public static Result<List<TechnologyDefinitionModel>> LoadTechnologiesFromFile(string filePath)
    {
        if (!File.Exists(filePath))
        {
            return Result<List<TechnologyDefinitionModel>>.Failure(new GameError("FILE_NOT_FOUND", $"Definition file not found: {filePath}"));
        }
        string json = File.ReadAllText(filePath);
        return LoadTechnologiesFromJson(json);
    }
}
