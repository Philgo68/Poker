using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Poker.Data;
using Poker.Models;
using System;
using System.Collections.Concurrent;
using System.Linq;

namespace Poker.Helpers
{
  public class Dealers
  {
    private readonly ConcurrentDictionary<Guid, TableDealer> _dealers;
    private Players _players;
    private IApplicationBuilder _provider;

    public Dealers()
    {
      _dealers = new ConcurrentDictionary<Guid, TableDealer>();
    }

    public void SetProvider(IApplicationBuilder provider)
    {
      _provider = provider;
      _players = _provider.ApplicationServices.CreateScope().ServiceProvider.GetService<Players>();
    }

    public TableDealer DealerForTable(Guid tableId)
    {
      return _dealers.GetOrAdd(tableId, (key) =>
      {
        PokerDbContext dbContext = _provider.ApplicationServices.CreateScope().ServiceProvider.GetService<PokerDbContext>();
        var table = dbContext.Tables
          .Include(t => t.Seats)
          .SingleOrDefault(t => t.Id == tableId);
        if (table == null) { return null; }
        return new TableDealer(dbContext, _players, table);
      });
    }

    public TableDealer SomeDealer()
    {
      return _dealers.FirstOrDefault().Value;
    }
  }
}

