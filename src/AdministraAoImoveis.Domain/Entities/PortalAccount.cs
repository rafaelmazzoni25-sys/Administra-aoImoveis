using AdministraAoImoveis.Domain.Enums;

namespace AdministraAoImoveis.Domain.Entities;

public sealed class PortalAccount : Entity
{
    private readonly List<PortalAccessLog> _logs = new();

    public PortalAccount(
        Guid id,
        string email,
        string name,
        PortalRole role,
        string hashedPassword) : base(id)
    {
        Email = email;
        Name = name;
        Role = role;
        HashedPassword = hashedPassword;
        Active = true;
        CreatedAt = DateTime.UtcNow;
        _logs.Add(new PortalAccessLog(DateTime.UtcNow, "Conta criada"));
    }

    public string Email { get; private set; }
    public string Name { get; private set; }
    public PortalRole Role { get; private set; }
    public string HashedPassword { get; private set; }
    public bool Active { get; private set; }
    public DateTime CreatedAt { get; }
    public IReadOnlyCollection<PortalAccessLog> Logs => _logs.AsReadOnly();

    public void Rename(string name)
    {
        Name = name;
        _logs.Add(new PortalAccessLog(DateTime.UtcNow, "Nome atualizado"));
    }

    public void ChangePassword(string hashedPassword)
    {
        HashedPassword = hashedPassword;
        _logs.Add(new PortalAccessLog(DateTime.UtcNow, "Senha redefinida"));
    }

    public void UpdateRole(PortalRole role)
    {
        Role = role;
        _logs.Add(new PortalAccessLog(DateTime.UtcNow, $"Perfil alterado para {role}"));
    }

    public void RecordAccess(string description)
    {
        _logs.Add(new PortalAccessLog(DateTime.UtcNow, description));
    }

    public void Activate()
    {
        Active = true;
        _logs.Add(new PortalAccessLog(DateTime.UtcNow, "Conta ativada"));
    }

    public void Deactivate(string reason)
    {
        Active = false;
        _logs.Add(new PortalAccessLog(DateTime.UtcNow, reason));
    }
}

public sealed record PortalAccessLog(DateTime OccurredAt, string Description);
