using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MyTurn.Presentation;

namespace MyTurn.Desktop;

internal sealed class DesktopRenderCaptureService
{
    private readonly GraphicsDevice _graphicsDevice;
    private readonly SpriteBatch _spriteBatch;
    private readonly DesktopRenderer _renderer;

    public DesktopRenderCaptureService(
        GraphicsDevice graphicsDevice,
        SpriteBatch spriteBatch,
        DesktopRenderer renderer)
    {
        _graphicsDevice = graphicsDevice;
        _spriteBatch = spriteBatch;
        _renderer = renderer;
    }

    public void Capture(GameViewModel view, string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path) ?? AppContext.BaseDirectory);

        using var target = new RenderTarget2D(_graphicsDevice, DesktopLayout.VirtualWidth, DesktopLayout.VirtualHeight);
        _graphicsDevice.SetRenderTarget(target);
        _graphicsDevice.Clear(new Color(8, 12, 20));
        _spriteBatch.Begin(
            SpriteSortMode.Deferred,
            BlendState.AlphaBlend,
            SamplerState.PointClamp,
            DepthStencilState.None,
            RasterizerState.CullNone);
        _renderer.Draw(view);
        _spriteBatch.End();
        _graphicsDevice.SetRenderTarget(null);

        using var stream = File.Create(path);
        target.SaveAsPng(stream, DesktopLayout.VirtualWidth, DesktopLayout.VirtualHeight);
    }
}
