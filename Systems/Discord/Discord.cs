
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Arguments.ServerEvents;
using LabApi.Events.Handlers;
using LabApi.Features.Console;
using LabApi.Features.Wrappers;
using PlayerStatsSystem;
using YamlDotNet.Core.Tokens;
using DCWB = NebMainPlugin.API.DiscordWebhookAPI;


namespace NebMainPlugin.Systems.Discord
{
    public static class Loggs
    {
        private static CancellationTokenSource _cts;
        public static void Enable()
        {
            ServerEvents.RoundStarted += OnRoundStarted;
            ServerEvents.RoundEnded += OnRoundEnded;
            PlayerEvents.Joined += OnJoined;
            PlayerEvents.Left += OnLeft;
            PlayerEvents.Death += OnDeath;
            PlayerEvents.Hurting += OnHurt;
            PlayerEvents.Cuffed += OnHandcuffing;
            PlayerEvents.Uncuffed += OnRemovedHandcuffes;
            PlayerEvents.Spawned += OnSpawned;

            _cts = new CancellationTokenSource();
            _ = Task.Run(() => StartLogSendingLoop(_cts.Token));
            _ = DCWB.SendMs("!!! Server Startup !!!");
        }

        public static void Disable()
        {
            ServerEvents.RoundStarted -= OnRoundStarted;
            ServerEvents.RoundEnded -= OnRoundEnded;
            PlayerEvents.Joined -= OnJoined;
            PlayerEvents.Left -= OnLeft;
            PlayerEvents.Death -= OnDeath;
            PlayerEvents.Hurting -= OnHurt;
            PlayerEvents.Cuffed -= OnHandcuffing;
            PlayerEvents.Uncuffed -= OnRemovedHandcuffes;
            PlayerEvents.Spawned -= OnSpawned;

            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
        }

        private static string msg = "";

        private static void AddLog(string Log)
        {
            msg += $"{Log}\\n";
        }

        private static async Task StartLogSendingLoop(CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    if (!string.IsNullOrWhiteSpace(msg))
                    {
                        string tmp = msg;
                        msg = "";
                        await DCWB.SendMs(tmp);
                    }
                    else
                    {
                        await Task.Delay(7000, token);
                    }

                    await Task.Delay(3000, token);
                }
            }
            catch (OperationCanceledException)
            {
                Logger.Info("Log sending loop cancelled.");
            }
            catch (Exception ex)
            {
                Logger.Error($"Unhandled error in StartLogSendingLoop: {ex}");
            }
        }


        // Start Log funcs

        private static void OnRoundStarted()
        {
            AddLog($"[{DateTime.Now}] \\n \\n \\n**-------------------------------------------------------------------------------------\\nEine neu Runde hat jetzt gestarted!\\n-------------------------------------------------------------------------------------\\n \\n \\n**");
        }

        private static void OnRoundEnded(RoundEndedEventArgs ev)
        {
            AddLog($"[{DateTime.Now}] \\n \\n \\n**-------------------------------------------------------------------------------------\\nDie Runde hat jetzt geendet!\\n-------------------------------------------------------------------------------------\\n \\n \\n**");
        }

        private static void OnJoined(PlayerJoinedEventArgs ev)
        {
            AddLog($"[{DateTime.Now}] `{ev.Player.Nickname}` `({ev.Player.UserId})` ist gejoint.");
        }

        private static void OnLeft(PlayerLeftEventArgs ev)
        {
            AddLog($"[{DateTime.Now}] `{ev.Player.Nickname}` `({ev.Player.UserId})` ist gegangen.");
        }

        // fucked sometimes... (NullReference exception)
        private static void OnDeath(PlayerDeathEventArgs ev)
        {
            try
            {
                if (ev.Attacker == null)
                    return;
                AddLog($"[{DateTime.Now}] `{ev.Attacker.Nickname}` `({ev.Attacker.UserId})` hat `{ev.Player.Nickname}` `({ev.Player.UserId})` umgebracht.");
            }
            catch (Exception ex)
            {
                Logger.Warn($"Another {ex.GetType()} in PRMainPlugin.Systems.Discord.Logs.OnDied");
            }
        }


        // fucked sometimes... (NullReference exception)
        private static void OnHurt(PlayerHurtingEventArgs ev)
        {
            try
            {
                if (ev == null || ev.Player == null || ev.Attacker == null)
                    return;

                if (Round.IsRoundEnded)
                    return;

                float damage = ev.DamageHandler is StandardDamageHandler std
                    ? std.Damage
                    : 0f;

                if (damage == 0)
                    return;

                if (ev.Player == ev.Attacker)
                {
                    AddLog($"[{DateTime.Now}] `{ev.Player.Nickname}` `({ev.Player.UserId})` hat sich selbst {damage} schaden zugefügt.");
                    return;
                }

                AddLog($"[{DateTime.Now}] `{ev.Attacker.Nickname}` `({ev.Attacker.UserId})` hat `{ev.Player.Nickname}` `({ev.Player.UserId})` {damage} schaden mit {ev.DamageHandler.GetType()} zugefügt.");
            }
            catch (Exception ex)
            {
                Logger.Warn($"Another {ex.GetType()} in PRMainPlugin.Systems.Discord.Logs.OnHurt");
            }
        }

        private static void OnHandcuffing(PlayerCuffedEventArgs ev)
        {
            AddLog($"[{DateTime.Now}] `{ev.Target.Nickname}` `({ev.Target.UserId})` wurde von `{ev.Player.Nickname}` `({ev.Player.UserId})` cuffed.");
        }

        private static void OnRemovedHandcuffes(PlayerUncuffedEventArgs ev)
        {
            AddLog($"[{DateTime.Now}] `{ev.Target.Nickname}` `({ev.Target.UserId})` wurde von `{ev.Player.Nickname}` `({ev.Player.UserId})` uncuffed.");
        }

        private static void OnSpawned(PlayerSpawnedEventArgs ev)
        {
            AddLog($"[{DateTime.Now}] `{ev.Player.Nickname}` `({ev.Player.UserId})` wurde als `{ev.Role.RoleName}`gespawned.");
        }
    }
}
