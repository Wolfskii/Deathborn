using Microsoft.Xna.Framework.Input;

namespace Deathborn.Client;

internal static class InputKeys
{
    public static bool EnterPressed(KeyboardState kb, KeyboardState prevKb) =>
        WasPressed(kb, prevKb, Keys.Enter);

    public static bool IsEnterDown(KeyboardState kb) =>
        kb.IsKeyDown(Keys.Enter);

    private static bool WasPressed(KeyboardState kb, KeyboardState prevKb, Keys key) =>
        kb.IsKeyDown(key) && !prevKb.IsKeyDown(key);
}
