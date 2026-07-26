using MEC;
using CustomPlayerEffects;
using InventorySystem.Items.MarshmallowMan;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Handlers;
using LabApi.Features.Wrappers;

namespace NebMainPluginLabApi.Systems.Events.ADHSL
{
    public class adhsl
    {
        public static bool ADHSLEnabled;
        public static void Enable()
        {
            ADHSLEnabled = Main.Instance.IsADHSLEnabled;
            PlayerEvents.Spawned += OnSpawned;
            PlayerEvents.UsedItem += OnUsingItem;
            PlayerEvents.UpdatedEffect += OnReceivingEffect;
            ServerEvents.RoundStarted += OnRoundRestart;
        }
        public static void Disable()
        {
            ADHSLEnabled = false;
            PlayerEvents.Spawned -= OnSpawned;
            PlayerEvents.UsedItem -= OnUsingItem;
            PlayerEvents.UpdatedEffect -= OnReceivingEffect;
            ServerEvents.RoundStarted -= OnRoundRestart;
        }
        public static void OnSpawned(PlayerSpawnedEventArgs ev)
        {
            if (ADHSLEnabled)
            {
                ev.Player.EnableEffect<MovementBoost>(255);
            }
        }
        public static void OnUsingItem(PlayerUsedItemEventArgs ev)
        {
            //WORKS 
            if (ev.UsableItem.Type == ItemType.SCP330)
            {
                if (!ADHSLEnabled) return;
                ev.Player.EnableEffect<MovementBoost>(255);
                Timing.CallDelayed(8f, () =>
                {
                    ev.Player.EnableEffect<MovementBoost>(255);
                });
            }
        }
        public static void OnReceivingEffect(PlayerEffectUpdatedEventArgs ev)
        {
            if (ev.Effect is MarshmallowEffect)
            {
                if (!ADHSLEnabled) return;
                Timing.CallDelayed(0.001f, () =>
                {
                    ev.Player.EnableEffect<MovementBoost>(255);
                });
            }
        }
        public static void OnRoundRestart()
        {
            ADHSLEnabled = Main.Instance.IsADHSLEnabled;
        }
        public static void RemoveMovementboost()
        {
            foreach (var player in Player.List)
            {
                player.DisableEffect<MovementBoost>();
            }
        }
        public static void GrantMovementboost()
        {
            foreach (var player in Player.List)
            {
                player.EnableEffect<MovementBoost>(255);
            }
        }
    }
}