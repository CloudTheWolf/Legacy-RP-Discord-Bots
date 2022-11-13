﻿using CloudTheWolf.DSharpPlus.Scaffolding.Shared.Interfaces;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;
//using DSharpPlus.Lavalink;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using CloudTheWolf.DSharpPlus.Scaffolding.Logging;
using Harmony.Module.Actions;
using Harmony.Module.Commands;


namespace Harmony.Module
{
    public class Main : IPlugin
    {
        public string Name => "Harmony Bot";
        public string Description => "Bot for LegacyRP Harmony Mechanic Shop";
        public int Version => 3;
        public static ILogger<Logger> Logger;
        public static InteractivityExtension Interactivity;
        public static DiscordClient Client;
        private static DiscordConfiguration _discordConfiguration;
        private static IConfiguration _applicationConfig;

        public void InitPlugin(IBot bot, ILogger<Logger> logger, DiscordConfiguration discordConfiguration, IConfigurationRoot applicationConfig)
        {
            try
            {
                Logger = logger;
                LoadConfig(applicationConfig, bot);
                _discordConfiguration = discordConfiguration;
                 logger.LogInformation(this.Name + ": Loaded successfully!");
                 if (!Libs.OperatingSystem.IsWindows())
                     logger.LogInformation("We are NOT on Windows");
                 Interactivity = bot.Client.GetInteractivity();
                 bot.Client.Intents.AddIntent(DiscordIntents.All);
                 Client = bot.Client;
                 bot.Client.GuildDownloadCompleted += SetStatus;
                AddCommands(bot, Name);

            }
            catch (Exception e)
            {
                logger.LogCritical($"Failed to load {Name} \n {e}");
            }
        }

        private static void LoadConfig(IConfiguration applicationConfig, IBot bot)
        {
            Options.MySqlHost = applicationConfig.GetValue<string>("SQL:Host");
            Options.MySqlPort = applicationConfig.GetValue<int>("SQL:Port");
            Options.MySqlUsername = applicationConfig.GetValue<string>("SQL:user");
            Options.MySqlPassword = applicationConfig.GetValue<string>("SQL:pass");
            Options.MySqlDatabase = applicationConfig.GetValue<string>("SQL:name");
            Options.CompanyName = applicationConfig.GetValue<string>("CompanyName");
            Options.GuildId = applicationConfig.GetValue<ulong>("GuildId");
            Options.ManagerRoleId = applicationConfig.GetValue<ulong>("ManagerRole");
        }

        private static void AddCommands(IBot bot, string Name)
        {
            bot.SlashCommandsExt.RegisterCommands<StaffActions>();
            bot.Commands.RegisterCommands<StaffCommands>();
            Logger.LogInformation(Name + ": Registered {0}!", nameof(StaffCommands));
            
        }

        private static async Task SetStatus(DiscordClient client, GuildDownloadCompletedEventArgs args)
        {
            var gName = Client.Guilds[Options.GuildId].Name;
            Options.ManagerRole = Client.Guilds[Options.GuildId]
                .GetRole(Options.ManagerRoleId);
            var status = new Random().Next(1,6);
            Console.WriteLine($"{gName} - {status}");
            switch (status)
            {
                case 1:
                    await client.UpdateStatusAsync(new DiscordActivity("Life", ActivityType.Competing));
                    break;
                case 2:
                    await client.UpdateStatusAsync(new DiscordActivity("LegacyRP", ActivityType.Playing));
                    break;
                case 3:
                    await client.UpdateStatusAsync(new DiscordActivity("Epic Music", ActivityType.ListeningTo));
                    break;
                case 4:
                    var stream = new DiscordActivity("On Twitch", ActivityType.Streaming)
                    {
                        Name = "On Twitch",
                        ActivityType = ActivityType.Streaming,
                        StreamUrl = "https://www.twitch.tv/monstercat"
                    };
                    await client.UpdateStatusAsync(stream);
                    break;
                default:
                    await client.UpdateStatusAsync(new DiscordActivity($"{gName}", ActivityType.Watching));
                    break;
            }
        }
    }
}