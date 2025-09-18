using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics; // for SpriteBatch type

namespace MonoGame;

public static class Globals
{
    public static float TotalSeconds { get; set; }
    public static ContentManager Content { get; set; }
    public static SpriteBatch SpriteBatch { get; set; }

    // ---- Pause (unchanged from your working baseline) ----
    public static bool Paused { get; private set; }
    public static void TogglePause() => Paused = !Paused;
    public static void SetPaused(bool paused) => Paused = paused;

    // ---- Menu toast (new) ----
    public static bool MenuOpen { get; private set; }
    public static string MenuToastText { get; private set; } = "";
    public static float MenuToastTimer { get; private set; } = 0f; // seconds

    public static void ToggleMenu()
    {
        MenuOpen = !MenuOpen;
        MenuToastText = MenuOpen ? "Menu Opened" : "Menu Closed";
        MenuToastTimer = 1.2f;

        // (optional) keep console/debug prints
        System.Diagnostics.Debug.WriteLine(MenuToastText);
        System.Console.WriteLine(MenuToastText);
    }

    public static void Update(GameTime gt)
    {
        TotalSeconds = (float)gt.ElapsedGameTime.TotalSeconds;

        // countdown the toast timer
        if (MenuToastTimer > 0f)
        {
            MenuToastTimer -= TotalSeconds;
            if (MenuToastTimer < 0f) MenuToastTimer = 0f;
        }
    }
}
