using System;
using HarmonyLib;
using LabApi.Features.Console;

namespace NebMainPluginLabApi.Systems.Database
{
    [HarmonyPatch(typeof(GameConsoleTransmission), nameof(GameConsoleTransmission.SendToClient))]
    internal static class BadgeConsoleSilencer
    {
        private static bool _active;

        private static bool Prefix() => !_active;

        internal static void Silent(Action action)
        {
            _active = true;

            try
            {
                action();
            }
            catch (Exception ex)
            {
                Logger.Error($"Error during silent badge update: {ex}");
            }
            finally
            {
                _active = false;
            }
        }
    }
}
