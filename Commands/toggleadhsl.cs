using CommandSystem;
using System;
using ICommand = CommandSystem.ICommand;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Features.Wrappers;
using NebMainPluginLabApi.Systems.Events.ADHSL;

namespace NebMainPluginLabApi.Commands
{
    [CommandHandler(typeof(RemoteAdminCommandHandler))]

    public class ToggleADHSL : ICommand, IUsageProvider
    {

        public string Command => "toggleadhsl";
        public string[] Aliases => new string[] { "tadhsl" };
        public string Description => "Toggles the ADHSL event wich will grant every Player movementboost 255.";
        public bool SanitizeResponse { get; } = false;
        public string[] Usage { get; } = { };
        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            var player = Player.Get(sender);
            if (!sender.CheckPermission(PlayerPermissions.Effects))
            {
                response = "Du hast keine Berechtigung diesen Command aus zu führen";
                return true;
            }
            if (adhsl.ADHSLEnabled)
            {
                adhsl.ADHSLEnabled = false;
                response = "ADHSL wurde deaktiviert und jedem wurden die Effecte weg genommen.";
                adhsl.RemoveMovementboost();
                return true;
            }
            adhsl.ADHSLEnabled = true;
            response = "ADHSL wird aktiviert.";
            adhsl.GrantMovementboost();
            return true;


        }

    }
}