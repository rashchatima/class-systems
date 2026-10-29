using ClassSystems.Core.Characters.Models;

namespace ClassSystems.Core.Characters.Rules;

/// <summary>
/// The basic rules of a fight: how damage and healing change a character.
/// </summary>
internal static class Combat
{
    // Any hit deals at least this much damage, no matter how strong the armor is.
    private const int MinDamage = 1;

    // The share of damage that gets through while the character is defending.
    private const double DefenseDamageRatio = 0.5;

    /// <summary>
    /// Applies incoming damage, reducing it by armor and the defensive stance.
    /// </summary>
    /// <returns>Amount of health actually lost.</returns>
    public static int TakeDamage(Character target, int amount, bool ignoreArmor = false)
    {
        int damage = ignoreArmor ? amount : amount - target.Armor;

        if (target.IsDefending)
        {
            damage = (int)(damage * DefenseDamageRatio);
            target.IsDefending = false;
        }

        // Health cannot go below zero, so the damage is limited by what is left.
        damage = Math.Min(Math.Max(damage, MinDamage), target.Health);
        target.Health -= damage;
        return damage;
    }

    /// <summary>
    /// Restores health without exceeding the maximum.
    /// </summary>
    /// <returns>Amount of health actually restored.</returns>
    public static int RestoreHealth(Character target, int amount)
    {
        int restored = Math.Min(amount, target.MaxHealth - target.Health);
        target.Health += restored;
        return restored;
    }
}
