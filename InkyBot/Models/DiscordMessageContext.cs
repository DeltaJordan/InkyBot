using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InkyBot.Models
{
    public class DiscordMessageContext : DbContext
    {
        public DiscordMessageContext(DbContextOptions<DiscordMessageContext> options)
        {
        }

        public DbSet<DiscordMessageItem> MessageItems { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseNpgsql(Settings.Instance.ConnectionString);
        }
    }
}
