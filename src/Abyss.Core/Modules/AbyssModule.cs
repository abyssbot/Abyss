﻿using Abyss.Attributes;
using Abyss.Checks.Command;
using Abyss.Entities;
using Abyss.Extensions;
using Abyss.Results;
using Discord;
using Discord.WebSocket;
using Humanizer;
using Qmmands;
using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.Versioning;
using System.Text;
using System.Threading.Tasks;

namespace Abyss.Modules
{
    [Name("Abyss")]
    [Description("Provides commands related to me.")]
    public class AbyssModule : AbyssModuleBase
    {
        private readonly ICommandService _commandService;
        private readonly AbyssConfig _config;

        public AbyssModule(ICommandService commandService, AbyssConfig config)
        {
            _commandService = commandService;
            _config = config;
        }

        [Command("Uptime")]
        [Example("uptime")]
        [Description("Displays the time that this bot process has been running.")]
        public Task<ActionResult> Command_GetUptimeAsync()
        {
            return Ok($"**Uptime:** {(DateTime.Now - Process.GetCurrentProcess().StartTime).Humanize(20)}");
        }

        [Command("Info", "Abyss", "About")]
        [Example("info")]
        [RunMode(RunMode.Parallel)]
        [Description("Shows some information about me.")]
        public async Task<ActionResult> Command_GetAbyssInfoAsync()
        {
            var dotnetVersion =
                Assembly.GetEntryAssembly()?.GetCustomAttribute<TargetFrameworkAttribute>()?.FrameworkName ??
                ".NET Core";

            var app = await Context.Client.GetApplicationInfoAsync().ConfigureAwait(false);
            var response = new EmbedBuilder
            {
                ThumbnailUrl = Context.Client.CurrentUser.GetEffectiveAvatarUrl(),
                Description = string.IsNullOrEmpty(app.Description) ? "None" : app.Description,
                Author = new EmbedAuthorBuilder
                {
                    Name = $"Information about {_config.Name}",
                    IconUrl = Context.Bot.GetEffectiveAvatarUrl()
                }
            };

            response
                .AddField("Uptime", DateTime.Now - Process.GetCurrentProcess().StartTime)
                .AddField("Heartbeat", Context.Client.Latency + "ms", true)
                .AddField("Commands", _commandService.GetAllCommands().Count(), true)
                .AddField("Modules", _commandService.GetAllModules().Count(), true)
                .AddField("Source", $"https://github.com/abyssal512/Abyss");

            if (!string.IsNullOrWhiteSpace(_config.Connections.Discord.SupportServer))
            {
                response.AddField("Support Server", _config.Connections.Discord.SupportServer, true);
            }

            response
                .AddField("Language",
                    $"C# 7.1 ({dotnetVersion})")
                .AddField("Libraries", $"Discord.Net {DiscordConfig.Version} w/ Qmmands");

            return Ok(response);
        }

        [Command("Feedback", "Request")]
        [Example("feedback Bot is great!", "feedback Add more cats!!!")]
        [Description("Sends feedback to the developer.")]
        [AbyssCooldown(1, 24, CooldownMeasure.Hours, CooldownType.User)]
        public async Task<ActionResult> Command_SendFeedbackAsync([Remainder] string feedback)
        {
            var app = await Context.Client.GetApplicationInfoAsync().ConfigureAwait(false);
            var _ = app.Owner.SendMessageAsync(
                $"Feedback from {Context.Invoker} in {Context.Guild?.ToString() ?? "their DM channel"}:\n\"{feedback}\"");

            return Ok("Feedback sent!");
        }

        [Command("Ping")]
        [Description("Benchmarks the connection to the Discord servers.")]
        [AbyssCooldown(1, 3, CooldownMeasure.Seconds, CooldownType.User)]
        public async Task<ActionResult> Command_PingAsync()
        {
            if (!Context.BotUser.GetPermissions(Context.Channel).SendMessages) return NoResult();
            var sw = Stopwatch.StartNew();
            var initial = await Context.Channel.SendMessageAsync("Pinging...").ConfigureAwait(false);
            var restTime = sw.ElapsedMilliseconds.ToString();

            Task Handler(SocketMessage msg)
            {
                if (msg.Id != initial.Id) return Task.CompletedTask;

                var _ = initial.ModifyAsync(m =>
                {
                    var rtt = sw.ElapsedMilliseconds.ToString();
                    Context.Client.MessageReceived -= Handler;
                    m.Content = null;
                    m.Embed = new EmbedBuilder()
                        .WithAuthor("Ping Results", Context.Bot.GetEffectiveAvatarUrl())
                        .WithRequesterFooter(Context)
                        .WithTimestamp(DateTime.Now)
                        .WithDescription(
                            "The **heartbeat** is a series of regular pings between me and Discord, to notify each other that we haven't gone down yet.\nThe **REST** timer tracks how long it takes to send a message.\nThe **Round-trip** timer tracks how long it takes to receive the message that we've sent.")
                        .AddField("Heartbeat", Context.Client.Latency + "ms", true)
                        .AddField("REST", restTime + "ms", true)
                        .AddField("Round-trip", rtt + "ms", true)
                        .WithColor(Context.Invoker.GetHighestRoleColourOrDefault())
                        .Build();
                });
                sw.Stop();

                return Task.CompletedTask;
            }

            Context.Client.MessageReceived += Handler;

            return NoResult();
        }

        [Command("Prefix")]
        [Example("prefix")]
        [Description("Shows the prefix.")]
        public Task<ActionResult> ViewPrefixesAsync()
        {
            return Ok($"The prefix is `{Context.GetPrefix()}`, but you can invoke commands by mention as well, such as: \"{Context.BotUser.Mention} help\".");
        }

        [Command("DevInfo")]
        [Example("devinfo")]
        [Description(
            "Dumps current information about the client, the commands system and the current execution environment.")]
        [RequireOwner]
        public Task<ActionResult> Command_MemoryDumpAsync()
        {
            return Ok(new StringBuilder()
                .AppendLine("```json")
                .AppendLine("== Core ==")
                .AppendLine($"{Context.Client.Guilds.Count} guilds")
                .AppendLine($"{Context.Client.DMChannels.Count} DM channels")
                .AppendLine($"{Context.Client.GroupChannels.Count} group channels")
                .AppendLine("== Commands ==")
                .AppendLine($"{_commandService.GetAllModules().Count()} modules")
                .AppendLine($"{_commandService.GetAllCommands().Count()} commands")
                .AppendLine("== Environment ==")
                .AppendLine($"Operating System: {Environment.OSVersion}")
                .AppendLine($"Processor Count: {Environment.ProcessorCount}")
                .AppendLine($"64-bit OS: {Environment.Is64BitOperatingSystem}")
                .AppendLine($"64-bit Process: {Environment.Is64BitProcess}")
                .AppendLine($"Current Thread ID: {Environment.CurrentManagedThreadId}")
                .AppendLine($"System Name: {Environment.MachineName}")
                .AppendLine($"CLR Version: {Environment.Version}")
                .AppendLine($"Culture: {CultureInfo.InstalledUICulture.EnglishName}")
                .AppendLine("```").ToString());
        }
    }
}