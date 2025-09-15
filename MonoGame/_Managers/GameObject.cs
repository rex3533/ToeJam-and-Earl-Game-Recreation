using Microsoft.Xna.Framework;

namespace MonoGame;

public enum GameRole { Player, NPC, Enemy, Item, Elevator, Tile, UI }

public class GameObject
{
    public Texture2D Sprite { get; }
    public Rectangle? Source;      // optional sprite-sheet crop
    public Vector2 Position;
    public Color Tint = Color.White;
    public float Scale = 1f;       // uniform scale if Size is not used
    public Point? Size;            // if set, draws into this destination size
    public GameRole Role;

    public GameObject(Texture2D sprite, Vector2 position, GameRole role, Rectangle? source = null)
    {
        Sprite = sprite;
        Position = position;
        Role = role;
        Source = source;
    }

    public virtual void Update() { }

    public virtual void Draw()
    {
        // Snap to whole pixels to keep things crisp
        var px = (int)System.Math.Floor(Position.X);
        var py = (int)System.Math.Floor(Position.Y);

        if (Size.HasValue)
        {
            var dest = new Rectangle(px, py, Size.Value.X, Size.Value.Y);
            Globals.SpriteBatch.Draw(Sprite, dest, Source, Tint);
        }
        else
        {
            Globals.SpriteBatch.Draw(Sprite, new Vector2(px, py), Source, Tint,
                                     0f, Vector2.Zero, Scale, SpriteEffects.None, 0f);
        }
    }
}
