using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace SdlWasmSample;

public class SampleGame : Game
{
    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;
    
    public SampleGame()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
        Console.WriteLine($"Game constructor");
    }
    
    protected override void Initialize()
    {
        // TODO: Add your initialization logic here
        Console.WriteLine($"Game Initialize");
 
        base.Initialize();
    }
    
    protected override void LoadContent()
    {
        Console.WriteLine($"Game LoadContent");
        _spriteBatch = new SpriteBatch(GraphicsDevice);
 
        // TODO: use this.Content to load your game content here
    }
    
    protected override void Update(GameTime gameTime)
    {
        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
            Exit();
 
        // TODO: Add your update logic here
 
        base.Update(gameTime);
    }
    
    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.CornflowerBlue);
 
        // TODO: Add your drawing code here
 
        base.Draw(gameTime);
    }
}