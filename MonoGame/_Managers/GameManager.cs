using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace MonoGame;

public class GameManager
{
    private ToeJam _toejam;
    private readonly List<GameObject> _world = new();

    // UI helpers
    private SpriteFont _font;    // for on-screen text
    private Texture2D _white;    // 1x1 white (used to draw translucent rectangles)

    public void Init(GraphicsDevice gd)
    {
        // Player
        var toeJamTexture = Globals.Content.Load<Texture2D>("ToeJam_Transparent");
        _toejam = new ToeJam(0, toeJamTexture);

        // 1x1 white for UI backdrops
        _white = new Texture2D(gd, 1, 1);
        _white.SetData(new[] { Color.White });

        // Font for menu toast & paused text (ensure Content asset name is "UIFont")
        _font = Globals.Content.Load<SpriteFont>("UIFont");

        // ---- textures (adjust names if your Pipeline differs) ----
        var texHud      = Globals.Content.Load<Texture2D>("HUD_Display");
        var texElevator = Globals.Content.Load<Texture2D>("Elevator(1)");
        var texTornado  = Globals.Content.Load<Texture2D>("Tornado");
        var texLemon    = Globals.Content.Load<Texture2D>("LemonadeStand");
        var texItems    = Globals.Content.Load<Texture2D>("Items_Transparent");
        var texFloor    = Globals.Content.Load<Texture2D>("floor_path_tiles");

        // NPC: Lemonade Stand  (8,8, 67x60)
        _world.Add(new GameObject(texLemon, new Vector2(300, 120), GameRole.NPC,
                                  new Rectangle(8, 8, 67, 60)));

        // Enemy: Tornado  (152,57, 34x33)
        _world.Add(new GameObject(texTornado, new Vector2(360, 120), GameRole.Enemy,
                                  new Rectangle(152, 57, 34, 33)));

        // Item: present  (4,39, 25x18)
        _world.Add(new GameObject(texItems, new Vector2(420, 120), GameRole.Item,
                                  new Rectangle(4, 39, 25, 18)));

        // Elevator: first frame  (2,3, 38x59)
        _world.Add(new GameObject(texElevator, new Vector2(480, 104), GameRole.Elevator,
                                  new Rectangle(2, 3, 38, 59)));

        // HUD slice: (8,87, 319x33) bottom
        _world.Add(new GameObject(texHud, new Vector2(0, 768 - 33), GameRole.UI,
                                  new Rectangle(8, 87, 319, 33)));

        // Tiles 3×3 from floor sheet: (64,0, 64x64)
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
        // Always process input so pause/menu toggles are detected even while paused
        InputManager.Update();

        // If paused, freeze simulation (draw still runs)
        if (Globals.Paused) return;

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

        // ===== Menu toast (top-center) with translucent backdrop =====
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

        // ===== PAUSE label (centered) with translucent backdrop =====
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
