using System.Collections.Generic;
using System.Linq;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Handlers;
using LabApi.Features.Wrappers;
using MEC;
using PlayerRoles;
using PlayerStatsSystem;
using UnityEngine;

namespace NebMainPluginLabApi.Systems.CustomHints{

    public class EventHandlers
    {
        public static Dictionary<Player,int> Kills = new();
        public static void Enable()
        {
            PlayerEvents.Joined += OnJoined;
            PlayerEvents.ChangingRole += OnChangingRole;
            PlayerEvents.Death += OnKill;
            PlayerEvents.ChangedSpectator += OnChangingSpec;
            ServerEvents.RoundRestarted += OnRoundRestart;
        }

        public static void Disable()
        {
            PlayerEvents.Joined -= OnJoined;
            PlayerEvents.ChangingRole -= OnChangingRole;
            PlayerEvents.Death -= OnKill;
            PlayerEvents.ChangedSpectator -= OnChangingSpec;
            ServerEvents.RoundRestarted -= OnRoundRestart;

        }

        private static void OnJoined(PlayerJoinedEventArgs ev)
        {
            Hints.RefreshTps(ev.Player);
            Hints.RefreshTime(ev.Player);
            Hints.RefreshRoundtime(ev.Player);
            Hints.RefreshKills(ev.Player);
            Hints.RefreshSpectators(ev.Player);
            Hints.RefreshServerName(ev.Player);
            Hints.RefreshRole(ev.Player);
            Hints.RegisterHintDisplay(ev.Player);
        }

        private static void OnChangingRole(PlayerChangingRoleEventArgs ev)
        { 
            Hints.RefreshSpectators(ev.Player);
            
            //Delayed because the event is called before the role gets changed
            Timing.CallDelayed(0.01f,() =>
            {
                Hints.RefreshRole(ev.Player);
                Hints.RefreshSCPHints(ev.Player);
                Hints.RegisterHintDisplay(ev.Player);
                Hints.RefreshKills(ev.Player);
            });
            
        }

        private static void OnKill(PlayerDeathEventArgs ev)
        {
            if (ev.DamageHandler is UniversalDamageHandler universal 
                && universal.TranslationId == DeathTranslations.PocketDecay.Id)
            {
                foreach (var scp106 in Player.List.Where(pl => pl.Role == RoleTypeId.Scp106))
                {
                    AddKill(scp106);
                }
            }

            if (ev.Attacker != null && ev.Attacker != ev.Player)
            {
                AddKill(ev.Attacker);
            }
        }

        private static void AddKill(Player p)
        {
            if (Kills.TryGetValue(p, out var kills))
            {
                Kills[p] = kills + 1;
            }
            else
            {
                Kills.Add(p, 1);
            }
            Hints.RefreshKills(p);
        }

        private static void OnChangingSpec(PlayerChangedSpectatorEventArgs ev)
        {
            Timing.CallDelayed(0.01f,() =>
            {
                Hints.RefreshKills(ev.Player);
                Hints.RefreshRole(ev.Player);
            });
        }

        private static void OnRoundRestart()
        {
            Kills.Clear();
            Hints.playerHints.Clear();
        }
    }
}