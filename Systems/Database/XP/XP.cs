using System.Linq;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Handlers;
using LabApi.Features.Console;
using LabApi.Features.Wrappers;
using PlayerRoles;
using PlayerStatsSystem;


namespace NebMainPluginLabApi.Systems.Database
{
    public class XP
    {
        public static bool XpSystemEnabled = true;

        public static void Enable()
        {
            PlayerEvents.UsingItem += OnUsingItem;
            PlayerEvents.UsedItem += Handle1344Use;
            PlayerEvents.Death += OnDied;
            ServerEvents.RoundStarted += OnRoundStarted;
            PlayerEvents.ThrewProjectile += OnThrowing;

        }

        public static void Disable()
        {
            PlayerEvents.UsingItem -= OnUsingItem;
            PlayerEvents.UsedItem -= Handle1344Use;
            PlayerEvents.Death -= OnDied;
            ServerEvents.RoundStarted -= OnRoundStarted;
            PlayerEvents.ThrewProjectile -= OnThrowing;
        }

        private static void OnRoundStarted()
        {
            XpSystemEnabled = true;
        }

        private static void OnThrowing(PlayerThrewProjectileEventArgs ev)
        {
            if (ev.ThrowableItem.Type == ItemType.SCP2176)
                Database.UpdateReconsAndXP(ev.Player, 20);
        }
        

        private static void OnUsingItem(PlayerUsingItemEventArgs ev)
        {
            if (!XpSystemEnabled)
                return;

            switch (ev.Item.Type)
            {
                case ItemType.Medkit:
                    if (ev.Player.Health < ev.Player.MaxHealth)
                    {
                        Logger.Info($"Player used Medkit; health:{ev.Player.Health}");
                        Database.UpdateReconsAndXP(ev.Player, 10);
                    }
                    break;

                case ItemType.Adrenaline:
                    Database.UpdateReconsAndXP(ev.Player, 10);
                    break;

                case ItemType.SCP207:
                    Database.UpdateReconsAndXP(ev.Player, 10);
                    break;

                case ItemType.AntiSCP207:
                    Database.UpdateReconsAndXP(ev.Player, 15);
                    break;

                case ItemType.SCP018:
                    Database.UpdateReconsAndXP(ev.Player, 20);
                    break;

                case ItemType.SCP1853:
                    Database.UpdateReconsAndXP(ev.Player, 25);
                    break;

                case ItemType.SCP244a:
                case ItemType.SCP244b:
                    Database.UpdateReconsAndXP(ev.Player, 20);
                    break;

                case ItemType.SCP268:
                    Database.UpdateReconsAndXP(ev.Player, 25);
                    break;

                case ItemType.SCP500:
                    Database.UpdateReconsAndXP(ev.Player, 20);
                    break;

                case ItemType.Painkillers:
                    if (ev.Player.Health < ev.Player.MaxHealth)
                        Database.UpdateReconsAndXP(ev.Player, 5);
                    break;
                case ItemType.SCP330 :
                    Database.UpdateReconsAndXP(ev.Player, 15);
                    break;
                default:
                    break;
            }
        }

        private static void Handle1344Use(PlayerUsedItemEventArgs ev)
        {
            if (ev.Item.Type == ItemType.SCP1344)
                Database.UpdateReconsAndXP(ev.Player, 20);
        }

        private static void OnDied(PlayerDeathEventArgs ev)
        {
            if (!XpSystemEnabled)
                return;
            try
            {
                Database.AddDeath(ev.Player);
                if (ev.Attacker != null && ev.Attacker != ev.Player)
                {
                    Database.AddKill(ev.Attacker);
                    Database.UpdateReconsAndXP(ev.Attacker, ev.Player.IsSCP ? 250 : 70);
                }

                // Check if the player died in pocket so that 106 gets XP
                if (ev.DamageHandler is UniversalDamageHandler universal 
                    && universal.TranslationId == DeathTranslations.PocketDecay.Id)
                {
                    foreach (var scp106 in Player.List.Where(pl => pl.Role == RoleTypeId.Scp106))
                    {
                        Database.AddKill(scp106);
                        Database.UpdateReconsAndXP(scp106, ev.Player.IsSCP ? 250 : 70);
                    }
                }
            }
            catch
            {
                Logger.Info("Died is null again D:");
            }
        }
    }
}