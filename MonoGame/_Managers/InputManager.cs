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

        public static void Update()
        {
            var kb = Keyboard.GetState();
            int dx = 0, dy = 0;

            // WASD + Arrow keys
            if (kb.IsKeyDown(Keys.A) || kb.IsKeyDown(Keys.Left))  dx--;
            if (kb.IsKeyDown(Keys.D) || kb.IsKeyDown(Keys.Right)) dx++;
            if (kb.IsKeyDown(Keys.W) || kb.IsKeyDown(Keys.Up))    dy--;
            if (kb.IsKeyDown(Keys.S) || kb.IsKeyDown(Keys.Down))  dy++;

            // Allow diagonals (no clamping to cardinal here)
            _direction = new Vector2(dx, dy);

            // Start button toggles pause: Enter or Space. Use edge-detect so holding
            // the key won't repeatedly toggle.
            bool startPressed = kb.IsKeyDown(Keys.Enter) || kb.IsKeyDown(Keys.Space);
            bool wasStartPressed = _prevKb.IsKeyDown(Keys.Enter) || _prevKb.IsKeyDown(Keys.Space);

            if (startPressed && !wasStartPressed)
            {
                Globals.TogglePause();
            }

            _prevKb = kb;
        }
    }
}