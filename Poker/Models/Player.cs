using Microsoft.AspNetCore.Identity;
using Poker.Data;
using Poker.Helpers;
using Poker.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Poker.Models
{
  public class Player : Entity
  {
    public string Name { get; set; }
    public int Bankroll { get; set; }

    public bool Computer { get; set; }
  }

}
