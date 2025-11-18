using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace MonoGame
{
    public class LilDevil : GameObject
    {
        // ===== FSM =====
        public enum DevilState { SleepIdle, AwakeIdle, Patrol, Alert, Dead }
        public DevilState State { get; private set; } = DevilState.SleepIdle;

        // ===== Tunables =====
        public float MoveSpeed = 70f;

        // Time flow just for this AI (if your engine dt feels fast, <1 slows it)
        public float AiTimeScale = 0.85f;

        // Timers (assignment spec)
        public float PatrolToAwakeSeconds = 15f;  // Patrol  → AwakeIdle
        public float AwakeToSleepSeconds  = 10f;  // AwakeIdle → SleepIdle

        // Perception
        public float WakeRadiusWalk  = 72f;  // wakes if walking inside this
        public float WakeRadiusSneak = 34f;  // wakes if sneaking inside this
        public float DetectionRadius = 120f; // enter Alert if inside this (when awake)
        public float LoseRadius      = 165f; // leave Alert if beyond this

        // Wander (random patrol) around the spawn anchor
        private Vector2 _anchor;
        private Vector2 _wanderTarget;
        private float _repathTimer = 0f;
        private readonly Vector2 _wanderRange = new Vector2(90f, 60f);
        private readonly Random _rng = new Random();

        // State timers
        private float _patrolTimer = 0f;
        private float _awakeIdleTimer = 0f;

        // ===== Combat / collisions (circle) =====
        public float Radius = 18f;                // bumped to match bigger sprite
        public Vector2 CircleOffset = Vector2.Zero;
        public float DamageCooldownSeconds = 0.40f;
        private float _cooldown = 0f;

        // ===== Animation =====
        private enum AnimSet { Sleep, AwakeIdle, MoveUp, MoveDown, MoveLeft, MoveRight }
        private AnimSet _anim = AnimSet.Sleep;

        // Frame sets
        private readonly List<Rectangle> _sleepFrames     = new(); // 2
        private readonly List<Rectangle> _awakeFrames     = new(); // 3
        private readonly List<Rectangle> _moveUpFrames    = new(); // 3
        private readonly List<Rectangle> _moveDownFrames  = new(); // 3
        private readonly List<Rectangle> _moveLeftFrames  = new(); // 4
        private readonly List<Rectangle> _moveRightFrames = new(); // 4

        // Animation pacing
        public float IdleFrameTime = 0.38f;   // slower idle = more “breathing”
        public float MoveFrameTime = 0.14f;   // snappier move
        private int _frameIndex = 0;
        private float _timer = 0f;
        private float _frameTime = 0.38f;     // will be set per-state each tick

        // Velocity for choosing facing animation
        private Vector2 _vel = Vector2.Zero;

        // Debug draw
        private static Texture2D _pixel;

        public LilDevil(Texture2D tex, Vector2 pos)
            : base(tex, pos, GameRole.Enemy)
        {
            _anchor = pos;

            // === Sleeping IDLE (2) ===
            // Position1:(156,152) Size 27x22  | Position2:(203,150) Size 29x24
            _sleepFrames.Add(new Rectangle(156, 152, 27, 22));
            _sleepFrames.Add(new Rectangle(203, 150, 29, 24));

            // === Awake IDLE (3) ===
            // Awake1:(10,148) 27x27 | Awake2:(57,147) 29x21 | Awake3:(107,149) 26x25
            _awakeFrames.Add(new Rectangle(10, 148, 27, 27));
            _awakeFrames.Add(new Rectangle(57, 147, 29, 21));
            _awakeFrames.Add(new Rectangle(107, 149, 26, 25));

            // === Move Up (3) ===
            // Up1:(12,104) 26x22 | Up2:(59,104) 28x22 | Up3:(104,104) 24x22
            _moveUpFrames.Add(new Rectangle(12, 104, 26, 22));
            _moveUpFrames.Add(new Rectangle(59, 104, 28, 22));
            _moveUpFrames.Add(new Rectangle(104, 104, 24, 22));

            // === Move Down (3) ===
            // Down1:(12,8) 22x22 | Down2:(57,8) 27x22 | Down3:(106,8) 26x23
            _moveDownFrames.Add(new Rectangle(12, 8, 22, 22));
            _moveDownFrames.Add(new Rectangle(57, 8, 27, 22));
            _moveDownFrames.Add(new Rectangle(106, 8, 26, 23));

            // === Move Left (4) ===
            // Left1:(10,64) 32x22 | Left2:(58,67) 32x22 | Left3:(106,64) 31x25 | Left4:(154,64) 32x24
            _moveLeftFrames.Add(new Rectangle(10, 64, 32, 22));
            _moveLeftFrames.Add(new Rectangle(58, 67, 32, 22));
            _moveLeftFrames.Add(new Rectangle(106, 64, 31, 25));
            _moveLeftFrames.Add(new Rectangle(154, 64, 32, 24));

            // === Move Right (4) ===
            // Right1:(4,36) 33x24 | Right2:(53,34) 31x26 | Right3:(100,38) 32x22 | Right4:(148,36) 32x24
            _moveRightFrames.Add(new Rectangle(4, 36, 33, 24));
            _moveRightFrames.Add(new Rectangle(53, 34, 31, 26));
            _moveRightFrames.Add(new Rectangle(100, 38, 32, 22));
            _moveRightFrames.Add(new Rectangle(148, 36, 32, 24));

            // start asleep
            _anim = AnimSet.Sleep;
            ApplyAnimFrame();
            PickNewWanderTarget();
        }

        // ===== Public helpers for demo =====
        public void Kill()
        {
            if (State == DevilState.Dead) return;
            State = DevilState.Dead;
            Tint = new Color(170, 170, 170, 200);
        }

        public void Respawn(Vector2? at = null)
        {
            State = DevilState.SleepIdle;
            Tint = Color.White;
            _cooldown = 0f;
            Position = at ?? _anchor;
            _anchor  = Position;
            _awakeIdleTimer = 0f;
            _patrolTimer = 0f;
            _frameIndex = 0;
            _timer = 0f;
            _vel = Vector2.Zero;
            _anim = AnimSet.Sleep;
            ApplyAnimFrame();
            PickNewWanderTarget();
        }

        public void DebugToggleSleep()
        {
            if (State == DevilState.Dead) return;
            if (State == DevilState.SleepIdle) EnterAwakeIdle();
            else EnterSleepIdle();
        }

        public string StateLabel => State.ToString();

        // ===== Update =====
        public override void Update()
        {
            // animation tick
            _timer += Globals.TotalSeconds;
            if (_timer >= _frameTime)
            {
                _timer -= _frameTime;
                _frameIndex = (_frameIndex + 1) % CurrentFrames().Count;
                ApplyAnimFrame();
            }
        }

        public void UpdateAI(Vector2 playerPos, bool playerSneaking)
        {
            if (State == DevilState.Dead) return;

            // scale “game speed” just for this AI if desired
            float dt = Globals.TotalSeconds * AiTimeScale;

            float dist = Vector2.Distance(CircleCenter, playerPos);

            switch (State)
            {
                case DevilState.SleepIdle:
                {
                    float wakeR = playerSneaking ? WakeRadiusSneak : WakeRadiusWalk;
                    if (dist <= wakeR)
                    {
                        EnterAwakeIdle();
                        Globals.ShowToast("Lil Devil: *wakes*", 0.6f);
                    }
                    _vel = Vector2.Zero;
                    break;
                }

                case DevilState.AwakeIdle:
                {
                    _awakeIdleTimer += dt;

                    if (dist <= DetectionRadius)
                    {
                        EnterAlert();
                        Globals.ShowToast("Lil Devil: ALERT!", 0.7f);
                    }
                    else if (_awakeIdleTimer >= AwakeToSleepSeconds)
                    {
                        // Explicitly go to sleep after 10s, no patrol here
                        EnterSleepIdle();
                    }
                    // else: remain in AwakeIdle (no auto-patrol)
                    _vel = Vector2.Zero;
                    break;
                }

                case DevilState.Patrol:
                {
                    _patrolTimer += dt;

                    // random wandering
                    _repathTimer -= dt;
                    MoveTowards(_wanderTarget, MoveSpeed, dt);
                    if (Vector2.Distance(Position, _wanderTarget) <= 3f || _repathTimer <= 0f)
                        PickNewWanderTarget();

                    if (dist <= DetectionRadius)
                    {
                        EnterAlert();
                        Globals.ShowToast("Lil Devil: ALERT!", 0.7f);
                    }
                    else if (_patrolTimer >= PatrolToAwakeSeconds)
                    {
                        // after 15s of patrol, post up again in AwakeIdle
                        EnterAwakeIdle();
                    }
                    break;
                }

                case DevilState.Alert:
                {
                    MoveTowards(playerPos - FrameCenterOffset(), MoveSpeed * 1.15f, dt);

                    if (dist > LoseRadius)
                    {
                        // lose sight → back to patrol
                        EnterPatrol();
                        Globals.ShowToast("Lil Devil: patrol", 0.6f);
                    }
                    break;
                }
            }
        }

        // ===== Collision (circle vs player rect) =====
        public bool UpdateCollision(float dtSeconds, Rectangle playerBounds, Action onPlayerHit)
        {
            if (!Alive || State == DevilState.Dead) return false;

            if (_cooldown > 0f) _cooldown = Math.Max(0f, _cooldown - dtSeconds);

            if (_cooldown <= 0f && RectVsCircle(playerBounds, CircleCenter, Radius))
            {
                _cooldown = DamageCooldownSeconds;
                onPlayerHit?.Invoke();
                return true;
            }
            return false;
        }

        // ===== Debug rings =====
        public void DrawDebugCircle(SpriteBatch sb, Color? color = null, int segments = 36, float thickness = 2f)
            => DrawRing(sb, CircleCenter, Radius, color ?? new Color(255, 0, 0, 190), segments, thickness);

        public void DrawDebugVision(SpriteBatch sb, int segments = 40, float thickness = 2f)
            => DrawRing(sb, CircleCenter, DetectionRadius, new Color(0, 255, 0, 80), segments, thickness);

        public void DrawDebugWake(SpriteBatch sb, bool playerSneaking, int segments = 40, float thickness = 2f)
        {
            float r = playerSneaking ? WakeRadiusSneak : WakeRadiusWalk;
            DrawRing(sb, CircleCenter, r, new Color(0, 200, 255, 80), segments, thickness); // cyan
        }

        private void DrawRing(SpriteBatch sb, Vector2 center, float radius, Color color, int segments, float thickness)
        {
            if (_pixel == null)
            {
                _pixel = new Texture2D(sb.GraphicsDevice, 1, 1);
                _pixel.SetData(new[] { Color.White });
            }
            float step = MathHelper.TwoPi / segments;
            Vector2 prev = center + new Vector2(radius, 0f);
            for (int i = 1; i <= segments; i++)
            {
                float t = i * step;
                Vector2 curr = center + new Vector2((float)Math.Cos(t), (float)Math.Sin(t)) * radius;
                DrawLine(sb, prev, curr, color, thickness);
                prev = curr;
            }
        }

        private void DrawLine(SpriteBatch sb, Vector2 a, Vector2 b, Color col, float thick)
        {
            if (_pixel == null)
            {
                _pixel = new Texture2D(sb.GraphicsDevice, 1, 1);
                _pixel.SetData(new[] { Color.White });
            }
            Vector2 d = b - a;
            float len = d.Length();
            if (len < 0.5f) return;
            float rot = (float)Math.Atan2(d.Y, d.X);

            sb.Draw(_pixel,
                destinationRectangle: new Rectangle((int)a.X, (int)a.Y, (int)len, (int)thick),
                sourceRectangle: null, color: col, rotation: rot, origin: Vector2.Zero,
                effects: SpriteEffects.None, layerDepth: 0f);
        }

        // ===== State helpers =====
        private void EnterAwakeIdle()
        {
            State = DevilState.AwakeIdle;
            _awakeIdleTimer = 0f;
            _patrolTimer = 0f;
            _frameIndex = 0;
            _timer = 0f;
            _vel = Vector2.Zero;
            _anim = AnimSet.AwakeIdle;
            ApplyAnimFrame();
        }

        private void EnterSleepIdle()
        {
            State = DevilState.SleepIdle;
            _awakeIdleTimer = 0f;
            _patrolTimer = 0f;
            _frameIndex = 0;
            _timer = 0f;
            _vel = Vector2.Zero;
            _anim = AnimSet.Sleep;
            ApplyAnimFrame();
        }

        private void EnterPatrol()
        {
            State = DevilState.Patrol;
            _patrolTimer = 0f;
            PickNewWanderTarget();
            // anim will swap to a Move* set once velocity is non-zero
        }

        private void EnterAlert()
        {
            State = DevilState.Alert;
            // anim will swap to a Move* set once velocity is non-zero
        }

        private void PickNewWanderTarget()
        {
            float dx = (float)(_rng.NextDouble() * 2 - 1) * _wanderRange.X;
            float dy = (float)(_rng.NextDouble() * 2 - 1) * _wanderRange.Y;
            _wanderTarget = _anchor + new Vector2(dx, dy);
            _repathTimer = 1.2f + (float)_rng.NextDouble() * 1.4f;
        }

        private void MoveTowards(Vector2 target, float speed, float dt)
        {
            Vector2 to = target - Position;
            if (to.LengthSquared() < 0.25f)
            {
                _vel = Vector2.Zero;
                return;
            }
            to.Normalize();
            _vel = to * speed;
            Position += _vel * dt;
            ChooseMoveAnimByVelocity();
        }

        private void ChooseMoveAnimByVelocity()
        {
            if (_vel.LengthSquared() < 1e-4f) return;

            // choose dominant axis for facing
            var v = _vel;
            if (Math.Abs(v.X) >= Math.Abs(v.Y))
                _anim = (v.X >= 0f) ? AnimSet.MoveRight : AnimSet.MoveLeft;
            else
                _anim = (v.Y >= 0f) ? AnimSet.MoveDown : AnimSet.MoveUp;

            _frameTime = MoveFrameTime;
            ApplyAnimFrame();
        }

        private Vector2 FrameCenterOffset()
        {
            var f = Source ?? new Rectangle(0, 0, Sprite?.Width ?? 0, Sprite?.Height ?? 0);
            return new Vector2(f.Width * 0.5f, f.Height * 0.5f);
        }

        public Vector2 CircleCenter
        {
            get
            {
                var f = Source ?? new Rectangle(0, 0, Sprite?.Width ?? 0, Sprite?.Height ?? 0);
                return new Vector2(
                    (int)Math.Floor(Position.X + f.Width  * 0.5f),
                    (int)Math.Floor(Position.Y + f.Height * 0.5f)
                ) + CircleOffset;
            }
        }

        private static bool RectVsCircle(Rectangle r, Vector2 c, float radius)
        {
            float nx = MathHelper.Clamp(c.X, r.Left,  r.Right);
            float ny = MathHelper.Clamp(c.Y, r.Top,   r.Bottom);
            float dx = c.X - nx;
            float dy = c.Y - ny;
            return dx*dx + dy*dy <= radius * radius;
        }

        private List<Rectangle> CurrentFrames()
        {
            return _anim switch
            {
                AnimSet.Sleep      => _sleepFrames,
                AnimSet.AwakeIdle  => _awakeFrames,
                AnimSet.MoveUp     => _moveUpFrames,
                AnimSet.MoveDown   => _moveDownFrames,
                AnimSet.MoveLeft   => _moveLeftFrames,
                AnimSet.MoveRight  => _moveRightFrames,
                _                  => _awakeFrames
            };
        }

        private void ApplyAnimFrame()
        {
            var frames = CurrentFrames();
            if (frames.Count == 0) return;
            if (_frameIndex >= frames.Count) _frameIndex = 0;
            Source = frames[_frameIndex];

            // set pacing per state
            _frameTime = (_anim == AnimSet.Sleep || _anim == AnimSet.AwakeIdle) ? IdleFrameTime : MoveFrameTime;
        }
    }
}
