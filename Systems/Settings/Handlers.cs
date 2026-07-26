using System;
using System.Linq;
using LabApi.Features.Wrappers;
using LabApi.Features.Console;
using LabApi.Features.Extensions;
using NebMainPluginLabApi.API;
using PlayerRoles;
using UserSettings.ServerSpecific;

namespace NebMainPluginLabApi.Systems.CustomSettings
{
    internal static class Actions
    {
        // Previous test button
        //internal static void ButtonPress(Player p, SettingBase s)
        //{
        //    Log.Info($"{p.Nickname} clicked the button!");
        //}

        // Potential bug #1: dropdown.Base.SendValueUpdate() might have the wrong filter
        // Potential bug #2: might still simply not work lmfao
        internal static void ScpSwapMenu(ReferenceHub hub, ServerSpecificSettingBase setting)
        {
            try
            {
                if (setting is not SSDropdownSetting dropdown) return;

                Player p = Player.Get(hub);
                if (p == null) return;

                RoleTypeId chosenRole = Main.Instance.SwapableScps[dropdown.SyncSelectionIndexRaw];

                if (dropdown.SyncSelectionIndexRaw == 0)
                {
                    return;
                }

                Logger.Info($"SCP Swap Menu triggered: index:{dropdown.SyncSelectionIndexRaw}\nParsed to role:{chosenRole}\nPlayer:{p.Nickname}");

                if (!p.Role.IsScp())
                {
                    HintsAPI.AddHint(p, "Du kannst diesen Command nur als SCP verwenden!", 3);
                    return;
                }
                if (Round.Duration > Commands.SCPSwap.SCPSwap.NoSwapTime)
                {
                    HintsAPI.AddHint(p, "Du kannst nicht mehr wechseln, du bist leider zu spät.", 3);
                    return;
                }
                if (chosenRole == RoleTypeId.None)
                {
                    HintsAPI.AddHint(p, "Dieses SCP gibt es nicht!", 3);
                    return;
                }
                if (chosenRole == RoleTypeId.Scp3114)
                {
                    if (Player.List.Count() < Main.Instance.SkelliCount)
                    {
                        HintsAPI.AddHint(p, string.Format("Du kannsr nur zu SCP-3114 wechseln, wenn die Spielerzahl bei {0} oder größer ist.", Main.Instance.SkelliCount), 3);
                        return;
                    }
                }
                if (p.Role == chosenRole)
                {
                    HintsAPI.AddHint(p, "Du kannst nicht zum gleichen SCP, welches du bereits bist wechseln.", 3);
                    return;
                }

                int ScpPlayers = 0;

                foreach (Player ply in Player.List)
                {
                    if (ply.Role.IsScp())
                    {
                        ScpPlayers++;
                        if (ply.Role == chosenRole)
                        {
                            HintsAPI.AddHint(p, "Jemand ist bereits dieses SCP!", 3);
                            return;
                        }
                    }
                }
                if (chosenRole == RoleTypeId.Scp079 && ScpPlayers < 2)
                {
                    HintsAPI.AddHint(p, "Du kannst nur bei mehr als einem SCP zu 079 werden!", 3);
                    return;
                }
                if (Main.Instance.SingleSwap)
                {
                    if (Commands.SCPSwap.SCPSwap.swaped.Contains(p.UserId))
                    {
                        HintsAPI.AddHint(p, "Du hast diese Runde bereits gewechselt!", 3);
                        return;
                    }
                }

                if (!Commands.SCPSwap.SCPSwap.swaped.Contains(p.UserId))
                    Commands.SCPSwap.SCPSwap.swaped.Add(p.UserId);

                p.SetRole(chosenRole);
                HintsAPI.AddHint(p, $"Du bist jetzt SCP{chosenRole}!", 3);
            }
            catch (Exception e)
            {
                Logger.Error("Error in SCP Swap:\n" + e.Message);
            }
        }
    }
}