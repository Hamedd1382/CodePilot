using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CodePilot.Models;
using Microsoft.AspNetCore.Identity;

namespace CodePilot.Areas.Identity.Data;

// Add profile data for application users by adding properties to the ApplicationUser class
public class ApplicationUser : IdentityUser
{
    public ICollection<Chat> Chats { get; set; }
}

