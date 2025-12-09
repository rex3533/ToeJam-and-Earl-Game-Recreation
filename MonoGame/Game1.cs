﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace MonoGame;

public class Game1 : Game
{
    private readonly GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;
    private GameManager _gameManager;
    private CameraManager _camera;

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
    }

    protected override void Initialize()
    {
        _graphics.PreferredBackBufferWidth = 1024;
        _graphics.PreferredBackBufferHeight = 768;
        _graphics.ApplyChanges();

        Globals.Content = Content;

        _gameManager = new();

        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        Globals.SpriteBatch = _spriteBatch;

        _gameManager.Init(GraphicsDevice);

        // Camera after GraphicsDevice is ready
        _camera = new CameraManager(GraphicsDevice);
    }

    protected override void Update(GameTime gameTime)
    {
        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed
            || Keyboard.GetState().IsKeyDown(Keys.Escape))
            Exit();

        Globals.Update(gameTime);
        _gameManager.Update();

        // Let the camera follow ToeJam
        if (_camera != null)
        {
            _camera.LookAt(_gameManager.CameraTarget);
        }

        // Let the main menu Quit button close the game
        if (_gameManager.ShouldQuit)
            Exit();

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.Beige);

        if (_gameManager.InMainMenu)
        {
            // Main menu: no camera, just screen-space UI
            _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
            _gameManager.DrawUI();
            _spriteBatch.End();
        }
        else
        {
            // World: with camera
            _spriteBatch.Begin(
                samplerState: SamplerState.PointClamp,
                transformMatrix: _camera?.Transform ?? Matrix.Identity);
            _gameManager.DrawWorld();
            _spriteBatch.End();

            // UI: no camera, locked to screen
            _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
            _gameManager.DrawUI();
            _spriteBatch.End();
        }

        base.Draw(gameTime);
    }
}
