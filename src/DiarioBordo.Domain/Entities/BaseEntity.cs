namespace DiarioBordo.Domain.Entities;

/// <summary>
/// Classe base para todas as entidades do domínio
/// </summary>
public abstract class BaseEntity
{
    public virtual int Id { get; set; }
    public virtual DateTime DataCriacao { get; set; } = DateTime.UtcNow;
    public virtual DateTime? DataModificacao { get; set; }

    public override bool Equals(object? obj)
    {
        if (obj is not BaseEntity other)
            return false;

        if (ReferenceEquals(this, other))
            return true;

        if (GetType() != other.GetType())
            return false;

        if (Id == 0 || other.Id == 0)
            return false;

        return Id == other.Id;
    }

    public override int GetHashCode()
    {
        return Id.GetHashCode();
    }
}