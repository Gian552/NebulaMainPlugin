using CustomPlayerEffects;
using System.Linq;
using Interactables.Interobjects.DoorUtils;
using LabApi.Features.Wrappers;

namespace NebMainPlugin.Systems.RemoteKeycard
{
    public static class Extensions
    {
        /// <summary>
        ///     Checks whether the player has a keycard of a specific permission.
        /// </summary>
        /// <param name="player"><see cref="Player" /> trying to interact.</param>
        /// <param name="permissions">The permission that's gonna be searched for.</param>
        /// <param name="requiresAllPermissions">Whether all permissions are required.</param>
        /// <returns>Whether the player has the required keycard.</returns>
        public static bool HasKeycardPermission(this Player player, DoorPermissionFlags permissions, bool requiresAllPermissions = false)
        {
            if (RemoteKeycards._config.AmnesiaMatters && player.ActiveEffects.Any(e => e is AmnesiaVision))
                return false;

            return requiresAllPermissions
                ? player.Items.Any(item => item is KeycardItem keycard && keycard.Permissions.HasFlag(permissions))
                : player.Items.Any(item => item is KeycardItem keycard && (keycard.Permissions & permissions) != 0);
        }
    }
}