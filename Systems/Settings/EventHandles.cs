using System.Linq;
using LabApi.Features.Console;
using LabApi.Features.Wrappers;
using NebMainPluginLabApi;
using UserSettings.ServerSpecific;

namespace NebMainPluginLabApi.Systems.CustomSettings
{
    /// <summary>
    /// Handles registration and dynamic updating of settings.
    /// </summary>
    internal static class EventHandles
    {
        private static readonly SSGroupHeader Header = new SSGroupHeader(
            "[Nebula]",
            true,
            "Einstellungen für dein Nebula Erlebniss. Sollte es Probleme geben, meldet solche gerne bei unserem Team oder auf unserem Discord.");

        /// <summary>
        /// Only supposed to be visible as SCP, handles swapping between SCPs.
        /// </summary>
        private static readonly SSDropdownSetting ScpSwap = new SSDropdownSetting(
            22,
            "SCP Swap",
            Main.Instance.SwapableScps.Select(r => r.ToString()).ToArray(),
            0,
            SSDropdownSetting.DropdownEntryType.Regular,
            $"Wenn du SCP bist, kannst du hier in den ersten {Main.Instance.ScpSwapTimeout} Sekunden dein SCP tauschen, solange es noch niemand anderes hat.");

        /// <summary>
        /// Mute toggle for the lobby music.
        /// </summary>
        internal static readonly SSTwoButtonsSetting MusicMute = new SSTwoButtonsSetting(
            23,
            "WarteMusik",
            "An",
            "Stumm",
            false,
            "Schaltet die Musik in der Warte-Lobby für dich stumm.");

        /// <summary>
        /// Volume slider for the lobby music.
        /// </summary>
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

        internal static void Enable()
        {
            ServerSpecificSettingsSync.DefinedSettings =
                (ServerSpecificSettingsSync.DefinedSettings ?? new ServerSpecificSettingBase[0])
                .Concat(new ServerSpecificSettingBase[] { Header, ScpSwap, MusicMute, MusicVolume })
                .ToArray();

            ServerSpecificSettingsSync.SendToAll();
            ServerSpecificSettingsSync.ServerOnSettingValueReceived += Actions.ScpSwapMenu;
            ServerSpecificSettingsSync.ServerOnSettingValueReceived += Actions.WarteMusikSettings;
        }

        internal static void Disable()
        {
            ServerSpecificSettingsSync.ServerOnSettingValueReceived -= Actions.ScpSwapMenu;
            ServerSpecificSettingsSync.ServerOnSettingValueReceived -= Actions.WarteMusikSettings;

            ServerSpecificSettingsSync.DefinedSettings = ServerSpecificSettingsSync.DefinedSettings?
                .Where(s => s != Header && s != ScpSwap && s != MusicMute && s != MusicVolume)
                .ToArray();

            ServerSpecificSettingsSync.SendToAll();
        }
    }
}