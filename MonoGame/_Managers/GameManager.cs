using System.Collections.Generic;


namespace MonoGame
{
    public class GameManager
    {
        private ToeJam _toejam;
        private readonly List<GameObject> _world = new();

        // UI helpers
        private SpriteFont _font;    // on-screen text
        private Texture2D _white;    // 1x1 white for translucent rects

        // === Simple inventory (list message) ===
        private bool _invOpen = false;
        private int _invIndex = 0;
        private KeyboardState _prevKb = Keyboard.GetState();

        // Can rename/expand this freely
        private readonly List<(string name, int count)> _inv = new()
        {
            ("Decoy", 3), ("Hi-Tops", 2), ("Tomatoes", 5)
        };

        // Equipped + active timed power
        private string _equipped = null;     // present name equipped from inventory
        private string _activePower = null;  // e.g., "Hi-Tops"
        private float _powerTimer = 0f;      // seconds remaining

        // --- Part 2: Facing check (dot product) ---
        private Vector2 _playerFacing = new(1f, 0f);  // last non-zero input direction
        private bool _facingEnemy = false;            // result for the nearest enemy
        private float _facingDot = 0f;                // for debugging/threshold tuning
        private bool _prevFacingEnemy;                // last printed "facing" state
        private float _prevDot;                       // last printed dot value

        // Debug HUD toggle (F3)
        private bool _debugHUD = false;

        public void Init(GraphicsDevice gd)
        {
            // Player
            var toeJamTexture = Globals.Content.Load<Texture2D>("ToeJam_Transparent");
            _toejam = new ToeJam(0, toeJamTexture);

            // 1x1 white
            _white = new Texture2D(gd, 1, 1);
            _white.SetData(new[] { Color.White });

            // Font
            _font = Globals.Content.Load<SpriteFont>("UIFont");

            // ---- textures (adjust asset names if needed) ----
            var texHud = Globals.Content.Load<Texture2D>("HUD_Display");
            var texElevator = Globals.Content.Load<Texture2D>("Elevator(1)");
            var texTornado = Globals.Content.Load<Texture2D>("Tornado");
            var texLemon = Globals.Content.Load<Texture2D>("LemonadeStand");
            var texItems = Globals.Content.Load<Texture2D>("Items_Transparent");
            var texFloor = Globals.Content.Load<Texture2D>("floor_path_tiles");

            // Assignment 3: Rotation demo — spinning present
            // (Using the "fast works" rectangle you said works)
            var presentSrc = new Rectangle(2, 6, 25, 18);

            var spinningPresent = new SpinningSprite(texItems, new Vector2(240, 160), GameRole.Item, presentSrc)
            {
                Scale = 1f,
                AngularVelocity = 3.0f, // spin in place
                OrbitRadius = 12f,      // set 0f if you only want in-place spin
                OrbitSpeed = 1.2f
            };
            _world.Add(spinningPresent);
            // end of spinning present

            // World items (unchanged examples)
            _world.Add(new GameObject(texLemon, new Vector2(300, 120), GameRole.NPC,
                                      new Rectangle(8, 8, 67, 60)));
            _world.Add(new GameObject(texTornado, new Vector2(360, 120), GameRole.Enemy,
                                      new Rectangle(152, 57, 34, 33)));
            _world.Add(new GameObject(texItems, new Vector2(420, 120), GameRole.Item,
                                      new Rectangle(4, 39, 25, 18)));
            _world.Add(new GameObject(texElevator, new Vector2(480, 104), GameRole.Elevator,
                                      new Rectangle(2, 3, 38, 59)));
            _world.Add(new GameObject(texHud, new Vector2(0, 768 - 33), GameRole.UI,
                                      new Rectangle(8, 87, 319, 33)));

            // Tiles 3×3
            var tileSrc = new Rectangle(64, 0, 64, 64);
            AddTileSection(texture: texFloor, source: tileSrc,
                           origin: new Vector2(600, 420), cols: 3, rows: 3,
                           tileSize: 64, tint: Color.White);
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
            // Always process input so toggles are detected
            InputManager.Update();

            // Update facing with last non-zero move input
            if (InputManager.Moving)
                _playerFacing = Vector2.Normalize(InputManager.Direction);

            // Find nearest enemy (for the facing check)
            GameObject nearestEnemy = null;
            float bestD2 = float.MaxValue;
            foreach (var o in _world)
            {
                if (o.Role != GameRole.Enemy) continue;
                float d2 = Vector2.DistanceSquared(o.Position, _toejam.Position);
                if (d2 < bestD2) { bestD2 = d2; nearestEnemy = o; }
            }

            // Compute facing result (only if we found an enemy)
            if (nearestEnemy != null)
            {
                _facingEnemy = IsFacingTarget(
                    _toejam.Position,
                    _playerFacing,
                    nearestEnemy.Position,
                    out _facingDot,
                    0f // minDot=0 -> anything in front (angle < 90°). Increase to narrow the cone.
                );

                // Log only when it meaningfully changes (less spam)
                if (_facingEnemy != _prevFacingEnemy || System.Math.Abs(_facingDot - _prevDot) > 0.05f)
                {
                    System.Diagnostics.Debug.WriteLine($"Facing={_facingEnemy} dot={_facingDot:0.00}");
                    System.Diagnostics.Trace.WriteLine($"Facing={_facingEnemy} dot={_facingDot:0.00}");
                    _prevFacingEnemy = _facingEnemy;
                    _prevDot = _facingDot;
                }
            }
            else
            {
                _facingEnemy = false;
                _facingDot = 0f;
            }

            // Toggle inventory with X
            if (InputManager.BPressed) _invOpen = !_invOpen;

            var kb = Keyboard.GetState();

            // Debug HUD toggle (F3)
            bool f3Now = kb.IsKeyDown(Keys.F3);
            bool f3Prev = _prevKb.IsKeyDown(Keys.F3);
            if (f3Now && !f3Prev) _debugHUD = !_debugHUD;

            if (_invOpen)
            {
                // Move selection (edge on left/right)
                bool leftNow = kb.IsKeyDown(Keys.Left);
                bool leftPrev = _prevKb.IsKeyDown(Keys.Left);
                bool rightNow = kb.IsKeyDown(Keys.Right);
                bool rightPrev = _prevKb.IsKeyDown(Keys.Right);

                if (leftNow && !leftPrev && _inv.Count > 0)
                    _invIndex = (_invIndex - 1 + _inv.Count) % _inv.Count;
                if (rightNow && !rightPrev && _inv.Count > 0)
                    _invIndex = (_invIndex + 1) % _inv.Count;

                // EQUIP with Z (does NOT consume)
                if (InputManager.APressed && _inv.Count > 0)
                {
                    _equipped = _inv[_invIndex].name;
                    Globals.ShowToast($"Equipped {_equipped}", 1.2f);
                    // Optional auto-close:
                    // _invOpen = false;
                }

                _prevKb = kb;
                return; // freeze world while inventory is open
            }

            _prevKb = kb;

            // If paused, freeze sim (draw still runs)
            if (Globals.Paused) return;

            // === Gameplay: Z pressed ===
            if (InputManager.APressed)
            {
                if (!string.IsNullOrEmpty(_equipped))
                {
                    UseEquipped(); // consume/effect
                }
                else
                {
                    // No item equipped: Z toggles sneak
                    _toejam.ToggleSneak();
                }
            }

            // Tick active power timers
            if (_activePower != null)
            {
                _powerTimer -= Globals.TotalSeconds;
                if (_powerTimer <= 0f)
                    EndActivePower();
            }

            // Normal gameplay
            _toejam.Update();
            foreach (var o in _world) o.Update();

            // Remove expired objects (e.g., decoy powerups)
            for (int i = _world.Count - 1; i >= 0; i--)
                if (!_world[i].Alive) _world.RemoveAt(i);
        }

        // Returns true if target lies in front of pos given a facing vector.
        // minDot = 0 means “anywhere in front (angle < 90°)”. Increase to narrow FOV.
        private static bool IsFacingTarget(
            Vector2 pos,
            Vector2 facing,
            Vector2 target,
            out float dotOut,
            float minDot = 0f)
        {
            dotOut = 0f;

            // Need non-zero facing
            if (facing.LengthSquared() < 1e-6f) return false;

            Vector2 toTarget = target - pos;
            if (toTarget.LengthSquared() < 1e-6f) { dotOut = 1f; return true; }

            facing = Vector2.Normalize(facing);
            toTarget = Vector2.Normalize(toTarget);

            float dot = Vector2.Dot(facing, toTarget);
            dotOut = dot;
            return dot > minDot;
        }

        private void UseEquipped()
        {
            // Locate equipped item in inventory to ensure we have charges
            int idx = _inv.FindIndex(slot => slot.name == _equipped);
            if (idx < 0 || _inv[idx].count <= 0)
            {
                Globals.ShowToast($"{_equipped} not available", 1.2f);
                _equipped = null;
                return;
            }

            switch (_equipped)
            {
                case "Decoy":
                    {
                        // Spawn animated decoy at player position (your 3 frames @ 27x42)
                        var decoy = Powerup.CreateDecoy(_toejam.Texture, _toejam.Position, lifeSeconds: 6f);
                        decoy.Scale = _toejam.Scale; // match player size
                        _world.Add(decoy);
                        ConsumeOne(idx);
                        Globals.ShowToast("Decoy deployed!", 1.2f);
                        _equipped = null; // one-shot
                        break;
                    }

                case "Hi-Tops":
                    {
                        // Example of a timed power (10s)
                        StartPower("Hi-Tops", seconds: 10f, speedMult: 1.75f);
                        ConsumeOne(idx);
                        _equipped = null;
                        break;
                    }

                default:
                    {
                        Globals.ShowToast($"Using {_equipped} not implemented yet", 1.2f);
                        _equipped = null; // or keep equipped until implemented
                        break;
                    }
            }
        }

        private void ConsumeOne(int idx)
        {
            var (n, c) = _inv[idx];
            c--;
            if (c <= 0) _inv.RemoveAt(idx);
            else _inv[idx] = (n, c);
        }

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
            // Tiles first
            foreach (var o in _world) if (o.Role == GameRole.Tile) o.Draw();

            // World objects
            foreach (var o in _world) if (o.Role != GameRole.Tile && o.Role != GameRole.UI) o.Draw();

            // Player
            _toejam.Draw();

            // UI last
            foreach (var o in _world) if (o.Role == GameRole.UI) o.Draw();

            // Menu toast (top-center)
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

            // Inventory list overlay (bottom-center)
            if (_invOpen && _font != null)
            {
                var vp = Globals.SpriteBatch.GraphicsDevice.Viewport;
                var box = new Rectangle(vp.Width / 2 - 300, vp.Height - 170, 600, 130);
                Globals.SpriteBatch.Draw(_white, box, new Color(0, 0, 0, 190));

                var y = box.Y + 12;
                var x = box.X + 14;

                if (_inv.Count == 0)
                {
                    Globals.SpriteBatch.DrawString(_font, "No presents", new Vector2(x, y), Color.White);
                }
                else
                {
                    for (int i = 0; i < _inv.Count; i++)
                    {
                        var s = (i == _invIndex)
                            ? $"> {_inv[i].name} x{_inv[i].count} <"
                            : $"{_inv[i].name} x{_inv[i].count}";
                        Globals.SpriteBatch.DrawString(_font, s, new Vector2(x, y), Color.White);
                        y += 20;
                    }
                }

                Globals.SpriteBatch.DrawString(_font, "Z: Equip   X: Close   Left/Right: Select",
                    new Vector2(box.X + 14, box.Bottom - 24), Color.White);
            }

            // Equipped + Power HUD (top-left)
            if (_font != null)
            {
                var hudText = "";
                if (!string.IsNullOrEmpty(_equipped)) hudText += $"Equipped: {_equipped}\n";
                if (_activePower != null) hudText += $"{_activePower}: {System.Math.Ceiling(_powerTimer)}s";

                if (!string.IsNullOrEmpty(hudText))
                {
                    var pos = new Vector2(12, 50);
                    var size = _font.MeasureString(hudText);
                    var rect = new Rectangle((int)(pos.X - 8), (int)(pos.Y - 6),
                                             (int)(size.X + 16), (int)(size.Y + 12));
                    Globals.SpriteBatch.Draw(_white, rect, new Color(0, 0, 0, 140));
                    Globals.SpriteBatch.DrawString(_font, hudText, pos, Color.White);
                }
            }

            // PAUSED label (center)
            if (Globals.Paused && _font != null)
            {
                const string ptext = "PAUSED";
                var vp = Globals.SpriteBatch.GraphicsDevice.Viewport;
                var size = _font.MeasureString(ptext);
                var pos = new Vector2((vp.Width - size.X) / 2f, (vp.Height - size.Y) / 2f);

                var pad = new Vector2(12, 6);
                var rect = new Rectangle(
                    (int)System.Math.Floor(pos.X - pad.X),
                    (int)System.Math.Floor(pos.Y - pad.Y),
                    (int)System.Math.Ceiling(size.X + pad.X * 2),
                    (int)System.Math.Ceiling(size.Y + pad.Y * 2)
                );
                Globals.SpriteBatch.Draw(_white, rect, new Color(0, 0, 0, 180));
                Globals.SpriteBatch.DrawString(_font, ptext, pos, Color.White);
            }

            // --- Simple Debug HUD (F3 to toggle) ---
            if (_debugHUD && _font != null)
            {
                string dbg = $"Facing: {_facingEnemy}\ndot: {_facingDot:0.00}";
                var pos = new Vector2(12, 12); // top-left corner
                var size = _font.MeasureString(dbg);
                var rect = new Rectangle((int)(pos.X - 8), (int)(pos.Y - 6),
                                         (int)(size.X + 16), (int)(size.Y + 12));
                Globals.SpriteBatch.Draw(_white, rect, new Color(0, 0, 0, 160));
                Globals.SpriteBatch.DrawString(_font, dbg, pos, Color.White);
            }
        }
    }
}
