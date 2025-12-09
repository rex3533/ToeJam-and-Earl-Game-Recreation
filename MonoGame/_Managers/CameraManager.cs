using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace MonoGame
{
    // simple 2D camera that follows a target position.
    public class CameraManager
    {
        private readonly GraphicsDevice _graphicsDevice;

        public Vector2 Position { get; private set; } = Vector2.Zero;
        public Matrix Transform { get; private set; } = Matrix.Identity;

        public CameraManager(GraphicsDevice graphicsDevice)
        {
            _graphicsDevice = graphicsDevice;
            RecalculateTransform();
        }

        // Tell the camera what world position to look at (center on)
        public void LookAt(Vector2 target)
        {
            Position = target;
            RecalculateTransform();
        }

        private void RecalculateTransform()
        {
            var vp = _graphicsDevice.Viewport;

            // Move world so that Position is at the center of the screen.
            Transform =
                Matrix.CreateTranslation(new Vector3(-Position, 0f)) *
                Matrix.CreateTranslation(new Vector3(vp.Width * 0.5f,
                                                     vp.Height * 0.5f,
                                                     0f));
        }
    }
}
