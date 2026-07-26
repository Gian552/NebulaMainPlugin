using CommandSystem;
using System;
using PlayerRoles;
using System.Collections.Generic;
using NebMainPluginLabApi.Systems.Discord;


namespace NebMainPluginLabApi.Commands
{
    [CommandHandler(typeof(GameConsoleCommandHandler))]
    public class TPTpost : ParentCommand
    {
        public override string Command { get; } = "tptpost";

        public override string[] Aliases { get; } = new string[] { "tptp" };

        public override string Description { get; } = "Postet die Wochenstunden aller Teamler in den vorgesehenen Channel, ohne die Zeiten zurück zu setzen.";

        public override void LoadGeneratedCommands() { }

        protected override bool ExecuteParent(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            _ = WeeklyPlaytime.SendWeeklyTeamlerReport(true);

            response = "Report gestartet, keine Garantie :3";
            return true;
        }
    }
}