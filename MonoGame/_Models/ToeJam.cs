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

        // Two animation sets: walk + sneak
        private readonly AnimationManager _walkAnims = new AnimationManager();
        private readonly AnimationManager _sneakAnims = new AnimationManager();
        private bool _hasSneakAnims = false;

        public float Scale { get; set; } = 1.5f;

        // Toggle sneaking
        private bool _sneaking;
        private const float SneakFactor = 0.45f;

        // Throw pose override (for tomato/slingshot)
        private bool _poseOverride;
        private Rectangle _poseRect;
        private float _poseTimer;

        // 5s “look in last direction” before idle
        private float _idleLookTimer = 0f;
        private const float IdleLookSeconds = 5f;
        private Vector2 _lastFacing = new(1f, 0f);

        // Tomato projectile sprite (projectile itself lives elsewhere)
        public static readonly Rectangle TomatoProjectile = new Rectangle(406, 372, 13, 7);

        // ----- TOMATO throw frames (corrected) -----
        private static readonly Rectangle TomatoDownReady   = new Rectangle(250, 320, 26, 31);
        private static readonly Rectangle TomatoDownThrown  = new Rectangle(285, 322, 21, 28);

        // UP (new values)
        private static readonly Rectangle TomatoUpReady     = new Rectangle(254, 364, 25, 33);
        private static readonly Rectangle TomatoUpThrown    = new Rectangle(290, 367, 23, 29);

        // Left/Right were swapped before — fixed here
        private static readonly Rectangle TomatoLeftReady   = new Rectangle(318, 320, 30, 29);
        private static readonly Rectangle TomatoLeftThrown  = new Rectangle(358, 322, 21, 28);
        private static readonly Rectangle TomatoRightReady  = new Rectangle(318, 364, 30, 30);
        private static readonly Rectangle TomatoRightThrown = new Rectangle(359, 364, 20, 28);

        // ----- SLINGSHOT frames (ready1, ready2, release) -----
        // DOWN
        private static readonly Rectangle SlingDown1 = new Rectangle(28, 434, 20, 31);
        private static readonly Rectangle SlingDown2 = new Rectangle(55, 435, 22, 30);
        private static readonly Rectangle SlingDown3 = new Rectangle(85, 434, 22, 30);
        // UP
        private static readonly Rectangle SlingUp1   = new Rectangle(25, 474, 25, 30);
        private static readonly Rectangle SlingUp2   = new Rectangle(55, 474, 24, 30);
        private static readonly Rectangle SlingUp3   = new Rectangle(85, 475, 20, 28);
        // LEFT
        private static readonly Rectangle SlingLeft1 = new Rectangle(115, 435, 23, 28);
        private static readonly Rectangle SlingLeft2 = new Rectangle(143, 433, 25, 29);
        private static readonly Rectangle SlingLeft3 = new Rectangle(175, 434, 23, 27);
        // RIGHT
        private static readonly Rectangle SlingRight1= new Rectangle(117, 476, 25, 28);
        private static readonly Rectangle SlingRight2= new Rectangle(149, 476, 23, 28);
        private static readonly Rectangle SlingRight3= new Rectangle(178, 476, 24, 27);

        // For timed boosts (Hi-Tops, etc.)
        public float SpeedMultiplier { get; set; } = 1f;

        // Expose texture + idle rect for decoy
        public Texture2D Texture => _tex;
        public Rectangle IdleRect => new Rectangle(15, 14, 22, 24);

        public ToeJam(int _, Texture2D toeJamTexture)
        {
            _tex = toeJamTexture;
            BuildWalkAnims();
            BuildSneakAnims();
        }

        private void BuildWalkAnims()
        {
            // IDLE (3)
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

            // DOWN (6)
            _walkAnims.AddMoveAnimation(new Vector2(0, 1), new Animations(
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
                0.10f
            ));
            // RIGHT (6)
            _walkAnims.AddMoveAnimation(new Vector2(1, 0), new Animations(
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
                0.10f
            ));
            // LEFT (6)
            _walkAnims.AddMoveAnimation(new Vector2(-1, 0), new Animations(
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
                0.10f
            ));
            // UP (6)
            _walkAnims.AddMoveAnimation(new Vector2(0, -1), new Animations(
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
                0.10f
            ));
        }

        public Vector2 Position => _position;

        private void BuildSneakAnims()
        {
            const int W = 18, H = 28;

            var down = new List<Rectangle> {
                new Rectangle(26, 204, W, H),
                new Rectangle(54, 204, W, H),
                new Rectangle(85, 204, W, H),
            };
            var up = new List<Rectangle> {
                new Rectangle(23, 248, W, H),
                new Rectangle(52, 246, W, H),
                new Rectangle(81, 247, W, H),
            };
            var left = new List<Rectangle> {
                new Rectangle(133, 202, W, H),
                new Rectangle(158, 204, W, H),
                new Rectangle(184, 205, W, H),
            };
            var right = new List<Rectangle> {
                new Rectangle(130, 247, W, H),
                new Rectangle(157, 246, W, H),
                new Rectangle(183, 244, W, H),
            };

            _sneakAnims.AddMoveAnimation(new Vector2(0,  1), new Animations(_tex, down,  0.10f));
            _sneakAnims.AddMoveAnimation(new Vector2(0, -1), new Animations(_tex, up,    0.10f));
            _sneakAnims.AddMoveAnimation(new Vector2(-1, 0), new Animations(_tex, left,  0.10f));
            _sneakAnims.AddMoveAnimation(new Vector2(1,  0), new Animations(_tex, right, 0.10f));

            _hasSneakAnims = true;
        }

        public void ToggleSneak() => _sneaking = !_sneaking;
        public void SetSneak(bool on) { _sneaking = on; }

        private bool _frozen = false;
        public void SetFrozen(bool on) { _frozen = on; }

        private enum FaceDir { Up, Down, Left, Right }
        private static FaceDir DirFromVector(Vector2 v)
        {
            if (System.Math.Abs(v.X) >= System.Math.Abs(v.Y))
                return v.X >= 0 ? FaceDir.Right : FaceDir.Left;
            return v.Y >= 0 ? FaceDir.Down : FaceDir.Up;
        }

        // Tomato helpers
        private static Rectangle TomatoReadyFor(FaceDir d) => d switch
        {
            FaceDir.Down  => TomatoDownReady,
            FaceDir.Up    => TomatoUpReady,
            FaceDir.Right => TomatoRightReady,
            _             => TomatoLeftReady
        };
        private static Rectangle TomatoThrownFor(FaceDir d) => d switch
        {
            FaceDir.Down  => TomatoDownThrown,
            FaceDir.Up    => TomatoUpThrown,
            FaceDir.Right => TomatoRightThrown,
            _             => TomatoLeftThrown
        };

        // Slingshot helpers
        private static Rectangle SlingReady1For(FaceDir d) => d switch
        {
            FaceDir.Down  => SlingDown1,
            FaceDir.Up    => SlingUp1,
            FaceDir.Right => SlingRight1,
            _             => SlingLeft1
        };
        private static Rectangle SlingReady2For(FaceDir d) => d switch
        {
            FaceDir.Down  => SlingDown2,
            FaceDir.Up    => SlingUp2,
            FaceDir.Right => SlingRight2,
            _             => SlingLeft2
        };
        private static Rectangle SlingReleaseFor(FaceDir d) => d switch
        {
            FaceDir.Down  => SlingDown3,
            FaceDir.Up    => SlingUp3,
            FaceDir.Right => SlingRight3,
            _             => SlingLeft3
        };

        // Public entry points used by RangedController
        public void ShowTomatoReady(Vector2 facing, float holdSeconds)
        {
            _poseOverride = true;
            _poseTimer    = holdSeconds;
            _poseRect     = TomatoReadyFor(DirFromVector(facing));
        }
        public void ShowTomatoRelease(Vector2 facing, float holdSeconds)
        {
            _poseOverride = true;
            _poseTimer    = holdSeconds;
            _poseRect     = TomatoThrownFor(DirFromVector(facing));
        }
        public void ShowSlingReady1(Vector2 facing, float holdSeconds)
        {
            _poseOverride = true;
            _poseTimer    = holdSeconds;
            _poseRect     = SlingReady1For(DirFromVector(facing));
        }
        public void ShowSlingReady2(Vector2 facing, float holdSeconds)
        {
            _poseOverride = true;
            _poseTimer    = holdSeconds;
            _poseRect     = SlingReady2For(DirFromVector(facing));
        }
        public void ShowSlingRelease(Vector2 facing, float holdSeconds)
        {
            _poseOverride = true;
            _poseTimer    = holdSeconds;
            _poseRect     = SlingReleaseFor(DirFromVector(facing));
        }

        public void Update()
        {
            // Throw pose timer
            if (_poseOverride)
            {
                _poseTimer -= Globals.TotalSeconds;
                if (_poseTimer <= 0f) _poseOverride = false;
            }

            var dir = _frozen ? Vector2.Zero : InputManager.Direction;

            // Move
            if (dir != Vector2.Zero)
            {
                var move = dir; move.Normalize();
                float speed = _speed * SpeedMultiplier * (_sneaking ? SneakFactor : 1f);
                _position += move * speed * Globals.TotalSeconds;

                // track last facing and refresh look timer
                _lastFacing = move;
                _idleLookTimer = IdleLookSeconds;
            }
            else
            {
                if (!_poseOverride && _idleLookTimer > 0f)
                    _idleLookTimer -= Globals.TotalSeconds;
            }

            // Choose and tick animation set
            bool useSneak = _sneaking && _hasSneakAnims && dir != Vector2.Zero;
            var active = useSneak ? _sneakAnims : _walkAnims;
            active.Update(dir);
        }

        public void Draw()
        {
            // 1) Throw pose wins
            if (_poseOverride)
            {
                DrawRect(_poseRect);
                return;
            }

            var dir = InputManager.Direction;

            // 2) Not moving and still within 5s look window — draw fixed facing pose
            if (dir == Vector2.Zero && _idleLookTimer > 0f)
            {
                var stand = StandRectFor(DirFromVector(
                    _lastFacing == Vector2.Zero ? new Vector2(1, 0) : _lastFacing));
                DrawRect(stand);
                return;
            }

            // 3) Otherwise: regular animations
            bool useSneak = _sneaking && _hasSneakAnims && dir != Vector2.Zero;
            var active = useSneak ? _sneakAnims : _walkAnims;
            active.Draw(_position, Scale);
        }

        // Your requested “look while stopped” frames per facing
        private static Rectangle StandRectFor(FaceDir d) => d switch
        {
            FaceDir.Right => new Rectangle(273, 133, 21, 27),
            FaceDir.Left  => new Rectangle(371,  84, 21, 29),
            FaceDir.Up    => new Rectangle(159, 133, 24, 30),
            _             => new Rectangle( 46,  12, 22, 28) // Down
        };

        private void DrawRect(Rectangle src)
        {
            var pos = new Vector2(
                (int)System.Math.Floor(_position.X),
                (int)System.Math.Floor(_position.Y));
            Globals.SpriteBatch.Draw(
                _tex, pos, src, Color.White,
                0f, Vector2.Zero, Scale, SpriteEffects.None, 0f);
        }
    }
}
