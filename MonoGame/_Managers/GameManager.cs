using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace MonoGame
{
    public class GameManager
    {
        // --- Player + world ---
        private PlayerActions _actions;
        private ToeJam _toejam;
        private readonly List<GameObject> _world = new();

        // Ranged shooter (tomatoes/slingshot)
        private RangedController _ranged;

        // UI helpers
        private SpriteFont _font;
        private Texture2D _white;

        // === Simple inventory (list message) ===
        private bool _invOpen = false;
        private int _invIndex = 0;
        private KeyboardState _prevKb = Keyboard.GetState();

        // === PRESENT INVENTORY ===
        private readonly List<(int id, int count)> _presentInv = new();

        // Timed power (for things like Hi-Tops)
        private string _activePower = null;
        private float _powerTimer = 0f;
        private int _bigBucks = 0;

        // --- Facing / math debug ---
        private Vector2 _playerFacing = new(1f, 0f);
        private bool _facingEnemy = false;
        private float _facingDot = 0f;
        private bool _prevFacingEnemy;
        private float _prevDot;
        private int _facingSide = 0;
        private float _crossZ = 0f;
        private int _prevFacingSide;
        private float _prevCross;

        // --- Nearest item ---
        private GameObject _nearestItem = null;
        private float _nearestItemDist = float.PositiveInfinity;
        private const float PICKUP_RADIUS = 28f;

        // Debug HUD toggle
        private bool _debugHUD = false;

        // Enemies
        private Enemy _tornado;
        private LilDevil _lilDevil;

        public void Init(GraphicsDevice gd)
        {
            // Player
            var toeJamTexture = Globals.Content.Load<Texture2D>("ToeJam_Transparent");
            _toejam = new ToeJam(0, toeJamTexture);

            // Actions router
            _actions = new PlayerActions();

            // Ranged controller (projectile system)
            _ranged = new RangedController(_toejam);

            // 1x1 white + font
            _white = new Texture2D(gd, 1, 1);
            _white.SetData(new[] { Color.White });
            _font = Globals.Content.Load<SpriteFont>("UIFont");

            // ---- textures ----
            var texHud     = Globals.Content.Load<Texture2D>("HUD_Display");
            var texElevator= Globals.Content.Load<Texture2D>("Elevator(1)");
            var texTornado = Globals.Content.Load<Texture2D>("Tornado");
            var texLilDevil= Globals.Content.Load<Texture2D>("Lil_Devil");
            var texLemon   = Globals.Content.Load<Texture2D>("LemonadeStand");
            var texItems   = Globals.Content.Load<Texture2D>("Items_Transparent");
            var texFloor   = Globals.Content.Load<Texture2D>("floor_path_tiles");

            // --- Audio ---
            AudioManager.Init(Globals.Content);
            AudioManager.StartBgm();

            // Spinning present demo
            var presentSrc = new Rectangle(2, 6, 25, 18);
            var presentHiTops = new Present(texItems, new Vector2(240, 160), presentSrc, id: 5)
            {
                Scale = 2f,
                AngularVelocity = 3.0f
            };
            _world.Add(presentHiTops);

            // World objects
            _world.Add(new GameObject(texLemon, new Vector2(300, 120), GameRole.NPC,
                                      new Rectangle(8, 8, 67, 60)));

            // Drawn tornado sprite (visual only)
            var tornadoPos = new Vector2(360, 120);
            var tornadoSrc = new Rectangle(152, 57, 34, 33);
            _world.Add(new GameObject(texTornado, tornadoPos, GameRole.Enemy, tornadoSrc));

            // Tornado hitbox
            _tornado = new Enemy("Tornado", position: tornadoPos, size: new Point(tornadoSrc.Width, tornadoSrc.Height))
            {
                DamageCooldownSeconds = 0.40f,
                ShrinkX = 4,
                ShrinkY = 6,
                HitboxOffset = new Point(0, -2)
            };

            // Lil Devil (animated circle enemy)
            _lilDevil = new LilDevil(texLilDevil, new Vector2(720, 120))
            {
                Scale = 1.25f,
                Radius = 18f,
                DamageCooldownSeconds = 0.40f,
                CircleOffset = new Vector2(0, 0),
                AiTimeScale = 1f
            };

            // Presents placed in world (examples)
            _world.Add(new Present(texItems, new Vector2(420, 120), new Rectangle(4, 39, 25, 18), id: 1)); // Decoy
            _world.Add(new Present(texItems, new Vector2(520, 120), new Rectangle(4, 39, 25, 18), id: 1)); // Decoy
            var hiTopsSrc = new Rectangle(2, 6, 25, 18);
            _world.Add(new Present(texItems, new Vector2(640, 220), hiTopsSrc, id: 5) { Scale = 1f });

            _world.Add(new GameObject(texElevator, new Vector2(480, 104), GameRole.Elevator,
                                      new Rectangle(2, 3, 38, 59)));
            _world.Add(new GameObject(texHud, new Vector2(0, 768 - 33), GameRole.UI,
                                      new Rectangle(8, 87, 319, 33)));

            // Tiles 3×3
            var tileSrc = new Rectangle(64, 0, 64, 64);
            AddTileSection(texFloor, tileSrc, new Vector2(600, 420), cols: 3, rows: 3, tileSize: 64, tint: Color.White);

            // --- Seed starting presents: 3x Tomatoes, 1x Slingshot ---
            int tomId = FindPresentIdByName("Tomatoes");
            if (tomId > 0) AddPresentToInventory(tomId, 3);
            int slingId = FindPresentIdByName("Slingshot");
            if (slingId > 0) AddPresentToInventory(slingId, 1);
        }

        private static int FindPresentIdByName(string name)
        {
            var names = PresentRegistry.DisplayNames;
            for (int i = 0; i < names.Length; i++)
                if (names[i] == name) return i;
            return -1;
        }

        private void AddTileSection(Texture2D texture, Rectangle? source, Vector2 origin,
                                    int cols, int rows, int tileSize, Color tint)
        {
            for (int y = 0; y < rows; y++)
                for (int x = 0; x < cols; x++)
                {
                    _world.Add(new GameObject(texture, origin + new Vector2(x * tileSize, y * tileSize),
                                              GameRole.Tile, source)
                    {
                        Size = new Point(tileSize, tileSize),
                        Tint = tint
                    });
                }
        }

        public void Update()
        {
            // Input
            InputManager.Update();

            // Actions (dt) + sneak binding
            _actions.Update(Globals.TotalSeconds);
            _toejam.SetSneak(_actions.IsSneaking);

            // Volume keys
            if (InputManager.VolumeDownPressed)
            {
                AudioManager.NudgeGlobalVolume(-0.1f);
                Globals.ShowToast($"Vol  SFX:{AudioManager.MasterSfxVolume:0.00}  BGM:{AudioManager.BgmVolume:0.00}", 0.8f);
            }
            if (InputManager.VolumeUpPressed)
            {
                AudioManager.NudgeGlobalVolume(+0.1f);
                Globals.ShowToast($"Vol  SFX:{AudioManager.MasterSfxVolume:0.00}  BGM:{AudioManager.BgmVolume:0.00}", 0.8f);
            }

            AudioManager.Update();

            var kb = Keyboard.GetState();

            // Wakeup chain demo
            bool cNow = kb.IsKeyDown(Keys.C), cPrev = _prevKb.IsKeyDown(Keys.C);
            if (cNow && !cPrev) { AudioManager.StartChainWakeup(); Globals.ShowToast("Chain: wakeup -> WAKEUP!", 0.8f); }

            // Track last non-zero facing
            if (InputManager.Moving) _playerFacing = Vector2.Normalize(InputManager.Direction);

            // Nearest enemy for dot/cross debug
            GameObject nearestEnemy = null;
            float bestD2 = float.MaxValue;
            foreach (var o in _world)
            {
                if (o.Role != GameRole.Enemy) continue;
                float d2 = Vector2.DistanceSquared(o.Position, _toejam.Position);
                if (d2 < bestD2) { bestD2 = d2; nearestEnemy = o; }
            }

            if (nearestEnemy != null)
            {
                _facingEnemy = IsFacingTarget(_toejam.Position, _playerFacing, nearestEnemy.Position, out _facingDot, 0f);
                _facingSide  = SideOfFacing(_toejam.Position, _playerFacing, nearestEnemy.Position, out _crossZ);

                bool changedFacing = (_facingEnemy != _prevFacingEnemy) || (System.Math.Abs(_facingDot - _prevDot) > 0.05f);
                bool changedSide   = (_facingSide  != _prevFacingSide)  || (System.Math.Abs(_crossZ - _prevCross) > 0.05f);
                if (changedFacing || changedSide)
                {
                    System.Diagnostics.Debug.WriteLine($"Facing={_facingEnemy} dot={_facingDot:0.00} side={SideLabel(_facingSide)} crossZ={_crossZ:0.00}");
                    _prevFacingEnemy = _facingEnemy; _prevDot = _facingDot;
                    _prevFacingSide  = _facingSide;  _prevCross = _crossZ;
                }
            }
            else { _facingEnemy = false; _facingDot = 0f; _facingSide = 0; _crossZ = 0f; }

            // Nearest item for pickup
            _nearestItem = null; _nearestItemDist = float.PositiveInfinity;
            foreach (var o in _world)
            {
                if (o.Role != GameRole.Item) continue;
                float d = Vector2.Distance(_toejam.Position + CenterOf(o), o.Position + CenterOf(o));
                if (d < _nearestItemDist) { _nearestItemDist = d; _nearestItem = o; }
            }

            // Auto-pickup presents
            if (_nearestItem is Present nearP && _nearestItemDist <= PICKUP_RADIUS)
            {
                AddPresentToInventory(nearP.Id, 1);
                Globals.ShowToast($"Picked Present{nearP.Id}: {PresentRegistry.GetLabel(nearP.Id)}", 1.2f);
                nearP.Alive = false;
            }

            // Inventory toggle
            if (InputManager.BPressed) _invOpen = !_invOpen;

            // Debug HUD toggle
            bool f3Now = kb.IsKeyDown(Keys.F3), f3Prev = _prevKb.IsKeyDown(Keys.F3);
            if (f3Now && !f3Prev) _debugHUD = !_debugHUD;

            // Inventory modal (freeze world)
            if (_invOpen)
            {
                bool leftNow = kb.IsKeyDown(Keys.Left),  leftPrev  = _prevKb.IsKeyDown(Keys.Left);
                bool rightNow= kb.IsKeyDown(Keys.Right), rightPrev = _prevKb.IsKeyDown(Keys.Right);
                if (leftNow && !leftPrev && _presentInv.Count > 0)  _invIndex = (_invIndex - 1 + _presentInv.Count) % _presentInv.Count;
                if (rightNow && !rightPrev && _presentInv.Count > 0) _invIndex = (_invIndex + 1) % _presentInv.Count;

                if (InputManager.APressed && _presentInv.Count > 0) UseSelectedPresent();

                _prevKb = kb;
                return;
            }

            if (Globals.Paused) { _prevKb = kb; return; }

            // Enemy collision → hurt sound
            Rectangle playerBounds = GetPlayerBounds();
            _tornado?.Update(Globals.TotalSeconds, playerBounds, () =>
            {
                AudioManager.PlayHurt();
                Globals.ShowToast("Ouch!", 0.7f);
            });

            // --- Ranged controller: wind-up, fire, projectile updates, hit detection ---
            _ranged.Update(_actions, _playerFacing, _lilDevil);

            if (_ranged.ConsumeDepletedFlag())
            {
                _actions.RevertToDefault();
                Globals.ShowToast("Out of ammo - back to Sneak", 1.0f);
            }

            // Lil Devil AI + collision
            _lilDevil?.Update();
            _lilDevil?.UpdateAI(_toejam.Position, _actions.IsSneaking);
            _lilDevil?.UpdateCollision(Globals.TotalSeconds, GetPlayerBounds(), () =>
            {
                AudioManager.PlayHurt();
                Globals.ShowToast("Ouch! Lil Devil!", 0.7f);
            });

            // DEBUG keys
            bool kNow = kb.IsKeyDown(Keys.K), kPrev = _prevKb.IsKeyDown(Keys.K);
            if (kNow && !kPrev && _lilDevil != null) { _lilDevil.Kill(); Globals.ShowToast("Lil Devil: DEAD", 0.7f); }
            bool rNow = kb.IsKeyDown(Keys.R), rPrev = _prevKb.IsKeyDown(Keys.R);
            if (rNow && !rPrev && _lilDevil != null) { _lilDevil.Respawn(); Globals.ShowToast("Lil Devil: respawn", 0.6f); }

            // Time scale debug
            bool f7Now = kb.IsKeyDown(Keys.F7), f7Prev = _prevKb.IsKeyDown(Keys.F7);
            bool f8Now = kb.IsKeyDown(Keys.F8), f8Prev = _prevKb.IsKeyDown(Keys.F8);
            bool f9Now = kb.IsKeyDown(Keys.F9), f9Prev = _prevKb.IsKeyDown(Keys.F9);
            if (f7Now && !f7Prev) { Globals.NudgeTimeScale(-0.1f); Globals.ShowToast($"TimeScale: {Globals.TimeScale:0.00}", 0.8f); }
            if (f8Now && !f8Prev) { Globals.NudgeTimeScale(+0.1f); Globals.ShowToast($"TimeScale: {Globals.TimeScale:0.00}", 0.8f); }
            if (f9Now && !f9Prev) { Globals.SetTimeScale(1f); Globals.ShowToast("TimeScale: 1.00", 0.8f); }

            // Tick timed power
            if (_activePower != null)
            {
                _powerTimer -= Globals.TotalSeconds;
                if (_powerTimer <= 0f) EndActivePower();
            }

            // Normal gameplay
            _toejam.Update();
            foreach (var o in _world) o.Update();

            // Cleanup
            for (int i = _world.Count - 1; i >= 0; i--)
                if (!_world[i].Alive) _world.RemoveAt(i);

            _prevKb = kb;
        }

        // Center helper
        private static Vector2 CenterOf(GameObject o)
        {
            var src = o.Source ?? new Rectangle(0, 0, o.Sprite.Width, o.Sprite.Height);
            return new Vector2(src.Width / 2f, src.Height / 2f);
        }

        // Rough ToeJam bounds (tune if needed)
        private Rectangle GetPlayerBounds()
        {
            const int baseW = 32, baseH = 44;
            const int shrinkX = 4, shrinkY = 6;
            const int offsetX = -2, offsetY = -3;
            var r = new Rectangle((int)_toejam.Position.X, (int)_toejam.Position.Y, baseW, baseH);
            r.Inflate(-shrinkX, -shrinkY);
            r.Offset(offsetX, offsetY);
            return r;
        }

        // === Inventory helpers ===
        private void AddPresentToInventory(int id, int amount)
        {
            int i = _presentInv.FindIndex(s => s.id == id);
            if (i >= 0) _presentInv[i] = (id, _presentInv[i].count + amount);
            else _presentInv.Add((id, amount));
            if (_invIndex >= _presentInv.Count) _invIndex = _presentInv.Count - 1;
        }

        private void UseSelectedPresent()
        {
            if (_presentInv.Count == 0) return;
            var (id, count) = _presentInv[_invIndex];

            bool firstTime = !PresentRegistry.Identified[id];
            PresentRegistry.Identified[id] = true;

            string name = PresentRegistry.DisplayNames[id];
            if (firstTime) Globals.ShowToast($"Identified: {name}", 1.4f);
            else Globals.ShowToast($"Opened: {name}", 1.1f);

            switch (name)
            {
                case "Hi-Tops":
                    StartPower("Hi-Tops", seconds: 10f, speedMult: 1.75f);
                    break;

                case "Decoy":
                {
                    var decoy = Powerup.CreateDecoy(_toejam.Texture, _toejam.Position, lifeSeconds: 6f);
                    decoy.Scale = _toejam.Scale;
                    _world.Add(decoy);
                    break;
                }

                case "Big Bucks":
                    _bigBucks += 25;
                    Globals.ShowToast($"+25 Big Bucks (total: {_bigBucks})", 1.2f);
                    break;

                case "Tomatoes":
                {
                    _actions.SetMode(ActionModeKind.PressFire);
                    _toejam.SetSneak(false);
                    // Slower ready + longer thrown hold
                    _ranged.Equip(
                        ProjectileKind.Tomato,
                        uses: 5,
                        prepA: 0.25f,   // was ~0.18
                        prepB: 0f,
                        speed: 260f,
                        range: 300f,
                        damage: 1,
                        thrownHold: 0.22f
                    );
                    Globals.ShowToast("Tomatoes: 5 shots (Z to fire)", 1.2f);
                    break;
                }

                case "Slingshot":
                {
                    _actions.SetMode(ActionModeKind.PressFire);
                    _toejam.SetSneak(false);
                    // Two-stage prep (ready1, ready2) then release; faster & stronger tomato
                    _ranged.Equip(ProjectileKind.Slingshot, uses: 10, prepA: 0.16f, prepB: 0.12f,
                                  speed: 360f, range: 420f, damage: 2);
                    Globals.ShowToast("Slingshot: 10 shots (Z to fire)", 1.2f);
                    break;
                }

                default:
                    Globals.ShowToast($"Opened: {name} (effect TBD)", 1.2f);
                    break;
            }

            // Consume one
            count--;
            if (count <= 0) _presentInv.RemoveAt(_invIndex);
            else _presentInv[_invIndex] = (id, count);

            if (_invIndex >= _presentInv.Count) _invIndex = _presentInv.Count - 1;
            if (_invIndex < 0) _invIndex = 0;
        }

        // Facing helpers
        private static bool IsFacingTarget(Vector2 pos, Vector2 facing, Vector2 target, out float dotOut, float minDot = 0f)
        {
            dotOut = 0f;
            if (facing.LengthSquared() < 1e-6f) return false;
            Vector2 toTarget = target - pos;
            if (toTarget.LengthSquared() < 1e-6f) { dotOut = 1f; return true; }
            facing = Vector2.Normalize(facing);
            toTarget = Vector2.Normalize(toTarget);
            float dot = Vector2.Dot(facing, toTarget);
            dotOut = dot; return dot > minDot;
        }

        private static int SideOfFacing(Vector2 pos, Vector2 facing, Vector2 target, out float crossOut, float eps = 1e-6f)
        {
            crossOut = 0f;
            if (facing.LengthSquared() < 1e-6f) return 0;
            Vector2 toTarget = target - pos;
            if (toTarget.LengthSquared() < eps) return 0;
            facing = Vector2.Normalize(facing);
            toTarget = Vector2.Normalize(toTarget);
            float z = facing.X * toTarget.Y - facing.Y * toTarget.X;
            crossOut = z;
            if (z > eps) return +1;
            if (z < -eps) return -1;
            return 0;
        }

        private static string SideLabel(int s) => s > 0 ? "Right" : (s < 0 ? "Left" : "On");

        private void StartPower(string name, float seconds, float speedMult)
        {
            _activePower = name;
            _powerTimer = seconds;
            _toejam.SpeedMultiplier = speedMult;
            Globals.ShowToast($"{name} ON ({(int)seconds}s)", 1.2f);
        }

        private void EndActivePower()
        {
            _toejam.SpeedMultiplier = 1f;
            Globals.ShowToast($"{_activePower} wore off", 1.2f);
            _activePower = null;
            _powerTimer = 0f;
        }

        public void Draw()
        {
            // Tiles
            foreach (var o in _world) if (o.Role == GameRole.Tile) o.Draw();

            // World objects & enemies (not UI)
            foreach (var o in _world) if (o.Role != GameRole.Tile && o.Role != GameRole.UI) o.Draw();

            // Ammo HUD (shows whenever a ranged present is active)
            if (_font != null && _ranged.Active)
            {
                string ammo = $"{_ranged.Kind} Ammo: {_ranged.Ammo}/{_ranged.AmmoMax}";
                var pos  = new Vector2(12, 50);
                var size = _font.MeasureString(ammo);
                var rect = new Rectangle(
                    (int)(pos.X - 8), (int)(pos.Y - 6),
                    (int)(size.X + 16), (int)(size.Y + 12)
                );
                Globals.SpriteBatch.Draw(_white, rect, new Color(0, 0, 0, 140));
                Globals.SpriteBatch.DrawString(_font, ammo, pos, Color.White);
            }

            // Projectiles (from controller)
            _ranged.Draw();
            

            // Lil Devil
            _lilDevil?.Draw();

            // Player
            _toejam.Draw();

            // UI
            foreach (var o in _world) if (o.Role == GameRole.UI) o.Draw();

            // Toast
            if (_font != null && Globals.MenuToastTimer > 0f && !string.IsNullOrEmpty(Globals.MenuToastText))
            {
                var vp = Globals.SpriteBatch.GraphicsDevice.Viewport;
                var text = Globals.MenuToastText;
                var size = _font.MeasureString(text);
                var pos = new Vector2((vp.Width - size.X) / 2f, 10f);

                var pad = new Vector2(8, 4);
                var rect = new Rectangle(
                    (int)System.Math.Floor(pos.X - pad.X),
                    (int)System.Math.Floor(pos.Y - pad.Y),
                    (int)System.Math.Ceiling(size.X + pad.X * 2),
                    (int)System.Math.Ceiling(size.Y + pad.Y * 2)
                );
                Globals.SpriteBatch.Draw(_white, rect, new Color(0, 0, 0, 170));
                Globals.SpriteBatch.DrawString(_font, text, pos, Color.White);
            }

            // Inventory overlay
            if (_invOpen && _font != null)
            {
                var vp = Globals.SpriteBatch.GraphicsDevice.Viewport;
                var box = new Rectangle(vp.Width / 2 - 300, vp.Height - 170, 600, 130);
                Globals.SpriteBatch.Draw(_white, box, new Color(0, 0, 0, 190));

                var y = box.Y + 12;
                var x = box.X + 14;

                if (_presentInv.Count == 0)
                {
                    Globals.SpriteBatch.DrawString(_font, "No presents", new Vector2(x, y), Color.White);
                }
                else
                {
                    for (int i = 0; i < _presentInv.Count; i++)
                    {
                        var (id, count) = _presentInv[i];
                        string label = PresentRegistry.GetLabel(id);
                        string line = (i == _invIndex)
                            ? $"> Present{id}: {label} x{count} <"
                            : $"Present{id}: {label} x{count}";
                        Globals.SpriteBatch.DrawString(_font, line, new Vector2(x, y), Color.White);
                        y += 20;
                    }
                }

                Globals.SpriteBatch.DrawString(_font, "Z: Use (identify/open)   X: Close   Left/Right: Select",
                    new Vector2(box.X + 14, box.Bottom - 24), Color.White);
            }

            // PAUSED overlay
            if (Globals.Paused && _font != null)
            {
                const string ptext = "PAUSED";
                var vp = Globals.SpriteBatch.GraphicsDevice.Viewport;
                var size = _font.MeasureString(ptext);
                var pos  = new Vector2((vp.Width - size.X) / 2f, (vp.Height - size.Y) / 2f);

                var pad = new Vector2(12, 6);
                var rect = new Rectangle(
                    (int)System.Math.Floor(pos.X - pad.X),
                    (int)System.Math.Floor(pos.Y - pad.Y),
                    (int)System.Math.Ceiling(size.X + pad.X * 2),
                    (int)(System.Math.Ceiling(size.Y + pad.Y * 2))
                );
                Globals.SpriteBatch.Draw(_white, rect, new Color(0, 0, 0, 180));
                Globals.SpriteBatch.DrawString(_font, ptext, pos, Color.White);
            }

            // Debug HUD
            if (_debugHUD && _font != null)
            {
                string itemLine = _nearestItem != null
                    ? $"nearest item: {_nearestItemDist:0.0} px (auto<={PICKUP_RADIUS})"
                    : "nearest item: none";

                string dbg = $"Facing: {_facingEnemy}\n" +
                             $"dot: {_facingDot:0.00}\n" +
                             $"side: {SideLabel(_facingSide)} (crossZ: {_crossZ:0.00})\n" +
                             itemLine +
                             $"\nVol SFX:{AudioManager.MasterSfxVolume:0.00}  BGM:{AudioManager.BgmVolume:0.00}" +
                             $"\nMode={_actions.Mode} Sneak={_actions.IsSneaking}" +
                             $"\nLil Devil: state={_lilDevil?.StateLabel ?? "n/a"}";

                var pos = new Vector2(12, 12);
                var size = _font.MeasureString(dbg);
                var rect = new Rectangle((int)(pos.X - 8), (int)(pos.Y - 6),
                                         (int)(size.X + 16), (int)(size.Y + 12));
                Globals.SpriteBatch.Draw(_white, rect, new Color(0, 0, 0, 160));
                Globals.SpriteBatch.DrawString(_font, dbg, pos, Color.White);

                // Visual debugs
                var pb = GetPlayerBounds();
                Globals.SpriteBatch.Draw(_white, pb, new Color(0, 255, 0, 80));
                _tornado?.DrawDebug(Globals.SpriteBatch);

                _lilDevil?.DrawDebugWake(Globals.SpriteBatch, _actions.IsSneaking);
                _lilDevil?.DrawDebugVision(Globals.SpriteBatch);
                _lilDevil?.DrawDebugCircle(Globals.SpriteBatch);
            }
        }
    }
}
