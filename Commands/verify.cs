using LabApi.Features.Wrappers;
using NebMainPluginLabApi.Systems.Database;

namespace NebMainPluginLabApi.Commands
{
    using CommandSystem;
    using System;
    using NebMainPluginLabApi.Systems.Database;

    [CommandHandler(typeof(ClientCommandHandler))]
    public class verify : ICommand
    {
        public string Command => "verify";

        public string[] Aliases => new string[] { };

        public string Description => "Verbinde deinen Discord account mit Server.";

        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            Player player = Player.Get(sender);

            if (player == null)
            {
                response = "Player Null! Bitte Melde das und erkläre genau was passiert ist!";
                return false;
            }
            if (string.IsNullOrEmpty(arguments.Array[1]))
            {
                response = "Bitte gib nach dem Command deinen Verifizierungs Token ein. {.verify <Token>}";
                return false;
            }

            var token = arguments.Array[1];
            var data = PlayerDataCache.Get(player.UserId);

            if (!string.IsNullOrEmpty(data.VerificationToken) && !string.IsNullOrEmpty(data.DiscordId) && !data.Verified)
            {
                data.Verified = true;
                PlayerDataCache.Set(player.UserId, data);
                response = "Du wurdest erfolgreich verifiziert!";
                return true;
            }
            if (string.IsNullOrEmpty(data.VerificationToken) || string.IsNullOrEmpty(data.DiscordId))
            {
                response = "Du hast noch nicht /verify auf unserem Discord ausgeführt, bitte tue das zu erst!";
                return false;
            }
            if (data.Verified)
            {
                response = "Du bist bereits verifiziert.";
                return false;
            }

            response = "Solltest du noch nicht verifiziert sein, bitte rejoin einmal.\n Sollte es immer noch nicht gehen, wende dich bitte an Skorp!";
            return false;
        }
    }
}