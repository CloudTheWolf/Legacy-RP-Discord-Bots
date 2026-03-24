using CloudTheWolf.DSharpPlus.Scaffolding.Logging;
using DOC.Module.Libs;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.Net;
using DSharpPlus.SlashCommands;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.Diagnostics.Metrics;
using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace DOC.Module.Common
{
    internal class StaffCommon
    {
        private static readonly ILogger<Logger> Logger = Main.Logger;

        public static async Task GetUserTime(InteractionContext ctx, string member, bool getLastWeek = false)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);
            try
            {
                var message = CalculateTime(member, getLastWeek);
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent(message));
            }
            catch (Exception ex)
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"Error: {ex.Message}\n```{ex}```"));
            }
        }

        public static async Task GetThisWeek(InteractionContext ctx)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);
            var (embed, embed2, embed3) = await GetWeekEmbed();

            if (embed3.Fields.Count > 0)
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                    .WithContent("This weeks timesheets so far...")
                    .AddEmbeds(new List<DiscordEmbed>() { embed, embed2, embed3 }));
                return;
            }
            if (embed3.Fields.Count == 0 && embed2.Fields.Count > 0)
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                    .WithContent("This weeks timesheets so far...")
                    .AddEmbeds(new List<DiscordEmbed>() { embed, embed2 }));
                return;
            }
            await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                .WithContent("This weeks timesheets so far...")
                .AddEmbeds(new List<DiscordEmbed>() { embed }));
        }

        public static async Task GetLastWeek(InteractionContext ctx)
        {
            try
            {
                await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);
                var (embed, embed2, embed3) = await GetWeekEmbed(false);

                if (embed3.Fields.Count > 0)
                {
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                        .WithContent("Last Weeks Timesheets")
                        .AddEmbeds(new List<DiscordEmbed>() { embed, embed2, embed3 }));
                    return;
                }
                if (embed3.Fields.Count == 0 && embed2.Fields.Count > 0)
                {
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                        .WithContent("Last Weeks Timesheets")
                        .AddEmbeds(new List<DiscordEmbed>() { embed, embed2 }));
                    return;
                }
                await ctx.EditResponseAsync(new DiscordWebhookBuilder()
                    .WithContent("Last Weeks Timesheets")
                    .AddEmbeds(new List<DiscordEmbed>() { embed }));
            }
            catch (Exception ex)
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"Error: {ex.Message}\n```{ex}```"));
            }
        }

        private static async Task<(DiscordEmbedBuilder embed, DiscordEmbedBuilder embed2, DiscordEmbedBuilder embed3)> GetWeekEmbed(bool currentWeek = true)
        {
            var weekId = ThisWeeksId() - (currentWeek ? 0 : 1);
            var dateRange = GetDateRange(weekId);
            var prefix = currentWeek ? "This" : "Last";

            try
            {
                var client = new RestClient($"{Options.RestApiUrl}/characters?select=character_id,first_name,last_name,department_name,on_duty_time&where=department_name=Bolingbroke Penitentiary");
                var request = new RestRequest()
                {
                    Method = Method.Get,
                    Timeout = -1
                };
                request.AddHeader("Authorization", $"Bearer {Options.ApiKey}");
                var response = await client.ExecuteAsync(request);

                if (response.StatusCode != HttpStatusCode.OK)
                    throw new Exception("Error with API, HTTP Status Code Not OK");

                // --- Safely parse API response (object or array) ---
                var token = JToken.Parse(response.Content);
                JArray dataArray;

                if (token is JObject obj && obj["data"] is JArray arr)
                    dataArray = arr;
                else if (token is JArray arr2)
                    dataArray = arr2;
                else
                    throw new Exception("Unexpected JSON format returned by API.");

                var records = new List<KeyValuePair<string, string>>();

                foreach (var staff in dataArray)
                {
                    var data = staff["on_duty_time"];

                    // Handle null or empty data
                    if (data == null || string.IsNullOrWhiteSpace(data.ToString()))
                    {
                        records.Add(new($"{staff["first_name"]} {staff["last_name"]}", $"No time on duty {prefix} week."));
                        continue;
                    }

                    // --- Fix: Skip invalid JSON arrays like "[]" ---
                    var raw = data.ToString().Trim();
                    if (raw.StartsWith("["))
                    {
                        records.Add(new($"{staff["first_name"]} {staff["last_name"]}", $"No time on duty {prefix} week."));
                        continue;
                    }

                    JObject dutyData;
                    try
                    {
                        dutyData = JObject.Parse(raw);
                    }
                    catch
                    {
                        records.Add(new($"{staff["first_name"]} {staff["last_name"]}", $"No time on duty {prefix} week."));
                        continue;
                    }

                    var leData = dutyData["Law Enforcement"];
                    if (leData == null || !leData.HasValues)
                    {
                        records.Add(new($"{staff["first_name"]} {staff["last_name"]}", $"No time on duty {prefix} week."));
                        continue;
                    }

                    // --- Calculate time ---
                    var workTimeNormal = new TimeSpan();
                    var workTimeUndercover = new TimeSpan();
                    var workTimeTraining = new TimeSpan();
                    var workTimeTotal = new TimeSpan();

                    if (weekId < 107)
                    {
                        workTimeTotal = TimeSpan.FromSeconds(int.Parse(leData[$"{weekId}"]?.ToString() ?? "0"));
                    }
                    else
                    {
                        try { workTimeNormal = TimeSpan.FromSeconds(int.Parse(leData[$"{weekId}"]?["normal"]?.ToString() ?? "0")); } catch { }
                        try { workTimeUndercover = TimeSpan.FromSeconds(int.Parse(leData[$"{weekId}"]?["undercover"]?.ToString() ?? "0")); } catch { }
                        try { workTimeTraining = TimeSpan.FromSeconds(int.Parse(leData[$"{weekId}"]?["training"]?.ToString() ?? "0")); } catch { }

                        workTimeTotal = workTimeNormal + workTimeUndercover + workTimeTraining;
                    }

                    var workTimeString = workTimeTotal.TotalSeconds > 0
                        ? $"{workTimeTotal.Days} Days, {workTimeTotal.Hours} Hours, {workTimeTotal.Minutes} Minutes and {workTimeTotal.Seconds} Seconds"
                        : "No Time On Duty";

                    records.Add(new($"{staff["first_name"]} {staff["last_name"]}", $"{workTimeString} {prefix} week."));
                }

                // --- Build embeds ---
                var embed = new DiscordEmbedBuilder()
                {
                    Title = $"{prefix} Weeks Timesheets",
                    Color = DiscordColor.Green
                };
                var embed2 = new DiscordEmbedBuilder()
                {
                    Title = $"{prefix} Weeks Timesheets Part 2",
                    Color = DiscordColor.Yellow
                };
                var embed3 = new DiscordEmbedBuilder()
                {
                    Title = $"{prefix} Weeks Timesheets Part 3",
                    Color = DiscordColor.Red
                };

                foreach (var record in records)
                {
                    if (embed.Fields.Count < 25)
                        embed.AddField(record.Key, record.Value);
                    else if (embed2.Fields.Count < 25)
                        embed2.AddField(record.Key, record.Value);
                    else if (embed3.Fields.Count < 25)
                        embed3.AddField(record.Key, record.Value);
                }

                return (embed, embed2, embed3);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex.Message);
                throw;
            }
        }

        private static string CalculateTime(string member, bool lastWeek = false)
        {
            Logger.LogInformation("Getting work time for " + member);
            var weekId = ThisWeeksId() - (lastWeek ? 1 : 0);
            var week = lastWeek ? "last" : "this";
            var dateRange = GetDateRange(weekId);
            var characterName = Regex.Replace(member, "^[^]]*]", "");
            characterName = characterName.Trim();
            var firstName = characterName.Split(' ', 2)[0];
            var lastName = characterName.Split(' ', 2)[1];

            try
            { 
                var client = new HttpClient();
                
                var url = $"{Options.RestApiUrl}/characters?select=character_id,first_name,last_name,department_name,on_duty_time&where=first_name={firstName},last_name={lastName}";
                var request = new RestRequest() { Method = Method.Get, Timeout = -1 };
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", $"{Options.ApiKey}");
                var response = client.GetAsync(url).Result;
                Console.WriteLine(response.Content);
                if (response.StatusCode != HttpStatusCode.OK)
                {
                    throw new Exception("Error with API, HTTP Status Code Not OK");
                }
                var responseBody = response.Content.ReadAsStringAsync().Result;
                var json = JObject.Parse(responseBody);
                if (!json["data"].Any())
                {
                    return "Unable to load character data  is you Discord Nickname the same as your Character Name?";
                }

                var characterData = json["data"][0]["on_duty_time"];


                if (characterData == null)
                {
                    return $"{characterName}, you have no time on duty {week} week";
                }

                characterData = JObject.Parse(characterData.ToString());

                if (!characterData["Law Enforcement"].HasValues)
                {
                    return $"{characterName}, you have no time on duty {week} week";
                }

                var workTimeNormal = new TimeSpan();
                var workTimeUndercover = new TimeSpan();
                var workTimeTraining = new TimeSpan();
                var workTimeTotal = new TimeSpan();
                try
                {
                    if (weekId < 107)
                    {
                        workTimeTotal = TimeSpan.FromSeconds(
                            int.Parse(characterData["Law Enforcement"][$"{weekId}"].ToString()));
                    }
                    else
                    {

                        try
                        {
                            workTimeNormal = TimeSpan.FromSeconds(
                                int.Parse(characterData["Law Enforcement"][$"{weekId}"]["normal"].ToString()));
                        }
                        catch
                        {
                            workTimeNormal = TimeSpan.FromSeconds(0);
                        }

                        try
                        {
                            workTimeUndercover = TimeSpan.FromSeconds(
                                int.Parse(
                                    characterData["Law Enforcement"][$"{weekId}"]["undercover"].ToString()));
                        }
                        catch
                        {
                            workTimeUndercover = TimeSpan.FromSeconds(0);
                        }

                        try
                        {
                            workTimeTraining = TimeSpan.FromSeconds(
                                int.Parse(
                                    characterData["Law Enforcement"][$"{weekId}"]["training"].ToString()));
                        }
                        catch
                        {
                            workTimeTraining = TimeSpan.FromSeconds(0);
                        }

                        workTimeTotal = workTimeNormal.Add(workTimeUndercover).Add(workTimeTraining);

                    }

                }
                catch (Exception e)
                {
                    return $"{characterName}, you have no time on duty {week} week";
                }

                var workTimeString =
                    ($"{workTimeTotal.Days} Days, {workTimeTotal.Hours} Hours, {workTimeTotal.Minutes} Minutes and {workTimeTotal.Minutes} Seconds");

                return $"{characterName}, you currently have total of **{workTimeString}** on duty {week} week";

            }
            catch (Exception ex)
            {
                Logger.LogError($"{ex}");
                throw;
            }
        }
        
        private static int ThisWeeksId()
        {            
            int epochNow = (int)new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds();
            int timestampDifference = epochNow - Options.WeekZero;
            return (int)Math.Floor((decimal)(timestampDifference / 604800));
            
        }

        private static string GetDateRange(int weekId)
        {
            CultureInfo _culture = (CultureInfo)CultureInfo.CurrentCulture.Clone();            
            _culture.DateTimeFormat.FirstDayOfWeek = DayOfWeek.Monday;
            System.Threading.Thread.CurrentThread.CurrentCulture = _culture;
            

            var timestamp = (weekId * 604800) + Options.WeekZero;
            var utcIsoWeek = DateTimeOffset.FromUnixTimeSeconds(timestamp).UtcDateTime;
            var isoWeek = TimeZoneInfo.ConvertTimeFromUtc(utcIsoWeek, TimeZoneInfo.FindSystemTimeZoneById("GMT Standard Time")); // Account for DST

            var startOfWeek = isoWeek.ToString("yyyy/MM/dd");
            var endOfWeek = isoWeek.AddDays(6).ToString("yyyy/MM/dd");

            return $"{startOfWeek} - {endOfWeek}";
        }
    }
}
