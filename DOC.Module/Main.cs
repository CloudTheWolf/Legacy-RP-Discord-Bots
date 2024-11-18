using CloudTheWolf.DSharpPlus.Scaffolding.Logging;
using CloudTheWolf.DSharpPlus.Scaffolding.Shared.Interfaces;

using DOC.Module.Actions;

using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace DOC.Module
{

    public class Main : IPlugin
    {
        public string Name => "DOC Bot";

        public string Description => "Bot for LegacyRP DOC";

        public int Version => 3;

        public static ILogger<Logger> Logger;

        public static InteractivityExtension Interactivity;

        public static DiscordClient Client;

        private static DiscordConfiguration _discordConfiguration;

        private static IConfiguration _applicationConfig;

        public void InitPlugin(
            IBot bot,
            ILogger<Logger> logger,
            DiscordConfiguration discordConfiguration,
            IConfigurationRoot applicationConfig)
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
                Client = bot.Client;
                bot.Client.Heartbeated += Heartbeat.GetOnDutyHeartbeatAsync;
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
            Options.GuildId = applicationConfig.GetValue<ulong>("GuildId");
            Options.WeekZero = applicationConfig.GetValue<int>("WeekZero");
            Options.RestApiUrl = applicationConfig.GetValue<string>("RestApiUrl");
            Options.ApiUrl = applicationConfig.GetValue<string>("ApiUrl");
            Options.ApiKey = applicationConfig.GetValue<string>("ApiKey");
            Options.OnDutyChannel = applicationConfig.GetValue<ulong>("dutyChannel");
        }

        private static void AddCommands(IBot bot, string Name)
        {
            bot.SlashCommandsExt.RegisterCommands<TimeActions>();
            Logger.LogInformation(Name + ": Registered {0}!", nameof(TimeActions));

        }

        private static async Task SetStatus(DiscordClient client, GuildDownloadCompletedEventArgs args)
        {
            var gName = Client.Guilds[Options.GuildId].Name;
            var status = new Random().Next(1, 6);
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