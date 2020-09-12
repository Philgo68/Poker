using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Poker.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;

namespace Poker.Models
{
  public class Player : IdentityUser
  {
    private int bankroll;

    public string ScreenName { get; set; }
    public int AddedChips { get; set; }
    public bool Computer { get; set; }
    public int Bankroll
    {
      get
      {
        return bankroll;
      }
      set
      {
        if (dbContext == null)
        {
          throw new ArgumentException("Unable to store Bankroll change.");
        }
        else
        {
          bankroll = value;
          StateHasChanged();
        }
      }
    }

    [NotMapped]
    public PokerDbContext dbContext;

    public event Action StateHasChangedDelegate;

    protected async void StateHasChanged()
    {
      StateHasChangedDelegate?.Invoke();
      await dbContext.SaveChangesAsync();
    }
  }

  public class ScreenNameValidator<TUser> : IUserValidator<TUser> where TUser : Player
  {

    public ScreenNameValidator()
    {
    }

    public async Task<IdentityResult> ValidateAsync(UserManager<TUser> manager, TUser user)
    {
      var errors = new List<IdentityError>();

      var owner = await manager.Users.FirstOrDefaultAsync(u => u.ScreenName == user.ScreenName);

      if (owner != null)
        errors.Add(new IdentityError() { Code = "Duplicate Screen Name", Description = $"Screen Name '{user.ScreenName}' is already in use.  Please select a different Screen Name." });

      return errors.Any()
          ? IdentityResult.Failed(errors.ToArray())
          : IdentityResult.Success;
    }
  }

}


