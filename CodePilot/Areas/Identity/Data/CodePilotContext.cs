using CodePilot.Areas.Identity.Data;
using CodePilot.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Net;

namespace CodePilot.Data;

public class CodePilotContext : IdentityDbContext<ApplicationUser>
{
    public DbSet<Chat> Chats { get; set; }
    public DbSet<Message> Messages { get; set; }

    public CodePilotContext(DbContextOptions<CodePilotContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        // Customize the ASP.NET Identity model and override the defaults if needed.
        // For example, you can rename the ASP.NET Identity table names and more.
        // Add your customizations after calling base.OnModelCreating(builder);
    }
}
