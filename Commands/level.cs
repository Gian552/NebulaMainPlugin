using LabApi.Features.Console;
using LabApi.Features.Wrappers;
using NebMainPluginLabApi.Systems.Database;

namespace NebMainPluginLabApi.Commands
{
    using CommandSystem;
    using NebMainPluginLabApi.Systems.Database;
    using System;

    [CommandHandler(typeof(RemoteAdminCommandHandler))]
    public class DataInfoServer : ICommand
    {
        public string Command { get; } = "playtimectl";

        public string[] Aliases { get; } = new string[] { "ptctl" };

        public string Description { get; } = $"See other players' playtime.\\n\" +\r\n            \"No arguments = playtime of every player on the server.\\n\" +\r\n            \"<int> Id of a player on the server = playtime of a certain player currently on the server.\\n\" +\r\n            \"<string> ID or SteamID of a player = playtime of a certain player.";

        public bool SanitizeResponse => true;

        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {

            if (arguments.Count == 0)
            {
                response = $"<color=green>Playtime of players on server: </color>\n";
                foreach (Player p in Player.List)
                {
                    PlayerData info = PlayerDataCache.Get(p.UserId);
                    if (info == null)
                        continue;

                    response += $"<color=yellow>{p.Nickname}</color> - {TimeSpan.FromSeconds(info.Playtime ?? 0):hh\\:mm\\:ss}\n";
                }

                return true;
            }
            else if (arguments.Count == 1)
            {
                try
                {
                    Player p = Player.Get(arguments.At(0));
                    PlayerData info;
                    if (p == null)
                    {
                        info = Database.GetPlayerInfoAsync(arguments.At(0) + "@steam").GetAwaiter().GetResult();

                        if (info == null)
                        {
                            response = $"<color=red>Wrong player ID/SteamID or Player's Data is not stored.</color>";
                            return false;
                        }

                        // $"" + String.Format gemischt: {0}/{1} wurden als Interpolation
                        // gefressen und die Ausgabe war literal "Playtime of 0: 1"
                        response = $"<color=green>Playtime of {arguments.At(0)}:</color> {TimeSpan.FromSeconds(info.Playtime ?? 0):hh\\:mm\\:ss}";
                        return true;

                    }

                    info = PlayerDataCache.Get(p.UserId);
                    if (info == null)
                    {
                        response = $"<color=red>Player's data is not saved due to them not agreeing to data collection.</color>";
                        return false;
                    }

                    response = $"<color=green>Playtime of {p.Nickname}:</color> {TimeSpan.FromSeconds(info.Playtime ?? 0):hh\\:mm\\:ss}";
                    return true;
                }
                catch (Exception e)
                {
                    response = "Error in DB querry! Contact Skorp";
                    Logger.Error($"{e.Message}");
                    return false;
                }
            }

            response = "";
            return false;
        }
    }
}