using DSharpPlus.Commands;
using DSharpPlus.Commands.ContextChecks;
using DSharpPlus.Commands.Processors.SlashCommands;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System.ComponentModel;
using System.Globalization;

namespace DOC.Module.Actions;

[Command("loa")]
[Description("LOA Commands")]
[RequirePermissions(
    botPermissions: [],
    userPermissions: [DiscordPermission.UseApplicationCommands])]
internal class LoaCommands
{
    [Command("request")]
    [Description("Request LOA")]
    [RequirePermissions(
        botPermissions: [],
        userPermissions: [DiscordPermission.UseApplicationCommands])]
    public async Task RequestLoa(SlashCommandContext ctx)
    {
        try
        {
            var permittedRoleIds = new[]
            {
                Options.DocRoleId,
                1049538413117849640UL
            };

            if (!ctx.Member.Roles.Any(
                    role => permittedRoleIds.Contains(role.Id)))
            {
                await ctx.RespondAsync(
                    new DiscordInteractionResponseBuilder()
                        .WithContent(
                            "You do not have permission to use this command.")
                        .AsEphemeral());

                return;
            }

            var interactivity = ctx.Client.ServiceProvider
                .GetRequiredService<InteractivityExtension>();

            var modal = new DiscordInteractionResponseBuilder()
                .WithTitle("LOA Request Form")
                .WithCustomId($"loa_request_form:{ctx.User.Id}")
                .AddTextInputComponent(
                    new DiscordTextInputComponent(
                        label: "LOA Start Date",
                        customId: "start_date",
                        placeholder: "MM/DD/YYYY",
                        required: true,
                        style: DiscordTextInputStyle.Short,
                        min_length: 10,
                        max_length: 10))
                .AddTextInputComponent(
                    new DiscordTextInputComponent(
                        label: "LOA End Date",
                        customId: "end_date",
                        placeholder: "MM/DD/YYYY",
                        required: true,
                        style: DiscordTextInputStyle.Short,
                        min_length: 10,
                        max_length: 10))
                .AddTextInputComponent(
                    new DiscordTextInputComponent(
                        label: "LOA Reason",
                        customId: "reason",
                        placeholder: "Because...",
                        required: true,
                        style: DiscordTextInputStyle.Paragraph,
                        min_length: 10,
                        max_length: 1024));

            var modalId = $"loa_request_form:{ctx.User.Id}";

            await ctx.Interaction.CreateResponseAsync(
                DiscordInteractionResponseType.Modal,
                modal);

            var modalResult = await interactivity.WaitForModalAsync(
                modalId,
                ctx.User,
                TimeSpan.FromMinutes(14));

            if (modalResult.TimedOut)
                return;

            var modalInteraction = modalResult.Result.Interaction;

            // Acknowledge the modal submission immediately.
            await modalInteraction.CreateResponseAsync(
                DiscordInteractionResponseType
                    .DeferredChannelMessageWithSource,
                new DiscordInteractionResponseBuilder()
                    .AsEphemeral());

            try
            {
                var values = modalResult.Result.Values;

                if (!values.TryGetValue("start_date", out var startDateText) ||
                    !values.TryGetValue("end_date", out var endDateText) ||
                    !values.TryGetValue("reason", out var reason))
                {
                    throw new InvalidOperationException(
                        "One or more modal values were not returned.");
                }

                if (!DateTime.TryParseExact(
                        startDateText,
                        "MM/dd/yyyy",
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.None,
                        out var startDate))
                {
                    await modalInteraction.EditOriginalResponseAsync(
                        new DiscordWebhookBuilder()
                            .WithContent(
                                "❌ The start date must use MM/DD/YYYY."));

                    return;
                }

                if (!DateTime.TryParseExact(
                        endDateText,
                        "MM/dd/yyyy",
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.None,
                        out var endDate))
                {
                    await modalInteraction.EditOriginalResponseAsync(
                        new DiscordWebhookBuilder()
                            .WithContent(
                                "❌ The end date must use MM/DD/YYYY."));

                    return;
                }

                if (endDate < startDate)
                {
                    await modalInteraction.EditOriginalResponseAsync(
                        new DiscordWebhookBuilder()
                            .WithContent(
                                "❌ The end date cannot be before the start date."));

                    return;
                }

                var loaChannel = await ctx.Client.GetChannelAsync(
                    1025095076784918539);

                var displayName =
                    ctx.Member.Nickname ??
                    ctx.User.Username;

                var embed = new DiscordEmbedBuilder()
                    .WithTitle("📨 LOA Request")
                    .WithColor(DiscordColor.DarkGreen)
                    .WithTimestamp(DateTimeOffset.UtcNow)
                    .WithFooter(
                        "Los Santos LOA Manager",
                        ctx.User.AvatarUrl)
                    .AddField(
                        "LOA Submitted",
                        $"{DateTime.UtcNow:MM/dd/yyyy} (UTC)",
                        true)
                    .AddField("Requested By", displayName)
                    .AddField("Reason", reason)
                    .AddField(
                        "LOA Start Date",
                        startDate.ToString("MM/dd/yyyy"))
                    .AddField(
                        "LOA End Date",
                        endDate.ToString("MM/dd/yyyy"),
                        true);

                await loaChannel.SendMessageAsync(embed);

                await modalInteraction.EditOriginalResponseAsync(
                    new DiscordWebhookBuilder()
                        .WithContent(
                            "✅ LOA request submitted successfully!"));
            }
            catch (Exception ex)
            {
                Log.Error(
                    ex,
                    "[LOA] Failed processing modal submission for {UserId}",
                    ctx.User.Id);

                await modalInteraction.EditOriginalResponseAsync(
                    new DiscordWebhookBuilder()
                        .WithContent(
                            "❌ Something went wrong while processing your request."));
            }
        }
        catch (Exception ex)
        {
            Log.Error(
                ex,
                "[LOA] Failed opening LOA request form for {UserId}",
                ctx.User.Id);

            try
            {
                await ctx.FollowupAsync(
                    new DiscordFollowupMessageBuilder()
                        .WithContent(
                            "❌ Something went wrong while opening the LOA form.")
                        .AsEphemeral());
            }
            catch (Exception responseException)
            {
                Log.Error(
                    responseException,
                    "[LOA] Failed sending error response for {UserId}",
                    ctx.User.Id);
            }
        }
    }
}