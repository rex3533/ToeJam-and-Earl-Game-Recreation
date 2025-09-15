using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace MonoGame
{
    // Uses a single idle animation + up to 4 move animations (U/D/L/R).
    // tie-breaking with the last facing axis to reduce flicker.
    public class AnimationManager
    {
        private readonly Dictionary<Vector2, Animations> _moveAnims = new();
        private Animations _idleAnim;
        private Animations _current;
        private Vector2 _lastFacing = new Vector2(0, 1); // default facing DOWN

        public void SetIdle(Animations idle)
        {
            _idleAnim = idle;
            if (_current == null) _current = _idleAnim;
        }

        public void AddMoveAnimation(Vector2 direction, Animations animation)
        {
            // Expected keys: (0,1) down, (0,-1) up, (-1,0) left, (1,0) right
            _moveAnims[direction] = animation;
        }

        private Vector2 ChooseAnimDirection(Vector2 dir)
        {
            if (dir == Vector2.Zero) return Vector2.Zero;

            // If diagonal, choose cardinal based on last facing axis (tie-break)
            if (dir.X != 0 && dir.Y != 0)
            {
                if (System.Math.Abs(_lastFacing.Y) >= System.Math.Abs(_lastFacing.X))
                    return new Vector2(0, System.Math.Sign(dir.Y));
                else
                    return new Vector2(System.Math.Sign(dir.X), 0);
            }

            // Non-diagonal: pick the axis with greater magnitude (they're 0/±1 anyway)
            return (System.Math.Abs(dir.Y) >= System.Math.Abs(dir.X))
                ? new Vector2(0, System.Math.Sign(dir.Y))
                : new Vector2(System.Math.Sign(dir.X), 0);
        }

        public void Update(Vector2 inputDirection)
        {
            var animDir = ChooseAnimDirection(inputDirection);

            if (animDir == Vector2.Zero)
            {
                if (_idleAnim != null)
                {
                    if (_current != _idleAnim)
                    {
                        _current = _idleAnim;
                        _current.Reset();
                    }
                    _current.Play(); // ensure idle resumes
                }
            }
            else
            {
                _lastFacing = animDir;

                if (_moveAnims.TryGetValue(animDir, out var move))
                {
                    if (_current != move)
                    {
                        _current = move;
                        _current.Reset();
                        _current.Play();
                    }
                }
                else
                {
                    // No move anim for this direction: hold idle frame while moving
                    if (_idleAnim != null)
                    {
                        if (_current != _idleAnim)
                        {
                            _current = _idleAnim;
                            _current.Reset();
                        }
                        _current.Stop(); // freeze one idle frame during movement
                    }
                }
            }

            _current?.Update();
        }

        public void Draw(Vector2 position) => _current?.Draw(position);
    }
}
