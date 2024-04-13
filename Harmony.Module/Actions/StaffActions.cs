using CloudTheWolf.DSharpPlus.Scaffolding.Logging;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using Harmony.Module.Common;
using Harmony.Module.Libs;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Harmony.Module.Actions
{
    [SlashCommandGroup("Timesheets", "Staff Timesheets", true)]
    class StaffActions : ApplicationCommandModule
    {
        private static DatabaseActions da = new DatabaseActions();

        [SlashCommand("time", "Get time for this week", true)]
        public async Task GetTime(InteractionContext ctx, [Option("user", "User to get time for", false)] DiscordUser user = null)
        {
            var member = user == (DiscordUser)null ? ctx.Member : await ctx.Guild.GetMemberAsync(user.Id);
            await StaffCommon.GetUserTime(ctx, member);
        }

        [SlashCommand("thisweek", "Get this weeks Timesheets", true)]
        public async Task GetThisWeek(InteractionContext ctx)
        {
            await StaffCommon.GetThisWeek(ctx);
        }

        [SlashCommand("lastweek", "Get last weeks Timesheets", true)]
        public async Task GetLastWeek(InteractionContext ctx)
        {
            await StaffCommon.GetLastWeek(ctx);
        }

        [SlashCommand("totaltime", "Get Total Time For User", true)]
        public async Task GetTotalTime(InteractionContext ctx, [Option("user", "User to get time for", false)] DiscordUser user = null)
        {
            var member = user == (DiscordUser)null ? ctx.Member : await ctx.Guild.GetMemberAsync(user.Id);            
            await StaffCommon.GetTotalTime(ctx, member);        }

        [SlashCommand("forceclockout", "Force someone off duty", true)]
        public async Task ForceOffDuty(InteractionContext ctx, [Option("user", "User to force off duty", false)] DiscordUser user, [Option("reason", "Reason to force off duty", false)] string reason = "")
        {
            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);
            var member = user != null ? await ctx.Guild.GetMemberAsync(user.Id) : ctx.Member;
            Main.Logger.LogInformation($"New Force Clock Out Request By {ctx.Member.Username}#{ctx.Member.Discriminator} in {ctx.Guild.Name} for {member.Nickname}");
            if (!ctx.Member.Roles.Contains<DiscordRole>(Options.ManagerRole))
            {
                DiscordMessage discordMessage1 = await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Oops, you don't have access to this command"));
                return;
            }
            StaffActions.da.ClockOutUser(member.Nickname);
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"{member.Mention} has been clocked out by {ctx.Member.Mention} with the reason: {reason}"));
            
        }

        [SlashCommand("clockin", "Clock In via the bot", true)]
        public async Task ClockIn(InteractionContext ctx,
            [Choice("Tow", "Tow")]
            [Choice("Mechanic", "Mechanic")]
            [Choice("Scuba", "Scuba")]
            [Choice("Event", "Event")]
            [Choice("Trainer", "Trainer")]
            [Choice("Management", "Management")]
            [Option("Clock-In-As", "What are you clocking in as?")]
            string state = "Off-Duty")
        {
            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);
            JArray user = da.GetUser(ctx.Member.Id);
            if (user.Count == 0)
            {
                user = da.GetUser(ctx.Member.Nickname);
            }
            
            try
            {
                Main.Logger.LogInformation($"Set User {user[0]["id"]} as {state}");
                var time = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                if((state == "Trainer" || state == "Management") && (int)user[0]["IsAdmin"] == 0)
                {
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("You can't clock in as that"));
                    return;
                }

                var embed = new DiscordEmbedBuilder
                {
                    Title = $"{ctx.Guild.Name} Time Tracker",
                    Color = DiscordColor.Green,

                };

                da.ClockInUser(ctx.Member.Nickname, (int)user[0]["cid"], state, ((int)user[0]["onDuty"] == 1));
                embed.AddField($"{ctx.Member.Nickname} has started work",
                    $"{ctx.Member.Nickname} has Clocked In As {state} @ <t:{time}:d> <t:{time}:T>");


                await ctx.EditResponseAsync(
                    new DiscordWebhookBuilder().AddEmbed(embed));
            }
            catch (Exception e)
            {
                Main.Logger.LogError(e.Message);
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent(
                    "Something went wrong, Make sure your Discord Nickname is the same as you name on the control panel."));
            }
        }

        [SlashCommand("clockout", "Clock Out via the bot", true)]
        public async Task ClockOut(InteractionContext ctx)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);
            dynamic user = da.GetUser(ctx.Member.Nickname);
            try
            {
                Main.Logger.LogInformation($"Set User {user[0]["id"]} as Off-Duty");
                var time = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                var embed = new DiscordEmbedBuilder
                {
                    Title = $"{ctx.Guild.Name} Time Tracker",
                    Color = DiscordColor.Red,

                };

                da.ClockOutUser(ctx.Member.Nickname);
                embed.AddField($"{ctx.Member.Nickname} has stopped work",
                    $"{ctx.Member.Nickname} has Clocked Out @ <t:{time}:d> <t:{time}:T>");


                await ctx.EditResponseAsync(
                    new DiscordWebhookBuilder().AddEmbed(embed));
            }
            catch (Exception e)
            {
                Main.Logger.LogError(e.Message);
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent(
                    "Something went wrong, Make sure your Discord Nickname is the same as you name on the control panel."));
            }
        }
    }
}