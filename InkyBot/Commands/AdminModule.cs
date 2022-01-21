using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace InkyBot.Commands
{
    public sealed class AdminModule : BaseCommandModule
    {
        [Command("eval"),
        Description("Warning! Dangerous command, do not use unless you know what you're doing."),
        RequireOwner]
        public async Task EvalAsync(CommandContext ctx, [RemainingText] string command)
        {
            command = string.Join("\n", command.Split('\n').Skip(1).Take(command.Split('\n').Skip(1).Count() - 1));

            ScriptRunner<object> script;

            try
            {
                script = CSharpScript.Create(command, ScriptOptions.Default
                    .WithReferences(typeof(object).GetTypeInfo().Assembly, typeof(Enumerable).GetTypeInfo().Assembly,
                                    typeof(PropertyInfo).GetTypeInfo().Assembly, typeof(Decoder).GetTypeInfo().Assembly,
                                    typeof(Regex).GetTypeInfo().Assembly, typeof(Task).GetTypeInfo().Assembly, typeof(CommandContext).GetTypeInfo().Assembly,
                                    typeof(DiscordMessage).GetTypeInfo().Assembly, typeof(Settings).GetTypeInfo().Assembly)
                    .WithImports("System", "System.Collections.Generic", "System.Linq", "System.Reflection", "System.Text",
                                 "System.Text.RegularExpressions", "System.Threading.Tasks", "DSharpPlus.CommandsNext", "DSharpPlus"), typeof(GlobalEvalContext))
                    .CreateDelegate();
            }
            catch (Exception e)
            {
                DiscordEmbedBuilder errorBuilder = new DiscordEmbedBuilder();
                errorBuilder.WithTitle("Exception occurred.");
                errorBuilder.AddField("Input", $"```cs\n{command}\n```");
                errorBuilder.AddField("Output", $"```\n[Exception ({(e.InnerException ?? e).GetType().Name})] {e.InnerException?.Message ?? e.Message}\n```");
                errorBuilder.WithColor(DiscordColor.Red);

                await ctx.Channel.SendMessageAsync(embed: errorBuilder.Build()).SafeAsync();

                return;
            }

            object result;

            try
            {
                result = await script(new GlobalEvalContext
                {
                    Ctx = ctx
                }).SafeAsync();
            }
            catch (Exception e)
            {
                DiscordEmbedBuilder errorBuilder = new DiscordEmbedBuilder();
                errorBuilder.WithTitle("Exception occurred.");
                errorBuilder.AddField("Input", $"```cs\n{command}\n```");
                errorBuilder.AddField("Output", $"```\n[Exception ({(e.InnerException ?? e).GetType().Name})] {e.InnerException?.Message ?? e.Message}\n```");
                errorBuilder.WithColor(DiscordColor.Red);

                await ctx.Channel.SendMessageAsync(embed: errorBuilder.Build()).SafeAsync();

                return;
            }

            DiscordEmbedBuilder builder = new DiscordEmbedBuilder();
            builder.AddField("Input", $"```cs\n{command}\n```");
            builder.AddField("Output", $"```\n{result}\n```");
            builder.WithColor(DiscordColor.Green);

            await ctx.Channel.SendMessageAsync(embed: builder.Build()).SafeAsync();
        }
    }

    public sealed class GlobalEvalContext
    {
        public CommandContext Ctx { get; set; }

        // ReSharper disable once InconsistentNaming
        // ReSharper disable once UnusedMember.Global
#pragma warning disable IDE1006 // Naming Styles
        public CommandContext ctx => this.Ctx;
#pragma warning restore IDE1006 // Naming Styles
    }
}
