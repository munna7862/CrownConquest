using System;

namespace CrownConquest.Data.Models;

public sealed class FormationDefinitionModel
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public float MeleeDamageMultiplier { get; set; } = 1.0f;
    public float ArmorBonus { get; set; } = 0.0f;
    public float MovementSpeedMultiplier { get; set; } = 1.0f;
    public float RangedDamageMitigation { get; set; } = 0.0f;
    public float ChargeDamageMultiplier { get; set; } = 1.0f;
    public bool CanBraceCavalry { get; set; } = false;
    public string Description { get; set; } = string.Empty;
}
