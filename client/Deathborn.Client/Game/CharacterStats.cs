namespace Deathborn.Client.Gameplay;

using Deathborn.Client;

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

    public void TickRegen(float dt, bool isRunning)
    {
        if (Hp < HpMax)
            Hp = MathF.Min(HpMax, Hp + Config.HpRegenPerSecond * dt);

        if (Mana < ManaMax)
            Mana = MathF.Min(ManaMax, Mana + Config.ManaRegenPerSecond * dt);

        if (Stamina >= StaminaMax) return;

        var rate = isRunning ? Config.StaminaRegenWhileRunningPerSecond : Config.StaminaRegenPerSecond;
        Stamina = MathF.Min(StaminaMax, Stamina + rate * dt);
    }

    public static CharacterStats CreateStarter() => new();
}
