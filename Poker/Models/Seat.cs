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
using System.ComponentModel.DataAnnotations.Schema;

namespace Poker.Models
{
  public class Entity
  {
    //public Guid Id { get; set; } = Guid.NewGuid();
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public Guid Id { get; set; }
  }

  public class Seat : Entity, IHandHolder
  {
    public Player Player { get; set; }
    public int Chips { get; set; }
    public int Position { get; set; }

    [NotMapped]
    public BaseHand Hand { get; set; }
    [NotMapped]
    public int ChipsMoving { get; set; }

    // Pre - calculated from display actions
    [NotMapped]
    public int ChipsOut { get; set; }
    [NotMapped]
    public int ChipsIn { get; set; }
    [NotMapped]
    public int HandsWon { get; set; }
    [NotMapped]
    public bool Button { get; set; }

    public Seat()
    {
      Position = -1;
      Hand = null;
      Player = null;
      Chips = 0;
      Button = false;
    }

    public Seat(int position, Player player = null, int chips = 500)
    {
      Position = position;
      Hand = null;
      Player = player;
      Chips = chips;
      Button = false;
    }
  }
}
  
