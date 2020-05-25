using Microsoft.AspNetCore.Mvc;
using Poker.Helpers;
using Poker.Models;
using System;
using System.Collections.Generic;

namespace Poker.Interfaces
{
	public interface IGame
	// Owns the rules, evaluation and mechanics of the game.
	{
		public string Name { get; }
		public IDeck GetDeck(ulong dealtCards = 0x0UL);
		public IHand GetHand();
		public IHand GetBoard();
		public DisplayStage[] ExecutePhase(int game_phase, BaseTable table);
		public (int, uint) Evaluate(IHand hand);
		public (int, uint) Evaluate(IHand hand, IHand board);
		public (int, uint) Evaluate(ulong hand, ulong board);
		public (int, uint) Evaluate(ulong cards);
	}
}
