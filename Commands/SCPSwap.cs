using CommandSystem;
using System;
using LabApi.Features;
using PlayerRoles;
using System.Collections.Generic;
using LabApi.Features.Extensions;
using LabApi.Features.Wrappers;
using NebMainPluginLabApi;

namespace NebMainPluginLabApi.Commands.SCPSwap
{
    [CommandHandler(typeof(ClientCommandHandler))]
    public class SCPSwap : ParentCommand
    {
        public static List<string> swaped = new List<string>();
        public static TimeSpan NoSwapTime => TimeSpan.FromSeconds(Main.Instance.ScpSwapTimeout);

        public SCPSwap()
        {
            LoadGeneratedCommands();
        }

        public override string Command { get; } = "scpswap";

        public override string[] Aliases { get; } = new string[] { "scps" };

        public override string Description { get; } = "Change your scp!";

        public override void LoadGeneratedCommands() { }

        protected override bool ExecuteParent(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            Player player = Player.Get(sender);

            if (arguments.Count < 1)
            {
                response = "Usage: .scpswap {scpNumber}";
                return false;
            }
            if (player == null)
            {
                response = "player ist null, bitte an admin/developer reporten!";
                return false;
            }
            if (!player.Role.IsScp())
            {
                response = "Du kannst diesen Command nur als SCP verwenden!";
                return false;
            }
            if (Round.Duration.TotalSeconds > NoSwapTime.TotalSeconds)
            {
                response = "Du kannst nicht mehr wechseln, du bist leider zu spät.";
                return false;
            }
            if (arguments.Array[1] == "0492" || arguments.Array[1] == "049-2")
            {
                response = "Du kannst nicht zu Zombies wechseln!";
                return false;
            }
            RoleTypeId GetSwap(string scpNum)
            {
                switch (scpNum)
                {
                    case "049":
                        return RoleTypeId.Scp049;
                    case "079":
                        return RoleTypeId.Scp079;
                    case "096":
                        return RoleTypeId.Scp096;
                    case "106":
                        return RoleTypeId.Scp106;
                    case "939":
                        return RoleTypeId.Scp939;
                    case "173":
                        return RoleTypeId.Scp173;
                    case "3114":
                        return RoleTypeId.Scp3114;
                    default:
                        return RoleTypeId.None;
                }
            }
            if (GetSwap(arguments.Array[1]) == RoleTypeId.None)
            {
                response = "Dieses SCP gibt es nicht!\"";
                return false;
            }
            if (GetSwap(arguments.Array[1]) == RoleTypeId.Scp3114)
            {
                if (Server.PlayerCount < Main.Instance.SkelliCount)
                {
                    response = string.Format("Du kannsr nur zu SCP-3114 wechseln, wenn die Spielerzahl bei {0} oder größer ist!", Main .Instance.SkelliCount);
                    return false;
                }
            }
            if (!Main.Instance.SwapableScps.Contains(GetSwap(arguments.Array[1])))
            {
                response = "You can not swap to this SCP!";
                return false;
            }
            if (player.Role == GetSwap(arguments.Array[1]))
            {
                response = "Du kannst nicht zum gleichen SCP, welches du bereits bist wechseln.";
                return false;
            }

            int ScpPlayers = 0;
            
            foreach (Player ply in Player.List)
            {
                if (ply.Role.IsScp())
                {
                    ScpPlayers++;
                    if (ply.Role == GetSwap(arguments.Array[1]))
                    {
                        response = "Jemand ist bereits dieses SCP!";
                        return false;
                    }
                }
            }
            if (GetSwap(arguments.Array[1]) == RoleTypeId.Scp079 && ScpPlayers < 2)
            {
                response = "Du kannst nur bei mehr als einem SCP zu 079 werden!";
                return false;
            }
            if (Main.Instance.SingleSwap)
            {
                if (swaped.Contains(player.UserId))
                {
                    response = "Du hast diese Runde bereits gewechselt!";
                    return false;
                }
            }

            if (!swaped.Contains(player.UserId))
                swaped.Add(player.UserId);

            player.SetRole(GetSwap(arguments.Array[1]));
            response = $"Du bist jetzt SCP{arguments.Array[1]}!";
            return true;
        }
    }
}