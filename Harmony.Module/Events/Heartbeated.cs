﻿using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Harmony.Module.Libs;
using Harmony.Module.Objects;
using Newtonsoft.Json;
using System.Security.Cryptography;

namespace Harmony.Module.Events
{
    internal class Heartbeated
    {

        internal static async Task AutoOffDuty(DiscordClient sender, HeartbeatEventArgs args)
        {
            var now = DateTime.UtcNow;
            if(now.Minute % 5 != 0) return;
           _ = PerformAutoClockoutCheck();
        }

        internal static async Task SyncCityData(DiscordClient sender, HeartbeatEventArgs args)
        {
            var now = DateTime.UtcNow;
            if (now.Minute % 10 != 0) return;
            _ = SyncTowData();
        }

        private static async Task PerformAutoClockoutCheck()
        {
            if (Options.DutyChannel == null) return;
            var db = new DatabaseActions();
            var client = new HttpClient();
            var request = new HttpRequestMessage(HttpMethod.Get, $"{Options.ApiUrl}/op-framework/players.json");
            request.Headers.Add("Authorization", $"Bearer {Options.ApiKey}");
            var response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            var rootObject = JsonConvert.DeserializeObject<PlayerRoot>(json);
            var players = rootObject.Data;
            var inCityList = (from player in players where player.Character != null select player.Character.Id)
                .ToList();
            var onDuty = db.GetAllStaffOnDuty();
            foreach (var staff in onDuty)
            {
                if (inCityList.Contains(staff["cid"].ToObject<int>())) continue;
                db.ClockOutUser(staff["name"].ToString());
                var embed = new DiscordEmbedBuilder()
                {
                    Title = $"{staff["name"]} has automatically been clocked out",
                    Author = new DiscordEmbedBuilder.EmbedAuthor() { Name = $"{Options.CompanyName} Time Tracker" },
                    Footer = new DiscordEmbedBuilder.EmbedFooter()
                    {
                        Text =
                            $"{staff["name"]} was automatically clocked off at {DateTime.UtcNow:R} as we could not see them in the city"
                    },
                    Color = new Optional<DiscordColor>(DiscordColor.Yellow)

                };
                Options.DutyChannel.SendMessageAsync(embed.Build());
            }
        }

        private static async Task SyncTowData()
        {
            if (Options.DutyChannel == null) return; //This is used as a buffer to make sure we are fully online
            var db = new DatabaseActions();
            var client = new HttpClient();
            var request = new HttpRequestMessage(HttpMethod.Get, $"{Options.ApiUrl}/op-framework/towImpounds.json");
            request.Headers.Add("Authorization", $"Bearer {Options.ApiKey}");
            var response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            var rootObject = JsonConvert.DeserializeObject<ImpoundRoot>(json);
            var staff = db.GetAllStaff();
            var staffIds = staff.Select(s => s["cid"].ToObject<int>()).ToList();

            foreach (var impoundLog in rootObject.Data)
            {
                var characterId = impoundLog.CharacterId;
                if(!staffIds.Contains(characterId)) continue;
                var impounds = impoundLog.TowImpounds;
                foreach (var impound in impounds)
                {
                    var playerVehicle = impound.PlayerVehicle ? 1 : 0;
                    db.InsertTowImpoundLog(impound, characterId, playerVehicle);
                }
            }

            
        }

    }
        
}

