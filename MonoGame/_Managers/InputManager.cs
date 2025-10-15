using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace MonoGame
{
    public static class InputManager
    {
        private static Vector2 _direction;
        private static KeyboardState _prevKb = Keyboard.GetState();

        public static Vector2 Direction => _direction;
        public static bool Moving => _direction != Vector2.Zero;

        // === A/B buttons ===
        public static bool APressed { get; private set; }   // edge (just pressed this frame)
        public static bool AHeld    { get; private set; }   // level (held down)
        public static bool BPressed { get; private set; }   // edge
        public static bool VolumeUpPressed   { get; private set; }  // edge: + (or numpad +)
        public static bool VolumeDownPressed { get; private set; }  // edge: - (or numpad -)


        private const Keys KeyA = Keys.Z;   // “A” action
        private const Keys KeyB = Keys.X;   // “B” inventory

        public static void Update()
        {
            var kb = Keyboard.GetState();
            int dx = 0, dy = 0;
            // Arrows or WASD
            if (kb.IsKeyDown(Keys.A) || kb.IsKeyDown(Keys.Left)) dx--;
            if (kb.IsKeyDown(Keys.D) || kb.IsKeyDown(Keys.Right)) dx++;
            if (kb.IsKeyDown(Keys.W) || kb.IsKeyDown(Keys.Up))    dy--;
            if (kb.IsKeyDown(Keys.S) || kb.IsKeyDown(Keys.Down))  dy++;

            _direction = new Vector2(dx, dy);

            // Pause toggle (edge)
            bool startNow  = kb.IsKeyDown(Keys.Enter) || kb.IsKeyDown(Keys.Space);
            bool startPrev = _prevKb.IsKeyDown(Keys.Enter) || _prevKb.IsKeyDown(Keys.Space);
            if (startNow && !startPrev)
            {
                Globals.TogglePause();

                // sync the BGM with game pause ONLY
                if (Globals.Paused) AudioManager.PauseBgm();
                else                AudioManager.ResumeBgm();
            }

            // Map/Menu toast (edge on M)
            bool mNow  = kb.IsKeyDown(Keys.M);
            bool mPrev = _prevKb.IsKeyDown(Keys.M);
            if (mNow && !mPrev) Globals.ToggleMenu();

            // === A/B (Z and X) handling ===
            bool aNow  = kb.IsKeyDown(KeyA);
            bool aPrev = _prevKb.IsKeyDown(KeyA);
            APressed = aNow && !aPrev;
            AHeld    = aNow;

            bool bNow  = kb.IsKeyDown(KeyB);
            bool bPrev = _prevKb.IsKeyDown(KeyB);
            BPressed = bNow && !bPrev;

            _prevKb = kb;

            // === Volume up/down (+ and -) handling ===
            // Volume keys: OemPlus ('= / +' key) or Numpad Add; OemMinus ('-' key) or Numpad Subtract
            bool plusNow  = kb.IsKeyDown(Keys.OemPlus)  || kb.IsKeyDown(Keys.Add);
            bool plusPrev = _prevKb.IsKeyDown(Keys.OemPlus) || _prevKb.IsKeyDown(Keys.Add);
            VolumeUpPressed = plusNow && !plusPrev;

            bool minusNow  = kb.IsKeyDown(Keys.OemMinus) || kb.IsKeyDown(Keys.Subtract);
            bool minusPrev = _prevKb.IsKeyDown(Keys.OemMinus) || _prevKb.IsKeyDown(Keys.Subtract);
            VolumeDownPressed = minusNow && !minusPrev;

        }
    }
}
