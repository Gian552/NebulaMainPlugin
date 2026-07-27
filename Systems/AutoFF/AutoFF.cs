using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Arguments.ServerEvents;
using LabApi.Events.Handlers;
using LabApi.Features.Console;
using LabApi.Features.Wrappers;
using NebMainPluginLabApi.API;

namespace NebMainPluginLabApi.Systems.AutoFF
{
    public static class AutoFF
    {
        
        public static void Enable()
        {
            Server.FriendlyFire = false;
            ServerEvents.RoundEnding += OnRoundEnding;
            ServerEvents.RoundRestarted += OnRoundRestarted;
        }

        public static void Disable()
        {
            ServerEvents.RoundEnding -= OnRoundEnding;
            ServerEvents.RoundRestarted -= OnRoundRestarted;
        }
        
        private static void OnRoundEnding(RoundEndingEventArgs ev)
        {
            Server.FriendlyFire = true;
        }
        

        private static void OnRoundRestarted()
        {
            Server.FriendlyFire = false;
        }
        
    }
}