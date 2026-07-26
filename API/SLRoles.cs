using System;
using System.Collections.Generic;
using System.Linq;
using LabApi.Features.Console;
using static NebMainPluginLabApi.API.Enums.Roles;

namespace NebMainPluginLabApi.API.Enums
{
    /// <summary>
    /// Strongly-typed role metadata and permission registry.
    /// Follows rule: internal identifiers = DiscordRoles enum names,
    /// displayed badge = <see cref="Roles.DiscordRoles.ToRoleString()"/>.
    /// Roles not present in DiscordRoles are skipped.
    /// </summary>
    public static class PermissionRegistry
    {
        /// <summary>Role metadata container.</summary>
        public class RoleInfo
        {
            public Roles.DiscordRoles RoleEnum { get; init; }
            public string InternalName => RoleEnum.ToString();    // e.g. "jr_supporter"
            public string DisplayName => DiscordRoleExtensions.FormatTeamRole(RoleEnum.ToString().ToLowerInvariant()); // uses ToRoleString()
            public ulong DiscordId => (ulong)RoleEnum;           // the numeric ID from enum
            public string Badge => DisplayName;                  // alias for clarity
            public string Color { get; init; }                   // e.g. "red", "magenta"
            public bool Cover { get; init; }
            public bool Hidden { get; init; }
            public byte KickPower { get; init; }
            public byte RequiredKickPower { get; init; }
            public ulong Permissions => RoleEnum.GetPermissionsForRole();
        }

        /// <summary>All validated roles.</summary>
        public static readonly IReadOnlyDictionary<Roles.DiscordRoles, RoleInfo> Roles;

        /// <summary>Permission → allowed roles mapping.</summary>
        public static readonly IReadOnlyDictionary<PlayerPermissions, Roles.DiscordRoles[]> PermissionRoles;

        static PermissionRegistry()
        {
            TryAddRole(Enums.Roles.DiscordRoles.admin, "red", true, false, 254, 255);
            TryAddRole(Enums.Roles.DiscordRoles.ek, "magenta", true, false, 254, 255);
            TryAddRole(Enums.Roles.DiscordRoles.jr_ek, "magenta", true, false, 253, 254);
            TryAddRole(Enums.Roles.DiscordRoles.jr_admin, "red", true, false, 252, 253);

            TryAddRole(Enums.Roles.DiscordRoles.jr_devleitung, "mint", true, false, 6, 7);   // jr_dev_leitung -> jr_devleitung
            TryAddRole(Enums.Roles.DiscordRoles.devleitung, "mint", true, false, 7, 8);
            TryAddRole(Enums.Roles.DiscordRoles.teamleitung, "cyan", true, false, 6, 7);
            TryAddRole(Enums.Roles.DiscordRoles.jr_teamleitung, "cyan", true, false, 6, 7);

            TryAddRole(Enums.Roles.DiscordRoles.moderator, "aqua", true, false, 5, 6);
            TryAddRole(Enums.Roles.DiscordRoles.jr_mod, "aqua", true, false, 5, 6);   // jr_moderator -> jr_mod

            TryAddRole(Enums.Roles.DiscordRoles.supporter, "blue_green", true, false, 2, 3);
            TryAddRole(Enums.Roles.DiscordRoles.jr_supporter, "blue_green", true, false, 0, 0);

            // Playtime / Ranks
            TryAddRole(Enums.Roles.DiscordRoles.keter, "tomato", true, false, 0, 0);
            TryAddRole(Enums.Roles.DiscordRoles.euclid, "yellow", true, false, 0, 0);
            TryAddRole(Enums.Roles.DiscordRoles.safe, "light_green", true, false, 0, 0);
            TryAddRole(Enums.Roles.DiscordRoles.pending, "mint", true, false, 0, 0);

            // Cosmetic / rewards 
            TryAddRole(Enums.Roles.DiscordRoles.femboy, "magenta", true, false, 0, 0);
            TryAddRole(Enums.Roles.DiscordRoles.furry, "cyan", true, false, 0, 0);
            TryAddRole(Enums.Roles.DiscordRoles.lgbtq, "pink", true, false, 0, 0); // lgbtqia+ -> lgbtq
            TryAddRole(Enums.Roles.DiscordRoles.femboy_furry,"magenta",true,false,0,0);

            // MTF / rewards 
            TryAddRole(Enums.Roles.DiscordRoles.xi_8, "magenta", true, false, 0, 0);
            TryAddRole(Enums.Roles.DiscordRoles.omega_1, "magenta", true, false, 0, 0);
            TryAddRole(Enums.Roles.DiscordRoles.alpha_1, "red", true, false, 0, 0);
            TryAddRole(Enums.Roles.DiscordRoles.nu_7, "deep_pink", true, false, 0, 0);
            TryAddRole(Enums.Roles.DiscordRoles.mu_4, "blue_green", true, false, 0, 0);
            TryAddRole(Enums.Roles.DiscordRoles.tau_5, "pink", true, false, 0, 0);
            TryAddRole(Enums.Roles.DiscordRoles.epsilon_11, "cyan", true, false, 0, 0);
            TryAddRole(Enums.Roles.DiscordRoles.beta_1, "aqua", true, false, 0, 0);
            TryAddRole(Enums.Roles.DiscordRoles.beta_7, "green", true, false, 0, 0);
            TryAddRole(Enums.Roles.DiscordRoles.zeta_9, "green", true, false, 0, 0);
            TryAddRole(Enums.Roles.DiscordRoles.eta_10, "pink", true, false, 0, 0);
            TryAddRole(Enums.Roles.DiscordRoles.resh_1,"carmine",true,false,0,0);

            // Additional rewards / ranks
            TryAddRole(Enums.Roles.DiscordRoles.mu_3, "magenta", true, false, 0, 0);
            TryAddRole(Enums.Roles.DiscordRoles.epsilon_6, "cyan", true, false, 0, 0);
            TryAddRole(Enums.Roles.DiscordRoles.psi_7, "pumpkin", true, false, 0, 0);
            TryAddRole(Enums.Roles.DiscordRoles.tau_5, "pink", true, false, 0, 0);
            TryAddRole(Enums.Roles.DiscordRoles.iota_10, "cyan", true, false, 0, 0);

            // Finalize readonly dictionary
            Roles = new Dictionary<Roles.DiscordRoles, RoleInfo>(_mutableRoles);

            // --- Build permission mapping ---
            var perms = new Dictionary<PlayerPermissions, Roles.DiscordRoles[]>
            {
                [PlayerPermissions.Vanish] = MapNames("admin", "admin_cc", "EK", "jr_admin", "jr_ek", "devleitung", "teamleitung","supporter", "jr_mod", "moderator"),
                [PlayerPermissions.ExecuteAs] = MapNames("admin_acc", "admin", "EK", "devleitung", "teamleitung", "jr_admin", "jr_ek"),
                [PlayerPermissions.KickingAndShortTermBanning] = MapNames("admin_acc", "supporter", "moderator", "teamleitung", "EK", "jr_admin", "jr_ek", "jr_mod", "jr_dev_leitung", "jr_teamleitung", "admin", "EK", "devleitung"),
                [PlayerPermissions.BanningUpToDay] = MapNames("admin_acc", "admin", "supporter", "moderator", "teamleitung", "EK", "jr_admin", "jr_ek", "jr_mod", "jr_dev_leitung", "jr_teamleitung", "devleitung"),
                [PlayerPermissions.LongTermBanning] = MapNames("admin_acc", "admin", "moderator", "teamleitung", "EK", "jr_admin", "jr_ek", "supporter", "jr_mod", "jr_dev_leitung", "jr_teamleitung", "devleitung"),
                [PlayerPermissions.ForceclassSelf] = MapNames("admin_acc", "admin", "jr_supporter", "supporter", "moderator", "teamleitung", "event_leitung", "EK", "jr_admin", "jr_ek", "jr_mod", "jr_dev_leitung", "jr_teamleitung", "devleitung"),
                [PlayerPermissions.ForceclassToSpectator] = MapNames("admin_acc", "admin", "jr_supporter", "supporter", "moderator", "teamleitung", "event_leitung", "EK", "jr_admin", "jr_ek", "jr_mod", "jr_dev_leitung", "jr_teamleitung", "devleitung"),
                [PlayerPermissions.ForceclassWithoutRestrictions] = MapNames("admin_acc", "admin", "jr_supporter", "supporter", "moderator", "teamleitung", "event_leitung", "EK", "jr_admin", "jr_ek", "jr_mod", "jr_dev_leitung", "jr_teamleitung", "devleitung"),
                [PlayerPermissions.GivingItems] = MapNames("admin_acc", "admin", "moderator", "teamleitung", "event_leitung", "EK", "jr_admin", "jr_ek", "jr_mod", "jr_dev_leitung", "jr_teamleitung", "devleitung"),
                [PlayerPermissions.WarheadEvents] = MapNames("admin_acc", "admin", "moderator", "teamleitung", "event_leitung", "EK", "jr_admin", "jr_ek", "jr_mod", "jr_dev_leitung", "jr_teamleitung", "devleitung"),
                [PlayerPermissions.RespawnEvents] = MapNames("admin_acc", "admin", "teamleitung", "event_leitung", "EK", "jr_admin", "jr_ek", "jr_dev_leitung", "jr_teamleitung", "devleitung", "moderator"),
                [PlayerPermissions.RoundEvents] = MapNames("admin_acc", "admin", "teamleitung", "event_leitung", "EK", "jr_admin", "jr_ek", "moderator", "jr_mod", "jr_dev_leitung", "supporter", "jr_teamleitung", "devleitung"),
                [PlayerPermissions.SetGroup] = MapNames("admin_acc", "admin", "EK", "jr_admin", "jr_ek","teamleitung","jr_teamleitung"),
                [PlayerPermissions.GameplayData] = MapNames("admin_acc", "admin", "jr_supporter", "supporter", "moderator", "teamleitung", "event_leitung", "EK", "jr_admin", "jr_ek", "jr_mod", "jr_dev_leitung", "jr_teamleitung", "devleitung"),
                [PlayerPermissions.Overwatch] = MapNames("admin_acc", "admin", "jr_supporter", "supporter", "moderator", "teamleitung", "event_leitung", "EK", "jr_admin", "jr_ek", "jr_mod", "jr_dev_leitung", "jr_teamleitung", "devleitung"),
                [PlayerPermissions.FacilityManagement] = MapNames("admin_acc", "admin", "supporter", "moderator", "teamleitung", "event_leitung", "EK", "jr_admin", "jr_ek", "jr_mod", "jr_dev_leitung", "jr_teamleitung", "devleitung"),
                [PlayerPermissions.PlayersManagement] = MapNames("admin_acc", "admin", "supporter", "moderator", "teamleitung", "event_leitung", "EK", "jr_admin", "jr_supporter", "jr_ek", "jr_mod", "jr_dev_leitung", "jr_teamleitung", "devleitung"),
                [PlayerPermissions.PermissionsManagement] = MapNames("admin_acc", "admin", "EK", "jr_admin", "jr_ek", "teamleitung"),
                [PlayerPermissions.ServerConsoleCommands] = MapNames("admin_acc", "admin", "EK", "jr_admin", "jr_ek", "devleitung", "teamleitung"),
                [PlayerPermissions.ViewHiddenBadges] = MapNames("admin_acc", "admin", "jr_supporter", "supporter", "moderator", "teamleitung", "event_leitung", "EK", "jr_admin", "jr_ek", "jr_mod", "jr_dev_leitung", "jr_teamleitung", "devleitung"),
                [PlayerPermissions.ServerConfigs] = MapNames("admin_acc", "admin", "teamleitung", "event_leitung", "EK", "jr_admin", "jr_ek", "jr_dev_leitung", "jr_teamleitung", "devleitung"),
                [PlayerPermissions.Broadcasting] = MapNames("admin_acc", "admin", "jr_supporter", "supporter", "moderator", "teamleitung", "event_leitung", "EK", "jr_admin", "jr_ek", "jr_mod", "jr_dev_leitung", "jr_teamleitung", "devleitung"),
                [PlayerPermissions.PlayerSensitiveDataAccess] = MapNames("admin_acc", "admin", "teamleitung", "EK", "jr_admin", "jr_ek", "jr_teamleitung", "devleitung"),
                [PlayerPermissions.Noclip] = MapNames("admin_acc", "admin", "supporter", "moderator", "teamleitung", "event_leitung", "EK", "jr_admin", "jr_ek", "jr_mod", "jr_dev_leitung", "jr_teamleitung", "devleitung"),
                [PlayerPermissions.AFKImmunity] = MapNames("admin_acc", "admin", "jr_supporter", "supporter", "moderator", "teamleitung", "event_leitung", "EK", "jr_admin", "jr_ek", "jr_mod", "jr_dev_leitung", "jr_teamleitung", "devleitung"),
                [PlayerPermissions.AdminChat] = MapNames("admin_acc", "admin", "jr_supporter", "supporter", "moderator", "teamleitung", "event_leitung", "EK", "jr_admin", "jr_ek", "jr_mod", "jr_dev_leitung", "jr_teamleitung", "discord_teamleitung", "jr_discord_teamleitung", "discord_teamler", "jr_discord_teamler", "dev_teamleitung", "developer", "jr_developer", "social_content_teamleitung", "jr_social_content_teamleitung", "social_content_teamler", "jr_social_content_teamler", "devleitung"),
                [PlayerPermissions.ViewHiddenGlobalBadges] = MapNames("admin_acc", "admin", "jr_supporter", "supporter", "moderator", "teamleitung", "event_leitung", "EK", "jr_admin", "jr_ek", "jr_mod", "jr_dev_leitung", "jr_teamleitung", "devleitung"),
                [PlayerPermissions.Announcer] = MapNames("admin_acc", "admin", "moderator", "teamleitung", "event_leitung", "EK", "supporter", "jr_admin", "jr_ek", "jr_mod", "jr_dev_leitung", "jr_teamleitung", "devleitung"),
                [PlayerPermissions.Effects] = MapNames("admin_acc", "admin", "moderator", "teamleitung", "event_leitung", "EK", "jr_admin", "jr_ek", "jr_mod", "jr_dev_leitung", "jr_teamleitung", "devleitung"),
                [PlayerPermissions.FriendlyFireDetectorImmunity] = MapNames("admin_acc", "admin", "moderator", "teamleitung", "event_leitung", "EK", "jr_admin", "jr_ek", "jr_mod", "jr_dev_leitung", "jr_teamleitung", "devleitung"),
                [PlayerPermissions.FriendlyFireDetectorTempDisable] = MapNames("admin_acc", "admin", "EK", "jr_admin", "jr_ek", "teamleitung"),
                [PlayerPermissions.ServerLogLiveFeed] = MapNames("admin_acc", "admin", "EK", "jr_admin", "jr_ek", "devleitung", "teamleitung"),
            };

            PermissionRoles = perms;
        }

        #region Helpers & Normalization

        /// <summary>
        /// Helper that adds a RoleInfo to the roles dictionary only if the DiscordRoles value exists.
        /// (We assume the caller passes a DiscordRoles member that actually exists.)
        /// </summary>
        private static void TryAddRole(Roles.DiscordRoles roleEnum, string color, bool cover, bool hidden, byte kickPower, byte requiredKickPower)
        {
            // If Roles already contains the key, skip (prevents duplicates)
            if (RolesInternalContains(roleEnum)) return;

            _rolesInternal ??= new Dictionary<Roles.DiscordRoles, RoleInfo>();

            _rolesInternal[roleEnum] = new RoleInfo
            {
                RoleEnum = roleEnum,
                Color = color,
                Cover = cover,
                Hidden = hidden,
                KickPower = kickPower,
                RequiredKickPower = requiredKickPower
            };
        }

        // Backing store used during static initialization
        private static Dictionary<Roles.DiscordRoles, RoleInfo> _rolesInternal
        {
            get
            {
                // on first access, we want to return the mutable dictionary used while constructing
                return _mutableRoles;
            }
            set { _mutableRoles = value; }
        }
        private static Dictionary<Roles.DiscordRoles, RoleInfo> _mutableRoles = new();

        // used by TryAddRole to check if a role was already added.
        private static bool RolesInternalContains(Roles.DiscordRoles role) => _mutableRoles.ContainsKey(role);

        // After static initialization Roles gets set to the readonly dictionary above.
        // But we must expose the actual roles: map _mutableRoles -> Roles in static ctor finalization.
        // (We handled that by assigning Roles at the end of ctor.)

        /// <summary>
        /// Maps a list of SCP/legacy role names to existing DiscordRoles values.
        /// Accepts SCP-style names (uppercase, hyphens, plus-signs etc.) and normalizes them.
        /// </summary>
        /// <param name="names">role names from SCP config</param>
        /// <returns>Array of DiscordRoles that exist in the enum</returns>
        private static Roles.DiscordRoles[] MapNames(params string[] names)
        {
            var list = new List<Roles.DiscordRoles>();

            if (names == null || names.Length == 0)
                return Array.Empty<Roles.DiscordRoles>();

            foreach (var n in names)
            {
                var normalized = NormalizeToEnumName(n);

                if (Enum.TryParse<Roles.DiscordRoles>(normalized, true, out var role))
                {
                    // Only add if this role is part of the validated Roles dictionary
                    if (_mutableRoles.ContainsKey(role))
                        list.Add(role);
                }
                // else: skip silently
            }

            return list.Distinct().ToArray();
        }


        /// <summary>
        /// Normalizes SCP-style names to your DiscordRoles enum identifiers.
        /// Examples:
        ///  "EK" -> "ek"
        ///  "jr_dev_leitung" -> "jr_devleitung"
        ///  "lgbtqia+" -> "lgbtq"
        ///  "nu-7" -> "nu_7"
        ///  "jr_mod" / "jr_moderator" -> "jr_mod"
        /// </summary>
        private static string NormalizeToEnumName(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return raw ?? string.Empty;

            var s = raw.Trim();

            // canonical replacements
            s = s.Replace(" ", "_");     // spaces -> underscore
            s = s.Replace("-", "_");     // hyphen -> underscore
            s = s.Replace("+", "");      // remove plus signs (lgbtqia+ -> lgbtqia)
            s = s.Replace(".", "_");     // dots -> underscore

            s = s.ToLowerInvariant();

            // Specific known normalizations:
            // SCP uses "EK" / "EK" variants -> enum is 'ek'
            if (s.Equals("ek", StringComparison.OrdinalIgnoreCase)) return "ek";

            // jr_dev_leitung (SCP) -> jr_devleitung (discord enum)
            if (s == "jr_dev_leitung" || s == "jr_dev_leitung" || s == "jr_dev_leiting") return "jr_devleitung";

            // jr_moderator -> jr_mod (discord uses jr_mod)
            if (s == "jr_moderator" || s == "jr_mod") return "jr_mod";

            // lgbtqia -> lgbtq (discord enum)
            if (s.StartsWith("lgbtq")) return "lgbtq";

            // nu_7 / nu-7 -> nu_7 : leave as-is
            // iota_10 / iota-10 -> iota_10
            // fallback: try to match enum name directly
            return s;
        }

        #endregion

        #region Utility Extensions

        /// <summary>
        /// Check whether the user's set of Discord roles grants the specified Permission.
        /// Accepts an IEnumerable of DiscordRoles (e.g. roles assigned to the player).
        /// Returns true if the user's roles intersect with the permission's allowed roles.
        /// </summary>
        public static bool HasPermission(this IEnumerable<Roles.DiscordRoles> userRoles, PlayerPermissions permission)
        {
            if (userRoles == null) return false;
            if (!PermissionRoles.TryGetValue(permission, out var allowed)) return false;
            return userRoles.Intersect(allowed).Any();
        }

        /// <summary>
        /// Check whether a single DiscordRoles value grants the permission.
        /// </summary>
        public static bool HasPermission(this Roles.DiscordRoles role, PlayerPermissions permission)
        {
            if (!PermissionRoles.TryGetValue(permission, out var allowed)) return false;
            return allowed.Contains(role);
        }

        /// <summary>
        /// Try to get RoleInfo for a DiscordRoles value.
        /// </summary>
        public static bool TryGetRoleInfo(Roles.DiscordRoles role, out RoleInfo info)
        {
            RoleInfo tinfo = null;

            try
            {
                Roles.TryGetValue(role, out tinfo);
                if (tinfo != null)
                {
                    info = tinfo;
                    return true;
                }
                else
                {
                    throw new Exception($"Error retrieving RoleInfo for role {role}");
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Error retrieving RoleInfo for role {role}\n {ex.StackTrace}");
                info = null;
                return false;
            }

        }

        /// <summary>
        /// Get all declared roles (read-only).
        /// </summary>
        public static IEnumerable<RoleInfo> GetAllRoles() => Roles.Values;

        /// <summary>
        /// Retrieves the permission mask for the specified Discord role.
        /// </summary>
        /// <remarks>This method aggregates all permissions assigned to the specified role by iterating
        /// through a predefined mapping of permissions to roles. The resulting bitmask can be used to evaluate the
        /// role's capabilities.</remarks>
        /// <param name="role">The Discord role for which to retrieve the associated permissions.</param>
        /// <returns>A bitmask representing the permissions associated with the specified role. Each bit in the mask corresponds
        /// to a specific permission, where a set bit indicates that the role has that permission.</returns>
        public static ulong GetPermissionsForRole(this Roles.DiscordRoles role)
        {
            ulong mask = 0;

            foreach (var kvp in PermissionRoles)
            {
                PlayerPermissions perm = kvp.Key;
                Roles.DiscordRoles[] roles = kvp.Value;

                // If this role is allowed for that permission
                if (roles.Contains(role))
                {
                    mask |= (ulong)perm;
                }
            }

            return mask;
        }

        #endregion
    }
}