using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using LabApi.Features.Wrappers;
using NebMainPluginLabApi;
using NebMainPluginLabApi.API;
using PlayerRoles;
using PlayerRoles.FirstPersonControl.NetworkMessages;
using PlayerRoles.PlayableScps.Scp079;
using PlayerRoles.Spectating;
using PlayerStatsSystem;
using RueI.API;
using RueI.API.Elements;
using RueI.API.Elements.Enums;
using Player = LabApi.Features.Wrappers.Player;
using Server = LabApi.Features.Wrappers.Server;
namespace NebMainPluginLabApi.Systems.CustomHints{

    public static class Hints
    {
        public static Dictionary<Player, List<HintData>> playerHints = new();
        private static readonly Tag ClockTag = new("hud_clock");
        private static readonly Tag TpsTag = new("hud_tps");
        private static readonly Tag RoundTimeTag = new("hud_roundtime");
        private static readonly Tag ServerName = new("hud_servername");
        
        private static readonly Tag Killcounter = new("hud_kills");
        private static readonly Tag PlayerRole = new("hud_playerrole"); 
        private static readonly Tag SpectatorsListTag = new("hud_spectatorslist");
        private static readonly Tag SCPHintsTag = new("hud_SCPHints");
        private static readonly Tag HintsStack = new("hud_HintsStack");


        public static void RefreshTps(Player player)
        {
            if (player == null) return; 
            RueDisplay display = RueDisplay.Get(player);
            var element = new DynamicElement(position: 980, contentGetter: rh =>
            {
                var p = Player.Get(rh);
                if (p == null)
                {
                    return string.Empty;
                }

                int TPS = (int)Server.Tps;
                int maxTPS = (int)Server.MaxTps;
                string color = API.HintsAPI.GetRoleColor(player);
                return $"<space=-60><size=30><color={color}>TPS:{TPS}/{maxTPS}</color></size>";
            })
            {
                ZIndex = 2,
                VerticalAlign = VerticalAlign.Down,
                UpdateInterval = TimeSpan.FromSeconds(1),
                ShowToSpectators = false
            };
            RueDisplay.Get(player).Show(TpsTag, element);

        }
        public static void RefreshTime(Player player)
        {
            if (player == null) return; 
            RueDisplay display = RueDisplay.Get(player);
            var element = new DynamicElement(position: 980, contentGetter: rh =>
            {
                var p = Player.Get(rh);
                if (p == null)
                {
                    return string.Empty;
                }
                
                TimeZoneInfo berlinZone;

                try
                {
                    // Windows
                    berlinZone = TimeZoneInfo.FindSystemTimeZoneById("W. Europe Standard Time");
                }
                catch
                {
                    // Linux / macOS
                    berlinZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Berlin");
                }
                String time = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, berlinZone).ToString("HH:mm");
                string color = API.HintsAPI.GetRoleColor(player);
                return $"<space=-480><size=30><color={color}>{time} Uhr</color></size>";
            })
            {
                ZIndex = 2,
                VerticalAlign = VerticalAlign.Down,
                UpdateInterval = TimeSpan.FromSeconds(1),
                ShowToSpectators = false
            };
            RueDisplay.Get(player).Show(ClockTag, element);

        }
        public static void RefreshRoundtime(Player player)
        {
            if (player == null) return; 
            RueDisplay display = RueDisplay.Get(player);
            var element = new DynamicElement(position: 980, contentGetter: rh =>
            {
                var p = Player.Get(rh);
                if (p == null)
                {
                    return string.Empty;
                }
                
                String time = Round.Duration.ToString(@"mm\:ss");
                string color = API.HintsAPI.GetRoleColor(player);
                return $"<space=420><size=30><color={color}>Rundenzeit {time}</color></size>";
            })
            {
                ZIndex = 2,
                VerticalAlign = VerticalAlign.Down,
                UpdateInterval = TimeSpan.FromSeconds(1),
                ShowToSpectators = false
            };
            RueDisplay.Get(player).Show(RoundTimeTag, element);

        }

        public static void RefreshKills(Player player)
        {
            if (player == null) return;
            RueDisplay display = RueDisplay.Get(player);
            EventHandlers.Kills.TryGetValue(player, out var kills);
            string color = HintsAPI.GetRoleColor(player);
            BasicElement element;
            if (player.Role == RoleTypeId.Scp079)
            {
                element = new BasicElement(position: 20,
                    content: $"<align=left><space=-350><size=30><color={color}>Kills:{kills.ToString()}</color></size></align>");
            }
            else
            { 
                Player? spectated = player.CurrentlySpectating;
                if (spectated != null && spectated.Role == RoleTypeId.Scp079)
                {
                    element = new BasicElement(position: 20,
                        content: $"<align=left><space=-350><size=30><color={color}>Kills:{kills.ToString()}</color></size></align>");
                }
                else
                { 
                    element = new BasicElement(position: 150, 
                        content: $"<align=left><space=-350><size=30><color={color}>Kills:{kills.ToString()}</color></size></align>");
                }
            }
            display.Show(Killcounter,element);
        }
        public static void RefreshSpectators(Player player)
        {
            if (player == null) return; 
            RueDisplay display = RueDisplay.Get(player);
            var element = new DynamicElement(position: 600, contentGetter: rh =>
            {
                var p = Player.Get(rh);
                if (p == null || !p.IsAlive)
                {
                    return string.Empty;
                }

                StringBuilder spectators = new StringBuilder();
                int totalSpec = 0;
                foreach (Player Player in p.CurrentSpectators)
                {
                    if (Player.Role != RoleTypeId.Spectator) continue;
                    spectators.AppendLine("<space=-350>"+HintsAPI.GetPlayerName(Player,15));
                    totalSpec++;
                }


                if (0 == spectators.Length) return string.Empty;
                string color = API.HintsAPI.GetRoleColor(player);
                return $"<align=left><size=20><color={color}><space=-350>Zuschauer({totalSpec.ToString()}):\n{spectators}</color></size></align>";
            })
            {
                ZIndex = 2,
                VerticalAlign = VerticalAlign.Down,
                UpdateInterval = TimeSpan.FromSeconds(1),
                ShowToSpectators = false
            };
            RueDisplay.Get(player).Show(SpectatorsListTag, element);
        }
        public static void RefreshRole(Player player)
        {
            if (player == null) return;
            RueDisplay display = RueDisplay.Get(player);
            string role = string.Empty;
            if (player.IsAlive)
            {
                string color = API.HintsAPI.GetRoleColor(player);
                if (player.Role == RoleTypeId.Scp079)
                {
                    role = $"<size=30><space=-175><color={color}><align=left>Rolle:\n<space=-175>{API.HintsAPI.GetRoleName(player)}</color></align></size>";
                }
                else
                {
                    role = $"<size=30><space=75><color={color}><align=left>Rolle:\n<space=75>{API.HintsAPI.GetRoleName(player)}</color></align></size>";
                }

            }
            else if (!player.IsAlive)
            {
                Player? spectated = player.CurrentlySpectating;
                if (spectated != null)
                {
                    string color = HintsAPI.GetRoleColor(spectated);
                    if (spectated.Role == RoleTypeId.Scp079)
                    {
                        role = $"<size=30><space=-175><align=left>Rolle:\n<space=-175>{API.HintsAPI.GetRoleName(spectated)}</align></size>";
                    }
                    else
                    {
                        role = $"<size=30><space=75><align=left>Rolle:\n<space=75>{API.HintsAPI.GetRoleName(spectated)}</align></size>";
                    }
                }
                else
                {
                    role = string.Empty;
                }
            }
            var element = new BasicElement(position: 40, content: role);
            RueDisplay.Get(player).Show(PlayerRole,element);
        }
        public static void RefreshServerName(Player player)
        {
            if (player == null) return;
            RueDisplay display = RueDisplay.Get(player);
            var element = new BasicElement(position: 10, content: $"<size=20>[{Main.Instance.serverName}]</size>");
            RueDisplay.Get(player).Show(ServerName,element);
        }

        public static void RefreshSCPHints(Player player)
        {
            if (player == null) return;
            RueDisplay display = RueDisplay.Get(player);
            var element = new DynamicElement(position: 400, contentGetter: rh =>
            {
                var p = Player.Get(rh);
                if(p == null || !p.IsSCP) return string.Empty;
                StringBuilder scps = new StringBuilder();
                foreach (Player scp in Player.List.Where(p => p.IsSCP).ToList())
                {
                    if (scp.Role == RoleTypeId.Scp079)
                    {
                        var scp079 = scp.RoleBase as Scp079Role;
                        if (scp079 != null)
                        {
                            float energy = 0f;
                            int level = 0;

                            if (scp079.SubroutineModule.TryGetSubroutine(out Scp079AuxManager auxManager))
                                energy = auxManager.CurrentAux;

                            if (scp079.SubroutineModule.TryGetSubroutine(out Scp079TierManager tierManager))
                                level = tierManager.AccessTierLevel;

                            scps.AppendLine($"{HintsAPI.GetPlayerName(scp,15)}|{HintsAPI.GetRoleName(scp)}|Energy:{(int)Math.Round(energy)}|Lvl:{level}");
                        }
                    }
                    else
                    { 
                        scps.AppendLine($"{HintsAPI.GetPlayerName(scp,15)} | {HintsAPI.GetRoleName(scp)} | HP: {(int)Math.Round(scp.Health)} | HS: {(int)Math.Round(scp.HumeShield)}");
                    }
                }
                string color = API.HintsAPI.GetRoleColor(player);
                return $"<size=20><align=right><color={color}>{scps}</color></align></size>";
            })
            {
                ZIndex = 2,
                VerticalAlign = VerticalAlign.Down,
                UpdateInterval = TimeSpan.FromSeconds(1),
                ShowToSpectators = false
            };
            RueDisplay.Get(player).Show(SCPHintsTag,element);
        }
        public static void RegisterHintDisplay(Player player)
        {
            var element = new DynamicElement(position: 750, contentGetter: rh =>
            {
                var p = Player.Get(rh);
                if (p == null) return string.Empty;
                string hilfe = "";
                var newList = new List<HintData>();
                playerHints.TryGetValue(player, out var hints);
                if (hints == null) return string.Empty;
                foreach (var hint in hints)
                {
                    if (hint.TimeLeft > 0)
                    {
                        newList.Add(new HintData(hint.Text, hint.TimeLeft-1));
                        hilfe = hilfe + hint.Text + "\n";
                    }
                }
                playerHints[player] = newList;
                return hilfe;
                
            })
            {
                ZIndex = 10,
                VerticalAlign = VerticalAlign.Down,
                UpdateInterval = TimeSpan.FromSeconds(1),
                ShowToSpectators = true
            };

            RueDisplay.Get(player).Show(HintsStack, element);
        }
        public class HintData
        {
            public string Text;
            public float TimeLeft;

            public HintData(string text, float duration)
            {
                Text = text;
                TimeLeft = duration;
            }
        }
    }
}