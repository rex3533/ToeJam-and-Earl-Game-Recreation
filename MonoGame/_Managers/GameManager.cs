using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input; // Keys, KeyboardState

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

        // === PRESENT INVENTORY ===
        // Each entry tracks a present type (id 1..28), count, and shows ??? until identified.
        private readonly List<(int id, int count)> _presentInv = new();

        // Equipped + active timed power (kept from before for Hi-Tops etc.)
        private string _equipped = null;     // (unused for presents; we "use" directly)
        private string _activePower = null;  // e.g., "Hi-Tops"
        private float _powerTimer = 0f;      // seconds remaining
        private int   _bigBucks = 0;         // simple currency counter

        // --- Part 2: Facing check (dot product) ---
        private Vector2 _playerFacing = new(1f, 0f);  // last non-zero input direction
        private bool _facingEnemy = false;            // whether nearest enemy is in front
        private float _facingDot = 0f;                // dot product value
        private bool _prevFacingEnemy;                // last printed "facing" state
        private float _prevDot;                       // last printed dot value

        // --- Part 2: Cross product (left/right of facing) ---
        // Y-down screen: + = Right, - = Left, 0 = On the line
        private int _facingSide = 0;                  // -1 = Left, 0 = On, +1 = Right
        private float _crossZ = 0f;                   // z-component value of facing x toEnemy
        private int _prevFacingSide;                  // last printed side
        private float _prevCross;                     // last printed cross value

        // --- Part 3: Item pickup distance ---
        private GameObject _nearestItem = null;       // nearest world item (present)
        private float _nearestItemDist = float.PositiveInfinity; // pixels (Position→Position)
        private const float PICKUP_RADIUS = 28f;      // auto-pick threshold (px)

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

            //--- Audio ----
            AudioManager.Init(Globals.Content);
            AudioManager.StartBgm();

            // Assignment 3: Rotation demo — spinning present (now using Present class)
            // Using the "fast works" rectangle I used earlier (adjust when map all 28)
            var presentSrc = new Rectangle(2, 6, 25, 18);
            // Example: ID 5 is "Hi-Tops" so your F3 can later show "present5: Hi-Tops"
            var presentHiTops = new Present(texItems, new Vector2(240, 160), presentSrc, id: 5)
            {
                Scale = 2f,
                AngularVelocity = 3.0f // only THIS one spins
            };
            _world.Add(presentHiTops);
            // end of spinning present  

            // World items 
            _world.Add(new GameObject(texLemon, new Vector2(300, 120), GameRole.NPC,
                                      new Rectangle(8, 8, 67, 60)));
            _world.Add(new GameObject(texTornado, new Vector2(360, 120), GameRole.Enemy,
                                      new Rectangle(152, 57, 34, 33)));
            _world.Add(new Present(texItems, new Vector2(420, 120), new Rectangle(4, 39, 25, 18), id: 1)); // Decoy present
            _world.Add(new Present(texItems, new Vector2(520, 120), new Rectangle(4, 39, 25, 18), id: 1));
            // --- Hi-Tops present (static, no spin) ---
            var hiTopsSrc = new Rectangle(2, 6, 25, 18);
            _world.Add(new Present(texItems, new Vector2(640, 220), hiTopsSrc, id: 5) { Scale = 1f });

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
            
            // Audio update (for chaining)
            AudioManager.Update();

            // Update facing with last non-zero move input
            if (InputManager.Moving)
                _playerFacing = Vector2.Normalize(InputManager.Direction);

            // --- Find nearest enemy (for dot/cross checks) ---
            GameObject nearestEnemy = null;
            float bestD2 = float.MaxValue;
            foreach (var o in _world)
            {
                if (o.Role != GameRole.Enemy) continue;
                float d2 = Vector2.DistanceSquared(o.Position, _toejam.Position);
                if (d2 < bestD2) { bestD2 = d2; nearestEnemy = o; }
            }

            // --- DOT/CROSS results (only if we found an enemy) ---
            if (nearestEnemy != null)
            {
                // DOT: enemy in front? (angle < 90° if dot > 0)
                _facingEnemy = IsFacingTarget(
                    _toejam.Position,
                    _playerFacing,
                    nearestEnemy.Position,
                    out _facingDot,
                    0f // minDot=0 -> anything in front; increase to narrow the cone
                );

                // CROSS: enemy on Right/Left of facing? (Y-down: + = Right, - = Left)
                _facingSide = SideOfFacing(
                    _toejam.Position,
                    _playerFacing,
                    nearestEnemy.Position,
                    out _crossZ
                );

                // Log only when it meaningfully changes (less spam)
                bool changedFacing = (_facingEnemy != _prevFacingEnemy) || (System.Math.Abs(_facingDot - _prevDot) > 0.05f);
                bool changedSide   = (_facingSide != _prevFacingSide)   || (System.Math.Abs(_crossZ   - _prevCross) > 0.05f);
                if (changedFacing || changedSide)
                {
                    System.Diagnostics.Debug.WriteLine($"Facing={_facingEnemy} dot={_facingDot:0.00} side={SideLabel(_facingSide)} crossZ={_crossZ:0.00}");
                    System.Diagnostics.Trace.WriteLine($"Facing={_facingEnemy} dot={_facingDot:0.00} side={SideLabel(_facingSide)} crossZ={_crossZ:0.00}");
                    _prevFacingEnemy = _facingEnemy;
                    _prevDot = _facingDot;
                    _prevFacingSide = _facingSide;
                    _prevCross = _crossZ;
                }
            }
            else
            {
                _facingEnemy = false;
                _facingDot = 0f;
                _facingSide = 0;
                _crossZ = 0f;
            }

            // --- Part 3: Nearest item distance (for pickup) ---
            _nearestItem = null;
            _nearestItemDist = float.PositiveInfinity;
            foreach (var o in _world)
            {
                if (o.Role != GameRole.Item) continue;
                float d = Vector2.Distance(_toejam.Position + CenterOf(o), o.Position + CenterOf(o));
                if (d < _nearestItemDist)
                {
                    _nearestItemDist = d;
                    _nearestItem = o;
                }
            }

            // Auto-pickup if close enough and it's a Present
            if (_nearestItem is Present p && _nearestItemDist <= PICKUP_RADIUS)
            {
                AddPresentToInventory(p.Id, 1);
                Globals.ShowToast($"Picked Present{p.Id}: {PresentRegistry.GetLabel(p.Id)}", 1.2f);
                p.Alive = false; // will be removed at end of Update
            }

            // Toggle inventory with X
            if (InputManager.BPressed) _invOpen = !_invOpen;

            var kb = Keyboard.GetState();

            // Debug HUD toggle (F3)
            bool f3Now  = kb.IsKeyDown(Keys.F3);
            bool f3Prev = _prevKb.IsKeyDown(Keys.F3);
            if (f3Now && !f3Prev) _debugHUD = !_debugHUD;

            if (_invOpen)
            {
                // Move selection (edge on left/right)
                bool leftNow  = kb.IsKeyDown(Keys.Left);
                bool leftPrev = _prevKb.IsKeyDown(Keys.Left);
                bool rightNow = kb.IsKeyDown(Keys.Right);
                bool rightPrev= _prevKb.IsKeyDown(Keys.Right);

                if (leftNow && !leftPrev && _presentInv.Count > 0)
                    _invIndex = (_invIndex - 1 + _presentInv.Count) % _presentInv.Count;
                if (rightNow && !rightPrev && _presentInv.Count > 0)
                    _invIndex = (_invIndex + 1) % _presentInv.Count;

                // USE a present with Z (opens it; identifies type; applies basic effect)
                if (InputManager.APressed && _presentInv.Count > 0)
                {
                    UseSelectedPresent();
                }

                _prevKb = kb;
                return; // freeze world while inventory is open
            }

            _prevKb = kb;

            // If paused, freeze sim (draw still runs)
            if (Globals.Paused) return;

            // === Gameplay: Z pressed (outside inventory) ===
            if (InputManager.APressed)
            {
                if (!string.IsNullOrEmpty(_equipped))
                {
                    UseEquipped(); // legacy path (kept if you want it)
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

            // Remove expired objects (e.g., decoy, picked-up presents)
            for (int i = _world.Count - 1; i >= 0; i--)
                if (!_world[i].Alive) _world.RemoveAt(i);
        }

        // Helper: center offset from Source or Texture
        private static Vector2 CenterOf(GameObject o)
        {
            var src = o.Source ?? new Rectangle(0, 0, o.Sprite.Width, o.Sprite.Height);
            return new Vector2(src.Width / 2f, src.Height / 2f);
        }

        // === PRESENT INVENTORY HELPERS ===
        private void AddPresentToInventory(int id, int amount)
        {
            int i = _presentInv.FindIndex(s => s.id == id);
            if (i >= 0) _presentInv[i] = (id, _presentInv[i].count + amount);
            else _presentInv.Add((id, amount));
            // keep selection within bounds
            if (_invIndex >= _presentInv.Count) _invIndex = _presentInv.Count - 1;
        }

        private void UseSelectedPresent()
        {
            if (_presentInv.Count == 0) return;

            var (id, count) = _presentInv[_invIndex];

            // Identify on first use
            bool firstTime = !PresentRegistry.Identified[id];
            PresentRegistry.Identified[id] = true;

            string name = PresentRegistry.DisplayNames[id];
            if (firstTime) Globals.ShowToast($"Identified: {name}", 1.4f);
            else          Globals.ShowToast($"Opened: {name}", 1.1f);

            // Apply a few simple effects now; leave most as TODO
            switch (name)
            {
                case "Hi-Tops":
                    StartPower("Hi-Tops", seconds: 10f, speedMult: 1.75f);
                    break;

                case "Decoy":
                    {
                        // Spawn the decoy at player position (your existing Powerup)
                        var decoy = Powerup.CreateDecoy(_toejam.Texture, _toejam.Position, lifeSeconds: 6f);
                        decoy.Scale = _toejam.Scale;
                        _world.Add(decoy);
                        break;
                    }

                case "Big Bucks":
                    _bigBucks += 25; // placeholder amount
                    Globals.ShowToast($"+25 Big Bucks (total: {_bigBucks})", 1.2f);
                    break;

                default:
                    // Not implemented effects yet
                    Globals.ShowToast($"Opened: {name} (effect TBD)", 1.2f);
                    break;
            }

            // Consume one
            count--;
            if (count <= 0) _presentInv.RemoveAt(_invIndex);
            else            _presentInv[_invIndex] = (id, count);

            // keep selection valid
            if (_invIndex >= _presentInv.Count) _invIndex = _presentInv.Count - 1;
            if (_invIndex < 0) _invIndex = 0;
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

            facing   = Vector2.Normalize(facing);
            toTarget = Vector2.Normalize(toTarget);

            float dot = Vector2.Dot(facing, toTarget);
            dotOut = dot;
            return dot > minDot;
        }

        // 2D side test using cross product (z-component of facing × toTarget).
        // Y-down screen: + => Right, - => Left, 0 => On the line.
        private static int SideOfFacing(
            Vector2 pos,
            Vector2 facing,
            Vector2 target,
            out float crossOut,
            float eps = 1e-6f)
        {
            crossOut = 0f;

            if (facing.LengthSquared() < 1e-6f) return 0;

            Vector2 toTarget = target - pos;
            if (toTarget.LengthSquared() < eps) return 0;

            // Normalizing not required for sign; keeps magnitudes stable
            facing   = Vector2.Normalize(facing);
            toTarget = Vector2.Normalize(toTarget);

            float z = facing.X * toTarget.Y - facing.Y * toTarget.X;
            crossOut = z;

            if (z >  eps) return +1;  // Right
            if (z < -eps) return -1;  // Left
            return 0;                  // On the line
        }

        private static string SideLabel(int s) => s > 0 ? "Right" : (s < 0 ? "Left" : "On");

        // === Legacy "equipped" path kept from before (not used by presents) ===
        private void UseEquipped()
        {
            // (Kept for compatibility;  can remove later if only presents are used)
            Globals.ShowToast("Equipped-use path (legacy) not used with present system", 1.2f);
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
                var vp   = Globals.SpriteBatch.GraphicsDevice.Viewport;
                var text = Globals.MenuToastText;
                var size = _font.MeasureString(text);
                var pos  = new Vector2((vp.Width - size.X) / 2f, 10f);

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
                var vp  = Globals.SpriteBatch.GraphicsDevice.Viewport;
                var box = new Rectangle(vp.Width/2 - 300, vp.Height - 170, 600, 130);
                Globals.SpriteBatch.Draw(_white, box, new Color(0,0,0,190));

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

            // Equipped + Power HUD (top-left)
            if (_font != null)
            {
                var hudText = "";
                if (_bigBucks > 0) hudText += $"Big Bucks: {_bigBucks}\n";
                if (_activePower != null) hudText += $"{_activePower}: {System.Math.Ceiling(_powerTimer)}s";

                if (!string.IsNullOrEmpty(hudText))
                {
                    var pos = new Vector2(12, 50);
                    var size = _font.MeasureString(hudText);
                    var rect = new Rectangle((int)(pos.X - 8), (int)(pos.Y - 6),
                                             (int)(size.X + 16), (int)(size.Y + 12));
                    Globals.SpriteBatch.Draw(_white, rect, new Color(0,0,0,140));
                    Globals.SpriteBatch.DrawString(_font, hudText, pos, Color.White);
                }
            }

            // PAUSED label (center)
            if (Globals.Paused && _font != null)
            {
                const string ptext = "PAUSED";
                var vp    = Globals.SpriteBatch.GraphicsDevice.Viewport;
                var size  = _font.MeasureString(ptext);
                var pos   = new Vector2((vp.Width - size.X) / 2f, (vp.Height - size.Y) / 2f);

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
                string itemLine = _nearestItem != null
                    ? $"nearest item: {_nearestItemDist:0.0} px (auto<={PICKUP_RADIUS})"
                    : "nearest item: none";

                string dbg = $"Facing: {_facingEnemy}\n" +
                             $"dot: {_facingDot:0.00}\n" +
                             $"side: {SideLabel(_facingSide)} (crossZ: {_crossZ:0.00})\n" +
                             itemLine;

                var pos  = new Vector2(12, 12); // top-left corner
                var size = _font.MeasureString(dbg);
                var rect = new Rectangle((int)(pos.X - 8), (int)(pos.Y - 6),
                                         (int)(size.X + 16), (int)(size.Y + 12));
                Globals.SpriteBatch.Draw(_white, rect, new Color(0,0,0,160));
                Globals.SpriteBatch.DrawString(_font, dbg, pos, Color.White);
            }
        }
    }
}
