using System.Collections.Generic;

namespace RatTracker.Infrastructure.Extensions
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