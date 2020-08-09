using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Poker.Data;
using Poker.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml.Schema;

namespace Poker.Helpers
{
  public class Dealers
  {
    private readonly ConcurrentDictionary<Guid, TableDealer> _dealers;
    private readonly PokerDbContext _pokerDbContext;

    public Dealers(PokerDbContext db)
    {
      _pokerDbContext = db;
      _dealers = new ConcurrentDictionary<Guid, TableDealer>();
    }

    public TableDealer DealerForTable(Table table)
    {
      return _dealers.GetOrAdd(table.Id, (key) => { 
        var dealer = new TableDealer(_pokerDbContext, table);
        dealer.TransitionToNextPhase();
        return dealer; 
      });
    }

    public TableDealer SomeDealer()
    {
      return _dealers.FirstOrDefault().Value;
    }
  }
}

