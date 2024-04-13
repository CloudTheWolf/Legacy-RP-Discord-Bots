using CloudTheWolf.DSharpPlus.Scaffolding.Shared.Interfaces;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using CloudTheWolf.DSharpPlus.Scaffolding.Logging;
using Harmony.Module.Actions;
using Harmony.Module.Events;


namespace Harmony.Module
{
    public class Main : IPlugin
    {
        public string Name => "Harmony Bot";
        public string Description => "Bot for LegacyRP Harmony Mechanic Shop";
        public int Version => 4;
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
                 logger.LogInformation("{0}: Loaded successfully!",Name);
                 if (!Libs.OperatingSystem.IsWindows())
                     logger.LogInformation("We are NOT on Windows");
                 Interactivity = bot.Client.GetInteractivity();
                 bot.Client.Intents.AddIntent(DiscordIntents.All);
                 Client = bot.Client;
                 bot.Client.GuildDownloadCompleted += SetStatus;
                 bot.Client.Heartbeated += Heartbeated.AutoOffDuty;
                 //TODO: Do this when we have CityData to play with
                 //bot.Client.Heartbeated += Heartbeated.SyncCityData;
                AddCommands(bot, Name);

            }
            catch (Exception e)
            {
                logger.LogCritical("Failed to load {0} \n {1}",Name,e.Message);
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
            Options.ApiKey = applicationConfig.GetValue<string>("ApiKey");
            Options.ApiUrl = applicationConfig.GetValue<string>("ApiUrl");
            Options.RestApiUrl = applicationConfig.GetValue<string>("RestApiUrl");
            Options.DutyChannelId = applicationConfig.GetValue<ulong>("dutyChannel");
        }

        private static void AddCommands(IBot bot, string Name)
        {
            bot.SlashCommandsExt.RegisterCommands<StaffActions>();
            Logger.LogInformation(Name + ": Registered {0}!", nameof(StaffActions));
            
        }

        private static async Task SetStatus(DiscordClient client, GuildDownloadCompletedEventArgs args)
        {
            var gName = Client.Guilds[Options.GuildId].Name;
            Options.ManagerRole = Client.Guilds[Options.GuildId]
                .GetRole(Options.ManagerRoleId);
            Options.DutyChannel = Client.Guilds[Options.GuildId].GetChannel(Options.DutyChannelId);
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