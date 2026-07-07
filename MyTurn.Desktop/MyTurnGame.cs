using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MyTurn.Infrastructure;
using MyTurn.Presentation;

namespace MyTurn.Desktop;

internal sealed class MyTurnGame : Game
{
    private const int VirtualWidth = DesktopLayout.VirtualWidth;
    private const int VirtualHeight = DesktopLayout.VirtualHeight;

    private readonly GraphicsDeviceManager _graphics;
    private readonly DesktopInputReader _input = new();
    private readonly DesktopRunOptions _options;
    private SpriteBatch? _spriteBatch;
    private Texture2D? _pixel;
    private DesktopAssetCatalog? _assets;
    private DesktopRenderer? _renderer;
    private GameClient? _client;
    private GameViewModel? _currentView;

    public MyTurnGame(DesktopRunOptions options)
    {
        _options = options;
        _graphics = new GraphicsDeviceManager(this)
        {
            PreferredBackBufferWidth = VirtualWidth,
            PreferredBackBufferHeight = VirtualHeight,
            SynchronizeWithVerticalRetrace = true
        };
        IsMouseVisible = true;
        Window.AllowUserResizing = true;
        Window.Title = "MyTurn Desktop";
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        _pixel = new Texture2D(GraphicsDevice, 1, 1);
        _pixel.SetData([Color.White]);

        var contentRoot = Path.Combine(AppContext.BaseDirectory, "Content");
        var tileMapLoader = new TiledTileMapLoader();
        var generationProfile = MapGenerationProfile.Load(Path.Combine(contentRoot, "Generation", "prototype_dungeon_profile.json"));
        var templates = new TiledRoomTemplateLoader(tileMapLoader).Load(generationProfile);
        var worldGenerator = new TemplateWorldGenerator(generationProfile, templates);

        _assets = new DesktopAssetCatalog(GraphicsDevice, contentRoot);
        _renderer = new DesktopRenderer(_spriteBatch, _pixel, _assets, VirtualWidth, VirtualHeight);

        if (_options.CaptureMode)
        {
            var captureService = new DesktopRenderCaptureService(GraphicsDevice, _spriteBatch, _renderer);
            new CaptureScenarioRunner(captureService, worldGenerator).CaptureAll(_options.CaptureDirectory);
            Exit();
            return;
        }

        var services = SqliteApplicationServices.CreateDefault(worldGenerator);
        _client = new GameClient(services, tileMapProvider: worldGenerator);
        _currentView = _client.CurrentView;
    }

    protected override void UnloadContent()
    {
        _assets?.Dispose();
        _pixel?.Dispose();
        _spriteBatch?.Dispose();
        base.UnloadContent();
    }

    protected override void Update(GameTime gameTime)
    {
        if (_client is null)
        {
            return;
        }

        var input = _input.Poll();
        _client.Update(input, gameTime.ElapsedGameTime);
        _currentView = _client.CurrentView;

        if (_currentView.ScreenKind == ScreenKind.Exit)
        {
            Exit();
        }

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        if (_client is null || _renderer is null || _spriteBatch is null || _currentView is null)
        {
            return;
        }

        GraphicsDevice.Clear(new Color(8, 12, 20));
        var viewport = CreateLetterboxViewport();
        var scale = (float)viewport.Width / VirtualWidth;
        var transform = Matrix.CreateScale(scale, scale, 1f) *
            Matrix.CreateTranslation(viewport.X, viewport.Y, 0f);

        _spriteBatch.Begin(
            SpriteSortMode.Deferred,
            BlendState.AlphaBlend,
            SamplerState.PointClamp,
            DepthStencilState.None,
            RasterizerState.CullNone,
            transformMatrix: transform);
        _renderer.Draw(_currentView);
        _spriteBatch.End();

        base.Draw(gameTime);
    }

    private Rectangle CreateLetterboxViewport()
    {
        var backBufferWidth = GraphicsDevice.PresentationParameters.BackBufferWidth;
        var backBufferHeight = GraphicsDevice.PresentationParameters.BackBufferHeight;
        var scale = Math.Min((float)backBufferWidth / VirtualWidth, (float)backBufferHeight / VirtualHeight);
        var width = (int)(VirtualWidth * scale);
        var height = (int)(VirtualHeight * scale);
        var x = (backBufferWidth - width) / 2;
        var y = (backBufferHeight - height) / 2;

        return new Rectangle(x, y, width, height);
    }
}
