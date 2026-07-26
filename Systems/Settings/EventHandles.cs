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

        internal static void Enable()
        {
            ServerSpecificSettingsSync.DefinedSettings =
                (ServerSpecificSettingsSync.DefinedSettings ?? new ServerSpecificSettingBase[0])
                .Concat(new ServerSpecificSettingBase[] { Header, ScpSwap })
                .ToArray();

            ServerSpecificSettingsSync.SendToAll();
            ServerSpecificSettingsSync.ServerOnSettingValueReceived += Actions.ScpSwapMenu;
        }

        internal static void Disable()
        {
            ServerSpecificSettingsSync.ServerOnSettingValueReceived -= Actions.ScpSwapMenu;

            ServerSpecificSettingsSync.DefinedSettings = ServerSpecificSettingsSync.DefinedSettings?
                .Where(s => s != Header && s != ScpSwap)
                .ToArray();

            ServerSpecificSettingsSync.SendToAll();
        }
    }
}