namespace AdministraAoImoveis.Domain.Entities;

public abstract class Entity
{
    protected Entity(Guid id)
    {
        Id = id == Guid.Empty ? Guid.NewGuid() : id;
    }

    public Guid Id { get; protected init; }
}
