using System;
using System.Linq;
using System.Numerics;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Handlers;
using LabApi.Features.Wrappers;
using MEC;
using PlayerRoles;
using Vector3 = UnityEngine.Vector3;

namespace NebMainPluginLabApi.Systems.AFKReplacer
{
    public static class EventHandlers
    {
        public static void Enable()
        {
            PlayerEvents.Kicking += OnKicking;
        }

        public static void Disable()
        {
            PlayerEvents.Kicking -= OnKicking;
        }

        private static void OnKicking(PlayerKickingEventArgs ev)
        {
            if (ev.Player.IsNpc) return;
            if (ev.Player.Role == RoleTypeId.Tutorial && ev.Reason == "AFK")
            {
                ev.IsAllowed = false;
                return;
            }

            if (ev.Reason == "AFK" && Server.PlayerCount < Server.MaxPlayers)
            {
                ev.IsAllowed = false;
                //select newPlayer
                var spectators = Player.List.Where(p => p.Role == RoleTypeId.Spectator).ToList();
                if (spectators.IsEmpty())
                {
                    ev.Player.Kill();
                    API.HintsAPI.AddHint(ev.Player, "Du wurdest ersetzt, da du AFK warst", 10f);
                    return;
                }

                Random rnd = new Random();
                int index = rnd.Next(spectators.Count);
                var newPlayer = spectators[index];
                //copy the information
                var itemTypes = ev.Player.Items.Select(i => i.Type).ToList();
                ev.Player.ClearInventory(); // safely drops/destroys the old player's items first
                RoleTypeId role = ev.Player.Role;
                float health = ev.Player.Health;
                Vector3 position = ev.Player.Position;

                newPlayer.Role = role;
                Timing.CallDelayed(0.15f, () =>
                {
                    newPlayer.Position = position;
                    newPlayer.Health = health;
                    foreach (var type in itemTypes)
                        newPlayer.AddItem(type);
                });
                API.HintsAPI.AddHint(newPlayer, $"Du ersetzt nun {ev.Player.Nickname}, da dieser AFK war", 10f);
                ev.Player.Role = RoleTypeId.Spectator;
                API.HintsAPI.AddHint(ev.Player, "Du wurdest ersetzt, da du AFK warst", 10f);
            }
            else if (ev.Player.IsAlive && Server.PlayerCount >= Server.MaxPlayers)
            {
                //select newPlayer
                var spectators = Player.List.Where(p => p.Role == RoleTypeId.Spectator).ToList();
                if (spectators.IsEmpty())
                {
                    return;
                }

                Random rnd = new Random();
                int index = rnd.Next(spectators.Count);
                var newPlayer = spectators[index];
                //copy the information

                var itemTypes = ev.Player.Items.Select(i => i.Type).ToList();
                ev.Player.ClearInventory(); // safely drops/destroys the old player's items first
                RoleTypeId role = ev.Player.Role;
                float health = ev.Player.Health;
                Vector3 position = ev.Player.Position;
                newPlayer.Role = role;
                Timing.CallDelayed(0.15f, () =>
                {
                    newPlayer.Position = position;
                    newPlayer.Health = health;
                    foreach (var type in itemTypes)
                        newPlayer.AddItem(type);
                });
                API.HintsAPI.AddHint(newPlayer, $"Du ersetzt nun {ev.Player.Nickname}, da dieser AFK war", 10f);

            }
        }


    }
}