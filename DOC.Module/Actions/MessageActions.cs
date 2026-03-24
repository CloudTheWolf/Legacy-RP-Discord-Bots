using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DOC.Module.Actions
{
    internal class MessageActions
    {
        private static readonly Dictionary<string, ulong> DocRoles = new Dictionary<string, ulong>
        {
            { "Warden", 1017563068424781854 },
            { "Deputy Warden", 1017804167936479325 },
            { "Assistant Warden", 1074389282619396176 },
            { "Captain", 1025037198237638696 },
            { "Lieutenant", 1025039938145751110 },
            { "Sergeant", 1017563496461897859 },
            { "Senior Corrections Officer", 1106363988104716428 },
            { "Corrections Officer", 1017563583510478929 },
            { "Cadet", 1017563749068058755 },
            { "DOC Assistant", 1163059087613886545 },
            { "DOC", 1017576852048584814 },
            { "Recruit", 1025074661844865075 }
        };

        internal static async Task OnMessageCreated(DiscordClient sender, MessageCreateEventArgs args)
        {
            ulong roleChannelId = 1114738852964352134;
            if (args.Message.Channel.Id == roleChannelId)
            {
                _ = PerformRoleRequest(sender, args);
                return;
            }
            return;
        }

        private static async Task PerformRoleRequest(DiscordClient sender, MessageCreateEventArgs args)
        {

            var successReact = await args.Guild.GetEmojiAsync(1486047567723757748);
            var failReact = await args.Guild.GetEmojiAsync(1486047630583926980);

            var approvedRoles = new HashSet<ulong>
            {
                    1025039938145751110, // Lieutenant
                    1025037198237638696, // Captain
                    1074389282619396176, // Assistant Warden
                    1017804167936479325, // Deputy Warden
                    1017563068424781854  // Warden

            };

            var author = await args.Guild.GetMemberAsync(args.Message.Author.Id);
            var hasRequiredRole = !author.Roles.Any(r => approvedRoles.Contains(r.Id));
            if (hasRequiredRole)
            {
                Main.Logger.LogInformation("[Roles] No Valid Role on User");
                await args.Message.CreateReactionAsync(failReact);
            }

            var targets = args.Message.MentionedUsers.ToList();
            try
            {
                var lineNumber = 1;
                var messageLines = args.Message.Content.Split('\n');
                foreach (var target in targets)
                {
                    var member = await args.Guild.GetMemberAsync(target.Id);
                    Main.Logger.LogInformation($"Modify Roles For {member.Username}");
                    foreach (var line in messageLines)
                    {

                        if(line.Trim().Length == 0) continue;
                        var action = line.Trim().Substring(0, 3);
                        Main.Logger.LogInformation($"{lineNumber}: {action} | {line.Trim().Substring(3).Trim()}");
                        var roleId = DocRoles.GetValueOrDefault(line.Trim().Substring(3).Trim(),ulong.MinValue);
                        if(roleId == ulong.MinValue) continue;
                        Main.Logger.LogInformation($"Modify Roles For {target.Username} {action} {roleId}");
                        DiscordRole role = args.Guild.GetRole(roleId);
                        Main.Logger.LogInformation($"Found Role {role.Name}");
                        if (role == null) continue;
                        switch (action)
                        {
                            case "[+]":
                                await member.GrantRoleAsync(role);
                                break;
                            case "[-]":
                                await member.RevokeRoleAsync(role);
                                break;
                            default:
                                break;
                        }
                        ++lineNumber;
                    }
                }

                await args.Message.CreateReactionAsync(successReact);
                _ = LogRoleRequest(args.Guild, args.Message, true);

            }
            catch (Exception ex)
            {
                Main.Logger.LogInformation($"{ex}");
                await args.Message.CreateReactionAsync(failReact);
                _ = LogRoleRequest(args.Guild, args.Message, false);
            }
        }
    
        private static async Task LogRoleRequest(DiscordGuild server, DiscordMessage message,bool success)
        {
            var logChannel = server.GetChannel(1030177104232464464);
            var logEmbed = new DiscordEmbedBuilder();
            logEmbed.AddField("Request By",message.Author.Mention);
            logEmbed.AddField("Outcome",success ? "Success" : "Failed");
            logEmbed.AddField("Request", $"```\n{message.Content}\n```",false);
            var logMessage = new DiscordMessageBuilder().WithEmbed(logEmbed.WithThumbnail(message.Author.AvatarUrl).WithColor(success ? DiscordColor.Green : DiscordColor.Red));
            await logChannel.SendMessageAsync(logMessage);
            
        }
    }
}
