using Poker.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Poker.Models
{
  public class StandardDeck : BaseDeck
  {
    public override string Name => "Standard Deck";
    public override int Suits => 4;
    public override string[] SuitDescriptions => new string[] { "c", "d", "h", "s" };
    public override string[] SuitDescriptionsLong => new string[] { "Clubs", "Diamonds", "Hearts", "Spades" };
    public override int Ranks => 13;
    public override string[] RankDescriptions => new string[] { "2", "3", "4", "5", "6", "7", "8", "9", "T", "J", "Q", "K", "A" };
    public override string[] RankDescriptionsLong => new string[] { "2", "3", "4", "5", "6", "7", "8", "9", "T", "J", "Q", "K", "A" };
    public StandardDeck(ulong dealtCards = 0x0UL) : base(dealtCards) { }

    // 


    /// Evaluates a hand (passed as a hand mask) and returns a hand value.
    /// A hand value can be compared against another hand value to
    /// determine which has the higher value.
    /// </summary>
    /// <param name="cards">hand mask</param>
    /// <param name="numberOfCards">number of cards in the hand</param>
    /// <returns>hand value</returns>
    public override (int, uint) PokerEvaluate(ulong cards, int numberOfCards)
    {
      uint retval = 0, four_mask, three_mask, two_mask;
      int rettype = 0;

#if DEBUG
      // This functions supports 1-7 cards
      if (numberOfCards < 1 || numberOfCards > 7)
        throw new ArgumentOutOfRangeException("numberOfCards");
#endif
      // Seperate out by suit
      uint sc = (uint)((cards >> (0)) & 0x1fffUL);
      uint sd = (uint)((cards >> (13)) & 0x1fffUL);
      uint sh = (uint)((cards >> (26)) & 0x1fffUL);
      uint ss = (uint)((cards >> (39)) & 0x1fffUL);

      uint ranks = sc | sd | sh | ss;
      uint n_ranks = Bits.nBitsTable[ranks];
      uint n_dups = ((uint)(numberOfCards - n_ranks));

      /* Check for straight, flush, or straight flush, and return if we can
         determine immediately that this is the best possible hand 
      */
      if (n_ranks >= 5)
      {
        if (Bits.nBitsTable[ss] >= 5)
        {
          if (Bits.straightTable[ss] != 0)
            return ((int)HandTypes.StraightFlush, HANDTYPE_VALUE_STRAIGHTFLUSH + (uint)(Bits.straightTable[ss] << TOP_CARD_SHIFT));
          else
            (rettype, retval) = ((int)HandTypes.Flush, HANDTYPE_VALUE_FLUSH + Bits.topFiveCardsTable[ss]);
        }
        else if (Bits.nBitsTable[sc] >= 5)
        {
          if (Bits.straightTable[sc] != 0)
            return ((int)HandTypes.StraightFlush, HANDTYPE_VALUE_STRAIGHTFLUSH + (uint)(Bits.straightTable[sc] << TOP_CARD_SHIFT));
          else
            (rettype, retval) = ((int)HandTypes.Flush, HANDTYPE_VALUE_FLUSH + Bits.topFiveCardsTable[sc]);
        }
        else if (Bits.nBitsTable[sd] >= 5)
        {
          if (Bits.straightTable[sd] != 0)
            return ((int)HandTypes.StraightFlush, HANDTYPE_VALUE_STRAIGHTFLUSH + (uint)(Bits.straightTable[sd] << TOP_CARD_SHIFT));
          else
            (rettype, retval) = ((int)HandTypes.Flush, HANDTYPE_VALUE_FLUSH + Bits.topFiveCardsTable[sd]);
        }
        else if (Bits.nBitsTable[sh] >= 5)
        {
          if (Bits.straightTable[sh] != 0)
            return ((int)HandTypes.StraightFlush, HANDTYPE_VALUE_STRAIGHTFLUSH + (uint)(Bits.straightTable[sh] << TOP_CARD_SHIFT));
          else
            (rettype, retval) = ((int)HandTypes.Flush, HANDTYPE_VALUE_FLUSH + Bits.topFiveCardsTable[sh]);
        }
        else
        {
          uint st = Bits.straightTable[ranks];
          if (st != 0)
            (rettype, retval) = ((int)HandTypes.Straight, HANDTYPE_VALUE_STRAIGHT + (st << TOP_CARD_SHIFT));
        };

        /* 
           Another win -- if there can't be a FH/Quads (n_dups < 3), 
           which is true most of the time when there is a made hand, then if we've
           found a five card hand, just return.  This skips the whole process of
           computing two_mask/three_mask/etc.
        */
        if (retval != 0 && n_dups < 3)
          return (rettype, retval);
      }

      /*
       * By the time we're here, either: 
         1) there's no five-card hand possible (flush or straight), or
         2) there's a flush or straight, but we know that there are enough
            duplicates to make a full house / quads possible.  
       */
      switch (n_dups)
      {
        case 0:
          /* It's a no-pair hand */
          return ((int)HandTypes.HighCard, HANDTYPE_VALUE_HIGHCARD + Bits.topFiveCardsTable[ranks]);
        case 1:
          {
            /* It's a one-pair hand */
            uint t, kickers;

            two_mask = ranks ^ (sc ^ sd ^ sh ^ ss);

            retval = (uint)(HANDTYPE_VALUE_PAIR + (Bits.topCardTable[two_mask] << TOP_CARD_SHIFT));
            t = ranks ^ two_mask;      /* Only one bit set in two_mask */
            /* Get the top five cards in what is left, drop all but the top three 
             * cards, and shift them by one to get the three desired kickers */
            kickers = (Bits.topFiveCardsTable[t] >> CARD_WIDTH) & ~FIFTH_CARD_MASK;
            retval += kickers;
            return ((int)HandTypes.Pair, retval);
          }

        case 2:
          /* Either two pair or trips */
          two_mask = ranks ^ (sc ^ sd ^ sh ^ ss);
          if (two_mask != 0)
          {
            uint t = ranks ^ two_mask; /* Exactly two bits set in two_mask */
            retval = (uint)(HANDTYPE_VALUE_TWOPAIR
                + (Bits.topFiveCardsTable[two_mask]
                & (TOP_CARD_MASK | SECOND_CARD_MASK))
                + (Bits.topCardTable[t] << THIRD_CARD_SHIFT));

            return ((int)HandTypes.TwoPair, retval);
          }
          else
          {
            uint t, second;
            three_mask = ((sc & sd) | (sh & ss)) & ((sc & sh) | (sd & ss));
            retval = (uint)(HANDTYPE_VALUE_TRIPS + (Bits.topCardTable[three_mask] << TOP_CARD_SHIFT));
            t = ranks ^ three_mask; /* Only one bit set in three_mask */
            second = Bits.topCardTable[t];
            retval += (second << SECOND_CARD_SHIFT);
            t ^= (1U << (int)second);
            retval += (uint)(Bits.topCardTable[t] << THIRD_CARD_SHIFT);
            return ((int)HandTypes.Trips, retval);
          }

        default:
          /* Possible quads, fullhouse, straight or flush, or two pair */
          four_mask = sh & sd & sc & ss;
          if (four_mask != 0)
          {
            uint tc = Bits.topCardTable[four_mask];
            retval = (uint)(HANDTYPE_VALUE_FOUR_OF_A_KIND
                + (tc << TOP_CARD_SHIFT)
                + ((Bits.topCardTable[ranks ^ (1U << (int)tc)]) << SECOND_CARD_SHIFT));
            return ((int)HandTypes.FourOfAKind, retval);
          };

          /* Technically, three_mask as defined below is really the set of
             bits which are set in three or four of the suits, but since
             we've already eliminated quads, this is OK */
          /* Similarly, two_mask is really two_or_four_mask, but since we've
             already eliminated quads, we can use this shortcut */

          two_mask = ranks ^ (sc ^ sd ^ sh ^ ss);
          if (Bits.nBitsTable[two_mask] != n_dups)
          {
            /* Must be some trips then, which really means there is a 
               full house since n_dups >= 3 */
            uint tc, t;
            three_mask = ((sc & sd) | (sh & ss)) & ((sc & sh) | (sd & ss));
            retval = HANDTYPE_VALUE_FULLHOUSE;
            tc = Bits.topCardTable[three_mask];
            retval += (tc << TOP_CARD_SHIFT);
            t = (two_mask | three_mask) ^ (1U << (int)tc);
            retval += (uint)(Bits.topCardTable[t] << SECOND_CARD_SHIFT);
            return ((int)HandTypes.FullHouse, retval);
          };

          if (retval != 0) /* flush and straight */
            return (rettype, retval);
          else
          {
            /* Must be two pair */
            uint top, second;

            retval = HANDTYPE_VALUE_TWOPAIR;
            top = Bits.topCardTable[two_mask];
            retval += (top << TOP_CARD_SHIFT);
            second = Bits.topCardTable[two_mask ^ (1 << (int)top)];
            retval += (second << SECOND_CARD_SHIFT);
            retval += (uint)((Bits.topCardTable[ranks ^ (1U << (int)top) ^ (1 << (int)second)]) << THIRD_CARD_SHIFT);
            return ((int)HandTypes.TwoPair, retval);
          }
      }
    }

    public override (int, uint) OmahaEvaluate(ulong hand, ulong board)
    {
      var handcards = Bits.IndividualMasks(hand);
      var boardcards = Bits.IndividualMasks(board);
      int bestType, nextType;
      uint bestRank, nextRank;

      (bestType, bestRank) = PokerEvaluate(handcards[0] | handcards[1] | boardcards[0] | boardcards[1] | boardcards[2]);

      (nextType, nextRank) = PokerEvaluate(handcards[0] | handcards[1] | boardcards[0] | boardcards[1] | boardcards[3]);
      if (nextRank > bestRank) (bestType, bestRank) = (nextType, nextRank);

      (nextType, nextRank) = PokerEvaluate(handcards[0] | handcards[1] | boardcards[0] | boardcards[2] | boardcards[3]);
      if (nextRank > bestRank) (bestType, bestRank) = (nextType, nextRank);

      (nextType, nextRank) = PokerEvaluate(handcards[0] | handcards[1] | boardcards[1] | boardcards[2] | boardcards[3]);
      if (nextRank > bestRank) (bestType, bestRank) = (nextType, nextRank);

      (nextType, nextRank) = PokerEvaluate(handcards[0] | handcards[1] | boardcards[0] | boardcards[1] | boardcards[4]);
      if (nextRank > bestRank) (bestType, bestRank) = (nextType, nextRank);

      (nextType, nextRank) = PokerEvaluate(handcards[0] | handcards[1] | boardcards[0] | boardcards[2] | boardcards[4]);
      if (nextRank > bestRank) (bestType, bestRank) = (nextType, nextRank);

      (nextType, nextRank) = PokerEvaluate(handcards[0] | handcards[1] | boardcards[1] | boardcards[2] | boardcards[4]);
      if (nextRank > bestRank) (bestType, bestRank) = (nextType, nextRank);

      (nextType, nextRank) = PokerEvaluate(handcards[0] | handcards[1] | boardcards[0] | boardcards[3] | boardcards[4]);
      if (nextRank > bestRank) (bestType, bestRank) = (nextType, nextRank);

      (nextType, nextRank) = PokerEvaluate(handcards[0] | handcards[1] | boardcards[1] | boardcards[3] | boardcards[4]);
      if (nextRank > bestRank) (bestType, bestRank) = (nextType, nextRank);

      (nextType, nextRank) = PokerEvaluate(handcards[0] | handcards[1] | boardcards[2] | boardcards[3] | boardcards[4]);
      if (nextRank > bestRank) (bestType, bestRank) = (nextType, nextRank);



      (nextType, nextRank) = PokerEvaluate(handcards[0] | handcards[2] | boardcards[0] | boardcards[1] | boardcards[3]);
      if (nextRank > bestRank) (bestType, bestRank) = (nextType, nextRank);

      (nextType, nextRank) = PokerEvaluate(handcards[0] | handcards[2] | boardcards[0] | boardcards[2] | boardcards[3]);
      if (nextRank > bestRank) (bestType, bestRank) = (nextType, nextRank);

      (nextType, nextRank) = PokerEvaluate(handcards[0] | handcards[2] | boardcards[1] | boardcards[2] | boardcards[3]);
      if (nextRank > bestRank) (bestType, bestRank) = (nextType, nextRank);

      (nextType, nextRank) = PokerEvaluate(handcards[0] | handcards[2] | boardcards[0] | boardcards[1] | boardcards[4]);
      if (nextRank > bestRank) (bestType, bestRank) = (nextType, nextRank);

      (nextType, nextRank) = PokerEvaluate(handcards[0] | handcards[2] | boardcards[0] | boardcards[2] | boardcards[4]);
      if (nextRank > bestRank) (bestType, bestRank) = (nextType, nextRank);

      (nextType, nextRank) = PokerEvaluate(handcards[0] | handcards[2] | boardcards[1] | boardcards[2] | boardcards[4]);
      if (nextRank > bestRank) (bestType, bestRank) = (nextType, nextRank);

      (nextType, nextRank) = PokerEvaluate(handcards[0] | handcards[2] | boardcards[0] | boardcards[3] | boardcards[4]);
      if (nextRank > bestRank) (bestType, bestRank) = (nextType, nextRank);

      (nextType, nextRank) = PokerEvaluate(handcards[0] | handcards[2] | boardcards[1] | boardcards[3] | boardcards[4]);
      if (nextRank > bestRank) (bestType, bestRank) = (nextType, nextRank);

      (nextType, nextRank) = PokerEvaluate(handcards[0] | handcards[2] | boardcards[2] | boardcards[3] | boardcards[4]);
      if (nextRank > bestRank) (bestType, bestRank) = (nextType, nextRank);

      (nextType, nextRank) = PokerEvaluate(handcards[0] | handcards[2] | boardcards[2] | boardcards[3] | boardcards[4]);
      if (nextRank > bestRank) (bestType, bestRank) = (nextType, nextRank);



      (nextType, nextRank) = PokerEvaluate(handcards[0] | handcards[3] | boardcards[0] | boardcards[1] | boardcards[3]);
      if (nextRank > bestRank) (bestType, bestRank) = (nextType, nextRank);

      (nextType, nextRank) = PokerEvaluate(handcards[0] | handcards[3] | boardcards[0] | boardcards[2] | boardcards[3]);
      if (nextRank > bestRank) (bestType, bestRank) = (nextType, nextRank);

      (nextType, nextRank) = PokerEvaluate(handcards[0] | handcards[3] | boardcards[1] | boardcards[2] | boardcards[3]);
      if (nextRank > bestRank) (bestType, bestRank) = (nextType, nextRank);

      (nextType, nextRank) = PokerEvaluate(handcards[0] | handcards[3] | boardcards[0] | boardcards[1] | boardcards[4]);
      if (nextRank > bestRank) (bestType, bestRank) = (nextType, nextRank);

      (nextType, nextRank) = PokerEvaluate(handcards[0] | handcards[3] | boardcards[0] | boardcards[2] | boardcards[4]);
      if (nextRank > bestRank) (bestType, bestRank) = (nextType, nextRank);

      (nextType, nextRank) = PokerEvaluate(handcards[0] | handcards[3] | boardcards[1] | boardcards[2] | boardcards[4]);
      if (nextRank > bestRank) (bestType, bestRank) = (nextType, nextRank);

      (nextType, nextRank) = PokerEvaluate(handcards[0] | handcards[3] | boardcards[0] | boardcards[3] | boardcards[4]);
      if (nextRank > bestRank) (bestType, bestRank) = (nextType, nextRank);

      (nextType, nextRank) = PokerEvaluate(handcards[0] | handcards[3] | boardcards[1] | boardcards[3] | boardcards[4]);
      if (nextRank > bestRank) (bestType, bestRank) = (nextType, nextRank);

      (nextType, nextRank) = PokerEvaluate(handcards[0] | handcards[3] | boardcards[2] | boardcards[3] | boardcards[4]);
      if (nextRank > bestRank) (bestType, bestRank) = (nextType, nextRank);

      (nextType, nextRank) = PokerEvaluate(handcards[0] | handcards[3] | boardcards[2] | boardcards[3] | boardcards[4]);
      if (nextRank > bestRank) (bestType, bestRank) = (nextType, nextRank);



      (nextType, nextRank) = PokerEvaluate(handcards[1] | handcards[2] | boardcards[0] | boardcards[1] | boardcards[3]);
      if (nextRank > bestRank) (bestType, bestRank) = (nextType, nextRank);

      (nextType, nextRank) = PokerEvaluate(handcards[1] | handcards[2] | boardcards[0] | boardcards[2] | boardcards[3]);
      if (nextRank > bestRank) (bestType, bestRank) = (nextType, nextRank);

      (nextType, nextRank) = PokerEvaluate(handcards[1] | handcards[2] | boardcards[1] | boardcards[2] | boardcards[3]);
      if (nextRank > bestRank) (bestType, bestRank) = (nextType, nextRank);

      (nextType, nextRank) = PokerEvaluate(handcards[1] | handcards[2] | boardcards[0] | boardcards[1] | boardcards[4]);
      if (nextRank > bestRank) (bestType, bestRank) = (nextType, nextRank);

      (nextType, nextRank) = PokerEvaluate(handcards[1] | handcards[2] | boardcards[0] | boardcards[2] | boardcards[4]);
      if (nextRank > bestRank) (bestType, bestRank) = (nextType, nextRank);

      (nextType, nextRank) = PokerEvaluate(handcards[1] | handcards[2] | boardcards[1] | boardcards[2] | boardcards[4]);
      if (nextRank > bestRank) (bestType, bestRank) = (nextType, nextRank);

      (nextType, nextRank) = PokerEvaluate(handcards[1] | handcards[2] | boardcards[0] | boardcards[3] | boardcards[4]);
      if (nextRank > bestRank) (bestType, bestRank) = (nextType, nextRank);

      (nextType, nextRank) = PokerEvaluate(handcards[1] | handcards[2] | boardcards[1] | boardcards[3] | boardcards[4]);
      if (nextRank > bestRank) (bestType, bestRank) = (nextType, nextRank);

      (nextType, nextRank) = PokerEvaluate(handcards[1] | handcards[2] | boardcards[2] | boardcards[3] | boardcards[4]);
      if (nextRank > bestRank) (bestType, bestRank) = (nextType, nextRank);

      (nextType, nextRank) = PokerEvaluate(handcards[1] | handcards[2] | boardcards[2] | boardcards[3] | boardcards[4]);
      if (nextRank > bestRank) (bestType, bestRank) = (nextType, nextRank);



      (nextType, nextRank) = PokerEvaluate(handcards[1] | handcards[3] | boardcards[0] | boardcards[1] | boardcards[3]);
      if (nextRank > bestRank) (bestType, bestRank) = (nextType, nextRank);

      (nextType, nextRank) = PokerEvaluate(handcards[1] | handcards[3] | boardcards[0] | boardcards[2] | boardcards[3]);
      if (nextRank > bestRank) (bestType, bestRank) = (nextType, nextRank);

      (nextType, nextRank) = PokerEvaluate(handcards[1] | handcards[3] | boardcards[1] | boardcards[2] | boardcards[3]);
      if (nextRank > bestRank) (bestType, bestRank) = (nextType, nextRank);

      (nextType, nextRank) = PokerEvaluate(handcards[1] | handcards[3] | boardcards[0] | boardcards[1] | boardcards[4]);
      if (nextRank > bestRank) (bestType, bestRank) = (nextType, nextRank);

      (nextType, nextRank) = PokerEvaluate(handcards[1] | handcards[3] | boardcards[0] | boardcards[2] | boardcards[4]);
      if (nextRank > bestRank) (bestType, bestRank) = (nextType, nextRank);

      (nextType, nextRank) = PokerEvaluate(handcards[1] | handcards[3] | boardcards[1] | boardcards[2] | boardcards[4]);
      if (nextRank > bestRank) (bestType, bestRank) = (nextType, nextRank);

      (nextType, nextRank) = PokerEvaluate(handcards[1] | handcards[3] | boardcards[0] | boardcards[3] | boardcards[4]);
      if (nextRank > bestRank) (bestType, bestRank) = (nextType, nextRank);

      (nextType, nextRank) = PokerEvaluate(handcards[1] | handcards[3] | boardcards[1] | boardcards[3] | boardcards[4]);
      if (nextRank > bestRank) (bestType, bestRank) = (nextType, nextRank);

      (nextType, nextRank) = PokerEvaluate(handcards[1] | handcards[3] | boardcards[2] | boardcards[3] | boardcards[4]);
      if (nextRank > bestRank) (bestType, bestRank) = (nextType, nextRank);

      (nextType, nextRank) = PokerEvaluate(handcards[1] | handcards[3] | boardcards[2] | boardcards[3] | boardcards[4]);
      if (nextRank > bestRank) (bestType, bestRank) = (nextType, nextRank);



      (nextType, nextRank) = PokerEvaluate(handcards[2] | handcards[3] | boardcards[0] | boardcards[1] | boardcards[3]);
      if (nextRank > bestRank) (bestType, bestRank) = (nextType, nextRank);

      (nextType, nextRank) = PokerEvaluate(handcards[2] | handcards[3] | boardcards[0] | boardcards[2] | boardcards[3]);
      if (nextRank > bestRank) (bestType, bestRank) = (nextType, nextRank);

      (nextType, nextRank) = PokerEvaluate(handcards[2] | handcards[3] | boardcards[1] | boardcards[2] | boardcards[3]);
      if (nextRank > bestRank) (bestType, bestRank) = (nextType, nextRank);

      (nextType, nextRank) = PokerEvaluate(handcards[2] | handcards[3] | boardcards[0] | boardcards[1] | boardcards[4]);
      if (nextRank > bestRank) (bestType, bestRank) = (nextType, nextRank);

      (nextType, nextRank) = PokerEvaluate(handcards[2] | handcards[3] | boardcards[0] | boardcards[2] | boardcards[4]);
      if (nextRank > bestRank) (bestType, bestRank) = (nextType, nextRank);

      (nextType, nextRank) = PokerEvaluate(handcards[2] | handcards[3] | boardcards[1] | boardcards[2] | boardcards[4]);
      if (nextRank > bestRank) (bestType, bestRank) = (nextType, nextRank);

      (nextType, nextRank) = PokerEvaluate(handcards[2] | handcards[3] | boardcards[0] | boardcards[3] | boardcards[4]);
      if (nextRank > bestRank) (bestType, bestRank) = (nextType, nextRank);

      (nextType, nextRank) = PokerEvaluate(handcards[2] | handcards[3] | boardcards[1] | boardcards[3] | boardcards[4]);
      if (nextRank > bestRank) (bestType, bestRank) = (nextType, nextRank);

      (nextType, nextRank) = PokerEvaluate(handcards[2] | handcards[3] | boardcards[2] | boardcards[3] | boardcards[4]);
      if (nextRank > bestRank) (bestType, bestRank) = (nextType, nextRank);

      (nextType, nextRank) = PokerEvaluate(handcards[2] | handcards[3] | boardcards[2] | boardcards[3] | boardcards[4]);
      if (nextRank > bestRank) (bestType, bestRank) = (nextType, nextRank);

      return (bestType, bestRank);
    }

    public (int, uint) OmahaEvaluateNew(ulong cards, int numberOfCards)
    {
      uint retval = 0, four_mask, three_mask, two_mask;
      int rettype = 0;

#if DEBUG
      // This functions supports 1-7 cards
      if (numberOfCards < 1 || numberOfCards > 7)
        throw new ArgumentOutOfRangeException("numberOfCards");
#endif
      // Seperate out by suit
      uint sc = (uint)((cards >> (0)) & 0x1fffUL);
      uint sd = (uint)((cards >> (13)) & 0x1fffUL);
      uint sh = (uint)((cards >> (26)) & 0x1fffUL);
      uint ss = (uint)((cards >> (39)) & 0x1fffUL);

      uint ranks = sc | sd | sh | ss;
      uint n_ranks = Bits.nBitsTable[ranks];
      uint n_dups = ((uint)(numberOfCards - n_ranks));

      /* Check for straight, flush, or straight flush, and return if we can
         determine immediately that this is the best possible hand 
      */
      if (n_ranks >= 5)
      {
        if (Bits.nBitsTable[ss] >= 5)
        {
          if (Bits.straightTable[ss] != 0)
            return ((int)HandTypes.StraightFlush, HANDTYPE_VALUE_STRAIGHTFLUSH + (uint)(Bits.straightTable[ss] << TOP_CARD_SHIFT));
          else
            retval = HANDTYPE_VALUE_FLUSH + Bits.topFiveCardsTable[ss];
        }
        else if (Bits.nBitsTable[sc] >= 5)
        {
          if (Bits.straightTable[sc] != 0)
            return ((int)HandTypes.StraightFlush, HANDTYPE_VALUE_STRAIGHTFLUSH + (uint)(Bits.straightTable[sc] << TOP_CARD_SHIFT));
          else
            retval = HANDTYPE_VALUE_FLUSH + Bits.topFiveCardsTable[sc];
        }
        else if (Bits.nBitsTable[sd] >= 5)
        {
          if (Bits.straightTable[sd] != 0)
            return ((int)HandTypes.StraightFlush, HANDTYPE_VALUE_STRAIGHTFLUSH + (uint)(Bits.straightTable[sd] << TOP_CARD_SHIFT));
          else
            retval = HANDTYPE_VALUE_FLUSH + Bits.topFiveCardsTable[sd];
        }
        else if (Bits.nBitsTable[sh] >= 5)
        {
          if (Bits.straightTable[sh] != 0)
            return ((int)HandTypes.StraightFlush, HANDTYPE_VALUE_STRAIGHTFLUSH + (uint)(Bits.straightTable[sh] << TOP_CARD_SHIFT));
          else
            (rettype, retval) = ((int)HandTypes.Flush, HANDTYPE_VALUE_FLUSH + Bits.topFiveCardsTable[sh]);
        }
        else
        {
          uint st = Bits.straightTable[ranks];
          if (st != 0)
            (rettype, retval) = ((int)HandTypes.Straight, HANDTYPE_VALUE_STRAIGHT + (st << TOP_CARD_SHIFT));
        };

        /* 
           Another win -- if there can't be a FH/Quads (n_dups < 3), 
           which is true most of the time when there is a made hand, then if we've
           found a five card hand, just return.  This skips the whole process of
           computing two_mask/three_mask/etc.
        */
        if (retval != 0 && n_dups < 3)
          return (rettype, retval);
      }

      /*
       * By the time we're here, either: 
         1) there's no five-card hand possible (flush or straight), or
         2) there's a flush or straight, but we know that there are enough
            duplicates to make a full house / quads possible.  
       */
      switch (n_dups)
      {
        case 0:
          /* It's a no-pair hand */
          return ((int)HandTypes.HighCard, HANDTYPE_VALUE_HIGHCARD + Bits.topFiveCardsTable[ranks]);
        case 1:
          {
            /* It's a one-pair hand */
            uint t, kickers;

            two_mask = ranks ^ (sc ^ sd ^ sh ^ ss);

            retval = (uint)(HANDTYPE_VALUE_PAIR + (Bits.topCardTable[two_mask] << TOP_CARD_SHIFT));
            t = ranks ^ two_mask;      /* Only one bit set in two_mask */
            /* Get the top five cards in what is left, drop all but the top three 
             * cards, and shift them by one to get the three desired kickers */
            kickers = (Bits.topFiveCardsTable[t] >> CARD_WIDTH) & ~FIFTH_CARD_MASK;
            retval += kickers;
            return ((int)HandTypes.Pair, retval);
          }

        case 2:
          /* Either two pair or trips */
          two_mask = ranks ^ (sc ^ sd ^ sh ^ ss);
          if (two_mask != 0)
          {
            uint t = ranks ^ two_mask; /* Exactly two bits set in two_mask */
            retval = (uint)(HANDTYPE_VALUE_TWOPAIR
                + (Bits.topFiveCardsTable[two_mask]
                & (TOP_CARD_MASK | SECOND_CARD_MASK))
                + (Bits.topCardTable[t] << THIRD_CARD_SHIFT));

            return ((int)HandTypes.TwoPair, retval);
          }
          else
          {
            uint t, second;
            three_mask = ((sc & sd) | (sh & ss)) & ((sc & sh) | (sd & ss));
            retval = (uint)(HANDTYPE_VALUE_TRIPS + (Bits.topCardTable[three_mask] << TOP_CARD_SHIFT));
            t = ranks ^ three_mask; /* Only one bit set in three_mask */
            second = Bits.topCardTable[t];
            retval += (second << SECOND_CARD_SHIFT);
            t ^= (1U << (int)second);
            retval += (uint)(Bits.topCardTable[t] << THIRD_CARD_SHIFT);
            return ((int)HandTypes.Trips, retval);
          }

        default:
          /* Possible quads, fullhouse, straight or flush, or two pair */
          four_mask = sh & sd & sc & ss;
          if (four_mask != 0)
          {
            uint tc = Bits.topCardTable[four_mask];
            retval = (uint)(HANDTYPE_VALUE_FOUR_OF_A_KIND
                + (tc << TOP_CARD_SHIFT)
                + ((Bits.topCardTable[ranks ^ (1U << (int)tc)]) << SECOND_CARD_SHIFT));
            return ((int)HandTypes.FourOfAKind, retval);
          };

          /* Technically, three_mask as defined below is really the set of
             bits which are set in three or four of the suits, but since
             we've already eliminated quads, this is OK */
          /* Similarly, two_mask is really two_or_four_mask, but since we've
             already eliminated quads, we can use this shortcut */

          two_mask = ranks ^ (sc ^ sd ^ sh ^ ss);
          if (Bits.nBitsTable[two_mask] != n_dups)
          {
            /* Must be some trips then, which really means there is a 
               full house since n_dups >= 3 */
            uint tc, t;
            three_mask = ((sc & sd) | (sh & ss)) & ((sc & sh) | (sd & ss));
            retval = HANDTYPE_VALUE_FULLHOUSE;
            tc = Bits.topCardTable[three_mask];
            retval += (tc << TOP_CARD_SHIFT);
            t = (two_mask | three_mask) ^ (1U << (int)tc);
            retval += (uint)(Bits.topCardTable[t] << SECOND_CARD_SHIFT);
            return ((int)HandTypes.FullHouse, retval);
          };

          if (retval != 0) /* flush and straight */
            return (rettype, retval);
          else
          {
            /* Must be two pair */
            uint top, second;

            retval = HANDTYPE_VALUE_TWOPAIR;
            top = Bits.topCardTable[two_mask];
            retval += (top << TOP_CARD_SHIFT);
            second = Bits.topCardTable[two_mask ^ (1 << (int)top)];
            retval += (second << SECOND_CARD_SHIFT);
            retval += (uint)((Bits.topCardTable[ranks ^ (1U << (int)top) ^ (1 << (int)second)]) << THIRD_CARD_SHIFT);
            return ((int)HandTypes.TwoPair, retval);
          }
      }
    }

    /// <summary>
    /// Possible types of hands in a texas holdem game.
    /// </summary>
    public enum HandTypes
    {
      /// <summary>
      /// Only a high card
      /// </summary>
      HighCard = 0,
      /// <summary>
      /// One Pair
      /// </summary>
      Pair = 1,
      /// <summary>
      /// Two Pair
      /// </summary>
      TwoPair = 2,
      /// <summary>
      /// Three of a kind (Trips)
      /// </summary>
      Trips = 3,
      /// <summary>
      /// Straight
      /// </summary>
      Straight = 4,
      /// <summary>
      /// Flush
      /// </summary>
      Flush = 5,
      /// <summary>
      /// FullHouse
      /// </summary>
      FullHouse = 6,
      /// <summary>
      /// Four of a kind
      /// </summary>
      FourOfAKind = 7,
      /// <summary>
      /// Straight Flush
      /// </summary>
      StraightFlush = 8
    }

    /// <exclude/>
    private static readonly int CARD_WIDTH = 4;
    /// <exclude/>
    private static readonly int TOP_CARD_SHIFT = 16;
    /// <exclude/>
    private static readonly int SECOND_CARD_SHIFT = 12;
    /// <exclude/>
    private static readonly int THIRD_CARD_SHIFT = 8;
    /// <exclude/>
    private static readonly int FOURTH_CARD_SHIFT = 4;
    /// <exclude/>
    private static readonly int FIFTH_CARD_SHIFT = 0;
    /// <exclude/>
    private static readonly System.UInt32 TOP_CARD_MASK = 0x000F0000;
    /// <exclude/>
    private static readonly System.UInt32 SECOND_CARD_MASK = 0x0000F000;
    /// <exclude/>
    private static readonly System.UInt32 FIFTH_CARD_MASK = 0x0000000F;
    /// <exclude/>
    private static readonly int HANDTYPE_SHIFT = 24;
    /// <exclude/>
    private static readonly uint HANDTYPE_VALUE_STRAIGHTFLUSH = (((uint)HandTypes.StraightFlush) << HANDTYPE_SHIFT);
    /// <exclude/>
    private static readonly uint HANDTYPE_VALUE_STRAIGHT = (((uint)HandTypes.Straight) << HANDTYPE_SHIFT);
    /// <exclude/>
    private static readonly uint HANDTYPE_VALUE_FLUSH = (((uint)HandTypes.Flush) << HANDTYPE_SHIFT);
    /// <exclude/>
    private static readonly uint HANDTYPE_VALUE_FULLHOUSE = (((uint)HandTypes.FullHouse) << HANDTYPE_SHIFT);
    /// <exclude/>
    private static readonly uint HANDTYPE_VALUE_FOUR_OF_A_KIND = (((uint)HandTypes.FourOfAKind) << HANDTYPE_SHIFT);
    /// <exclude/>
    private static readonly uint HANDTYPE_VALUE_TRIPS = (((uint)HandTypes.Trips) << HANDTYPE_SHIFT);
    /// <exclude/>
    private static readonly uint HANDTYPE_VALUE_TWOPAIR = (((uint)HandTypes.TwoPair) << HANDTYPE_SHIFT);
    /// <exclude/>
    private static readonly uint HANDTYPE_VALUE_PAIR = (((uint)HandTypes.Pair) << HANDTYPE_SHIFT);
    /// <exclude/>
    private static readonly uint HANDTYPE_VALUE_HIGHCARD = (((uint)HandTypes.HighCard) << HANDTYPE_SHIFT);
    /// <exclude/>
  }
}
