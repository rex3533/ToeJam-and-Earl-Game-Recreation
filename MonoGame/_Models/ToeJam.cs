using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace MonoGame
{
    public class ToeJam
    {
        private Vector2 _position = new Vector2(100, 100);
        private readonly float _speed = 200f;
        private readonly AnimationManager _anims = new AnimationManager();
        private readonly Microsoft.Xna.Framework.Graphics.Texture2D _tex;

        public ToeJam(int _, Microsoft.Xna.Framework.Graphics.Texture2D toeJamTexture)
        {
            _tex = toeJamTexture;

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
            _anims.SetIdle(idle);

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
            _anims.AddMoveAnimation(new Vector2(0, 1), moveDown);

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
            _anims.AddMoveAnimation(new Vector2(1, 0), moveRight);

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
            _anims.AddMoveAnimation(new Vector2(-1, 0), moveLeft);

            // --- MOVE UP (6 frames)  ---
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
            _anims.AddMoveAnimation(new Vector2(0, -1), moveUp);
        }

        public void Update()
        {
            var dir = InputManager.Direction;

            // Normalize so diagonals aren't faster
            if (dir != Vector2.Zero)
            {
                var move = dir;
                move.Normalize();
                _position += move * _speed * Globals.TotalSeconds;
            }

            // Animations decide which cardinal to show for diagonals
            _anims.Update(dir);
        }

        public void Draw() => _anims.Draw(_position);
    }
}
