using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace MonoGame
{
    public class RangedController
    {
        private readonly ToeJam _tj;

        private readonly List<Projectile> _shots = new();

        private ProjectileKind _kind = ProjectileKind.Tomato;
        private int _ammo = 0, _ammoMax = 0;

        private float _prepA = 0f;     // stage1 hold
        private float _prepB = 0f;     // stage2 hold (slingshot)
        private float _thrownHold = 0f; // how long we show the “thrown” pose

        private float _speed = 260f;
        private float _range = 300f;
        private int   _damage = 1;

        // firing state
        private bool _isFiring = false;
        private int  _stage = 0; // 0 none, 1=ready1, 2=ready2
        private float _timer = 0f;

        // one-shot flag so GameManager can revert to sneak cleanly
        private bool _depletedThisFrame = false;

        public RangedController(ToeJam tj) { _tj = tj; }

        public bool   Active      => _ammo > 0;
        public int    Ammo        => _ammo;
        public int    AmmoMax     => _ammoMax;
        public ProjectileKind Kind=> _kind;

        public void Equip(ProjectileKind kind, int uses, float prepA, float prepB, float speed, float range, int damage, float thrownHold = 0.20f)
        {
            _kind = kind;
            _ammo = _ammoMax = uses;

            _prepA = prepA;
            _prepB = prepB;
            _thrownHold = thrownHold;

            _speed = speed;
            _range = range;
            _damage = damage;

            _isFiring = false;
            _stage = 0;
            _timer = 0f;
            _depletedThisFrame = false;
            _shots.Clear();
        }

        public bool ConsumeDepletedFlag()
        {
            if (!_depletedThisFrame) return false;
            _depletedThisFrame = false;
            return true;
        }

        public void Update(PlayerActions actions, Vector2 facing, LilDevil target)
        {
            // Update existing projectiles
            for (int i = _shots.Count - 1; i >= 0; i--)
            {
                var p = _shots[i];
                p.Update();

                if (target != null && target.TryHit(p.Position, p.Radius, p.Damage))
                {
                    p.Alive = false;
                    AudioManager.PlaySmoosh();
                    Globals.ShowToast("Hit!", 0.6f);
                }

                if (!p.Alive && p.ReachedMax) AudioManager.PlaySmoosh();
                if (!p.Alive) _shots.RemoveAt(i);
            }

            // Only handle fire logic when actions are in Press/Fire
            if (actions.Mode != ActionModeKind.PressFire) return;

            // Start windup on press
            if (!_isFiring && actions.FireJustPressed && _ammo > 0)
            {
                _isFiring = true;
                _stage = 1;
                _timer = _prepA;
                _tj.SetFrozen(true);

                if (_kind == ProjectileKind.Slingshot)
                    _tj.ShowSlingReady1(facing, _prepA);
                else
                    _tj.ShowTomatoReady(facing, _prepA);
            }

            if (!_isFiring) return;

            _timer -= Globals.TotalSeconds;

            if (_stage == 1 && _timer <= 0f)
            {
                if (_kind == ProjectileKind.Slingshot && _prepB > 0f)
                {
                    // go to stage 2
                    _stage = 2;
                    _timer = _prepB;
                    _tj.ShowSlingReady2(facing, _prepB);
                }
                else
                {
                    Fire(facing);
                }
            }
            else if (_stage == 2 && _timer <= 0f)
            {
                Fire(facing);
            }
        }

        private void Fire(Vector2 facing)
        {
            // Release!
            _isFiring = false;
            _stage = 0;
            _tj.SetFrozen(false);

            // Show “thrown” pose slightly longer so it reads
            if (_kind == ProjectileKind.Slingshot)
                _tj.ShowSlingRelease(facing, _thrownHold);
            else
                _tj.ShowTomatoRelease(facing, _thrownHold);

            // Spawn tomato projectile (same sprite for both kinds)
            var dir = facing.LengthSquared() > 1e-6f ? Vector2.Normalize(facing) : new Vector2(1, 0);

            // start near player center-ish
            var start = new Vector2((int)(_tj.Position.X + 16), (int)(_tj.Position.Y + 16) - 6);
            var vel = dir * _speed;

            var proj = new Projectile(_tj.Texture, ToeJam.TomatoProjectile, start, vel, _range, _kind)
            {
                Damage = _damage,
                Radius = (_kind == ProjectileKind.Slingshot) ? 6.5f : 6f,
                Scale  = 1f
            };
            _shots.Add(proj);

            AudioManager.PlayTomatoLaunch();

            // Ammo
            _ammo--;
            if (_ammo <= 0)
            {
                _depletedThisFrame = true; // GameManager will switch back to sneak
            }
        }

        public void Draw()
        {
            foreach (var p in _shots) p.Draw();
        }
    }
}
