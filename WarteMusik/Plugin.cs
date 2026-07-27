using System;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Loader.Features.Plugins;
using WarteMusik.Features;
using PlayerEvents = LabApi.Events.Handlers.PlayerEvents;
using ServerEvents = LabApi.Events.Handlers.ServerEvents;
using Round = LabApi.Features.Wrappers.Round;
using Logger = LabApi.Features.Console.Logger;

namespace WarteMusik
{
    public class Plugin : Plugin<Config>
    {
        public static Plugin Instance { get; private set; }

        public MusicLibrary Library { get; private set; }

        public MusicPreferences Preferences { get; private set; }

        public LobbyMusicPlayer Music { get; private set; }

        public override string Name => "WarteMusik";

        public override string Description => "Plays MP3 waiting music in the lobby.";

        public override string Author => "Gian";

        public override Version Version { get; } = new Version(1, 0, 0);

        public override Version RequiredApiVersion { get; } = new Version(1, 1, 0);

        public override void Enable()
        {
            Instance = this;

            if (!Config.IsEnabled)
            {
                Logger.Info($"{Name} is switched off via config (is_enabled: false).");
                return;
            }

            if (!HasDecoder())
                return;

            Library = new MusicLibrary(this);
            Preferences = new MusicPreferences(this);
            Music = new LobbyMusicPlayer(this);

            Preferences.Register();

            ServerEvents.WaitingForPlayers += OnWaitingForPlayers;
            ServerEvents.RoundStarted += OnRoundStarted;
            ServerEvents.RoundRestarted += OnRoundRestarted;
            PlayerEvents.Joined += OnJoined;
            PlayerEvents.Left += OnLeft;

            if (GameCore.RoundStart.singleton != null && !Round.IsRoundStarted)
                Music.Start();

            Logger.Info($"{Name} v{Version} has been enabled. Music folder: {Library.Directory}");
        }

        public override void Disable()
        {
            if (Music == null)
            {
                Instance = null;
                return;
            }

            ServerEvents.WaitingForPlayers -= OnWaitingForPlayers;
            ServerEvents.RoundStarted -= OnRoundStarted;
            ServerEvents.RoundRestarted -= OnRoundRestarted;
            PlayerEvents.Joined -= OnJoined;
            PlayerEvents.Left -= OnLeft;

            Music.Stop();
            Preferences.Unregister();

            Music = null;
            Preferences = null;
            Library = null;
            Instance = null;

            Logger.Info($"{Name} has been disabled.");
        }

        public void LogDebug(string message)
        {
            if (Config.Debug)
                Logger.Debug($"[WarteMusik] {message}");
        }

        private void OnWaitingForPlayers() => Music.Start();

        private void OnRoundStarted() => Music.Stop();

        private void OnRoundRestarted() => Music.Stop();

        private void OnJoined(PlayerJoinedEventArgs args) => Preferences.OnJoined(args.Player?.ReferenceHub);

        private void OnLeft(PlayerLeftEventArgs args) => Preferences.OnLeft(args.Player?.ReferenceHub);

        private bool HasDecoder()
        {
            try
            {
                Mp3Decoder.GetDecoderName();
                return true;
            }
            catch (Exception exception)
            {
                Logger.Error($"{Name} could not load the MP3 decoder ({exception.Message}). " +
                             "Copy NLayer.dll into the LabAPI 'dependencies' folder - see README.");
                return false;
            }
        }
    }
}
