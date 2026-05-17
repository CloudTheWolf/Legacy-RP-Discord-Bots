using CloudTheWolf.DSharpPlus.Scaffolding.Logging;
using CloudTheWolf.DSharpPlus.Scaffolding.Shared.Interfaces;
using Serilog;
using DOC.Module.Actions;
using ILogger = Serilog.ILogger;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.Net.Gateway;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using DSharpPlus.Commands.Trees;

namespace DOC.Module
{

    public class Main : IPlugin
    {
        public string Name => "DOC Bot";

        public string Description => "Bot for LegacyRP DOC";

        public int Version => 3;

        public static DiscordClient Client;

        public static IGatewayController GatewayController;

        private static DiscordConfiguration _discordConfiguration;


        public void InitPlugin(IBot bot, ILogger logger, DiscordConfiguration discordConfiguration,
            IConfigurationRoot applicationConfig)
        {
            try
            {
                Options.LogPrefix = Name;
                Logger.Initialize();
                LoadConfig(applicationConfig, bot);
                _discordConfiguration = discordConfiguration;
                Log.Information(this.Name + ": Loaded successfully!");
                if (!Libs.OperatingSystem.IsWindows())
                Log.Information("We are NOT on Windows");             
                Client = bot.Client;
                bot.EventHandlerRegistry.Register(e => e.HandleMessageCreated(MessageActions.OnMessageCreated));
                bot.EventHandlerRegistry.Register(e => e.HandleGuildDownloadCompleted(SetStatus));
                bot.EventHandlerRegistry.Register(e => e.HandleGuildDownloadCompleted(CleanupOldMessage));
                AddCommands(bot, Name);

            }
            catch (Exception e)
            {
               
                    Log.Fatal($"Failed to load {Name} \n {e}");
            }
        }

        private async Task CleanupOldMessage(DiscordClient sender, GuildDownloadCompletedEventArgs args)
        {
            Log.Information("Cleaning Old Message(s)");
            var guild = await sender.GetGuildAsync(Options.GuildId);
            var channel = await guild.GetChannelAsync(Options.OnDutyChannel);
            await foreach (var message in channel.GetMessagesAsync())
            {
                try
                {
                    if (message.Author.IsCurrent || message.Author.IsBot)
                    {
                        _ = message.DeleteAsync();
                    }
                }
                catch (Exception e)
                {
                    Log.Error($"Failed to cleanup messages \n{e}");
                }
            }
            _ = Heartbeat.UpdateDutyMessageAsync(sender);

        }

        private static void LoadConfig(IConfiguration applicationConfig, IBot bot)
        {
            Options.GuildId = applicationConfig.GetValue<ulong>("GuildId");
            Options.WeekZero = applicationConfig.GetValue<int>("WeekZero");
            Options.RestApiUrl = applicationConfig.GetValue<string>("RestApiUrl");
            Options.ApiUrl = applicationConfig.GetValue<string>("ApiUrl");
            Options.ApiKey = applicationConfig.GetValue<string>("ApiKey");
            Options.OnDutyChannel = applicationConfig.GetValue<ulong>("dutyChannel");
            Options.DocRoleId = applicationConfig.GetValue<ulong>("docRoleId");
        }

        private static void AddCommands(IBot bot, string Name)
        {
            var TimeCommands = CommandBuilder.From(typeof(TimeActions));
            bot.CommandsList.Add(TimeCommands);
            Log.Information(Name + ": Registered {0}!", nameof(TimeActions));

        }

        private static async Task SetStatus(DiscordClient client, GuildDownloadCompletedEventArgs args)
        {
            var gName = Client.Guilds[Options.GuildId].Name;
            var status = new Random().Next(1, 6);
            Console.WriteLine($"{gName} - {status}");
            switch (status)
            {
                case 1:
                    await client.UpdateStatusAsync(new DiscordActivity("Life", DiscordActivityType.Competing));
                    break;
                case 2:
                    await client.UpdateStatusAsync(new DiscordActivity("LegacyRP", DiscordActivityType.Playing));
                    break;
                case 3:
                    await client.UpdateStatusAsync(new DiscordActivity("Epic Music", DiscordActivityType.ListeningTo));
                    break;
                case 4:
                    var stream = new DiscordActivity("On Twitch", DiscordActivityType.Streaming)
                                     {
                                         Name = "On Twitch",
                                         ActivityType = DiscordActivityType.Streaming,
                                         StreamUrl = "https://www.twitch.tv/monstercat"
                                     };
                    await client.UpdateStatusAsync(stream);
                    break;
                default:
                    await client.UpdateStatusAsync(new DiscordActivity($"{gName}", DiscordActivityType.Watching));
                    break;
            }
        }
    }
}