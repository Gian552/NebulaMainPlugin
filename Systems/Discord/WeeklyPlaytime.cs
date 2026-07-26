using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using LabApi.Features.Console;
using NebMainPluginLabApi.API;
using NebMainPluginLabApi.API.Enums;
using NebMainPluginLabApi.Systems.Database;
using YamlDotNet.Core.Tokens;

namespace NebMainPluginLabApi.Systems.Discord
{
    internal class WeeklyPlaytime
    {
        private static List<PlayerData> Teamlers = new();
        private static CancellationTokenSource _cts = new();

        internal static void Enable()
        {
            Task.Run(() => TimeCheck(_cts.Token));
        }

        internal static void Disable()
        {
            _cts.Cancel();
            _cts.Dispose();
            _cts = null;
        }

        internal static async Task TimeCheck(CancellationToken ct)
        {
            try
            {
                while (!ct.IsCancellationRequested)
                {
                    if (DateTime.Now.DayOfWeek == DayOfWeek.Saturday && DateTime.Now.Hour == 19 && (DateTime.Now.Minute == 0 || DateTime.Now.Minute == 1))
                    {
                        if (!await SendWeeklyTeamlerReport())
                        {
                            Logger.Error("Teamler Zeit Report fehlgeschlagen!");
                        }

                        Logger.Info("Teamler Report sollte gestartet worden sein... hats safe einfach nicht gemacht lol");

                        await Task.Delay(3600000, ct);
                    }

                    await Task.Delay(30000, ct);
                }
            }
            catch (OperationCanceledException)
            {
                Logger.Debug("Playtime SendTimeLoop cancled.");
            }
            catch (Exception ex)
            {
                Logger.Error($"Unhandled error in SendTimeLoop: {ex}");
            }
        }

        private static async Task UpdateTeamList()
        {
            Teamlers.Clear();

            foreach (PlayerData plyd in PlayerDataCache.Data.Values)
            {
                if (plyd.dcRole.GetDiscordRoleType() == "Team")
                    Teamlers.Add(plyd);
            }
        }

        internal static async Task<bool> SendWeeklyTeamlerReport(bool command = false)
        {
            await UpdateTeamList();

            ConcurrentDictionary<string, string> TeamEmbed = new();

            foreach (PlayerData ply in Teamlers)
            {
                if (ply.DiscordId.IsEmpty() || ply.DiscordId == null)
                    continue;

                var seconds = (ply.Playtime ?? 0) - (ply.WeekStart ?? 0);
                var ts = TimeSpan.FromSeconds(seconds);

                if (ply.WeekStart == null || seconds <= 0)
                {
                    TeamEmbed.TryAdd($"@{ply.Nickname}", "Hatte diese Woche keine Spielzeit!");
                    continue;
                }
                
                TeamEmbed.TryAdd($"@{ply.Nickname}", $"{Math.Truncate(ts.TotalHours)}h {ts.Minutes}min");
            }
            
            if (!command)
            {
                await Database.Database.ResetWeeklyPlaytime();
            }

            try
            {
                await DiscordWebhookAPI.SendMs("Spielzeiten:", "Teamler Spielzeiten Report", "Die Spielzeiten für Nebula Staff.", TeamEmbed);
            }
            catch (Exception e)
            {
                Logger.Error($"Problem mit Teamler Report senden: \n{e}");
                throw;
            }
            
            Logger.Info("Teamler Report sollte funktioniert haben.");
            return true;
        }
    }
}
