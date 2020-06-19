using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.Threading;
using System.Threading.Tasks;
using Poker.CFHandEvaluator;
using Poker.Helpers;
using Poker.Models;

namespace Poker.Interfaces
{
  public interface IHand : IComparable
  {
    public virtual string Name => "BaseHand";

    public ulong CardsMask { get; set; }
    public BaseDeck Deck { get; set; }
    public long Wins { get; set; }
    public long Loses { get; set; }
    public long Ties { get; set; }
    public bool Changed { get; set; }
    public (int, uint) LastEvaluation { get; set; }
    public bool CommunityCards { get; set; }
    public decimal Percent { get; }
    public int CardCount { get; }
    public int CardsNeeded { get; }
    public string CardDescriptions { get; }
    public event Action StateHasChangedDelegate;
    bool HandsLaidOut { get; }

    public void Reset();

    public void SetCards(ulong cardsMask);
    public void SetCards(string cards);

    public void AddCards(ulong cardsMask);
    public void AddCards(string cards);
    public void AddCard(int card);

    public int BestCard();

    public long LayoutHand(double duration = 0.1); 
  }
}
