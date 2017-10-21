using System;

namespace RatTracker.Models.Api
{
  public abstract class ModelBase
  {
    public Guid Id { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public override bool Equals(object obj)
    {
      if (ReferenceEquals(null, obj))
      {
        return false;
      }

      if (ReferenceEquals(this, obj))
      {
        return true;
      }

      if (obj.GetType() != GetType())
      {
        return false;
      }

      return Equals((ModelBase) obj);
    }

    public override int GetHashCode()
    {
      // ReSharper disable once NonReadonlyMemberInGetHashCode
      return Id.GetHashCode();
    }

    public static bool operator ==(ModelBase left, ModelBase right)
    {
      return Equals(left, right);
    }

    public static bool operator !=(ModelBase left, ModelBase right)
    {
      return !Equals(left, right);
    }

    protected bool Equals(ModelBase other)
    {
      return Id.Equals(other.Id);
    }
  }
}