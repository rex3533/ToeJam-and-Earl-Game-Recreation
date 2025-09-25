using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace MonoGame
{
    // Generic on-map powerup/usables actor (e.g., Decoy, Spring Shoes prop, etc.)
    public class Powerup : GameObject
    {
        private readonly List<Rectangle> _frames;
        private readonly float _frameDuration;
        private float _frameTimer;
        private int _frameIndex;
        private float _life; // seconds; <=0 means infinite

        public Powerup(Texture2D tex, Vector2 pos, List<Rectangle> frames, float frameDuration, float lifeSeconds = 0f)
            : base(tex, pos, GameRole.Item)
        {
            _frames = frames ?? new List<Rectangle>();
            _frameDuration = frameDuration <= 0f ? 0.1f : frameDuration;
            _life = lifeSeconds;
            Tint = new Color(255, 255, 255, 200); // slight transparency so it's visually distinct
        }

        public override void Update()
        {
            // advance animation
            if (_frames.Count > 1)
            {
                _frameTimer -= Globals.TotalSeconds;
                if (_frameTimer <= 0f)
                {
                    _frameTimer += _frameDuration;
                    _frameIndex = (_frameIndex + 1) % _frames.Count;
                }
            }

            // tick lifespan
            if (_life > 0f)
            {
                _life -= Globals.TotalSeconds;
                if (_life <= 0f) Alive = false;
            }
        }

        public override void Draw()
        {
            var src = _frames.Count > 0 ? _frames[_frameIndex] : (Source ?? new Rectangle(0, 0, Sprite.Width, Sprite.Height));
            var px = (int)Math.Floor(Position.X);
            var py = (int)Math.Floor(Position.Y);

            if (Size.HasValue)
            {
                var dest = new Rectangle(px, py, Size.Value.X, Size.Value.Y);
                Globals.SpriteBatch.Draw(Sprite, dest, src, Tint);
            }
            else
            {
                Globals.SpriteBatch.Draw(
                Sprite,
                new Vector2(px, py),
                src,
                Tint,
                0f,
                Vector2.Zero,
                Scale,                 // <— now uses GameObject.Scale
                SpriteEffects.None,
                0f
                );
            }
        }

        // ---- Convenience factory for Decoy using your sheet positions (27x42) ----
        public static Powerup CreateDecoy(Texture2D tex, Vector2 pos, float lifeSeconds = 6f)
        {
            const int W = 27, H = 42;
            var frames = new List<Rectangle>
            {
                new Rectangle(10, 1032, W, H),
                new Rectangle(45, 1032, W, H),
                new Rectangle(79, 1032, W, H),
            };
            return new Powerup(tex, pos, frames, frameDuration: 0.18f, lifeSeconds: lifeSeconds);
        }
    }
}
