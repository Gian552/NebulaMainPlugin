using System;
using Interactables.Interobjects.DoorUtils;
using LabApi.Events.Arguments.PlayerEvents;
using Players = LabApi.Events.Handlers.PlayerEvents;

using LabApi.Features.Console;
using LabApi.Features.Wrappers;
using NebMainPluginLabApi;

namespace NebMainPluginLabApi.Systems.RemoteKeycard
{
    public class RemoteKeycards
    {
        internal static Config _config = Main.Instance;

        /// <summary>
        ///     Registers all events used.
        /// </summary>
        public static void Enable()
        {
            Logger.Debug("Registering Events");
            Players.InteractingDoor += OnDoorInteract;
            Players.UnlockingGenerator += OnGeneratorUnlock;
            Players.InteractingLocker += OnLockerInteract;
            Players.UnlockingWarheadButton += OnWarheadUnlock;
        }

        /// <summary>
        ///     Unregisters all events used.
        /// </summary>
        public static void Disable()
        {
            Players.InteractingDoor -= OnDoorInteract;
            Players.UnlockingGenerator -= OnGeneratorUnlock;
            Players.InteractingLocker -= OnLockerInteract;
            Players.UnlockingWarheadButton -= OnWarheadUnlock;
        }

        private static void OnDoorInteract(PlayerInteractingDoorEventArgs ev)
        {
            Logger.Debug("Door Interact Event");
            try
            {
                if (!_config.AffectDoors)
                    return;

                Logger.Debug(
                    $"Allowed: {ev.IsAllowed}, CanOpen: {ev.CanOpen}, Permission?: {ev.Player.HasKeycardPermission(ev.Door.Permissions)}, Current Item: ${ev.Player.CurrentItem}");

                if (ev.IsAllowed && !ev.CanOpen && ev.Player.HasKeycardPermission(ev.Door.Permissions) &&
                    !ev.Door.IsLocked)
                    ev.CanOpen = true;
            }
            catch (Exception e)
            {
                if (_config.ShowExceptions)
                    Logger.Warn($"{nameof(OnDoorInteract)}: {e.Message}\n{e.StackTrace}");
            }
        }

        private static void OnWarheadUnlock(PlayerUnlockingWarheadButtonEventArgs ev)
        {
            Logger.Debug("Warhead Unlock Event");
            try
            {
                if (!_config.AffectWarheadPanel)
                    return;

                Logger.Debug(
                    $"Allowed: {ev.IsAllowed}, Permission?: {ev.Player.HasKeycardPermission(DoorPermissionFlags.AlphaWarhead)}");

                if (!ev.IsAllowed && ev.Player.HasKeycardPermission(DoorPermissionFlags.AlphaWarhead))
                    ev.IsAllowed = true;
            }
            catch (Exception e)
            {
                if (_config.ShowExceptions)
                    Logger.Warn($"{nameof(OnWarheadUnlock)}: {e.Message}\n{e.StackTrace}");
            }
        }

        private static void OnGeneratorUnlock(PlayerUnlockingGeneratorEventArgs ev)
        {
            Logger.Debug("Generator Unlock Event");
            try
            {
                if (!_config.AffectGenerators)
                    return;

                Logger.Debug(
                    $"Allowed: {ev.IsAllowed}, CanOpen: {ev.CanOpen}, Permission?: {ev.Player.HasKeycardPermission(ev.Generator.RequiredPermissions)}");

                if (ev.IsAllowed && !ev.CanOpen && ev.Player.HasKeycardPermission(ev.Generator.RequiredPermissions))
                    ev.CanOpen = true;
            }
            catch (Exception e)
            {
                if (_config.ShowExceptions)
                    Logger.Warn($"{nameof(OnGeneratorUnlock)}: {e.Message}\n{e.StackTrace}");
            }
        }

        private static void OnLockerInteract(PlayerInteractingLockerEventArgs ev)
        {
            Logger.Debug("Locker Interact Event");
            try
            {
                if (!_config.AffectScpLockers)
                    return;

                if (ev.Chamber == null)
                    return;

                Logger.Debug(
                    $"Allowed: {ev.IsAllowed}, CanOpen: {ev.CanOpen}, Permission?: {ev.Player.HasKeycardPermission(ev.Chamber.RequiredPermissions, true)}");

                if (ev.IsAllowed && !ev.CanOpen &&
                    ev.Player.HasKeycardPermission(ev.Chamber.RequiredPermissions, true))
                    ev.CanOpen = true;
            }
            catch (Exception e)
            {
                if (_config.ShowExceptions)
                    Logger.Warn($"{nameof(OnLockerInteract)}: {e.Message}\n{e.StackTrace}");
            }
        }
    }
}