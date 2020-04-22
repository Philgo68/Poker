﻿using System;
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

namespace Poker.Interfaces
{
  public interface IHand : IComparable
  {
    public virtual string Name => "BaseHand";
    public ulong CardsMask { get; set; }
    public ulong FillerMask { get; set; }
    public long Wins { get; set; }
    public long Loses { get; set; }
    public long Ties { get; set; }
    public bool CommunityCards { get; set; }
    public decimal Percent { get; }
    public int CardCount { get; }
    public int CardsNeeded { get; }
    public void Reset();

    public void SetCards(ulong cardsMask);
    public void SetCards(string cards);

    public void AddCards(ulong cardsMask);
    public void AddCards(string cards);

    public long LayoutHand(double duration = 0.1); 
  }
}