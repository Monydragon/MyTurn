namespace MyTurn.Domain;

public interface IEntity
{
    Guid Id { get; }
    string Name { get; set; }
}
