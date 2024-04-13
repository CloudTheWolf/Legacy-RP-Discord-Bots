using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.EventArgs;

namespace BCFD.Module.Actions
{
    using System.Diagnostics;

    using DSharpPlus.Entities;
    using DSharpPlus.Net.Models;

    using Microsoft.Extensions.Logging;

    using Newtonsoft.Json.Linq;

    internal class Heartbeat
    {

        private static bool firstRun = true;

        private static JObject AllFdStaff = new JObject();

        public static async Task Heartbeated(DiscordClient sender, HeartbeatEventArgs args)
        {
            SetStaffJOject();
            if (firstRun)
            {
                firstRun = false;
                return;
            }

            if (Options.LastMessage != DateTime.MinValue && (DateTime.UtcNow - Options.LastMessage).TotalMinutes < 2)
            {
                Main.Logger.LogInformation("[Heartbeat-Duty] Nothing To Do...");
                return;
            }

            var targetGuild = sender.Guilds[Options.GuildId];
            var targetMessage = Options.onDutyMessage;
            var targetChannel = targetGuild.GetChannel(Options.onDutyChannel);
            var newMessage = await CreateDutyMessage();
            DiscordMessage message = null;

            if (targetMessage != ulong.MinValue)
            {
                message = await targetChannel.GetMessageAsync(targetMessage, true);
            }
            
            if (targetMessage != ulong.MinValue
                && (DateTimeOffset.UtcNow - message.CreationTimestamp.UtcDateTime).TotalMinutes > 30)
            {
                message.DeleteAsync("cleanup");
                targetMessage = ulong.MinValue;
            }

            if (targetMessage == ulong.MinValue)
            {
                var finalMessage = await targetChannel.SendMessageAsync(newMessage);
                Options.onDutyMessage = finalMessage.Id;
                return;
            }

            await message.ModifyAsync(newMessage);
        }

        private static async Task<string> CreateDutyMessage()
        {
            var duty = await GetDuty();
            var fdDuty = new JArray();

            var message = "# Blaine County Fire Department Status\n";
            message += "*Status may be delayed by up to 5 minutes*\n";
            message += $"*Last Updated:<t:{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}:R>*\n\n";

            try
            {
                foreach (var onDuty in duty["data"]["Medical"])
                {
                    if (onDuty["department"].ToString() != "bcfd") continue;
                    fdDuty.Add(onDuty);
                }
            }
            catch
            {
                Main.Logger.LogInformation("[Heartbeat-CreateMessage] No Medical Duty");
            }

            var trainingCount = 0;

            message += $"**Total:** {fdDuty.Count} | **Training**: [TRAINING_COUNT] \n\n";

            message += "__**On Duty**__\n";

            
            foreach (var user in fdDuty)
            {
                try
                {
                    var name = "";
                    foreach (var staff in AllFdStaff["data"])
                    {
                        if (staff["character_id"].ToString() != user["characterId"].ToString()) continue;
                        name = $"{staff["first_name"]} {staff["last_name"]}";
                    }

                    message += $"<:BCFD:995436961848365146> {name} ";
                    
                    if (bool.Parse(user["training"].ToString()))
                    {
                        message +=  "[Training]";
                        trainingCount++;
                    }
                    message += "\n";
                }
                catch (Exception ex)
                {
                    Main.Logger.LogError($"[ERROR] {ex.Message}\n{ex}");
                }
            }

            return message.Replace("[TRAINING_COUNT]", $"{trainingCount}"); ;
        }

        private static async Task<JObject> GetDuty()
        {
            var client = new HttpClient();
            var request = new HttpRequestMessage(HttpMethod.Get, $"{Options.ApiUrl}/op-framework/duty.json");
            request.Headers.Add("Authorization", $"Bearer {Options.ApiKey}");
            var response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            return JObject.Parse(json);
        }

        private static async Task SetStaffJOject()
        {
            var client = new HttpClient();
            var request = new HttpRequestMessage(HttpMethod.Get, $"{Options.RestApiUrl}/characters/job~Fire/data,job,duty");
            request.Headers.Add("Authorization", $"Bearer {Options.ApiKey}");
            var response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            AllFdStaff = JObject.Parse(json);
        }
    }
}
