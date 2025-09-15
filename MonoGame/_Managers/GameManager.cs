using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace MonoGame;

public class GameManager
{
    private ToeJam _toejam;
    private readonly List<GameObject> _world = new();
    private Texture2D _white;

    // Call from Game1.LoadContent: _gameManager.Init(GraphicsDevice);
    public void Init(GraphicsDevice gd)
    {
        // Player
        var toeJamTexture = Globals.Content.Load<Texture2D>("ToeJam_Transparent");
        _toejam = new ToeJam(0, toeJamTexture);

        // Utility 1x1 (kept for future use)
        _white = new Texture2D(gd, 1, 1);
        _white.SetData(new[] { Color.White });

        // ---- Spritesheets / textures ----
        var texHud      = Globals.Content.Load<Texture2D>("HUD_Display");
        var texElevator = Globals.Content.Load<Texture2D>("Elevator(1)");
        var texTornado  = Globals.Content.Load<Texture2D>("Tornado");
        var texLemon    = Globals.Content.Load<Texture2D>("LemonadeStand");
        var texItems    = Globals.Content.Load<Texture2D>("Items_Transparent");
        var texFloor    = Globals.Content.Load<Texture2D>("floor_path_tiles"); // <-- NEW

        // NPC: Lemonade Stand  (8,8, 67x60)
        _world.Add(new GameObject(texLemon, new Vector2(300, 120), GameRole.NPC,
                                  new Rectangle(8, 8, 67, 60)));

        // Enemy: Tornado fully formed  (152,57, 34x33)
        _world.Add(new GameObject(texTornado, new Vector2(360, 120), GameRole.Enemy,
                                  new Rectangle(152, 57, 34, 33)));

        // Item: present  (4,39, 25x18)
        _world.Add(new GameObject(texItems, new Vector2(420, 120), GameRole.Item,
                                  new Rectangle(4, 39, 25, 18)));

        // Elevator: first frame  (2,3, 38x59)
        _world.Add(new GameObject(texElevator, new Vector2(480, 104), GameRole.Elevator,
                                  new Rectangle(2, 3, 38, 59)));

        // HUD slice: (8,87, 319x33) anchored bottom-left
        _world.Add(new GameObject(texHud, new Vector2(0, 768 - 33), GameRole.UI,
                                  new Rectangle(8, 87, 319, 33)));

        // ---------- TILES (3x3) ----------
        // floor_path_tiles.png -> tile at (64,0) size 64x64
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
        InputManager.Update();
        _toejam.Update();
        foreach (var o in _world) o.Update();
    }

    public void Draw()
    {
        // TILES FIRST — they render below everything
        foreach (var o in _world) if (o.Role == GameRole.Tile) o.Draw();

        // World objects
        foreach (var o in _world) if (o.Role != GameRole.Tile && o.Role != GameRole.UI) o.Draw();

        // Player
        _toejam.Draw();

        // UI last
        foreach (var o in _world) if (o.Role == GameRole.UI) o.Draw();
    }
}
