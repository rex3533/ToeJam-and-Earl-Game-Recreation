using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

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
        private readonly List<(string name, int count)> _inv = new()
        {
            ("Hi-Tops", 2), ("Tomatoes", 5), ("Rocket Skates", 1)
        };

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
            var texHud      = Globals.Content.Load<Texture2D>("HUD_Display");
            var texElevator = Globals.Content.Load<Texture2D>("Elevator(1)");
            var texTornado  = Globals.Content.Load<Texture2D>("Tornado");
            var texLemon    = Globals.Content.Load<Texture2D>("LemonadeStand");
            var texItems    = Globals.Content.Load<Texture2D>("Items_Transparent");
            var texFloor    = Globals.Content.Load<Texture2D>("floor_path_tiles");

            // World items (unchanged)
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

            // Toggle inventory with X
            if (InputManager.BPressed) _invOpen = !_invOpen;

            var kb = Keyboard.GetState();

            if (_invOpen)
            {
                // Move selection (edge on left/right)
                bool leftNow  = kb.IsKeyDown(Keys.Left);
                bool leftPrev = _prevKb.IsKeyDown(Keys.Left);
                bool rightNow = kb.IsKeyDown(Keys.Right);
                bool rightPrev= _prevKb.IsKeyDown(Keys.Right);

                if (leftNow && !leftPrev && _inv.Count > 0)
                    _invIndex = (_invIndex - 1 + _inv.Count) % _inv.Count;
                if (rightNow && !rightPrev && _inv.Count > 0)
                    _invIndex = (_invIndex + 1) % _inv.Count;

                // Use/confirm with Z
                if (InputManager.APressed && _inv.Count > 0)
                {
                    var (n, c) = _inv[_invIndex];
                    if (c > 0)
                    {
                        _inv[_invIndex] = (n, c - 1);
                        Globals.ShowToast($"Used {n}", 1.2f);
                        if (c - 1 == 0)
                        {
                            _inv.RemoveAt(_invIndex);
                            if (_invIndex >= _inv.Count) _invIndex = System.Math.Max(0, _inv.Count - 1);
                        }
                    }
                }

                _prevKb = kb;
                return; // freeze world while inventory is open
            }

            _prevKb = kb;

            // If paused, freeze sim (draw still runs)
            if (Globals.Paused) return;

            // Normal gameplay
            _toejam.Update();
            foreach (var o in _world) o.Update();
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
                var box = new Rectangle(vp.Width/2 - 260, vp.Height - 150, 520, 110);
                Globals.SpriteBatch.Draw(_white, box, new Color(0,0,0,190));

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

                Globals.SpriteBatch.DrawString(_font, "Z: Use   X: Close   Left/Right: Select",
                new Vector2(box.X + 14, box.Bottom - 24), Color.White);
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
        }
    }
}
