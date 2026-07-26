using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using static NebMainPluginLabApi.API.Enums.Roles;

namespace NebMainPluginLabApi.API.Enums
{
    public static class Roles
    {
        public enum DiscordRoles : ulong
        {
            // misc
            [Description("Misc")]
            None = 0,
            [Description("Misc")]
            Verified = 1418676310888288396,

            // Team
            [Description("Team")]
            admin = 1357113653513556138,
            [Description("Team")]
            ek = 1378381250317779066,
            [Description("Team")]
            jr_ek = 1378381487665315890,
            [Description("Team")]
            jr_admin = 1376365031503171636,
            [Description("Team")]
            teamleitung = 1365994618201706558,
            [Description("Team")]
            jr_teamleitung = 1416502573707432069,
            [Description("Team")]
            jr_devleitung = 1410678045240463410,
            [Description("Team")]
            devleitung = 1358498999589666822,
            [Description("Team")]
            moderator = 1358500228457693245,
            [Description("Team")]
            jr_mod = 1410610388571000943,
            [Description("Team")]
            supporter = 1358500836354949313,
            [Description("Team")]
            jr_supporter = 1358500946799362138,

            // Playtime
            [Description("Playtime")]
            keter = 1359895815832993943,
            [Description("Playtime")]
            euclid = 1359895763458719765,
            [Description("Playtime")]
            safe = 1359894271896850653,
            [Description("Playtime")]
            pending = 1359897053265657916,

            // Cosmetic
            [Description("Cosmetic")]
            femboy = 1397315198058234029,
            [Description("Playtime")]
            lgbtq = 1397315521887862824,
            [Description("Playtime")]
            furry = 1397315446012641451,
            [Description("Playtime")]
            booster = 1361871816494416093,
            [Description("Playtime")]
            xi_8 = 1378720091872694352,
            [Description("Cosmetic")]
            femboy_furry = 1476566926695202960,
            // Rewards
            [Description("Rewards")]
            iota_10 = 1378720510153850920,
            [Description("Rewards")]
            mu_3 = 1365749086665445537,
            [Description("Rewards")]
            epsilon_6 = 1365749046215577651,
            [Description("Rewards")]
            psi_7 = 1365746129052106762,
            [Description("Rewards")]
            tau_5 = 1396819907324285090,
            [Description("Rewards")]
            nu_7 = 1396820040455684106,
            [Description("Rewards")]
            zeta_9 = 1396820219913048074,
            [Description("Rewards")]
            beta_7 = 1396820645240770580,
            [Description("Rewards")]
            beta_1 = 1396820854565765230,
            [Description("Rewards")]
            epsilon_11 = 1396820981158510633,
            [Description("Rewards")]
            alpha_1 = 1396821234163126302,
            [Description("Rewards")]
            omega_1 = 1396821351314231377,
            [Description("Rewards")]
            mu_4 = 1396821976554930267,
            [Description("Rewards")]
            eta_10 = 1396822165734690917,
            [Description("Rewards")]
            resh_1 = 1428108998187552919,
        }

        public static string GetDiscordRoleType(this DiscordRoles value)
        {
            var field = value.GetType().GetField(value.ToString());
            var attribute = field?.GetCustomAttribute<DescriptionAttribute>();
            return attribute?.Description ?? value.ToString();
        }

        public static DiscordRoles GetDiscordRoleById(ulong id)
        {
            foreach (DiscordRoles role in Enum.GetValues(typeof(DiscordRoles)))
            {
                if ((ulong)role == id)
                {
                    return role;
                }
            }
            return DiscordRoles.None;
        }
    }

    public static class DiscordRoleExtensions
    {
        public static string ToRoleString(this Roles.DiscordRoles role)
        {
            // Get the [Description] category (e.g., "Team", "Rewards", "Cosmetic")
            var category = role.GetType()
                .GetField(role.ToString())
                .GetCustomAttributes(typeof(DescriptionAttribute), false)
                .Cast<DescriptionAttribute>()
                .FirstOrDefault()?.Description ?? "";

            string name = role.ToString();

            // Replace underscores with spaces
            name = name.Replace("_", " ");

            // Lowercase for normalization
            name = name.ToLowerInvariant();

            // Category-based formatting
            switch (category)
            {
                case "Cosmetic":
                case "Rewards":
                case "Playtime":
                    return CapitalizeWords(name);

                case "Team":
                case "Misc":
                default:
                    return FormatTeamRole(name);
            }
        }

        private static string CapitalizeWords(string input)
        {
            return string.Join(" ",
                input
                    .Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(w => char.ToUpper(w[0]) + w.Substring(1))
            );
        }

        public static string FormatTeamRole(string name)
        {
            var words = name.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);

            for (int i = 0; i < words.Length; i++)
            {
                switch (words[i])
                {
                    case "jr":
                    case "jr.":
                    case "jr_":
                        words[i] = "Jr";
                        break;

                    case "admin":
                        words[i] = "Administrator";
                        break;

                    case "teamleitung":
                        words[i] = "Teamleitung";
                        break;

                    case "devleitung":
                        words[i] = "Devleitung";
                        break;

                    case "mod":
                        words[i] = "Moderator";
                        break;

                    case "supporter":
                        words[i] = "Supporter";
                        break;

                    case "ek":
                        words[i] = "Ethikkomitee";
                        break;
                    case "iota_10":
                        words[i] = "MTF Iota-10 (“Damn Feds”)";
                        break;
                    case "mu_3":
                        words[i] = "MTF Mu-3 (“Highest Bidders”)";
                        break;
                    case "epsilon_6":
                        words[i] = "MTF Epsilon-6 (“Village Idiots”)";
                        break;
                    case "psi_7":
                        words[i] = "MTF Psi-7 (“Home Improvement”)";
                        break;
                    case "tau_5":
                        words[i] = "MTF Tau-5 (“Samsara”)";
                        break;
                    case "nu_7":
                        words[i] = "MTF Nu-7 (“Hammer Down”)";
                        break;
                    case "zeta_9":
                        words[i] = "MTF Zeta-9 (“Mole Rats”)";
                        break;
                    case "beta_7":
                        words[i] = "MTF Beta-7 (“Maz Hatters”)";
                        break;
                    case "beta_1":
                        words[i] = "MTF Beta-1 (“Cauterizers”)";
                        break;
                    case "epsilon_11":
                        words[i] = "MTF Epsilon-11 (“Nine-Tailed Fox”)";
                        break;
                    case "alpha_1":
                        words[i] = "MTF Alpha-1 (“Red Right Hand”)";
                        break;
                    case "omega_1":
                        words[i] = "MTF Omega-1 (“Law's Left Hand”)";
                        break;
                    case "mu_4":
                        words[i] = "MTF Mu-4 (“Debuggers”)";
                        break;
                    case "eta_10":
                        words[i] = "MTF Eta-10 (“See No Evil”)";
                        break;
                    case "xi_8":
                        words[i] = "MTF Xi-8 (“Last to Fall”)";
                        break;
                    case "resh_1":
                        words[i] = "MTF Rēsh-1 (“Seat of Consciousness”)";
                        break;
                    case "femboy_furry":
                        words[i] = "Femboy-Furry";
                        break;
                    default:
                        words[i] = char.ToUpper(words[i][0]) + words[i].Substring(1);
                        break;
                }
            }

            return string.Join(" ", words);
        }
    }

}