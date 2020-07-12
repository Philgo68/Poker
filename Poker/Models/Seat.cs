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

  [Serializable]
  public class Seat : IHandHolder
  {
    public BaseHand Hand { get; set; }
    public BasePlayer Player { get; set; }
    public int Chips { get; set; }
    public int ChipsMoving { get; set; }

    // Pre - calculated from display actions
    public int ChipsOut { get; set; }
    public int ChipsIn { get; set; }
    public int HandsWon { get; set; }

    public bool Button { get; set; }

    public Seat(BaseHand hand, BasePlayer player = null, int chips = 500)
    {
      Hand = hand;
      Player = player;
      Chips = chips;
      Button = true;
    }
  }
}
  
