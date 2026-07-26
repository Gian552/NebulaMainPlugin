using Interactables.Interobjects;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Arguments.Scp049Events;
using LabApi.Events.Arguments.Scp096Events;
using LabApi.Events.Arguments.Scp173Events;
using LabApi.Features.Console;
using LabApi.Features.Wrappers;
using MapGeneration;
using MEC;
using PlayerRoles;
using System;
using System.Collections.Generic;

namespace NebMainPluginLabApi.Systems.SpawnProtection
{
    public class SpawnProtection
    {
        public static bool IsProtectionEnabled = Main.Instance.IsProtectionEnabled;
        public static void Enable()
        {
                LabApi.Events.Handlers.PlayerEvents.Spawned += OnSpawned;
                LabApi.Events.Handlers.PlayerEvents.Joined += OnVerified;
                LabApi.Events.Handlers.PlayerEvents.Hurting += OnHurting;
                LabApi.Events.Handlers.ServerEvents.RoundStarted += OnRoundStarted;
                LabApi.Events.Handlers.PlayerEvents.Cuffing += OnCuffed;
                LabApi.Events.Handlers.Scp049Events.Attacking += On049Andalfern;
                LabApi.Events.Handlers.Scp096Events.AddingTarget += OnEnraging;
                LabApi.Events.Handlers.Scp173Events.AddingObserver += On173Watching;
                LabApi.Events.Handlers.PlayerEvents.ChangingRole += OnChangingRole;
                LabApi.Events.Handlers.PlayerEvents.Dying += OnDying;
        }
        public static void Disable()
        {
            LabApi.Events.Handlers.PlayerEvents.Spawned -= OnSpawned;
            LabApi.Events.Handlers.PlayerEvents.Joined -= OnVerified;
            LabApi.Events.Handlers.PlayerEvents.Hurting -= OnHurting;
            LabApi.Events.Handlers.ServerEvents.RoundStarted -= OnRoundStarted;
            LabApi.Events.Handlers.PlayerEvents.Cuffing -= OnCuffed;
            LabApi.Events.Handlers.Scp049Events.Attacking -= On049Andalfern;
            LabApi.Events.Handlers.Scp096Events.AddingTarget -= OnEnraging;
            LabApi.Events.Handlers.Scp173Events.AddingObserver -= On173Watching;
            LabApi.Events.Handlers.PlayerEvents.ChangingRole -= OnChangingRole;
            LabApi.Events.Handlers.PlayerEvents.Dying -= OnDying;
        }
        public class ProtectionData
        {
            public CoroutineHandle TimeoutHandle;
            public CoroutineHandle CheckerHandle;
            public CoroutineHandle CountdownHandle;
        }
        public static readonly Dictionary<int, ProtectionData> protections = new Dictionary<int, ProtectionData>();
        public static void OnChangingRole(PlayerChangingRoleEventArgs ev)
        {
            if (protections.ContainsKey(ev.Player.PlayerId))
            {
                RemoveProtection(ev.Player);
            }
        }
        public static void OnRoundStarted()
        {
            IsProtectionEnabled = Main.Instance.IsProtectionEnabled;
        }
        public static void OnVerified(PlayerJoinedEventArgs ev)
        {
            Timing.CallDelayed(0.2f, () =>
            {
                if (ev.Player.IsHuman && ((ev.Player.Team == Team.FoundationForces) || ev.Player.Team == Team.ChaosInsurgency))
                {
                    GiveProtection(ev.Player);
                }
            });
        }
        public static void OnSpawned(PlayerSpawnedEventArgs ev)
        {
            if (!IsProtectionEnabled) return;
            if (Warhead.IsDetonated) return;
            if (ev.Role.ServerSpawnReason == RoleChangeReason.Resurrected) return;
            if (!ev.Player.IsHuman) return;
            if (ev.Player.Team != Team.FoundationForces && ev.Player.Team != Team.ChaosInsurgency) return;
            if (ev.Player.Role == RoleTypeId.FacilityGuard) return;
            if (ev.Player?.Role == null) return;

            try
            {
                GiveProtection(ev.Player);
            }
            catch (System.NullReferenceException ex)
            {
                Logger.Error($"NullReferenceException in GiveProtection: {ex.Message}");
                if (ev.Player == null) Logger.Error("player war null");
                if (Main.Instance == null) Logger.Error("Plugin.Instance war null");
            }
        }
        public static void OnCuffed(PlayerCuffingEventArgs ev)
        {
            if (!protections.ContainsKey(ev.Target.PlayerId))
                return;
            if (protections.ContainsKey(ev.Target.PlayerId))
                ev.IsAllowed = false;
            return;
        }
        public static void OnHurting(PlayerHurtingEventArgs ev)
        {
            try
            {

                string TargetHasProtectionMessage =
                    Main.Instance.TargetHasProtectionMessage.Replace("{target}", ev.Player.Nickname);
                Player target = ev.Player; // Das Opfer
                Player attacker = ev.Attacker; // Der Angreifer

                // Wenn der Angreifer protection hat wegnehmen
                if (protections.ContainsKey(attacker.PlayerId) && Main.Instance.LoseProtectionOnShooting)
                {
                    RemoveProtection(attacker);
                }

                // Wenn das Opfer keine Protection hat → raus
                if (!protections.ContainsKey(target.PlayerId))
                    return;

                if (target == null)
                {
                    ev.IsAllowed = true;
                    return;
                }

                // Falls kein Angreifer existiert (Fall Damage, Umwelt)
                if (attacker == null)
                {
                    ev.IsAllowed = true;
                    return;
                }

                // Eigener Schaden (z.B. eigene Granate, Pink Candy)
                if (attacker == target)
                {
                    ev.IsAllowed = true;
                    return;
                }

                //Keine Aktion wenn im selben Team
                if (attacker.Team == target.Team)
                {
                    return;
                }

                // Angreifer hat Spawnschutz → darf trotzdem angreifen
                if (protections.ContainsKey(attacker.PlayerId) && protections.ContainsKey(target.PlayerId))
                {
                    ev.Attacker.SendHint(TargetHasProtectionMessage,
                        Main.Instance.TargetHasProtectionMessageTime);
                    ev.IsAllowed = false;
                    return;
                }

                //Angreifer darf pew pew
                if (protections.ContainsKey(attacker.PlayerId))
                {
                    ev.IsAllowed = true;
                    return;
                }

                //SCPs dürfen nicht angreifen
                if (attacker.Team == Team.SCPs)
                {
                    ev.Attacker.SendHint(TargetHasProtectionMessage,
                        Main.Instance.TargetHasProtectionMessageTime);
                    ev.IsAllowed = false;
                    return;
                }

                // Das Opfer ist geschützt, aber Angreifer nicht
                ev.Attacker.SendHint(TargetHasProtectionMessage, Main.Instance.TargetHasProtectionMessageTime);
                ev.IsAllowed = false;
            }
            catch (Exception ex)
            {
                Logger.Debug($"Exception in OnHurting: {ex}");
            }
        }
        public static void On049Andalfern(Scp049AttackingEventArgs ev)
        {
            string TargetHasProtectionMessage = Main.Instance.TargetHasProtectionMessage.Replace("{target}", ev.Target.Nickname);
            if (protections.ContainsKey(ev.Target.PlayerId))
            {
                ev.Player.SendHint(TargetHasProtectionMessage, Main.Instance.TargetHasProtectionMessageTime);
                ev.IsAllowed = false;
            }
        }
        public static void OnEnraging(Scp096AddingTargetEventArgs ev)
        {
            if (!Main.Instance.Enrage096)
            {
                if (protections.ContainsKey(ev.Target.PlayerId))
                {
                    ev.IsAllowed = Main.Instance.Enrage096;
                }
            }
        }
        public static void On173Watching(Scp173AddingObserverEventArgs ev)
        {
            if (protections.ContainsKey(ev.Target.PlayerId))
            {
                ev.IsAllowed = Main.Instance.Stop173;
            }
        }
        public static void OnDying(PlayerDyingEventArgs ev)
        {
            if (protections.ContainsKey(ev.Player.PlayerId))
            {
                RemoveProtection(ev.Player);
            }
        }
        private static IEnumerator<float> CheckPosition(Player player)
        {
            while (player != null && !player.IsDestroyed && protections.ContainsKey(player.PlayerId))
            {
                yield return Timing.WaitForSeconds(0.1f);

                var pos = player.Position;
                var room = player.Room;
                var zone = player.Zone;

                bool IsAtSurfaceMiddleGate(UnityEngine.Vector3 p)
                {
                    return p.x >= 37.644f && p.x <= 49.000f
                        && p.y >= 291.794f && p.y <= 296.000f
                        && p.z >= -46.550f && p.z <= -37.000f;
                }

                if (IsAtSurfaceMiddleGate(pos))
                {
                    Logger.Debug("Player is at Surface Middle Gate area, removing protection.");
                    RemoveProtection(player);
                    yield break;
                }

                bool IsInsideGateElevator(Player pl)
                {
                    foreach (Elevator lift in Elevator.List)
                    {
                        if (lift.Group != ElevatorGroup.GateB && lift.Group != ElevatorGroup.GateA01 && lift.Group != ElevatorGroup.GateA02)
                            continue;

                        if (lift.WorldSpaceRelativeBounds.Contains(pl.Position))
                            return true;
                    }

                    return false;
                }

                if (room == null)
                    continue;

                bool IsAtGateEZ = room.Name == RoomName.EzGateA || room.Name == RoomName.EzGateB;
                bool IsInSurface = zone == FacilityZone.Surface;
                if (IsAtGateEZ && !IsInsideGateElevator(player) || !IsInSurface && !IsInsideGateElevator(player))
                {
                    Logger.Debug("Player has left the spawn area, removing protection.");
                    RemoveProtection(player);
                    yield break;
                }
                if (Warhead.IsDetonated)
                {
                    Logger.Debug("Warhead detonated, removing protection.");
                    RemoveProtection(player);
                    yield break;
                }
            }
        }
        private static IEnumerator<float> CountdownCoroutine(Player player, float duration)
        {
            //IT TOOK AGES FOR THAT TO WORK I DONT WANT TO CODE ANYMORE but it works :3
            Logger.Debug($"Starting countdown coroutine for {player.Nickname}");
            float remainingTime = duration;
            Logger.Debug($"Protection duration for player {player.Nickname}: {duration} seconds");
            while (remainingTime > 0f && player != null && !player.IsDestroyed && protections.ContainsKey(player.PlayerId))
            {
                Logger.Debug($"Player {player.Nickname} - Remaining protection time: {remainingTime} seconds");
                string hintMessageTemplate = Main.Instance.ProtectionCountdownMessage.Replace("{time}", remainingTime.ToString("0"));
                string hintMessageSpectator = Main.Instance.ProtectionCountdownMessageSpectator.Replace("{time}", remainingTime.ToString("0"));
                hintMessageSpectator = hintMessageSpectator.Replace("{player}", player.Nickname);
                player.SendHint(hintMessageTemplate, 1f);
                var spectator = player.CurrentSpectators;
                foreach (var spec in spectator)
                {
                    spec.SendHint(hintMessageSpectator, 1f);
                }
                remainingTime -= 1f;
                yield return Timing.WaitForSeconds(1f);
            }
        }
        public static void ClearProtection()
        {
            foreach (var kvp in protections)
            {
                int id = kvp.Key;
                ProtectionData data = kvp.Value;
                Player pl = Player.Get(id);
                if (pl != null)
                {
                    pl.IsGodModeEnabled = false;
                }
                if (data.TimeoutHandle.IsRunning)
                    Timing.KillCoroutines(data.TimeoutHandle);
                if (data.CheckerHandle.IsRunning)
                    Timing.KillCoroutines(data.CheckerHandle);
                if (data.CountdownHandle.IsRunning)
                    Timing.KillCoroutines(data.CountdownHandle);
            }
            protections.Clear();
        }
        private static void Cleanup(Player player)
        {
            if (protections.TryGetValue(player.PlayerId, out ProtectionData data))
            {
                if (data.TimeoutHandle.IsRunning)
                    Timing.KillCoroutines(data.TimeoutHandle);

                if (data.CheckerHandle.IsRunning)
                    Timing.KillCoroutines(data.CheckerHandle);

                if (data.CountdownHandle.IsRunning)
                    Timing.KillCoroutines(data.CountdownHandle);

                protections.Remove(player.PlayerId);
            }

        }
        public static void GiveProtection(Player player)
        {

            Logger.Debug("Removing Protection if already exists.");
            Cleanup(player);


            CoroutineHandle timeout = Timing.CallDelayed(
                Main.Instance.ProtectionDuration,
                () => RemoveProtection(player)
            );

            CoroutineHandle checker = default;

            Timing.CallDelayed(0.1f, () =>
            {
                if (player?.GameObject != null)
                    checker = Timing.RunCoroutine(CheckPosition(player)
                        .CancelWith(player.GameObject));
            });
            float duration = Main.Instance.ProtectionDuration;

            protections[player.PlayerId] = new ProtectionData
            {
                TimeoutHandle = timeout,
                CheckerHandle = checker,
                CountdownHandle = default,
            };
            Timing.CallDelayed(0.2f, () =>
            {
                if (player != null && !player.IsDestroyed && protections.ContainsKey(player.PlayerId))
                {
                    protections[player.PlayerId].CountdownHandle =
                        Timing.RunCoroutine(CountdownCoroutine(player, duration));
                }
            });
        }
        public static void RemoveProtection(Player player)
        {
            if (protections.TryGetValue(player.PlayerId, out ProtectionData data))
            {
                if (data.TimeoutHandle.IsRunning)
                    Timing.KillCoroutines(data.TimeoutHandle);

                if (data.CheckerHandle.IsRunning)
                    Timing.KillCoroutines(data.CheckerHandle);

                if (data.CountdownHandle.IsRunning)
                    Timing.KillCoroutines(data.CountdownHandle);

                protections.Remove(player.PlayerId);
            }



            player.SendHint(
                Main.Instance.ProtectionDisabledMessage,
                Main.Instance.ProtectionDisabledMessageDuration
             );
            var spectators = player.CurrentSpectators;
            foreach (var spectator in spectators)
            {
                spectator.SendHint(Main.Instance.ProtectionDisabledMessage, Main.Instance.ProtectionDisabledMessageDuration);
            }

        }
    }
}
