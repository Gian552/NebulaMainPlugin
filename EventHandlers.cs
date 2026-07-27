using PlayerRoles;
using System;
using System.Collections.Generic;
using System.Linq;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Arguments.ServerEvents;
using LabApi.Events.Handlers;
using NebMainPluginLabApi;
using LabApi.Features.Console;
using LabApi.Features.Wrappers;
using NebMainPluginLabApi.Systems.Database;

namespace NebMainPluginLabApi
{
    public static class EventHandlers
    {
        private static System.Random random = new System.Random();
        internal static void Enable()
        {
            LabApi.Events.Handlers.PlayerEvents.InteractingScp330 += PickingUpCandy;
            ServerEvents.CommandExecuting += SendingValidCommand;
            LabApi.Events.Handlers.PlayerEvents.Banned += OnBanned;
        }

        internal static void Disable()
        {
            LabApi.Events.Handlers.PlayerEvents.InteractingScp330 -= PickingUpCandy;
            ServerEvents.CommandExecuting -= SendingValidCommand;
            LabApi.Events.Handlers.PlayerEvents.Banned -= OnBanned;
        }

        private static void PickingUpCandy(PlayerInteractingScp330EventArgs ev)
        {
            if (random.NextDouble() < Main.Instance.PinkCandyChance / 100.0)
                ev.CandyType = InventorySystem.Items.Usables.Scp330.CandyKindID.Pink;
        }

        private static void SendingValidCommand(CommandExecutingEventArgs ev)
        {
            var cmd = ev.Command;
            
            if (cmd == null)
                return;

            if (string.Equals(cmd.Command, "st", StringComparison.OrdinalIgnoreCase) || cmd.Aliases?.Any(a => a.Equals("st", StringComparison.OrdinalIgnoreCase)) == true)
            {
                // Sender ist ein CommandSender - bei Server-Konsole/RA ohne Spieler gibt es keinen Player.
                if (!Player.TryGet(ev.Sender, out var player))
                    return;

                try
                {
                    Database.UpdatePlayerRank(player);
                }
                catch (Exception ex)
                {
                    Logger.Error($"Error in Rank updates {ex}");
                }
            }
        }

        private static void OnBanned(PlayerBannedEventArgs ev)
        {
            // Offline-Bans laufen nur über die UserId, dort gibt es keinen Player.
            if (ev.Player == null)
                return;

            try
            {
                Database.AddBan(ev.Player, ev.Issuer, ev.Reason, ev.Duration);
            }
            catch (Exception ex)
            {
                Logger.Error($"Error while saving ban for {ev.PlayerId}: {ex}");
            }
        }
    }
}