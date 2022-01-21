using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InkyBot.Models
{
    public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<DiscordMessageContext>
    {
        DiscordMessageContext IDesignTimeDbContextFactory<DiscordMessageContext>.CreateDbContext(string[] args)
        {
            string connectionString = Environment.GetEnvironmentVariable("CONNECTION_STRING");

            var optionsBuilder = new DbContextOptionsBuilder<DiscordMessageContext>();
            optionsBuilder.UseNpgsql(connectionString);

            return new DiscordMessageContext(optionsBuilder.Options);
        }
    }
}
