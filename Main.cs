using System;
using System.Collections.Generic;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Arguments.ServerEvents;
using LabApi.Events.Handlers;
using LabApi.Features.Wrappers;
using LabApi.Loader.Features.Plugins;
using LabApi.Features.Console;
using NebMainPluginLabApi;
using NebMainPluginLabApi.Systems.AutoFF;
using NebMainPluginLabApi.Systems.Database;
using NebMainPluginLabApi.Systems.Discord;
using NebMainPluginLabApi.Systems.Events.ADHSL;
using NebMainPluginLabApi.Systems.RemoteKeycard;
using NebMainPluginLabApi.Systems.Settings;
using NebMainPluginLabApi.Systems.SpawnProtection;
using NebMainPluginLabApi.Systems.WarteMusik;

namespace NebMainPluginLabApi
{
  public class Main : Plugin<Config>
{
    public override string Author => "Skorp 1.0 & MisterT13 & Gian";
        public override string Name => "Nebula Main Plugin";

        public override string Description => "Das offizielle Plugin vom Nebula Development Team";

        public override Version Version => new Version(1, 0, 1, 1);
        public override Version RequiredApiVersion => new Version(1, 0, 0);

        internal static Config Instance;

        private const string HarmonyId = "nebula.mainplugin";

        private HarmonyLib.Harmony _harmony;

        public override void Enable()
        {
            if (DateTime.Now.Month == 12)
            {
                //SKORP IF YOU DELET MY CHRISTMAS ASCII THINGI IM GONNA JUMP OUT OF A WINDOW THIS TOOK AGES
                Logger.Info("\n       __           ███▄▄▄▄      ▄████████ ▀█████████▄  ███    █▄   ▄█          ▄████████ \r\n    .-'  |          ███▀▀▀██▄   ███    ███   ███    ███ ███    ███ ███         ███    ███ \r\n   /   <\\|          ███   ███   ███    █▀    ███    ███ ███    ███ ███         ███    ███ \r\n  /     \\'          ███   ███  ▄███▄▄▄      ▄███▄▄▄██▀  ███    ███ ███         ███    ███ \r\n  |_.- o-o          ███   ███ ▀▀███▀▀▀     ▀▀███▀▀▀██▄  ███    ███ ███       ▀███████████ \r\n  / C  -._)\\        ███   ███   ███    █▄    ███    ██▄ ███    ███ ███         ███    ███ \r\n /',        |       ███   ███   ███    ███   ███    ███ ███    ███ ███▌    ▄   ███    ███ \r\n|   `-,_,__,'        ▀█   █▀    ██████████ ▄█████████▀  ████████▀  █████▄▄██   ███    █▀  \r\n(,,)====[_]=|                                                     ▀                      \r\n  '.   ____/        \r\n   | -|-|_          \r\n   |____)_)         ");
            }
            else
            {
                Logger.Info("\n███▄▄▄▄      ▄████████ ▀█████████▄  ███    █▄   ▄█          ▄████████ \r\n███▀▀▀██▄   ███    ███   ███    ███ ███    ███ ███         ███    ███ \r\n███   ███   ███    █▀    ███    ███ ███    ███ ███         ███    ███ \r\n███   ███  ▄███▄▄▄      ▄███▄▄▄██▀  ███    ███ ███         ███    ███ \r\n███   ███ ▀▀███▀▀▀     ▀▀███▀▀▀██▄  ███    ███ ███       ▀███████████ \r\n███   ███   ███    █▄    ███    ██▄ ███    ███ ███         ███    ███ \r\n███   ███   ███    ███   ███    ███ ███    ███ ███▌    ▄   ███    ███ \r\n ▀█   █▀    ██████████ ▄█████████▀  ████████▀  █████▄▄██   ███    █▀  \r\n                                               ▀                      ");
            }

            Logger.Info("[Setting first Instances and Patches...]");

            Instance = this.Config;

            _harmony = new HarmonyLib.Harmony(HarmonyId);
            _harmony.PatchAll();

            Logger.Info("[Enabling Systems]");

            Logger.Info("Enabling Base EventHandlers...");
            EventHandlers.Enable();

            Logger.Info("Starting Database...");
            Database.InitDB();

            Logger.Info("Enabling Discord Loggers...");
            Loggs.Enable();

            Logger.Info("Enabling RemoteKeycards...");
            RemoteKeycards.Enable();

            Logger.Info("Enabling Spawnprotection...");
            SpawnProtection.Enable();

            Logger.Info("Enabling ADHSL...");
            adhsl.Enable();

            Logger.Info("Enabling TeamTimereport...");
            WeeklyPlaytime.Enable();

            Logger.Info("Enabling User Settings...");
            EventHandles.Enable();
            
            Logger.Info("Enabling Custom Hud");
            Systems.CustomHints.EventHandlers.Enable();

            Logger.Info("Enabling WarteMusik...");
            WarteMusik.Enable();

            if (Config.IsAutoFFEnabled == true)
            {
                Logger.Info("Enabling AutoFF...");
                AutoFF.Enable();
            }
            else
            {
                Logger.Warn("AutoFF is Disabled.");
            }
            
            Logger.Info("Enabling AFKReplacer");
            Systems.AFKReplacer.EventHandlers.Enable();
        }

        public override void Disable()
        {
            Logger.Info("[Unsetting Instances and Unpatching other things...]");

            Instance = null;

            Logger.Info("[Disabling Systems]");

            Logger.Info("Disabling User Settings...");
            EventHandles.Disable();

            Logger.Info("Disabling RemoteKeycards...");
            RemoteKeycards.Disable();

            Logger.Info("Disabling Spawnprotection...");
            SpawnProtection.Disable();

            Logger.Info("Disabling Discord Loggers...");
            Loggs.Disable();

            Logger.Info("Closing Database...");
            Database.CloseDB();

            Logger.Info("Disabling Base EventHandlers...");
            EventHandlers.Disable();

            Logger.Info("Disabling ADHSL...");
            adhsl.Disable();

            Logger.Info("Disabling TeamTimereport...");
            WeeklyPlaytime.Disable();
            
            Logger.Info("Disabling Custom Hud");
            Systems.CustomHints.EventHandlers.Disable();

            Logger.Info("Disabling WarteMusik...");
            WarteMusik.Disable();
            
            Logger.Info("Disabling AFKReplacer");
            Systems.AFKReplacer.EventHandlers.Enable();

            _harmony?.UnpatchAll(HarmonyId);
            _harmony = null;
        }
    }
}