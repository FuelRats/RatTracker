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

    public static void RemoveAll<T>(this ICollection<T> collection, IEnumerable<T> elementsToRemove)
    {
      foreach (var element in elementsToRemove)
      {
        collection.Remove(element);
      }
    }
  }
}