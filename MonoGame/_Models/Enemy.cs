using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace MonoGame
{
    public class Enemy
    {
        public string Type { get; }
        public Vector2 Position;
        public Point   Size;
        public bool    Active = true;

        public float DamageCooldownSeconds = 0.40f;
        private float _cooldownTimer = 0f;

        public int   ShrinkX = 0;                 // shrink hitbox horizontally (px on each side)
        public int   ShrinkY = 0;                 // shrink hitbox vertically   (px on each side)
        public Point HitboxOffset = Point.Zero;   // offset hitbox from sprite top-left

        public Rectangle Bounds => new Rectangle((int)Position.X, (int)Position.Y, Size.X, Size.Y);

        public Enemy(string type, Vector2 position, Point size)
        {
            Type = type;
            Position = position;
            Size = size;
        }

        public Rectangle Hitbox
        {
            get
            {
                var r = Bounds;
                r.Inflate(-ShrinkX, -ShrinkY);    // negative inflates shrink
                r.Offset(HitboxOffset);
                return r;
            }
        }
        // Use Hitbox for collision (instead of Bounds)
        public bool Update(float dtSeconds, Rectangle playerBounds, Action onPlayerHit)
        {
            if (!Active) return false;

            if (_cooldownTimer > 0f)
                _cooldownTimer = Math.Max(0f, _cooldownTimer - dtSeconds);

            if (_cooldownTimer <= 0f && playerBounds.Intersects(Hitbox))
            {
                _cooldownTimer = DamageCooldownSeconds;
                onPlayerHit?.Invoke();
                return true;
            }
            return false;
        }

        // Visualize hitbox (semi-transparent red)
        private static Texture2D _pixel;
        public void DrawDebug(SpriteBatch spriteBatch)
        {
            if (!Active) return;
            if (_pixel == null)
            {
                _pixel = new Texture2D(spriteBatch.GraphicsDevice, 1, 1);
                _pixel.SetData(new[] { Color.White });
            }
            spriteBatch.Draw(_pixel, Hitbox, new Color(255, 0, 0, 100));
        }
    }
}
