using Poker.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Poker.Models
{
  public class TexasHoldem : PokerGame
  {
    public TexasHoldem() : base()
    {
    }

    public override void SetupPhases()
    {
      PhaseActions = new List<Func<TableDealer, DisplayStage[]>>();

      var chipMovingPhases = new DisplayStage[] { DisplayStage.DealtCards, DisplayStage.BetsOut, DisplayStage.Scooping, DisplayStage.PotScooped, DisplayStage.Delivering, DisplayStage.DeliverPot, DisplayStage.PotDelivered };

      //Reset table for new hand
      PhaseActions.Add((tableDealer) =>
      {
        var displayStages = StartNewHand(tableDealer);
        if (displayStages != null)
        {
          tableDealer.SetButton();
        }
        return displayStages;
      });

      PhaseActions.Add( HandsDealt );

      //Blinds and set first bet action
      PhaseActions.Add((tableDealer) =>
      {
        tableDealer.Table.PhaseTitle = "Preflop";
        tableDealer.Table.PhaseMessage = "";

        var seatsAroundTheTable = tableDealer.Table.SeatsWithHands().OrderBy(s => s.Position).ToArray();
        var players = seatsAroundTheTable.Count();
        var buttonSeat = Array.FindIndex<Seat>(seatsAroundTheTable, 0, seatsAroundTheTable.Count(), s => s.Button);

        var smallBlind = buttonSeat + 1;
        if (smallBlind == players) smallBlind = 0;
        seatsAroundTheTable[smallBlind].ChipsOut = 1;

        var bigBlind = smallBlind + 1;
        if (bigBlind == players) bigBlind = 0;
        seatsAroundTheTable[bigBlind].ChipsOut = 2;

        var betAction = bigBlind + 1;
        if (betAction == players) betAction = 0;
        seatsAroundTheTable[betAction].ActionOn = true;

        return new DisplayStage[] { DisplayStage.BetsOut, DisplayStage.SeatAction };
      });

      //Scoop Pot
      PhaseActions.Add((tableDealer) =>
      {
        tableDealer.Table.PhaseTitle = "Preflop";
        tableDealer.Table.PhaseMessage = "";
        return new DisplayStage[] { DisplayStage.Scooping, DisplayStage.PotScooped };
      });

      //Flop
      PhaseActions.Add((tableDealer) =>
      {
        tableDealer.Table.PhaseTitle = "Flop";
        tableDealer.Table.PhaseMessage = "";
        var board = GetBoard();
        board.CardsMask = tableDealer.Deck.DealCards(3);
        tableDealer.Table.SetBoard(board);

        var seatsAroundTheTable = tableDealer.Table.SeatsWithHands().OrderBy(s => s.Position).ToArray();
        var players = seatsAroundTheTable.Count();
        var buttonSeat = Array.FindIndex<Seat>(seatsAroundTheTable, 0, seatsAroundTheTable.Count(), s => s.Button);

        var smallBlind = buttonSeat + 1;
        if (smallBlind == players) smallBlind = 0;
        seatsAroundTheTable[smallBlind].ActionOn = true;
        
        return new DisplayStage[] { DisplayStage.DealtCards, DisplayStage.SeatAction };
      });

      //Scoop Pot
      PhaseActions.Add((tableDealer) =>
      {
        tableDealer.Table.PhaseTitle = "Flop";
        tableDealer.Table.PhaseMessage = "";
        return new DisplayStage[] { DisplayStage.Scooping, DisplayStage.PotScooped };
      });

      //Turn
      PhaseActions.Add((tableDealer) =>
      {
        tableDealer.Table.PhaseTitle = "Turn";
        tableDealer.Table.PhaseMessage = "";
        tableDealer.Table.Board.CardsMask |= tableDealer.Deck.DealCards(1);

        var seatsAroundTheTable = tableDealer.Table.SeatsWithHands().OrderBy(s => s.Position).ToArray();
        var players = seatsAroundTheTable.Count();
        var buttonSeat = Array.FindIndex<Seat>(seatsAroundTheTable, 0, seatsAroundTheTable.Count(), s => s.Button);

        var smallBlind = buttonSeat + 1;
        if (smallBlind == players) smallBlind = 0;
        seatsAroundTheTable[smallBlind].ActionOn = true;

        return new DisplayStage[] { DisplayStage.DealtCards, DisplayStage.SeatAction };
      });

      //Scoop Pot
      PhaseActions.Add((tableDealer) =>
      {
        tableDealer.Table.PhaseTitle = "Turn";
        tableDealer.Table.PhaseMessage = "";
        return new DisplayStage[] { DisplayStage.Scooping, DisplayStage.PotScooped };
      });

      //River
      PhaseActions.Add((tableDealer) =>
      {
        tableDealer.Table.PhaseTitle = "River";
        tableDealer.Table.PhaseMessage = "";
        tableDealer.Table.Board.CardsMask |= tableDealer.Deck.DealCards(1);

        var seatsAroundTheTable = tableDealer.Table.SeatsWithHands().OrderBy(s => s.Position).ToArray();
        var players = seatsAroundTheTable.Count();
        var buttonSeat = Array.FindIndex<Seat>(seatsAroundTheTable, 0, seatsAroundTheTable.Count(), s => s.Button);

        var smallBlind = buttonSeat + 1;
        if (smallBlind == players) smallBlind = 0;
        seatsAroundTheTable[smallBlind].ActionOn = true;

        return new DisplayStage[] { DisplayStage.DealtCards, DisplayStage.SeatAction };
      });

      //Scoop Pot
      PhaseActions.Add((tableDealer) =>
      {
        tableDealer.Table.PhaseTitle = "Showdown";
        tableDealer.Table.PhaseMessage = "";
        return new DisplayStage[] { DisplayStage.Scooping, DisplayStage.PotScooped };
      });

      // Award Pot
      PhaseActions.Add((tableDealer) =>
      {
        tableDealer.Table.PhaseTitle = "Showdown";
        tableDealer.Table.PhaseMessage = "";

        var table = tableDealer.Table;

        HandEvaluation best = new HandEvaluation();

        // Find best hand
        foreach (var seat in table.SeatsWithHands())
        {
          seat.Hand.LastEvaluation = TexasHoldem.EvaluateHand(seat.Hand.CardsMask | table.Board.CardsMask);
          if (seat.Hand.LastEvaluation.Value > best.Value)
          {
            best = seat.Hand.LastEvaluation;
          }
        }

        int pot = table.Pot;
        int winners = 0;
        int bestCard = 0;

        table.PhaseMessage = $"{TexasHoldem.HandTypeDescriptions[(int)best.Type]}";

        // Determine winners and the best card from the winners
        foreach (var seat in table.SeatsWithHands())
        {
            if (seat.Hand.LastEvaluation.Value == best.Value)
            {
              winners++;
              bestCard = Math.Max(bestCard, seat.Hand.BestCard());
              table.HighlightCards |= TexasHoldem.FindCards(best);
            }
        }

        int cut = pot / winners;
        int remainder = pot % winners;

        // Determine chips in
        foreach (var seat in table.SeatsWithHands())
        {
            if (seat.Hand.LastEvaluation.Value == best.Value)
            {
              seat.ChipsIn = cut + (seat.Hand.BestCard() == bestCard ? remainder : 0);
            }
        }

        return new DisplayStage[] { DisplayStage.Delivering, DisplayStage.DeliverPot, DisplayStage.PotDelivered };
      });

    }

    public override string Name { get { return "Texas Hold'em"; } }

    public override BaseDeck GetDeck(ulong dealtCards = 0)
    {
      return new StandardDeck(dealtCards);
    }

    public override BaseHand GetHand()
    {
      return new BaseHand(2);
    }

  }
}
