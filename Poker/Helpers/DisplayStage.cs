using Microsoft.AspNetCore.Mvc;
using Poker.Helpers;
using System;
using System.Collections.Generic;
using Poker.Interfaces;
using System.Linq;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.CodeAnalysis.Operations;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

namespace Poker.Models
{

  public enum DisplayStage
  {
    DealtCards,
    BetsOut,
    Scooping,
    PotScooped,
    Delivering,
    DeliverPot
  }

}
