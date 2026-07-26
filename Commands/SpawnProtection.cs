using CommandSystem;
using System;
using System.Linq;
using LabApi.Events.Handlers;
using LabApi.Features.Wrappers;
using ICommand = CommandSystem.ICommand;

namespace NebMainPluginLabApi.Commands
{
    [CommandHandler(typeof(RemoteAdminCommandHandler))]

    public class SpawnProtection : ParentCommand
    {
        public SpawnProtection()
        {
            LoadGeneratedCommands();
        }


        public override string Command { get; } = "spawnprotection";
        public override string[] Aliases { get; } = { "sp" };
        public override string Description { get; } = "Prefix for Spawnprotection commands. Type sp or spawnprotection to see every command";
        public string[] Usage { get; } = { };
        public override void LoadGeneratedCommands()
        {
            RegisterCommand(new GrantSpawnprotection());
            RegisterCommand(new ListSpawnprotectionPlayer());
            RegisterCommand(new RemoveSpawnProtection());
            RegisterCommand(new ToggleSpawnProtection());
        }


        protected override bool ExecuteParent(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            response = "List of all Commands:\n" +
                "spawnprotection toggle - toggles Spawnprotection (short:sp t)\n" +
                "spawnprotection grant <PlayerID> - grants a player spawnprotection (short:sp g)\n" +
                "spawnprotection remove <PlayerID> - removes spawnprotection for a player (short:sp r)\n" +
                "spawnprotection list - lists every player with spawnprotection (short:sp ls)";
            return true;
        }
    }
    public class GrantSpawnprotection : ICommand
    {
        public string Command { get; } = "grant";
        public string[] Aliases { get; } = { "g" };
        public string Description { get; } = "Grants spawnprotection for a specific player (onetime).";
        public string[] Usage { get; } = { "PlayerID" };

        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            
            var player = Player.Get(sender);
            
            if (!sender.CheckPermission(PlayerPermissions.RespawnEvents, out _))
            {
                response = "Du bist nun ein Femboy (Du hast keine Berechtigung diesen Befehl zu benutzen.)";
                return false;
            }

            if (arguments.Count == 0)
            {
                response = "Usage: g <playerId>";
                return false;
            }
            if (!int.TryParse(arguments.At(0), out int id))
            {
                response = "Ungültige Player-ID.";
                return false;
            }
            Player target = Player.Get(id);
            if (target == null)
            {
                response = "Dieser Spieler existiert nicht.";
                return false;
            }
            NebMainPluginLabApi.Systems.SpawnProtection.SpawnProtection.GiveProtection(target);

            response = $"Der Spieler {target.Nickname} hat jetzt Spawnprotection. :3";
            return true;
        }
    }
    public class ListSpawnprotectionPlayer : ICommand
    {

        public string Command => "list";
        public string[] Aliases { get; } = { "ls" };
        public string Description { get; } = "Lists every player with Spawnprotection.";
        public string[] Usage { get; } = { };


        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            string list = "Players with Spawnprotection:\n";
            var protectionlist = NebMainPluginLabApi.Systems.SpawnProtection.SpawnProtection.protections;
            foreach (var key in protectionlist.Keys)
            {
                var player = Player.List.FirstOrDefault(p => p.PlayerId == key);
                if (player == null) continue;
                list += $"- {player.Nickname} (ID: {player.PlayerId})\n";
            }

            response = list;
            return true;
        }
    }
    public class RemoveSpawnProtection : ICommand
    {
        public string Command { get; } = "remove";
        public string[] Aliases { get; } = { "r" };
        public string Description { get; } = "Removes spawnprotection for a specific player (onetime).";
        public string[] Usage { get; } = { "PlayerID" };

        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            var player = Player.Get(sender);
            if (!sender.CheckPermission(PlayerPermissions.RespawnEvents, out _))
            {
                response = "Du bist nun ein Femboy (Du hast keine Berechtigung diesen Befehl zu benutzen.)";
                return false;
            }

            if (arguments.Count == 0)
            {
                response = "Usage: r <playerId>";
                return false;
            }
            if (!int.TryParse(arguments.At(0), out int id))
            {
                response = "Ungültige Player-ID.";
                return false;
            }
            Player target = Player.Get(id);
            if (target == null)
            {
                response = "Dieser Spieler existiert nicht.";
                return false;
            }
            NebMainPluginLabApi.Systems.SpawnProtection.SpawnProtection.RemoveProtection(target);

            response = $"Spawnprotection für {target.Nickname} entfernt (Wenn er respawnt hat er wieder Spawnprotection) :3";
            return true;
        }
    }
    public class ToggleSpawnProtection : ICommand
    {

        public string Command { get; } = "toggle";
        public string[] Aliases { get; } = { "t" };
        public string Description { get; } = "Toggles spawn protection for everyone.";
        public bool SanitizeResponse { get; } = false;
        public string[] Usage { get; } = { };


        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            var player = Player.Get(sender);
            string steamID = player.UserId.Split('@')[0];
            const string specialSteamID = "76561199106592719";

            if (sender.CheckPermission(PlayerPermissions.RespawnEvents, out _))
            {
                if (steamID == specialSteamID)
                {
                    if (NebMainPluginLabApi.Systems.SpawnProtection.SpawnProtection.IsProtectionEnabled)
                    {
                        NebMainPluginLabApi.Systems.SpawnProtection.SpawnProtection.IsProtectionEnabled = false;
                        NebMainPluginLabApi.Systems.SpawnProtection.SpawnProtection.ClearProtection();
                    }
                    else NebMainPluginLabApi.Systems.SpawnProtection.SpawnProtection.IsProtectionEnabled = true;
                    response = $"Spawn protection ist für diese Runde (brr skibidi dop dop bip bup) {(NebMainPluginLabApi.Systems.SpawnProtection.SpawnProtection.IsProtectionEnabled ? "aktiviert" : "deaktiviert")}.";
                    return true;
                }
                else
                {
                    if (NebMainPluginLabApi.Systems.SpawnProtection.SpawnProtection.IsProtectionEnabled)
                    {
                        NebMainPluginLabApi.Systems.SpawnProtection.SpawnProtection.IsProtectionEnabled = false;
                        NebMainPluginLabApi.Systems.SpawnProtection.SpawnProtection.ClearProtection();
                    }
                    else NebMainPluginLabApi.Systems.SpawnProtection.SpawnProtection.IsProtectionEnabled = true;
                    response = $"Spawn protection ist für diese Runde {(NebMainPluginLabApi.Systems.SpawnProtection.SpawnProtection.IsProtectionEnabled ? "aktiviert" : "deaktiviert")}.";
                    return true;
                }
            }
            else
            {
                response = "Du bist nun ein Femboy (Du hast keine Berechtigung diesen Befehl zu benutzen.)";
                return false;
            }

        }
    }

}