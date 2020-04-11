using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.SignalR.Protocol;
using Poker.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Poker.Models
{
  public class TaiwaneseHand : BaseHand
  {
    public override string Name => "Taiwanese";
    public override byte CardCount => 7;
    public override string Description => $"{TopHand.Description} / {MiddleHand.Description} / {BottomHand.Description}";

    public OneCardHand TopHand { get; set; }
    public HoldemHand MiddleHand { get; set; }
    public OmahaHand BottomHand { get; set; }

    public override int CompareTo(object obj)
    {
      return -RunningScore.CompareTo(((TaiwaneseHand)obj).RunningScore);
    }

    public TaiwaneseHand() : base() {}

    public TaiwaneseHand(OneCardHand topCardMask, HoldemHand middleHandMask, OmahaHand bottomHandMask) : this()
    {
      TopHand = topCardMask;
      MiddleHand = middleHandMask;
      BottomHand = bottomHandMask;
    }

    public int PointTopHand(int handType)
    {
      return handType switch
      {
        2 => 1,
        3 => 2,
        4 => 3,
        5 => 3,
        6 => 4,
        7 => 6,
        8 => 12,
        _ => 0
      };
    }

    public int PointMiddleHand(int handType)
    {
      return handType switch
      {
        2 => 0,
        3 => 1,
        4 => 2,
        5 => 2,
        6 => 3,
        7 => 5,
        8 => 10,
        _ => 0
      };
    }

    public int PointBottomHand(int handType)
    {
      return handType switch
      {
        2 => 0,
        3 => 0,
        4 => 0,
        5 => 0,
        6 => 2,
        7 => 4,
        8 => 8,
        _ => 0
      };
    }

    public int ScoreTopHand(ulong boardmask)
    {
      (var handType, _) = TopHand.Evaluate(boardmask);
      return PointTopHand(handType);
    }

    public int ScoreMiddleHand(ulong boardmask)
    {
      (var handType, _) = MiddleHand.Evaluate(boardmask);
      return PointMiddleHand(handType);
    }

    public int ScoreBottomHand(ulong boardmask)
    {
      (var handType, _) = BottomHand.Evaluate(boardmask);
      return PointBottomHand(handType);
    }

    public void ScoreAgainst(ulong boardmask)
    {
      RunningScore += ScoreTopHand(boardmask) + ScoreMiddleHand(boardmask) + ScoreBottomHand(boardmask);
    }

    public void PlayAgainst(TaiwaneseHand[] otherHands, ulong boardmask)
    {
      if (otherHands.Length == 1)
      {
        PlayAgainst(otherHands[0], boardmask);
        return;
      }

      // Play top hands
      (var myType, var myRank) = TopHand.Evaluate(boardmask);
      (var bestType, var bestRank) = (myType, myRank);
      var tiedHands = 0;
      foreach (var otherHand in otherHands)
      {
        (var type, var rank) = otherHand.Evaluate(boardmask);
        if (rank > bestRank)
        {
          (bestType, bestRank) = (type, rank);
          tiedHands = 0;
        } else if (rank == bestRank)
        {
          tiedHands++;
        }
      }


      // scoop bonus of 3 if you win them all
    }

    public void PlayAgainst(TaiwaneseHand otherHand, ulong boardmask)
    {
      // Play top hands
      int winCnt = 0;
      (var myType, var myRank) = TopHand.Evaluate(boardmask);
      (var oType, var oRank) = otherHand.TopHand.Evaluate(boardmask);
      switch (myRank.CompareTo(oRank)) 
      {
        case 1:
          Wins += 1 + ((myType == oType) ? 0 : PointTopHand(myType));
          winCnt++;
          break;
        case -1:
          Loses += 1 + ((myType == oType) ? 0 : PointTopHand(oType));
          winCnt--;
          break;
        default:
          break;
      };

      // Play middle hands
      (myType, myRank) = MiddleHand.Evaluate(boardmask);
      (oType, oRank) = otherHand.MiddleHand.Evaluate(boardmask);
      switch (myRank.CompareTo(oRank))
      {
        case 1:
          Wins += 2 + ((myType == oType) ? 0 : PointMiddleHand(myType));
          winCnt++;
          break;
        case -1:
          Loses += 2 + ((myType == oType) ? 0 : PointMiddleHand(oType));
          winCnt--;
          break;
        default:
          break;
      };

      // Play bottom hands
      (myType, myRank) = BottomHand.Evaluate(boardmask);
      (oType, oRank) = otherHand.BottomHand.Evaluate(boardmask);
      switch (myRank.CompareTo(oRank))
      {
        case 1:
          Wins += 3 + ((myType == oType) ? 0 : PointBottomHand(myType)) + ((winCnt == 2) ? 3 : 0);
          break;
        case -1:
          Loses += 3 + ((myType == oType) ? 0 : PointBottomHand(oType)) + ((winCnt == -2) ? 3 : 0);
          break;
        default:
          break;
      };


    }

    public static TaiwaneseHand[] AllSetups(ulong cardsMask)
    {
      TaiwaneseHand[] hands = new TaiwaneseHand[105];
      var cards = Bits.IndividualMasks(cardsMask);
      int i = 0;
      for (int _c1 = 6; _c1 >= 0; _c1--)
      {
        var _card1 = cards[_c1];
        for (int _c2 = 6; _c2 >= 0; _c2--)
        {
          if (_c2 == _c1) continue;
          var _card2 = cards[_c2];
          for (int _c3 = _c2 - 1; _c3 >= 0; _c3--)
          {
            if (_c3 == _c1) continue;
            var _card3 = cards[_c3];
            var _dead = _card1 | _card2 | _card3;
            for (int _c4 = 6; _c4 >= 0; _c4--)
            {
              var _card4 = cards[_c4];
              if ((_card4 & _dead) != 0) continue;
              for (int _c5 = _c4 - 1; _c5 >= 0; _c5--)
              {
                var _card5 = cards[_c5];
                if ((_card5 & _dead) != 0) continue;
                for (int _c6 = _c5 - 1; _c6 >= 0; _c6--)
                {
                  var _card6 = cards[_c6];
                  if ((_card6 & _dead) != 0) continue;
                  for (int _c7 = _c6 - 1; _c7 >= 0; _c7--)
                  {
                    var _card7 = cards[_c7];
                    if ((_card7 & _dead) != 0) continue;
                    hands[i++] = new TaiwaneseHand(new OneCardHand(_card1), new HoldemHand(_card2 | _card3), new OmahaHand(_card4 | _card5 | _card6 | _card7));
                  }
                }
              }
            }
          }

        }

      }
      return hands;
    }

  }
}
