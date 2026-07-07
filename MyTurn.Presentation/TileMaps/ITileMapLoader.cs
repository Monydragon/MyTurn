namespace MyTurn.Presentation;

public interface ITileMapLoader
{
    TileMapDefinition Load(string mapPath);
}
