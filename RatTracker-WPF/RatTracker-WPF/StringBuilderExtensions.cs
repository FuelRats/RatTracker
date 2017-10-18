using System;
using System.Text;

namespace RatTracker_WPF
{
  public static class StringBuilderExtensions
  {
    public static StringBuilder AppendIf(this StringBuilder builder, bool? condition, string str)
    {
      bool realcondition;
      if (condition == null)
      {
        realcondition = false;
      }
      else
      {
        realcondition = (bool) condition;
      }

      if (builder == null)
      {
        throw new ArgumentNullException(nameof(builder));
      }

      if (realcondition)
      {
        builder.Append(str);
      }

      return builder;
    }
  }
}