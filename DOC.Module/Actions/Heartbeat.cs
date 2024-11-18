using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.EventArgs;

namespace DOC.Module.Actions
{
    using System.Diagnostics;

    using DSharpPlus.Entities;
    using DSharpPlus.Net.Models;

    using Microsoft.Extensions.Logging;

    using Newtonsoft.Json.Linq;

    internal class Heartbeat
    {
        /// <summary>
        /// Is this the first heartbeat since the bot started?
        /// </summary>
        private static bool firstHeartbeat = true;
        
        /// <summary>
        /// All DOC Staff
        /// </summary>
        private static JObject allDocStaff = new ();

        /// <summary>
        /// On Heartbeat, Get on duty staff and update message
        /// </summary>
        /// <param name="sender">The <see cref="DiscordClient"/></param>
        /// <param name="args">The<see cref="HeartbeatEventArgs"/></param>
        /// <returns><see cref="Task"/></returns>
        public static async Task GetOnDutyHeartbeatAsync(DiscordClient sender, HeartbeatEventArgs args)
        {
            await GetStaffAsync();
            if (firstHeartbeat)
            {
                firstHeartbeat = false;
                return;
            }

            if (Options.LastMessage != DateTime.MinValue && (DateTime.UtcNow - Options.LastMessage).TotalMinutes < 2)
            {
                Main.Logger.LogInformation("[Heartbeat-Duty] Nothing To Do...");
                return;
            }

            var targetGuild = sender.Guilds[Options.GuildId];
            var targetMessage = Options.OnDutyMessage;
            var targetChannel = targetGuild.GetChannel(Options.OnDutyChannel);
            var newMessage = await CreateDutyMessageAsync();
            DiscordMessage message = null;

            if (targetMessage != ulong.MinValue)
            {
                message = await targetChannel.GetMessageAsync(targetMessage, true);
            }
            
            if (targetMessage != ulong.MinValue
                && (DateTimeOffset.UtcNow - message.CreationTimestamp.UtcDateTime).TotalMinutes > 30)
            {
                _ = message.DeleteAsync("cleanup");
                targetMessage = ulong.MinValue;
            }

            if (targetMessage == ulong.MinValue)
            {
                var finalMessage = await targetChannel.SendMessageAsync(newMessage);
                Options.OnDutyMessage = finalMessage.Id;
                return;
            }

            if (message != null)
            {
                await message.ModifyAsync(newMessage);
            }
        }

        /// <summary>
        /// Create the OnDuty Message
        /// </summary>
        /// <returns>A <see cref="string"/> for use in a <see cref="DiscordMessage"/></returns>
        private static async Task<string> CreateDutyMessageAsync()
        {
            var duty = await GetDutyAsync();
            var docDuty = new JArray();

            var message = "# San Andreas Department of Corrections Status\n";
            message += "*Status may be delayed by up to 5 minutes*\n";
            message += $"*Last Updated:<t:{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}:R>*\n\n";

            try
            {
                foreach (var onDuty in duty["data"]["Law Enforcement"])
                {
                    if (onDuty["department"].ToString().ToLower() != "doc") continue;
                    docDuty.Add(onDuty);
                }
            }
            catch
            {
                Main.Logger.LogInformation("[Heartbeat-CreateMessage] No DOC Duty");
            }

            message += $"**Total:** {docDuty.Count}\n\n";

            message += "__**On Duty**__\n";
            foreach (var user in docDuty)
            {
                var name = "";
                foreach (var staff in allDocStaff["data"])
                {
                    if (staff["character_id"].ToString() != user["characterId"].ToString()) continue;
                    name = $"{staff["first_name"]} {staff["last_name"]}";
                }
                message += $"<:DOC:1046006478693224498> {name} ";
                message += bool.Parse(user["training"].ToString()) ? " [Training]\n" : "\n";
            }

            return message;
        }

        /// <summary>
        /// Get On Duty From Server
        /// </summary>
        /// <returns><see cref="JObject"/> containing all on duty staff</returns>
        private static async Task<JObject> GetDutyAsync()
        {
            var client = new HttpClient();
            var request = new HttpRequestMessage(HttpMethod.Get, $"{Options.ApiUrl}/op-framework/duty.json");
            request.Headers.Add("Authorization", $"Bearer {Options.ApiKey}");
            var response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            return JObject.Parse(json);
        }

        /// <summary>
        /// Get Staff from OPFW API
        /// </summary>
        /// <returns><see cref="Task"/></returns>
        private static async Task GetStaffAsync()
        {
            var client = new HttpClient();
            var request = new HttpRequestMessage(HttpMethod.Get, $"{Options.RestApiUrl}/characters?select=*&where=department_name=Bolingbroke Penitentiary");
            request.Headers.Add("Authorization", $"Bearer {Options.ApiKey}");
            var response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            allDocStaff = JObject.Parse(json);
        }
    }
}
