using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace MonoGame
{
    // Enemy with a CIRCLE hitbox + simple 2-frame idle animation
    public class LilDevil : GameObject
    {
        // ---- Animation ----
        private readonly List<Rectangle> _frames = new();
        private int _frameIndex = 0;
        private float _frameTime = 0.18f;      // seconds per frame
        private float _timer = 0f;

        // ---- Circle hitbox ----
        public float Radius = 14f;             // tune as you like
        public float DamageCooldownSeconds = 0.40f;
        private float _cooldown = 0f;

        // Optional center nudge if art is off-center
        public Vector2 CircleOffset = Vector2.Zero;

        public LilDevil(Texture2D tex, Vector2 pos)
            : base(tex, pos, GameRole.Enemy)
        {
            // Idle frames from Lil_Devil.png  (coords)
            // 1) at (203,106) size 26x22
            // 2) at (250,105) size 28x23
            _frames.Add(new Rectangle(203, 106, 26, 22));
            _frames.Add(new Rectangle(250, 105, 28, 23));

            // Start displaying the first frame
            Source = _frames[_frameIndex];
        }

        // --- Animation tick (no gameplay params) ---
        public override void Update()
        {
            // animate
            _timer += Globals.TotalSeconds;
            if (_timer >= _frameTime)
            {
                _timer -= _frameTime;
                _frameIndex = (_frameIndex + 1) % _frames.Count;
                Source = _frames[_frameIndex];
            }
        }

        // --- Collision tick (circle vs player rect) ---
        public bool UpdateCollision(float dtSeconds, Rectangle playerBounds, Action onPlayerHit)
        {
            if (!Alive) return false;

            if (_cooldown > 0f)
                _cooldown = Math.Max(0f, _cooldown - dtSeconds);

            if (_cooldown <= 0f && RectVsCircle(playerBounds, CircleCenter, Radius))
            {
                _cooldown = DamageCooldownSeconds;
                onPlayerHit?.Invoke();
                return true;
            }
            return false;
        }

        // Center is current frame center + optional offset
        public Vector2 CircleCenter
        {
            get
            {
                var f = Source ?? new Rectangle(0, 0, Sprite.Width, Sprite.Height);
                return new Vector2(
                    (int)Math.Floor(Position.X + f.Width  * 0.5f),
                    (int)Math.Floor(Position.Y + f.Height * 0.5f)
                ) + CircleOffset;
            }
        }

        // --- Debug ring so graders can SEE the circle primitive ---
        private static Texture2D _pixel;
        public void DrawDebugCircle(SpriteBatch sb, Color? color = null, int segments = 36, float thickness = 2f)
        {
            if (_pixel == null)
            {
                _pixel = new Texture2D(sb.GraphicsDevice, 1, 1);
                _pixel.SetData(new[] { Color.White });
            }
            var col = color ?? new Color(255, 0, 0, 190); //red, semi-transparent

            float step = MathHelper.TwoPi / segments;
            Vector2 prev = CircleCenter + new Vector2(Radius, 0);
            for (int i = 1; i <= segments; i++)
            {
                float t = i * step;
                Vector2 curr = CircleCenter + new Vector2((float)Math.Cos(t), (float)Math.Sin(t)) * Radius;
                DrawLine(sb, prev, curr, col, thickness);
                prev = curr;
            }
        }

        private void DrawLine(SpriteBatch sb, Vector2 a, Vector2 b, Color col, float thick)
        {
            Vector2 d = b - a;
            float len = d.Length();
            if (len < 1f) return;
            float rot = (float)Math.Atan2(d.Y, d.X);

            if (_pixel == null)
            {
                _pixel = new Texture2D(sb.GraphicsDevice, 1, 1);
                _pixel.SetData(new[] { Color.White });
            }

            sb.Draw(_pixel,
                destinationRectangle: new Rectangle((int)a.X, (int)a.Y, (int)len, (int)thick),
                sourceRectangle: null, color: col, rotation: rot, origin: Vector2.Zero,
                effects: SpriteEffects.None, layerDepth: 0f);
        }

        // --- AABB (rect) vs circle helper ---
        private static bool RectVsCircle(Rectangle r, Vector2 c, float radius)
        {
            float nx = MathHelper.Clamp(c.X, r.Left,  r.Right);
            float ny = MathHelper.Clamp(c.Y, r.Top,   r.Bottom);
            float dx = c.X - nx;
            float dy = c.Y - ny;
            return dx*dx + dy*dy <= radius * radius;
        }
    }
}
