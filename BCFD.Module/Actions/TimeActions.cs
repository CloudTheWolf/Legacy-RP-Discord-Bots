using DSharpPlus.SlashCommands;
using BCFD.Module.Common;

namespace BCFD.Module.Actions
{
    using System.Text.RegularExpressions;
    using DSharpPlus;
    using DSharpPlus.Entities;

    [SlashCommandGroup("Timesheets", "Staff Timesheets", true)]
    class TimeActions : ApplicationCommandModule
    {

        [SlashCommand("time", "Get time for this week", true)]
        public async Task GetTime(InteractionContext ctx,[Option("user", "User to get time for", false)] DiscordUser user = null, [Option("lastweek", "Get time for Last Week", true)] bool lastWeek = false)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);
            var member = user == null ? ctx.Member.Nickname : (await ctx.Guild.GetMemberAsync(user.Id)).Nickname;
            var match = Regex.Match(member, @"\[\d+\]\s(.+)");
            if (match.Success)
            {
                member = match.Groups[1].Value.Replace("Dr. ","");
            }
            await StaffCommon.GetUserTime(ctx, member,lastWeek);
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

    }
}