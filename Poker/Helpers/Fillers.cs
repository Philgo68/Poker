﻿using Poker.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace Poker.Helpers
{
  public class Fillers : List<IHand>
  {
    public IEnumerable<List<IHand>> CompletedHands(IDeck deck, IHand board, int threadNum, int threadCnt, Random rand, double duration, long iterations = 0)
    {
      // detemine the amount of possible work and prep for dealing all combinations
      var cardsLeft = deck.RemainingInDeck;
      var shuffleCnt = 1;
      var totalIterations = 1;

      var newHand = new List<bool>();
      var hands = new List<IHand>();

      board.CardsMask = 0;
      for (var i = 1; i <= board.CardCount; i++)
      {
        totalIterations = (totalIterations * cardsLeft--) / shuffleCnt++;
        newHand.Add(i == 1);
        hands.Add(board);
      }

      foreach (var hand in this)
      {
        hand.CardsMask = 0;
        shuffleCnt = 1;
        for (var i = 1; i <= hand.CardCount; i++)
        {
          totalIterations = (totalIterations * cardsLeft--) / shuffleCnt++;
          newHand.Add(i == 1);
          hands.Add(hand);
        }
      }

      // deal out all combinations
     if (duration == 0 && (iterations == 0 || (iterations * threadCnt) > totalIterations))
      {
        var start = 0x1UL << (deck.CardCount - 1);
        var levelZeroStart = start >> (threadNum - 1); 

        var returnCards = new ulong[newHand.Count];

        var level = 0;
        do
        {
          // If we are just coming into this level, start at the start or the card after the previous level
          if (returnCards[level] == 0)
            returnCards[level] = newHand[level] ? (level == 0 ? levelZeroStart : start) : returnCards[level - 1] >> 1;
          else
          {
            // undeal the last card
            deck.DealtCards ^= returnCards[level];
            hands[level].CardsMask ^= returnCards[level];
            returnCards[level] >>= (level == 0 ? threadCnt : 1);
          }

          // skip if it's already been dealt
          while ((returnCards[level] > 0) && ((returnCards[level] & deck.DealtCards) > 0))
            returnCards[level] >>= (level == 0 ? threadCnt : 1);

          // If we've past the end of this level, go back a level
          if (returnCards[level] == 0)
            level--;
          // otherwise use this card and move on
          else
          {
            // deal this card
            deck.DealtCards ^= returnCards[level];
            hands[level].CardsMask ^= returnCards[level];
            // not that this filler has chnaged
            hands[level].Changed = true;
            // goto next level
            level++;
          }

          // if we are the whole way in, so return the cards
          if (level == newHand.Count)
          {
            yield return this;
            level--;
          }
        } while (returnCards[0] > 0);
      }
      // deal iterations number of random hands
      else if (iterations > 0)
      {
        var dealtCards = deck.DealtCards;
        long cnt = 0;
        do
        {
          board.CardsMask = deck.DealCards(board.CardCount, rand);
          board.Changed = true;
          foreach (var hand in this)
          {
            hand.CardsMask = deck.DealCards(hand.CardCount, rand);
            hand.Changed = true;
          }
          deck.Reset(dealtCards);
          yield return this;
          cnt++;
        } while (cnt < iterations);
      }
      // deal random hands until 'end' time
      else
      {
        var dealtCards = deck.DealtCards;
        long end = Convert.ToInt64(Stopwatch.GetTimestamp() + (duration * Stopwatch.Frequency));
        do
        {
          board.CardsMask = deck.DealCards(board.CardCount, rand);
          board.Changed = true;
          foreach (var hand in this)
          {
            hand.CardsMask = deck.DealCards(hand.CardCount, rand);
            hand.Changed = true;
          }
          deck.Reset(dealtCards);
          yield return this;
        } while ((Stopwatch.GetTimestamp()) < end);
      }
    }
  }
}
