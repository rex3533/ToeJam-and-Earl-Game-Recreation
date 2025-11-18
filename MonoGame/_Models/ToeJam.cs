using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace MonoGame
{
    public class ToeJam
    {
        private Vector2 _position = new Vector2(100, 100);
        private readonly float _speed = 200f;

        private readonly Texture2D _tex;

        // Two animation sets: walk (existing) + sneak (new)
        private readonly AnimationManager _walkAnims = new AnimationManager();
        private readonly AnimationManager _sneakAnims = new AnimationManager();
        private bool _hasSneakAnims = false; // becomes true once we fill rectangles

        // Drawing scale (1 for original size)
        public float Scale { get; set; } = 1.5f;
        // Sneaking state + speed factor
        private bool _sneaking;
        private const float SneakFactor = 0.45f;

        // For timed boosts (Hi-Tops, etc.)
        public float SpeedMultiplier { get; set; } = 1f;

        // Expose texture + idle rect so GameManager can build a decoy sprite
        public Texture2D Texture => _tex;
        public Rectangle IdleRect => new Rectangle(15, 14, 22, 24);

        public ToeJam(int _, Texture2D toeJamTexture)
        {
            _tex = toeJamTexture;

            BuildWalkAnims();
            BuildSneakAnims(); // uses your provided 18x28 coords
        }

        private void BuildWalkAnims()
        {
            // --- IDLE (3 frames) ---
            var idle = new Animations(
                _tex,
                new List<Rectangle>
                {
                    new Rectangle(15, 14, 22, 24),
                    new Rectangle(44, 14, 22, 24),
                    new Rectangle(76, 13, 22, 24)
                },
                frameTime: 0.12f
            );
            _walkAnims.SetIdle(idle);

            // --- MOVE DOWN (6 frames) ---
            var moveDown = new Animations(
                _tex,
                new List<Rectangle>
                {
                    new Rectangle(19,  80, 22, 26),
                    new Rectangle(54,  80, 22, 26),
                    new Rectangle(85,  80, 22, 26),
                    new Rectangle(123, 80, 22, 26),
                    new Rectangle(156, 81, 22, 26),
                    new Rectangle(186, 80, 22, 26),
                },
                frameTime: 0.10f
            );
            _walkAnims.AddMoveAnimation(new Vector2(0, 1), moveDown);

            // --- MOVE RIGHT (6 frames) ---
            var moveRight = new Animations(
                _tex,
                new List<Rectangle>
                {
                    new Rectangle(238, 133, 22, 26),
                    new Rectangle(272, 133, 22, 26),
                    new Rectangle(305, 133, 22, 26),
                    new Rectangle(337, 133, 22, 26),
                    new Rectangle(368, 133, 22, 26),
                    new Rectangle(405, 133, 22, 26),
                },
                frameTime: 0.10f
            );
            _walkAnims.AddMoveAnimation(new Vector2(1, 0), moveRight);

            // --- MOVE LEFT (6 frames) ---
            var moveLeft = new Animations(
                _tex,
                new List<Rectangle>
                {
                    new Rectangle(238, 84, 22, 26),
                    new Rectangle(272, 84, 22, 26),
                    new Rectangle(305, 84, 22, 26),
                    new Rectangle(337, 84, 22, 26),
                    new Rectangle(368, 84, 22, 26),
                    new Rectangle(405, 84, 22, 26),
                },
                frameTime: 0.10f
            );
            _walkAnims.AddMoveAnimation(new Vector2(-1, 0), moveLeft);

            // --- MOVE UP (6 frames) ---
            var moveUp = new Animations(
                _tex,
                new List<Rectangle>
                {
                    new Rectangle(20,  132, 22, 26),
                    new Rectangle(61,  132, 22, 26),
                    new Rectangle(91,  132, 22, 26),
                    new Rectangle(124, 132, 22, 26),
                    new Rectangle(156, 132, 22, 26),
                    new Rectangle(189, 132, 22, 26),
                },
                frameTime: 0.10f
            );
            _walkAnims.AddMoveAnimation(new Vector2(0, -1), moveUp);
        }
        // can drop powerups at the player’s location
        public Vector2 Position => _position;

        private void BuildSneakAnims()
        {
            // all sneak frames are 18x28 (from your notes)
            const int W = 18, H = 28;

            // ↓ Down (3)
            var down = new List<Rectangle> {
                new Rectangle(26, 204, W, H),
                new Rectangle(54, 204, W, H),
                new Rectangle(85, 204, W, H),
            };

            // ↑ Up (3)
            var up = new List<Rectangle> {
                new Rectangle(23, 248, W, H),
                new Rectangle(52, 246, W, H),
                new Rectangle(81, 247, W, H),
            };

            // ← Left (3)
            var left = new List<Rectangle> {
                new Rectangle(133, 202, W, H),
                new Rectangle(158, 204, W, H),
                new Rectangle(184, 205, W, H),
            };

            // → Right (3)
            var right = new List<Rectangle> {
                new Rectangle(130, 247, W, H),
                new Rectangle(157, 246, W, H),
                new Rectangle(183, 244, W, H),
            };

            // We only define moving sneak loops; idle stays regular idle.
            _sneakAnims.AddMoveAnimation(new Vector2(0,  1), new Animations(_tex, down,  0.10f)); // down
            _sneakAnims.AddMoveAnimation(new Vector2(0, -1), new Animations(_tex, up,    0.10f)); // up
            _sneakAnims.AddMoveAnimation(new Vector2(-1, 0), new Animations(_tex, left,  0.10f)); // left
            _sneakAnims.AddMoveAnimation(new Vector2(1,  0), new Animations(_tex, right, 0.10f)); // right

            _hasSneakAnims = true;
        }

        // Called by GameManager when Z should toggle sneaking (only if nothing equipped)
        public void ToggleSneak() => _sneaking = !_sneaking;
        public void SetSneak(bool on) { _sneaking = on; }


        public void Update()
        {
            var dir = InputManager.Direction;

            // Move (delta-time); when sneaking, slower; include any SpeedMultiplier
            if (dir != Vector2.Zero)
            {
                var move = dir; move.Normalize();
                float speed = _speed * SpeedMultiplier * (_sneaking ? SneakFactor : 1f);
                _position += move * speed * Globals.TotalSeconds;
            }

            // Choose animation set:
            // - If sneaking AND moving -> use sneak set
            // - Else -> use regular walk set (includes idle when dir == zero)
            bool useSneak = _sneaking && _hasSneakAnims && dir != Vector2.Zero;
            var active = useSneak ? _sneakAnims : _walkAnims;

            active.Update(dir);
        }

        public void Draw()
        {
            var dir = InputManager.Direction;
            bool useSneak = _sneaking && _hasSneakAnims && dir != Vector2.Zero;
            var active = useSneak ? _sneakAnims : _walkAnims;
            active.Draw(_position, Scale);
        }
    }
}
