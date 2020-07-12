using Poker.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Poker.Models
{
  [Serializable]
  public class BasePlayer

  {
    public string Name { get; set; }
    public bool Computer { get; set; }
  }
}
