using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Microsoft.Extensions.Logging;
using Serilog;
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

        private static readonly HashSet<ulong> approvedRoles = new()
        {
                    1025039938145751110, // Lieutenant
                    1025037198237638696, // Captain
                    1074389282619396176, // Assistant Warden
                    1017804167936479325, // Deputy Warden
                    1017563068424781854  // Warden

        };

        internal static async Task OnMessageCreated(DiscordClient sender, MessageCreatedEventArgs args)
        {
            ulong roleChannelId = 1114738852964352134;
            if (args.Message.Channel.Id == roleChannelId)
            {
                _ = PerformRoleRequest(sender, args);
                return;
            }
            return;
        }

        private static async Task PerformRoleRequest(DiscordClient sender, MessageCreatedEventArgs args)
        {

            var successReact = await args.Guild.GetEmojiAsync(1486047567723757748);
            var failReact = await args.Guild.GetEmojiAsync(1486047630583926980);

            var author = await args.Guild.GetMemberAsync(args.Message.Author.Id);
            var hasRequiredRole = !author.Roles.Any(r => approvedRoles.Contains(r.Id));
            if (hasRequiredRole)
            {
                Log.Information("[Roles] No Valid Role on User");
                await args.Message.CreateReactionAsync(failReact);
            }
           
            var isWarden = author.Roles.Contains(await args.Guild.GetRoleAsync(1017563068424781854));
            var isDeptWarden = author.Roles.Contains(await args.Guild.GetRoleAsync(1017804167936479325));
            var isAssistWarden = author.Roles.Contains(await args.Guild.GetRoleAsync(1074389282619396176));
            var isCaptain = author.Roles.Contains(await args.Guild.GetRoleAsync(1017804167936479325));
            
            var targets = args.Message.MentionedUsers.ToList();
            try
            {
                var messageLines = args.Message.Content.Split('\n');
                var forceError = false;
                foreach (var target in targets)
                {
                    var member = await args.Guild.GetMemberAsync(target.Id);
                    Log.Information($"Modify Roles For {member.Username}");
                    foreach (var line in messageLines)
                    {
                        if(line.Trim().Length == 0) continue;
                        var action = line.Trim().Substring(0, 3);
                        var targetRoleName = line.Trim().Substring(3).Trim();
                        if ($"{action} {targetRoleName}" == "[-] All Roles")
                        {
                            var success = await RemoveAllRoles(member);
                            if (!success)
                            {
                                forceError = true;
                            }
                            break;
                        }
                        var roleId = DocRoles.GetValueOrDefault(targetRoleName,ulong.MinValue);
                        if(roleId == ulong.MinValue) continue;
                        Log.Information($"Modify Roles For {target.Username} {action} {roleId}");
                        DiscordRole role = await args.Guild.GetRoleAsync(roleId);
                        Log.Information($"Found Role {role.Name}");
                        if (role == null) continue;
                        if(!CanUserControlRole(role,isWarden,isDeptWarden,isAssistWarden,isCaptain))
                        {
                            forceError = true;
                            continue;
                        }
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
                    }
                }

                await args.Message.CreateReactionAsync(forceError ? failReact : successReact);
                _ = LogRoleRequest(args.Guild, args.Message, !forceError);

            }
            catch (Exception ex)
            {
                Log.Information($"{ex}");
                await args.Message.CreateReactionAsync(failReact);
                _ = LogRoleRequest(args.Guild, args.Message, false);
            }
        }

        private static async Task<bool> RemoveAllRoles(DiscordMember member)
        {
            if (member.Roles.Any(r => approvedRoles.Contains(r.Id))) {
                return false;
            }
            foreach (var role in member.Roles)
            {
                await member.RevokeRoleAsync(role);
            }
            return true;

        }

        private static bool CanUserControlRole(DiscordRole role, bool isWarden, bool isDeptWarden, bool isAssistWarden, bool isCaptain)
        {
            if (role.Name.Equals("Warden",StringComparison.OrdinalIgnoreCase) && !isWarden) return false;
            if (role.Name.Equals("Deputy Warden", StringComparison.OrdinalIgnoreCase) && (!isWarden)) return false;
            if (role.Name.Equals("Assistant Warden", StringComparison.OrdinalIgnoreCase) && (!isWarden && !isDeptWarden)) return false;
            if (role.Name.Equals("Captain", StringComparison.OrdinalIgnoreCase) && (!isWarden && !isDeptWarden && !isAssistWarden && !isDeptWarden)) return false; 
            return true;
        }

        private static async Task LogRoleRequest(DiscordGuild server, DiscordMessage message,bool success)
        {
            var logChannel = await server.GetChannelAsync(1030177104232464464);
            var logEmbed = new DiscordEmbedBuilder();
            logEmbed.AddField("Request By",message.Author.Mention);
            logEmbed.AddField("Outcome",success ? "Success" : "Failed");
            logEmbed.AddField("Request", $"```\n{message.Content}\n```",false);
            var logMessage = new DiscordMessageBuilder().AddEmbed(logEmbed.WithThumbnail(message.Author.AvatarUrl).WithColor(success ? DiscordColor.Green : DiscordColor.Red));
            await logChannel.SendMessageAsync(logMessage);
            
        }
    }
}
