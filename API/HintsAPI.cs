using System.Collections.Generic;
using LabApi.Features.Extensions;
using NebMainPluginLabApi.API.Enums;
using NebMainPluginLabApi.Systems.CustomHints;
using PlayerRoles;
using Respawning.Objectives;
using UnityEngine;
using Player = LabApi.Features.Wrappers.Player;

namespace NebMainPluginLabApi.API{

    public static class HintsAPI
    {
        public static string GetRoleColor(Player player)
        {
            return "#"+ ColorUtility.ToHtmlStringRGB(player.Role.GetRoleColor());
        }

        public static string GetRoleName(Player player)
        {
            //this is fucking weird fix this
            switch (player.Role)
            {
                case RoleTypeId.AlphaFlamingo:
                    return "SCP-1507 Alpha";
                case RoleTypeId.Flamingo:
                    return "SCP-1507";
                case RoleTypeId.ChaosFlamingo:
                    return "SCP-1507";
                case RoleTypeId.NtfFlamingo:
                    return "SCP-1507";
                case RoleTypeId.ZombieFlamingo:
                    return "SCP-1507";
                case RoleTypeId.CustomRole:
                    return player.Role.GetFullName();
                case RoleTypeId.ChaosConscript:
                    return "Chaos Rekrut";
                case RoleTypeId.ChaosMarauder:
                    return "Chaos Marodeur";
                case RoleTypeId.ChaosRepressor:
                    return "Chaos Unterdrücker";
                case RoleTypeId.ChaosRifleman:
                    return "Chaos Gewehrschütze";
                case RoleTypeId.ClassD:
                    return "Klasse-D";
                case RoleTypeId.Destroyed:
                    return "Destroyed";
                case RoleTypeId.FacilityGuard:
                    return "Sicherheitspersonal";
                case RoleTypeId.NtfCaptain:
                    return "NTF Hauptmann";
                case RoleTypeId.NtfPrivate:
                    return "NTF Gefreiter";
                case RoleTypeId.NtfSergeant:
                    return "NTF Unteroffizier";
                case RoleTypeId.NtfSpecialist:
                    return "NTF Spezialist";
                case RoleTypeId.Scientist:
                    return "Wissenschaftler";
                case RoleTypeId.Tutorial:
                    return "Tutorial";
                case RoleTypeId.Overwatch:
                    return "Overwatch";
                case RoleTypeId.Filmmaker:
                    return "Filmmacher";
                case RoleTypeId.Spectator:
                    return "Zuschauer";
                case RoleTypeId.None:
                    return "None";
                case RoleTypeId.Scp049:
                    return "SCP-049";
                case RoleTypeId.Scp0492:
                    return "SCP-049-2";
                case RoleTypeId.Scp079:
                    return "SCP-079";
                case RoleTypeId.Scp096:
                    return "SCP-096";
                case RoleTypeId.Scp106:
                    return "SCP-106";
                case RoleTypeId.Scp173:
                    return "SCP-173";
                case RoleTypeId.Scp939:
                    return "SCP-939";
                case RoleTypeId.Scp3114:
                    return "SCP-3114";
                default:
                    return player.Role.GetFullName();
            }
        }
        public static void AddHint(Player player, string text, float duration)
        {
            var hint = new Systems.CustomHints.Hints.HintData(text, duration);
            if (!Systems.CustomHints.Hints.playerHints.ContainsKey(player))
            {
                Systems.CustomHints.Hints.playerHints.Add(player,new  List<Systems.CustomHints.Hints.HintData>());
            } 
            Systems.CustomHints.Hints.playerHints[player].Add(hint);
        }

        public static string GetPlayerName(Player player,int? maxChars = null)
        {
            string name = player.Nickname;
            if (maxChars == null)
            {
                return name;
            }

            if (name.Length > maxChars)
            {
                name = name.Remove(maxChars.Value,name.Length-maxChars.Value)+ "...";
            }
            return name;
        }
    }
}