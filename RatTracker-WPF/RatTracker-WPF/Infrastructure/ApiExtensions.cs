namespace RatTracker_WPF.Infrastructure
{
  public static class ApiExtensions
  {
    public static string ToApiName(this object value)
    {
      return value.ToString().ToLower();
    }
  }
}