using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
                PerformRoleRequest(sender, args);
                return;
            }
            return;
        }

        private static async Task PerformRoleRequest(DiscordClient sender, MessageCreateEventArgs args)
        {
            var approvedRoles = new List<ulong>
            {
                    1025039938145751110, // Lieutenant
                    1025037198237638696, // Captain
                    1074389282619396176, // Assistant Warden
                    1017804167936479325, // Deputy Warden
                    1017563068424781854  // Warden

            };

            var successReact = await args.Guild.GetEmojiAsync(1486047567723757748);
            var failReact = await args.Guild.GetEmojiAsync(1486047630583926980);

            var author = args.Message.Author;
            if (!approvedRoles.Contains(author.Id))
            {
                await args.Message.CreateReactionAsync(failReact);
            }

            var targets = args.Message.MentionedUsers.ToList();
            var messageLines = args.Message.Content.Split('\n');
            foreach (var target in targets)
            {
                var member = await args.Guild.GetMemberAsync(target.Id);
                foreach (var line in messageLines)
                {
                    var action = line.Trim().Substring(0, 3);
                    DiscordRole role = null;
                    if (DocRoles.TryGetValue(line.Trim().Substring(3), out ulong roleId))
                    {
                        role = args.Guild.GetRole(roleId);
                    }
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
                }
            }
        }
    }
}
