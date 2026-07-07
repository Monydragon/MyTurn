using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace MyTurn.Desktop;

internal sealed class DesktopAssetCatalog : IDisposable
{
    private readonly GraphicsDevice _graphicsDevice;

    public DesktopAssetCatalog(GraphicsDevice graphicsDevice, string contentRoot)
    {
        _graphicsDevice = graphicsDevice ?? throw new ArgumentNullException(nameof(graphicsDevice));
        ContentRoot = !string.IsNullOrWhiteSpace(contentRoot)
            ? contentRoot
            : throw new ArgumentException("Content root is required.", nameof(contentRoot));
        PrototypeTileset = LoadTexture(
            Path.Combine(ContentRoot, "Tiles", "prototype_tileset.png"),
            CreateFallbackTileset);
    }

    public string ContentRoot { get; }
    public Texture2D PrototypeTileset { get; }

    public void Dispose()
    {
        PrototypeTileset.Dispose();
    }

    private Texture2D LoadTexture(string path, Func<Texture2D> fallbackFactory)
    {
        if (!File.Exists(path))
        {
            return fallbackFactory();
        }

        using var stream = File.OpenRead(path);
        return Texture2D.FromStream(_graphicsDevice, stream);
    }

    private Texture2D CreateFallbackTileset()
    {
        const int tileSize = 16;
        const int columns = 4;
        var texture = new Texture2D(_graphicsDevice, tileSize * columns, tileSize);
        var data = new Color[tileSize * columns * tileSize];
        var colors = new[]
        {
            new Color(70, 84, 76),
            new Color(40, 46, 58),
            new Color(116, 96, 58),
            new Color(34, 68, 96)
        };

        for (var tile = 0; tile < columns; tile++)
        {
            for (var y = 0; y < tileSize; y++)
            {
                for (var x = 0; x < tileSize; x++)
                {
                    var absoluteX = (tile * tileSize) + x;
                    var shade = (x + y) % 5 == 0 ? 14 : 0;
                    var baseColor = colors[tile];
                    data[(y * tileSize * columns) + absoluteX] = new Color(
                        Math.Min(255, baseColor.R + shade),
                        Math.Min(255, baseColor.G + shade),
                        Math.Min(255, baseColor.B + shade));
                }
            }
        }

        texture.SetData(data);
        return texture;
    }
}
