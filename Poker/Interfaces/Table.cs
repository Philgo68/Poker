using Microsoft.AspNetCore.Mvc;
using Poker.Helpers;
using Poker.Models;
using System;
using System.Collections.Generic;
using System.Security.Policy;

namespace Poker.Interfaces
{
	// Groups and orders the players, controls the cards and executes the game flow.
	public interface ITable : IHandHolder
	{
		public string Name { get; }

		public void Reset(ulong dealtCards = 0x0UL);

		public void PlayHand();

		public long PlayHandTrials(double duration, long iterations);
		public Seat AddHand(IHand hand, IPlayer player = null);
		public void SetBoard(IHand board);
		
		public IEnumerable<Seat> Opponents();
		public Seat Hero();
	}
}
