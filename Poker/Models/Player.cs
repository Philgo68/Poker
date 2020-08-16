using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Poker.Data;
using Poker.Helpers;
using Poker.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Poker.Models
{
  public class Player : IdentityUser
  {
    public string ScreenName { get; set; }
    public int Bankroll { get; set; }
    public bool Computer { get; set; }
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
        errors.Add(new IdentityError() { Code = "Duplicate Screen Name", Description = $"Screen Name '{user.ScreenName}' is already in use.  Please select a different Screen Name."});

      return errors.Any()
          ? IdentityResult.Failed(errors.ToArray())
          : IdentityResult.Success;
    }
  }

}


