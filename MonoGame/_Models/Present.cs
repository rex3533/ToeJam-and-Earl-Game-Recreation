using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace MonoGame
{
    // Registry for 28 present types: display names + identified flags (persist during session)
    public static class PresentRegistry
    {
        public const int Count = 28;

        // 1-based for readability: use indices 1..28
        public static readonly string[] DisplayNames = new string[Count + 1];
        public static readonly bool[]   Identified   = new bool[Count + 1];

        static PresentRegistry()
        {
            // Fill known items (can hopefully adjust IDs to match the sprite sheet later)
            DisplayNames[1]  = "Decoy";
            DisplayNames[2]  = "Tomatoes";
            DisplayNames[3]  = "Spring Shoes";
            DisplayNames[4]  = "Icarus Wings";
            DisplayNames[5]  = "Hi-Tops";           
            DisplayNames[6]  = "Rocket Skates";
            DisplayNames[7]  = "Slingshot";
            DisplayNames[8]  = "Innertube";
            DisplayNames[9]  = "Rosebuds";
            DisplayNames[10] = "Rain Cloud";
            DisplayNames[11] = "Big Bucks";
            DisplayNames[12] = "Fudge Sundae";     // always great
            DisplayNames[13] = "Random Food";
            // Fill the rest as food placeholders for now (can rename later)
            for (int i = 14; i <= 27; i++)
                DisplayNames[i] = $"Food {i - 13}";
            DisplayNames[28] = "Mystery Present";
        }

        public static string GetLabel(int id)
            => Identified[id] ? DisplayNames[id] : "???";
    }

    // World object for a present. Inherits SpinningSprite for a simple idle spin.
    public class Present : SpinningSprite
    {
        public int Id;    // 1..28 present type id

        public Present(Texture2D tex, Vector2 pos, Rectangle src, int id)
            : base(tex, pos, GameRole.Item, src)
        {
            Id = id;
            // for spinning presents change from 0
            AngularVelocity = 0f;
            OrbitRadius = 0f;
            OrbitSpeed  = 0f;
        }
    }
}
