using System.ComponentModel;

namespace WarteMusik
{
    public class Config
    {
        [Description("Whether the plugin is active. If 'false', no lobby music is played at all.")]
        public bool IsEnabled { get; set; } = true;

        [Description("Adds a 'Lobby music' toggle to the server-specific settings menu (ESC -> Settings -> " +
                     "Server-specific). Every player can then turn the waiting music off for themselves. " +
                     "The toggle is inserted at the TOP of the server-specific settings list.")]
        public bool ServerSpecificToggle { get; set; } = true;

        [Description("If 'true', the waiting music is off for everyone until a player turns it on in the " +
                     "server-specific settings. If 'false' (default), it is on unless a player turns it off.")]
        public bool DisabledByDefault { get; set; } = false;

        [Description("Write debug messages to the server console.")]
        public bool Debug { get; set; } = false;

        [Description("Name of the folder the .mp3 files are read from. It lives next to this config file " +
                     "(.../LabAPI/configs/<port>/WarteMusik/) and is created automatically on startup.")]
        public string MusicFolder { get; set; } = "music";

        [Description("Play the tracks in random order. If 'false' they are played in alphabetical order.")]
        public bool Shuffle { get; set; } = true;

        [Description("Start over from the beginning once every track has been played. " +
                     "If 'false' the lobby stays silent after the last track.")]
        public bool LoopPlaylist { get; set; } = true;

        [Description("Volume of the speaker. 1.0 = unchanged, 0.5 = half as loud. " +
                     "Values above 1 distort the audio.")]
        public float Volume { get; set; } = 0.6f;

        [Description("Radius in metres inside which the music is audible. The client silently drops audio " +
                     "from speakers further away than this - and it does so even for non-spatial speakers, " +
                     "so for lobby music this has to stay far larger than the map. Only lower it if you " +
                     "deliberately want the music to be local.")]
        public float HearingRange { get; set; } = 10000f;

        [Description("Tracks longer than this are cut off after this many seconds. Decoded audio is kept in " +
                     "RAM (roughly 11 MB per minute), so this is a memory safety net. 0 = no limit.")]
        public int MaxTrackSeconds { get; set; } = 420;

        [Description("Seconds to wait after the lobby starts before the first track is played.")]
        public float StartDelaySeconds { get; set; } = 2f;

        [Description("Audio channel of the speaker (0-255). Only change this if another plugin uses the same " +
                     "id and the audio streams interfere with each other.")]
        public byte SpeakerControllerId { get; set; } = 231;

        [Description("Id of the group header in the server-specific settings. The toggle itself uses the next " +
                     "id (i.e. this + 1). Change both away from other plugins' ids if they collide.")]
        public int ServerSpecificSettingId { get; set; } = 7411;

        [Description("Heading above the toggle in the server-specific settings. Leave empty for no heading.")]
        public string SettingsHeader { get; set; } = "Lobby";

        [Description("Label of the toggle in the server-specific settings.")]
        public string SettingsLabel { get; set; } = "Lobby music";

        [Description("Label of the 'music on' button.")]
        public string SettingsOptionOn { get; set; } = "On";

        [Description("Label of the 'music off' button.")]
        public string SettingsOptionOff { get; set; } = "Off";

        [Description("Tooltip of the toggle.")]
        public string SettingsHint { get; set; } = "Turns the waiting music in the lobby on or off for you.";
    }
}
