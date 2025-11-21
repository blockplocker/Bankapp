using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bankapp.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Identity.Client;

namespace Bankapp.Areas.Identity.Data;

// Add profile data for application users by adding properties to the BankappUser class
public class BankappUser : IdentityUser
{
    public string FirstName { get; set; }
    public string LastName { get; set; }

    public ICollection<Account> Accounts { get; set; }
}

