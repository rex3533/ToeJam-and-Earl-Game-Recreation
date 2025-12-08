using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics; // for SpriteBatch type

namespace MonoGame;

public static class Globals
{
    public static float TotalSeconds { get; set; }
    public static ContentManager Content { get; set; }
    public static SpriteBatch SpriteBatch { get; set; }

    // ---- Pause (unchanged from working baseline) ----
    public static bool Paused { get; private set; }
    public static void TogglePause() => Paused = !Paused;
    public static void SetPaused(bool paused) => Paused = paused;

    // ---- Menu toast  ----
    public static bool MenuOpen { get; private set; }
    public static string MenuToastText { get; private set; } = "";
    public static float MenuToastTimer { get; private set; } = 0f; // seconds

    public static float TimeScale { get; private set; } = 1f;   // 1 = normal speed
    public static float UnscaledSeconds { get; private set; }   // raw dt (unscaled)

    public static void SetTimeScale(float value)
    {
        // clamp to something sane; allow 0 for full pause if you want
        TimeScale = MathHelper.Clamp(value, 0f, 4f);
    }
    public static void NudgeTimeScale(float delta) => SetTimeScale(TimeScale + delta);


    public static void ToggleMenu()
    {
        MenuOpen = !MenuOpen;
        MenuToastText = MenuOpen ? "Menu Opened" : "Menu Closed";
        MenuToastTimer = 1.2f;

        // (optional) keep console/debug prints
        System.Diagnostics.Debug.WriteLine(MenuToastText);
        System.Console.WriteLine(MenuToastText);
    }
    public static void ShowToast(string text, float seconds = 1.2f)
    {
        MenuToastText = text ?? "";
        MenuToastTimer = seconds;
    }
    public static void Update(GameTime gt)
    {
    var rawDt = (float)gt.ElapsedGameTime.TotalSeconds;
    UnscaledSeconds = rawDt;
    TotalSeconds    = rawDt * TimeScale;

    // countdown the toast timer (scaled so it slows during slow-mo)
    if (MenuToastTimer > 0f)
    {
        MenuToastTimer -= TotalSeconds;
        if (MenuToastTimer < 0f) MenuToastTimer = 0f;
    }
    }

}
