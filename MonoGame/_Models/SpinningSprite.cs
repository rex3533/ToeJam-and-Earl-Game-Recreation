using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace MonoGame
{
    // Rotates in place; can optionally orbit in a small circle.
    public class SpinningSprite : GameObject
    {
        public float Angle = 0f;            // radians
        public float AngularVelocity = 0f;  // radians/sec

        public float OrbitRadius = 0f;      // pixels (0 = no orbit)
        public float OrbitSpeed  = 0f;      // radians/sec
        private float _orbitTheta = 0f;

        public SpinningSprite(Texture2D tex, Vector2 pos, GameRole role, Rectangle? source = null)
            : base(tex, pos, role, source) { }

        public override void Update()
        {
            Angle += AngularVelocity * Globals.TotalSeconds;
            _orbitTheta += OrbitSpeed * Globals.TotalSeconds;
        }

        public override void Draw()
        {
            var src = Source ?? new Rectangle(0, 0, Sprite.Width, Sprite.Height);
            var origin = new Vector2(src.Width / 2f, src.Height / 2f);

            int px = (int)System.Math.Floor(Position.X);
            int py = (int)System.Math.Floor(Position.Y);
            var center = new Vector2(px, py) + origin;

            var orbit = OrbitRadius > 0f
                ? new Vector2((float)System.Math.Cos(_orbitTheta), (float)System.Math.Sin(_orbitTheta)) * OrbitRadius
                : Vector2.Zero;

            Globals.SpriteBatch.Draw(
                Sprite,
                center + orbit,
                src,
                Tint,
                Angle,      // <-- geometric rotation (counts for Part 1)
                origin,
                Scale,
                SpriteEffects.None,
                0f
            );
        }
    }
}
