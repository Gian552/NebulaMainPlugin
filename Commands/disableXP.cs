using LabApi.Features.Wrappers;
using NebMainPluginLabApi.Systems.Database;

namespace NebMainPluginLabApi.Commands
{
    using CommandSystem;
    using System;
    using NebMainPluginLabApi.Systems.Database;

    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    public class DisableXP : ICommand
    {
        public string Command => "togglexp";

        public string[] Aliases => new string[] { "txp" };

        public string Description => "Deaktiviert/Aktiviert das XP-System bis zum nächsten Rundenneustart";

        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            Player player = Player.Get(sender);

            if (player.HasPermission(PlayerPermissions.RoundEvents))
            {
                XP.XpSystemEnabled = !XP.XpSystemEnabled;

                response = "XP-System ist jetzt " + (XP.XpSystemEnabled ? "Aktiv." : "Inaktiv.");
                return true;
            }

            response = "Du hast keine Berechtigung, diesen Befehl zu verwenden.";
            return false;
        }
    }
}