using System;
using System.Collections.Generic;
using UserSettings.ServerSpecific;
using Logger = LabApi.Features.Console.Logger;

namespace WarteMusik.Features
{
    /// <summary>
    /// Adds a "Lobby music" toggle to the server-specific settings menu
    /// (ESC -> Settings -> Server-specific) and remembers who switched it off.
    /// The entries are put in front of everything else so they show up at the
    /// very top of the list.
    /// </summary>
    public sealed class MusicPreferences
    {
        private readonly Plugin _plugin;
        private readonly HashSet<ReferenceHub> _mutedPlayers = new HashSet<ReferenceHub>();

        private SSGroupHeader _header;
        private SSTwoButtonsSetting _toggle;
        private bool _registered;

        public MusicPreferences(Plugin plugin)
        {
            _plugin = plugin;
        }

        private int HeaderId => _plugin.Config.ServerSpecificSettingId;

        private int ToggleId => _plugin.Config.ServerSpecificSettingId + 1;

        /// <summary>Whether the waiting music should currently be audible for this player.</summary>
        public bool WantsMusic(ReferenceHub hub)
        {
            return hub != null && !_mutedPlayers.Contains(hub);
        }

        /// <summary>Puts the toggle at the top of the server-specific settings.</summary>
        public void Register()
        {
            if (_registered || !_plugin.Config.ServerSpecificToggle)
                return;

            _toggle = new SSTwoButtonsSetting(
                ToggleId,
                _plugin.Config.SettingsLabel,
                _plugin.Config.SettingsOptionOn,
                _plugin.Config.SettingsOptionOff,
                _plugin.Config.DisabledByDefault,
                _plugin.Config.SettingsHint);

            List<ServerSpecificSettingBase> settings = new List<ServerSpecificSettingBase>();

            if (!string.IsNullOrEmpty(_plugin.Config.SettingsHeader))
            {
                _header = new SSGroupHeader(HeaderId, _plugin.Config.SettingsHeader);
                settings.Add(_header);
            }

            settings.Add(_toggle);

            // Ours first, then whatever other plugins already defined.
            foreach (ServerSpecificSettingBase setting in Existing())
            {
                if (!IsOurs(setting))
                    settings.Add(setting);
            }

            ServerSpecificSettingsSync.DefinedSettings = settings.ToArray();
            ServerSpecificSettingsSync.SendToAll();
            ServerSpecificSettingsSync.ServerOnSettingValueReceived += OnSettingValueReceived;

            _registered = true;
            _plugin.LogDebug($"Server-specific toggle registered (header {HeaderId}, toggle {ToggleId}).");
        }

        /// <summary>Removes our entries again, leaving other plugins' settings alone.</summary>
        public void Unregister()
        {
            if (!_registered)
                return;

            ServerSpecificSettingsSync.ServerOnSettingValueReceived -= OnSettingValueReceived;

            List<ServerSpecificSettingBase> remaining = new List<ServerSpecificSettingBase>();

            foreach (ServerSpecificSettingBase setting in Existing())
            {
                if (!IsOurs(setting))
                    remaining.Add(setting);
            }

            ServerSpecificSettingsSync.DefinedSettings = remaining.ToArray();
            ServerSpecificSettingsSync.SendToAll();

            _mutedPlayers.Clear();
            _header = null;
            _toggle = null;
            _registered = false;
        }

        /// <summary>
        /// A joining player has not answered yet, so seed them with the configured
        /// default. Their stored choice arrives right after and overrides this.
        /// </summary>
        public void OnJoined(ReferenceHub hub)
        {
            if (hub == null)
                return;

            if (_plugin.Config.DisabledByDefault)
                _mutedPlayers.Add(hub);
            else
                _mutedPlayers.Remove(hub);
        }

        public void OnLeft(ReferenceHub hub)
        {
            if (hub != null)
                _mutedPlayers.Remove(hub);
        }

        private void OnSettingValueReceived(ReferenceHub hub, ServerSpecificSettingBase setting)
        {
            if (hub == null || setting == null || setting.SettingId != ToggleId)
                return;

            SSTwoButtonsSetting twoButtons = setting as SSTwoButtonsSetting;
            if (twoButtons == null)
                return;

            // Option A = music on, option B = music off.
            if (twoButtons.SyncIsB)
                _mutedPlayers.Add(hub);
            else
                _mutedPlayers.Remove(hub);

            _plugin.LogDebug($"{hub.LoggedNameFromRefHub()} set the lobby music to " +
                             (twoButtons.SyncIsB ? "off" : "on") + ".");
        }

        private bool IsOurs(ServerSpecificSettingBase setting)
        {
            return setting != null && (setting.SettingId == HeaderId || setting.SettingId == ToggleId);
        }

        private static IEnumerable<ServerSpecificSettingBase> Existing()
        {
            ServerSpecificSettingBase[] defined = ServerSpecificSettingsSync.DefinedSettings;
            return defined ?? Array.Empty<ServerSpecificSettingBase>();
        }
    }
}
