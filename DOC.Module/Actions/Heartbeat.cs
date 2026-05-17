using DSharpPlus;
using DSharpPlus.Entities;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using OpenSearch.Net.Specification.IndicesApi;
using Serilog;
using System.Data;
using System.Diagnostics;

namespace DOC.Module.Actions;

internal class Heartbeat
{
    /// <summary>
    /// Is this the first update since the bot started?
    /// </summary>
    private static bool firstRun = true;

    private static bool ranCleanup = false;

    /// <summary>
    /// All DOC Staff
    /// </summary>
    private static JObject allDocStaff = new();

    private static CancellationTokenSource? _loopCancellation;

    /// <summary>
    /// Start the periodic duty update loop
    /// </summary>
    /// <param name="client"></param>
    public static void Start(DiscordClient client)
    {
        if (_loopCancellation != null)
            return;

        _loopCancellation = new CancellationTokenSource();

        _ = Task.Run(() => DutyLoopAsync(client, _loopCancellation.Token));
    }

    /// <summary>
    /// Stop the periodic loop
    /// </summary>
    public static void Stop()
    {
        _loopCancellation?.Cancel();
        _loopCancellation = null;
    }

    /// <summary>
    /// Main periodic loop
    /// </summary>
    private static async Task DutyLoopAsync(
        DiscordClient client,
        CancellationToken cancellationToken)
    {
        PeriodicTimer timer = new(TimeSpan.FromMinutes(1));

        try
        {
            while (await timer.WaitForNextTickAsync(cancellationToken))
            {
                try
                {
                    await UpdateDutyMessageAsync(client);
                }
                catch (Exception ex)
                {
                    Log.Error(
                        ex,
                        "[HeartbeatLoop] Failed updating duty message");
                    Environment.Exit(1); //Force App to restart
                }
            }
        }
        catch (OperationCanceledException)
        {
            // expected during shutdown
        }
    }

    /// <summary>
    /// Update the duty message
    /// </summary>
    public static async Task UpdateDutyMessageAsync(DiscordClient sender)
    {
        await GetStaffAsync();

        var targetGuild = sender.Guilds[Options.GuildId];

        var targetMessage = Options.OnDutyMessage;

        var targetChannel =
            await targetGuild.GetChannelAsync(Options.OnDutyChannel);

        if (firstRun)
        {
            firstRun = false;
            return;
        }

        if (Options.LastMessage != DateTime.MinValue &&
            (DateTime.UtcNow - Options.LastMessage).TotalMinutes < 2)
        {
            Log.Information(
                "[Heartbeat-Duty] Nothing To Do...");

            return;
        }

        if (!ranCleanup)
        {

            await foreach (var channelMessage in targetChannel.GetMessagesAsync())
            {
                _ = channelMessage.DeleteAsync();
            }

            ranCleanup = true;
        }

        var newMessage = await CreateDutyMessageAsync();

        DiscordMessage existingMessage = null;

        if (targetMessage != ulong.MinValue)
        {
            existingMessage =
                await targetChannel.GetMessageAsync(
                    targetMessage,
                    true);
        }

        if (targetMessage != ulong.MinValue &&
            existingMessage != null &&
            (DateTimeOffset.UtcNow -
             existingMessage.CreationTimestamp.UtcDateTime)
            .TotalMinutes > 30)
        {
            _ = existingMessage.DeleteAsync("cleanup");

            targetMessage = ulong.MinValue;
        }

        if (targetMessage == ulong.MinValue)
        {
            var finalMessage =
                await targetChannel.SendMessageAsync(newMessage);

            Options.OnDutyMessage = finalMessage.Id;

            return;
        }

        if (existingMessage != null)
        {
            await existingMessage.ModifyAsync(newMessage);
        }
    }

    /// <summary>
    /// Create the OnDuty Message
    /// </summary>
    private static async Task<string> CreateDutyMessageAsync()
    {
        var duty = await GetDutyAsync();

        var docDuty = new JArray();

        var message =
            "# San Andreas Department of Corrections Status\n";

        message += "*Status may be delayed by up to 5 minutes*\n";

        message +=
            $"*Last Updated:<t:{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}:R>*\n\n";

        try
        {
            foreach (var onDuty in duty["data"]["Law Enforcement"])
            {
                if (onDuty["department"]
                        .ToString()
                        .ToLower() != "doc")
                    continue;

                docDuty.Add(onDuty);
            }
        }
        catch
        {
            Log.Information(
                "[Heartbeat-CreateMessage] No DOC Duty");
        }

        message += $"**Total:** {docDuty.Count}\n\n";

        message += "__**On Duty**__\n";

        foreach (var user in docDuty)
        {
            var name = "";

            foreach (var staff in allDocStaff["data"])
            {
                if (staff["character_id"].ToString() !=
                    user["characterId"].ToString())
                    continue;

                name =
                    $"{staff["first_name"]} {staff["last_name"]}";
            }

            message +=
                $"<:DOC:1046006478693224498> {name} ";

            message += bool.Parse(user["training"].ToString())
                ? " [Training]\n"
                : "\n";
        }

        return message;
    }

    /// <summary>
    /// Get On Duty From Server
    /// </summary>
    private static async Task<JObject> GetDutyAsync()
    {
        using var client = new HttpClient();

        var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"{Options.ApiUrl}/op-framework/duty.json");

        request.Headers.Add(
            "Authorization",
            $"Bearer {Options.ApiKey}");

        var response = await client.SendAsync(request);

        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();

        return JObject.Parse(json);
    }

    /// <summary>
    /// Get Staff from OPFW API
    /// </summary>
    private static async Task GetStaffAsync()
    {
        using var client = new HttpClient();

        var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"{Options.RestApiUrl}/characters?select=*&where=department_name=Bolingbroke Penitentiary");

        request.Headers.Add(
            "Authorization",
            $"Bearer {Options.ApiKey}");

        var response = await client.SendAsync(request);

        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();

        allDocStaff = JObject.Parse(json);
    }
}