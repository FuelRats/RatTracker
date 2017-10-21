namespace RatTracker.Infrastructure.Extensions
{
  public static class ApiExtensions
  {
    public static string ToApiName(this object value)
    {
      return value.ToString().ToLower();
    }
  }
}