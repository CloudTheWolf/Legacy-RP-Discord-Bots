using DSharpPlus.Entities;

namespace DOC.Module.Actions
{
    using DOC.Module.Common;
    using DSharpPlus.Commands;
    using DSharpPlus.Commands.ContextChecks;
    using DSharpPlus.Commands.Processors.SlashCommands;
    using System.ComponentModel;
    using System.ComponentModel.DataAnnotations;
    using System.Text.RegularExpressions;

    [Command("Timesheets")]
    [Description("Staff Timesheets")]
    [RequirePermissions(botPermissions: [], userPermissions: [DiscordPermission.UseApplicationCommands])]
    class TimeActions
    {

        [Command("time")]
        [Description("Get time for this week")]
        [RequirePermissions(botPermissions: [], userPermissions: [DiscordPermission.UseApplicationCommands])]
        public async Task GetTime(SlashCommandContext ctx, [Parameter("user")] [Description("User to get time for")] DiscordUser user = null, [Parameter("lastweek")][Description("Get time for Last Week")] bool lastWeek = false)
        {
            var member = user == null ? ctx.Member.Nickname : (await ctx.Guild.GetMemberAsync(user.Id)).Nickname;
            var match = Regex.Match(member, @"\[\d+\]\s(.+)");

            if (match.Success)
            {
                member = match.Groups[1].Value;
            }

            await StaffCommon.GetUserTime(ctx, member, lastWeek);
        }

        [Command("thisweek")]
        [Description("Get this weeks Timesheets")]
        [RequirePermissions(botPermissions: [], userPermissions: [DiscordPermission.UseApplicationCommands])]
        public async Task GetThisWeek(SlashCommandContext ctx)
        {
            await StaffCommon.GetThisWeek(ctx);
        }

        [Command("lastweek")]
        [Description("Get last weeks Timesheets")]
        [RequirePermissions(botPermissions: [], userPermissions: [DiscordPermission.UseApplicationCommands])]
        public async Task GetLastWeek(SlashCommandContext ctx)
        {
            await StaffCommon.GetLastWeek(ctx);
        }

    }
}