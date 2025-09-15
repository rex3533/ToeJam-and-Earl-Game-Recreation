using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace MonoGame;

public class Animations
{
    private readonly Texture2D _texture;
    private readonly List<Rectangle> _framesList = new();
    private int _frame;
    private readonly float _frameTime;
    private float _frameTimeLeft;
    private bool _active = true;

    // Grid-based (kept for compatibility, but we won’t use it for uneven sheets)
    public Animations(Texture2D texture, int framesX, int framesY, float frameTime, int row = 1)
    {
        _texture = texture;
        _frameTime = frameTime;
        _frameTimeLeft = _frameTime;

        var fw = _texture.Width / framesX;
        var fh = _texture.Height / framesY;

        for (int i = 0; i < framesX; i++)
            _framesList.Add(new Rectangle(i * fw, (row - 1) * fh, fw, fh));
    }

    // Horizontal strip helper (startX/Y + size + count [+ optional gap])
    public Animations(Texture2D texture, int startX, int startY, int frameWidth, int frameHeight,
                      int frames, float frameTime, int stepX = 0)
    {
        _texture = texture;
        _frameTime = frameTime;
        _frameTimeLeft = _frameTime;

        if (stepX == 0) stepX = frameWidth;

        for (int i = 0; i < frames; i++)
            _framesList.Add(new Rectangle(startX + i * stepX, startY, frameWidth, frameHeight));
    }

    // Explicit rectangles (best for uneven spacing or per-frame Y offsets)
    public Animations(Texture2D texture, IEnumerable<Rectangle> frames, float frameTime)
    {
        _texture = texture;
        _framesList.AddRange(frames);
        _frameTime = frameTime;
        _frameTimeLeft = _frameTime;
    }

    public void Stop() => _active = false;
    public void Play() => _active = true;

    public void Reset()
    {
        _frame = 0;
        _frameTimeLeft = _frameTime;
    }

    public void Update()
    {
        if (!_active) return;

        _frameTimeLeft -= Globals.TotalSeconds;
        if (_frameTimeLeft <= 0f)
        {
            _frame = (_frame + 1) % _framesList.Count;
            _frameTimeLeft = _frameTime;
        }
    }

    public void Draw(Vector2 pos)
    {
        // snap to whole pixels to avoid shimmer/bleed
        pos.X = (float)System.Math.Floor(pos.X);
        pos.Y = (float)System.Math.Floor(pos.Y);

        Globals.SpriteBatch.Draw(
            _texture,
            pos,
            _framesList[_frame],
            Microsoft.Xna.Framework.Color.White,
            0f,
            Vector2.Zero,
            1f,
            SpriteEffects.None,
            0f
        );
    }
}
