using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace MonoGame
{
    public enum ProjectileKind { Tomato, Slingshot }

    // Simple moving sprite with a circle hitbox
    public class Projectile : GameObject
    {
        public Vector2 Velocity { get; private set; }
        public float   Radius   { get; set; } = 6f;
        public int     Damage   { get; set; } = 1;

        public float   MaxDistance { get; private set; }
        public float   Traveled    { get; private set; }
        public bool    ReachedMax  { get; private set; }

        public ProjectileKind Kind { get; private set; }

        // Full sprite constructor (what we use for tomatoes)
        public Projectile(Texture2D sprite, Rectangle source, Vector2 start, Vector2 velocity, float range, ProjectileKind kind)
            : base(sprite, start, GameRole.Item, source)
        {
            Velocity    = velocity;
            MaxDistance = range;
            Kind        = kind;
            Alive       = true;
            Scale       = 1f;
        }

        // Minimal constructor (kept for compatibility if you ever used it)
        public Projectile(Vector2 start, Vector2 velocity, float range, ProjectileKind kind)
            : base(null, start, GameRole.Item)
        {
            Velocity    = velocity;
            MaxDistance = range;
            Kind        = kind;
            Alive       = true;
            Scale       = 1f;
        }

        public override void Update()
        {
            if (!Alive) return;

            var dt = Globals.TotalSeconds;
            var step = Velocity * dt;

            Position += step;
            Traveled += step.Length();

            if (Traveled >= MaxDistance)
            {
                Alive = false;
                ReachedMax = true;
            }
        }

        public override void Draw()
        {
            // If we have a sprite, draw via base. Otherwise, draw nothing.
            if (Sprite != null)
                base.Draw();
        }
    }
}
