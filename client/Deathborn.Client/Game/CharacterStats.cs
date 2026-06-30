namespace Deathborn.Client.Gameplay;

public sealed class CharacterStats
{
    public int Level = 1;
    public float Hp = 100f;
    public float HpMax = 100f;
    public float Stamina = 100f;
    public float StaminaMax = 100f;
    public float Mana = 100f;
    public float ManaMax = 100f;
    public float ExpPercent;

    public static CharacterStats CreateStarter() => new();
}
