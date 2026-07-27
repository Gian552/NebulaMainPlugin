using System;
using System.Collections.Generic;
using System.Linq;
using LabApi.Features.Console;
using LabApi.Features.Wrappers;
using NebMainPluginLabApi;
using NebMainPluginLabApi.API.Enums;
using UserSettings.ServerSpecific;

namespace NebMainPluginLabApi.Systems.Settings
{
    internal static class EventHandles
    {
        private static readonly SSGroupHeader Header = new SSGroupHeader(
            "[Nebula]",
            true,
            "Einstellungen für dein Nebula Erlebnis. Sollte es Probleme geben, meldet solche gerne bei unserem Team oder auf unserem Discord.");
        
        internal const int ScpSwapId = 22;

        private static readonly SSDropdownSetting ScpSwap = new SSDropdownSetting(
            ScpSwapId,
            "SCP Swap",
            Main.Instance.SwapableScps.Select(r => r.ToString()).ToArray(),
            0,
            SSDropdownSetting.DropdownEntryType.Regular,
            $"Wenn du SCP bist, kannst du hier in den ersten {Main.Instance.ScpSwapTimeout} Sekunden dein SCP tauschen, solange es noch niemand anderes hat.");

        internal static readonly SSTwoButtonsSetting MusicMute = new SSTwoButtonsSetting(
            23,
            "WarteMusik",
            "An",
            "Stumm",
            false,
            "Schaltet die Musik in der Warte-Lobby für dich stumm.");

        internal static readonly SSSliderSetting MusicVolume = new SSSliderSetting(
            24,
            "WarteMusik Lautstärke",
            0f,
            100f,
            100f,
            true,
            "0",
            "{0}%",
            "Wie laut die Musik in der Warte-Lobby für dich ist.");

        internal const int RoleSelectId = 25;

        private const string NoRoleOption = "Kein Rang";

        private static readonly SSDropdownSetting RoleSelect = BuildRoleDropdown(new[] { NoRoleOption }, 0);

        private static readonly Dictionary<string, List<Roles.DiscordRoles>> SentRoleOptions =
            new Dictionary<string, List<Roles.DiscordRoles>>();

        private static SSDropdownSetting BuildRoleDropdown(string[] options, int defaultIndex)
            => new SSDropdownSetting(
                RoleSelectId,
                "Angezeigter Rang",
                options,
                defaultIndex,
                SSDropdownSetting.DropdownEntryType.Regular,
                "Welcher deiner Ränge im Spiel als Badge angezeigt wird.");

        internal static void Enable()
        {
            ServerSpecificSettingsSync.DefinedSettings =
                (ServerSpecificSettingsSync.DefinedSettings ?? new ServerSpecificSettingBase[0])
                .Concat(new ServerSpecificSettingBase[] { Header, ScpSwap, RoleSelect, MusicMute, MusicVolume })
                .ToArray();

            ServerSpecificSettingsSync.SendToAll();
            ServerSpecificSettingsSync.ServerOnSettingValueReceived += Actions.ScpSwapMenu;
            ServerSpecificSettingsSync.ServerOnSettingValueReceived += Actions.RoleSelectMenu;
            ServerSpecificSettingsSync.ServerOnSettingValueReceived += Actions.WarteMusikSettings;
        }

        internal static void Disable()
        {
            ServerSpecificSettingsSync.ServerOnSettingValueReceived -= Actions.ScpSwapMenu;
            ServerSpecificSettingsSync.ServerOnSettingValueReceived -= Actions.RoleSelectMenu;
            ServerSpecificSettingsSync.ServerOnSettingValueReceived -= Actions.WarteMusikSettings;

            ServerSpecificSettingsSync.DefinedSettings = ServerSpecificSettingsSync.DefinedSettings?
                .Where(s => s != Header && s != ScpSwap && s != RoleSelect && s != MusicMute && s != MusicVolume)
                .ToArray();

            SentRoleOptions.Clear();
            ServerSpecificSettingsSync.SendToAll();
        }

        /// <summary>
        /// Sends the settings list to a single player with the rank dropdown filled with that player's own roles.
        /// </summary>
        internal static void SendRoleOptions(Player player)
        {
            if (player?.ReferenceHub == null || player.UserId == null)
                return;

            try
            {
                var roles = Systems.Database.Database.GetSelectableRoles(player);
                SentRoleOptions[player.UserId] = roles;

                var selected = Systems.Database.PlayerDataCache.Get(player.UserId)?.dcRole ?? Roles.DiscordRoles.None;
                int selectedIndex = roles.IndexOf(selected) + 1;

                var options = new List<string> { NoRoleOption };
                options.AddRange(roles.Select(r => r.ToRoleString()));

                var personal = BuildRoleDropdown(options.ToArray(), selectedIndex);

                var collection = (ServerSpecificSettingsSync.DefinedSettings ?? new ServerSpecificSettingBase[0])
                    .Select(s => s == RoleSelect ? personal : s)
                    .ToArray();

                ServerSpecificSettingsSync.SendToPlayer(player.ReferenceHub, collection);
            }
            catch (Exception ex)
            {
                Logger.Error($"Error while sending rank options to {player.Nickname}: {ex.Message}");
            }
        }

        /// <summary>
        /// Maps the index the client reported back to the role it was offered.
        /// </summary>
        internal static bool TryGetRoleForIndex(Player player, int index, out Roles.DiscordRoles role)
        {
            role = Roles.DiscordRoles.None;

            if (player?.UserId == null)
                return false;

            if (index == 0)
                return true;

            if (!SentRoleOptions.TryGetValue(player.UserId, out var roles))
                return false;

            if (index < 1 || index > roles.Count)
                return false;

            role = roles[index - 1];
            return true;
        }

        internal static void Forget(string userId)
        {
            if (userId != null)
                SentRoleOptions.Remove(userId);
        }
    }
}