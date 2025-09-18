using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;

namespace MonoGame;

public static class Globals
{
    public static float TotalSeconds { get; set; }
    public static ContentManager Content { get; set; }
    public static SpriteBatch SpriteBatch { get; set; }

    // Pause state for the game. When true, most game updates should early-return
    // while input and draw remain active so the player can unpause.
    public static bool Paused { get; private set; }

    public static void TogglePause()
    {
        Paused = !Paused;
    }

    public static void SetPaused(bool paused)
    {
        Paused = paused;
    }

    public static void Update(GameTime gt)
    {
        TotalSeconds = (float)gt.ElapsedGameTime.TotalSeconds;
    }
}