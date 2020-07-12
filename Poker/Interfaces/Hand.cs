using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.Threading;
using System.Threading.Tasks;
using Poker.CFHandEvaluator;
using Poker.Helpers;
using Poker.Models;

namespace Poker.Interfaces
{
  public interface IHandHolder
  {
    public BaseHand Hand { get; }
  }
}
