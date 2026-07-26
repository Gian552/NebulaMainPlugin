using PlayerRoles;
using System;
using System.Collections.Generic;
using System.Linq;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Arguments.ServerEvents;
using LabApi.Events.Handlers;
using NebMainPluginLabApi;
using LabApi.Features.Console;

namespace NebMainPluginLabApi
{
    public static class EventHandlers
    {
        private static System.Random random = new System.Random();
        internal static void Enable()
        {
            LabApi.Events.Handlers.PlayerEvents.InteractingScp330 += PickingUpCandy;
            ServerEvents.CommandExecuting += SendingValidCommand;
            LabApi.Events.Handlers.PlayerEvents.Banning += OnBanning;
        }

        internal static void Disable()
        {
            LabApi.Events.Handlers.PlayerEvents.InteractingScp330 -= PickingUpCandy;
            ServerEvents.CommandExecuting -= SendingValidCommand;
            LabApi.Events.Handlers.PlayerEvents.Banning -= OnBanning;
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
                try
                {
//                    Database.UpdatePlayerRank(ev.Sender);
                }
                catch (Exception ex)
                {
                    Logger.Error($"Error in Rank updates {ex}");
                }
            }
        }

        private static void OnBanning(PlayerBanningEventArgs ev)
        {
//            Database.AddBan(ev.Player, ev.Issuer, ev.Reason, ev.Duration);
        }
    }
}