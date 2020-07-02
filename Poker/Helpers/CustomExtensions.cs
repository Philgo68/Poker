using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Poker.Helpers
{
  public static class CustomExtensions
  {
    public static string Classify(this string s)
    {
      return s == null ? s : Regex.Replace(s.Replace(" ", "-").Replace("_", "-"), @"(?<!-|^)([A-Z])", "-$1").ToLower();
    }
  }
}
