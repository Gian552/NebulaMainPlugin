using LabApi.Features.Wrappers;
using NebMainPluginLabApi;

namespace NebMainPlugin.Commands
{
    using CommandSystem;
    using NebMainPlugin.Systems.Database;
    using System;

    [CommandHandler(typeof(ClientCommandHandler))]
    public class Level : ICommand
    {
        public string Command { get; } = "level";

        public string[] Aliases { get; } = new string[] { "lvl" };

        public string Description { get; } = "Zeigt alle Level Statistiken an.";

        public bool SanitizeResponse => true;

        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            Player player = Player.Get(sender);
            var data = PlayerDataCache.Get(player.UserId);

            if (data != null)
            {
                var ts = TimeSpan.FromSeconds((double)data.Playtime);

                string pt = String.Format("Spielzeit: {0} Stunden, {1} Minuten, {2} Sekunden", (int)ts.TotalHours, ts.Minutes, ts.Seconds);
                response = $"\n<color=white>[</color>{Main.Instance.serverName}<color=white>]</color>\n<color=white>Deine Stats: </color>\n<color=white>Level: {data.Level}</color>\n<color=white>Xp: {data.XP} / {data.RequiredXP}</color>\n<color=white>{pt}</color>\n<color=white>Rang: </color><color={player.GroupColor}>{data.slRole}</color>";
                return true;
            }

            response = "<color=red>Es gab einen Fehler, bitte reporte das in unserem Discord!.</color>";
            return false;
        }
    }
}