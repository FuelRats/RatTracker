using System.Collections.Generic;

namespace RatTracker_WPF.Infrastructure.Extensions
{
  public static class EnumerableExtensions
  {
    public static void AddAll<T>(this ICollection<T> collection, IEnumerable<T> elementsToAdd)
    {
      foreach (var element in elementsToAdd)
      {
        collection.Add(element);
      }
    }
  }
}