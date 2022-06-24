using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using Newtonsoft.Json;

namespace InkyBot.Commands;

public class SpanModule : BaseCommandModule
{
    [Command("workplacesafety"), Aliases("ws")]
    public async Task WorkplaceSafetyAsync(CommandContext context)
    {
        string lastMentionedFile = Path.Combine(Globals.AppPath, "vore.dat");
        if (File.Exists(lastMentionedFile))
        {
            DateTime lastMentioned = JsonConvert.DeserializeObject<DateTime>(await File.ReadAllTextAsync(lastMentionedFile).SafeAsync());
            TimeSpan timeDifference = DateTime.Now - lastMentioned;

            string responseMessage;
            if (timeDifference.TotalDays >= 365)
            {
                responseMessage = (int)(timeDifference.TotalDays / 365) + " years of workplace safety.";
            }
            else if (timeDifference.TotalDays >= 30)
            {
                responseMessage = (int)(timeDifference.TotalDays / 30) + " months of workplace safety.";
            }
            else if (timeDifference.Days > 0)
            {
                responseMessage = timeDifference.Days + " days of workplace safety.";
            }
            else if (timeDifference.Hours > 0)
            {
                responseMessage = timeDifference.Hours + " hours of workplace safety.";
            }
            else if (timeDifference.Minutes > 0)
            {
                responseMessage = timeDifference.Minutes + " minutes of workplace safety.";
            }
            else if (timeDifference.Seconds > 0)
            {
                responseMessage = timeDifference.Seconds + " seconds of workplace safety.";
            }
            else
            {
                responseMessage = timeDifference.Milliseconds + " milliseconds of workplace safety.";
            }

            await context.RespondAsync(responseMessage).SafeAsync();
        }
        else
        {
            await context.RespondAsync("We have not had a reported instance of unsafe workplace behavior!").SafeAsync();
        }
    }
}