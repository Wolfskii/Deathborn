using Microsoft.Xna.Framework.Input;

namespace Deathborn.Client;

internal static class InputKeys
{
    public static bool EnterPressed(KeyboardState kb, KeyboardState prevKb) =>
        kb.IsKeyDown(Keys.Enter) && !prevKb.IsKeyDown(Keys.Enter);
}
