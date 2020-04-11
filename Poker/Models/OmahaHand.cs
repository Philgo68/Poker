using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Poker.Models
{
  public class OmahaHand : BaseHand
  {
    public override string Name { get { return "Omaha"; } }
    public override byte CardCount { get { return 4; } }
    public OmahaHand() : base() { }
    public OmahaHand(ulong cardsMask) : base(cardsMask) { }
    public override (int, uint) Evaluate(ulong board, ulong filler = 0x0UL)
    {
      return base.OmahaEvaluate(board, filler);
    }
  }
}
