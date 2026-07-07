using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MyTurn.Domain;
using MyTurn.Presentation;

namespace MyTurn.Desktop;

internal sealed class TileMapRenderer
{
    private readonly SpriteBatch _spriteBatch;
    private readonly JrpgWindowRenderer _windows;
    private readonly DesktopAssetCatalog _assets;

    public TileMapRenderer(SpriteBatch spriteBatch, JrpgWindowRenderer windows, DesktopAssetCatalog assets)
    {
        _spriteBatch = spriteBatch;
        _windows = windows;
        _assets = assets;
    }

    public bool Draw(WorldViewModel view, Rectangle bounds)
    {
        if (view.TileMap is null)
        {
            return false;
        }

        var map = view.TileMap;
        var scale = CalculateScale(map, bounds, view.Camera);
        var tileWidth = map.TileWidth * scale;
        var tileHeight = map.TileHeight * scale;
        var visibleColumns = view.Camera?.VisibleColumns ?? map.Width;
        var visibleRows = view.Camera?.VisibleRows ?? map.Height;
        var mapWidth = visibleColumns * tileWidth;
        var mapHeight = visibleRows * tileHeight;
        var origin = new Point(
            bounds.X + ((bounds.Width - mapWidth) / 2),
            bounds.Y + ((bounds.Height - mapHeight) / 2));

        _windows.Fill(bounds, new Color(6, 10, 16));

        foreach (var layer in map.Layers.Where(layer => layer.Visible))
        {
            foreach (var tile in layer.Tiles.Where(tile => tile.GlobalId > 0 && IsVisible(tile.Position, view.Camera)))
            {
                DrawTile(map, tile, origin, scale, view.Camera);
            }
        }

        DrawObjects(map, view, origin, scale);
        DrawPlayer(view, map, origin, scale);

        return true;
    }

    private void DrawTile(TileMapViewModel map, TileViewModel tile, Point origin, int scale, WorldCameraViewModel? camera)
    {
        var localId = Math.Max(0, tile.GlobalId - 1);
        var columns = Math.Max(1, map.TilesetColumns);
        var source = new Rectangle(
            (localId % columns) * map.TileWidth,
            (localId / columns) * map.TileHeight,
            map.TileWidth,
            map.TileHeight);
        var visibleX = tile.Position.X - (camera?.MinTileX ?? 0);
        var visibleY = tile.Position.Y - (camera?.MinTileY ?? 0);
        var destination = new Rectangle(
            origin.X + (visibleX * map.TileWidth * scale),
            origin.Y + (visibleY * map.TileHeight * scale),
            map.TileWidth * scale,
            map.TileHeight * scale);

        _spriteBatch.Draw(_assets.PrototypeTileset, destination, source, Color.White);

        if (tile.IsBlocking)
        {
            _windows.Fill(new Rectangle(destination.X, destination.Y, destination.Width, 3), new Color(12, 14, 22, 90));
        }
    }

    private void DrawObjects(TileMapViewModel map, WorldViewModel view, Point origin, int scale)
    {
        var objects = view.Objects?.Where(obj => obj.IsActive) ?? map.Objects.Select(obj =>
            new WorldObjectViewModel(obj.Name, obj.Type, obj.Position, "Active", false, true));

        foreach (var obj in objects.Where(obj => IsVisible(obj.Position, view.Camera)))
        {
            var visibleX = obj.Position.X - (view.Camera?.MinTileX ?? 0);
            var visibleY = obj.Position.Y - (view.Camera?.MinTileY ?? 0);
            var destination = new Rectangle(
                origin.X + (visibleX * map.TileWidth * scale),
                origin.Y + (visibleY * map.TileHeight * scale),
                map.TileWidth * scale,
                map.TileHeight * scale);
            var marker = GetObjectMarker(obj.Type);

            if (marker.Symbol is null)
            {
                continue;
            }

            var markerSize = Math.Max(12, destination.Width / 2);
            var markerScale = Math.Max(1, destination.Width / 18);
            var markerRect = new Rectangle(
                destination.X + ((destination.Width - markerSize) / 2),
                destination.Y + ((destination.Height - markerSize) / 2),
                markerSize,
                markerSize);

            _windows.Fill(markerRect, new Color(4, 8, 14, 180));
            _windows.DrawText(
                marker.Symbol,
                markerRect.X + Math.Max(2, markerSize / 5),
                markerRect.Y + Math.Max(2, markerSize / 8),
                marker.Color,
                markerScale);
        }
    }

    private void DrawPlayer(WorldViewModel view, TileMapViewModel map, Point origin, int scale)
    {
        var cameraOffsetX = (view.Camera?.MinTileX ?? 0) * map.TileWidth;
        var cameraOffsetY = (view.Camera?.MinTileY ?? 0) * map.TileHeight;
        var x = origin.X + (int)MathF.Round((view.PlayerPixelX - cameraOffsetX) * scale);
        var y = origin.Y + (int)MathF.Round((view.PlayerPixelY - cameraOffsetY) * scale);
        var tileWidth = map.TileWidth * scale;
        var tileHeight = map.TileHeight * scale;
        var bodyWidth = Math.Max(6, tileWidth / 3);
        var bodyHeight = Math.Max(10, (tileHeight * 5) / 8);
        var body = new Rectangle(
            x + ((tileWidth - bodyWidth) / 2),
            y + Math.Max(5, tileHeight / 5),
            bodyWidth,
            bodyHeight);
        var headWidth = Math.Max(5, tileWidth / 3);
        var headHeight = Math.Max(5, tileHeight / 4);
        var head = new Rectangle(
            x + ((tileWidth - headWidth) / 2),
            y + Math.Max(2, tileHeight / 10),
            headWidth,
            headHeight);
        var indicatorSize = Math.Max(3, tileWidth / 8);
        var foot = view.FacingDirection switch
        {
            MyTurn.Domain.Direction.North => new Rectangle(x + ((tileWidth - indicatorSize) / 2), y + 3, indicatorSize, indicatorSize),
            MyTurn.Domain.Direction.South => new Rectangle(x + ((tileWidth - indicatorSize) / 2), y + tileHeight - indicatorSize - 3, indicatorSize, indicatorSize),
            MyTurn.Domain.Direction.West => new Rectangle(x + 4, y + ((tileHeight - indicatorSize) / 2), indicatorSize, indicatorSize),
            MyTurn.Domain.Direction.East => new Rectangle(x + tileWidth - indicatorSize - 4, y + ((tileHeight - indicatorSize) / 2), indicatorSize, indicatorSize),
            _ => new Rectangle(x + ((tileWidth - indicatorSize) / 2), y + tileHeight - indicatorSize - 3, indicatorSize, indicatorSize)
        };
        var outline = new Rectangle(
            x + ((tileWidth - Math.Max(body.Width, head.Width) - 4) / 2),
            y + 2,
            Math.Max(body.Width, head.Width) + 4,
            tileHeight - 5);

        _windows.Fill(new Rectangle(x + (tileWidth / 4), y + tileHeight - Math.Max(4, tileHeight / 7), tileWidth / 2, Math.Max(3, tileHeight / 10)), new Color(0, 0, 0, 120));
        _windows.Fill(body, new Color(36, 134, 168));
        _windows.Fill(head, new Color(236, 192, 126));
        _windows.Fill(foot, Color.Gold);
        _windows.Stroke(outline, new Color(16, 24, 38));
    }

    private static int CalculateScale(TileMapViewModel map, Rectangle bounds, WorldCameraViewModel? camera)
    {
        var visibleColumns = camera?.VisibleColumns ?? map.Width;
        var visibleRows = camera?.VisibleRows ?? map.Height;
        var horizontalScale = bounds.Width / Math.Max(1, visibleColumns * map.TileWidth);
        var verticalScale = bounds.Height / Math.Max(1, visibleRows * map.TileHeight);
        return Math.Clamp(Math.Min(horizontalScale, verticalScale), 1, 3);
    }

    private static (string? Symbol, Color Color) GetObjectMarker(string type)
    {
        return type.Replace(" ", string.Empty).Replace("-", string.Empty).ToUpperInvariant() switch
        {
            "ENEMY" or "ENCOUNTER" => ("!", Color.OrangeRed),
            "TREASURE" or "CHEST" => ("$", Color.Gold),
            "EXIT" => (">", Color.MediumPurple),
            "DOOR" => ("D", new Color(180, 112, 58)),
            "LOCKEDDOOR" => ("L", new Color(142, 98, 52)),
            "KEY" => ("K", Color.Gold),
            "PICKUP" => ("+", new Color(98, 196, 110)),
            "HAZARD" => ("!", Color.Red),
            "NPC" => ("N", new Color(88, 182, 190)),
            "SIGN" => ("?", new Color(190, 170, 98)),
            "BLOCKINGPROP" => ("#", Color.Gray),
            _ => (null, Color.Transparent)
        };
    }

    private static bool IsVisible(WorldPosition position, WorldCameraViewModel? camera)
    {
        return camera is null ||
            (position.X >= camera.MinTileX &&
                position.X <= camera.MaxTileX &&
                position.Y >= camera.MinTileY &&
                position.Y <= camera.MaxTileY);
    }
}
