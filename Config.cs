using PlayerRoles;
using System.Collections.Generic;
using System.ComponentModel;

namespace NebMainPluginLabApi
{
    public class Config
    {
        public bool IsEnabled { get; set; } = true;
        public bool Debug { get; set; } = false;

        [Description("Der Servername, der in jeder benachrichtigung angezeigt wird.")]
        public string serverName { get; set; } = "<color=#ffd700>N</color><color=#ffb347>e</color><color=#ff7f50>b</color><color=#ff6ec7>u</color><color=#ba55d3>l</color><color=#8a2be2>a</color>";

        [Description("Das Passwort, welches für die DB verwendet wird.")]
        public string dbConnectionString { get; set; }

        [Description("Der Webhook Link an welchen die Server Logs gesendet werden.")]
        public string WebHookLogs { get; set; }

        [Description("Der Webhook, wo die Teamler Spielzeiten gepostet werden.")]
        public string TeamTimeControllWebhook { get; set; }

        [Description("Nach wie vielen Sekunden, welche eine Runde läuft, man nicht mehr SCP tauschen kann.")]
        public int ScpSwapTimeout { get; set; } = 40;

        [Description("Ab wie vielen Spielern man zu SCP-3114 (skellet) wechseln kann.")]
        public int SkelliCount { get; set; }

        [Description("Ihr könnt diese config ändern, aber für den Fall ihr wollt eine Rolle hinzufügen geht das nocht nicht, nur entfernen und wieder hinzufügen der bereits existierenden Rollen!")]
        public List<RoleTypeId> SwapableScps { get; set; } = new List<RoleTypeId>()
        {
            RoleTypeId.None,
            RoleTypeId.Scp049,
            RoleTypeId.Scp079,
            RoleTypeId.Scp3114,
            RoleTypeId.Scp096,
            RoleTypeId.Scp173,
            RoleTypeId.Scp106,
            RoleTypeId.Scp939,
        };

        [Description("Sollte man nur einmal swapen dürfen.")]
        public bool SingleSwap { get; set; } = false;

        [Description("Die cahnce Pink Candy aus der Schüssel zu ziehen. (0.0 bis 100.0; default: 25.0)")]
        public double PinkCandyChance { get; set; } = 25.0;

        /// <summary>
        ///     Whether Amnesia affects the usage of keycards.
        /// </summary>
        [Description("Whether Amnesia affects the usage of keycards.")]
        public bool AmnesiaMatters { get; set; } = true;

        /// <summary>
        ///     Whether this plugin works on generators.
        /// </summary>
        [Description("Whether this plugin works on generators.")]
        public bool AffectGenerators { get; set; } = true;

        /// <summary>
        ///     Whether this plugin works on Warhead's panel.
        /// </summary>
        [Description("Whether this plugin works on Warhead's panel.")]
        public bool AffectWarheadPanel { get; set; } = true;

        /// <summary>
        ///     Whether this plugin works on SCP lockers.
        /// </summary>
        [Description("Whether this plugin works on SCP lockers.")]
        public bool AffectScpLockers { get; set; } = true;

        /// <summary>
        ///     Whether this plugin works on doors.
        /// </summary>
        [Description("Whether this plugin works on doors.")]
        public bool AffectDoors { get; set; } = true;

        /// <summary>
        ///     Gets whether exceptions should be shown.
        /// </summary>
        [Description("Toggle on/off exceptions/errors in console. (Enable this before reporting ANY bugs)")]
        public bool ShowExceptions { get; set; } = false;

        //Spawnprotection Configs
        /// <summary>
        ///     Whether spawnprotection is enabled or not.
        /// </summary>
        [Description("Whether the spawnprotection is enabled or not. You can change the value ingame with toggelspawprotection for that round.")]
        public bool IsProtectionEnabled { get; set; } = true;
        /// <summary>
        ///     Whether players lose protection if they shoot or not.  
        /// </summary>
        [Description("Whether players lose protection if they shoot or not.")]
        public bool LoseProtectionOnShooting { get; set; } = false;
        /// <summary>
        ///     The time, players have spawnprotection  
        /// </summary>
        [Description("The time, players have spawnprotection")]
        public float ProtectionDuration { get; set; } = 120f;
        /// <summary>
        ///     The message displayed to the player with Spawnprotection  
        /// </summary>
        [Description("The message displayed to show the countdown. To display the time use {time}")]
        public string ProtectionCountdownMessage { get; set; } = "<size=15><color=#00FF00>Spawn-Schutz: {time} Sekunden verbleibend</color></size>";
        /// <summary>
        ///     The message displayed to a player spectating a person with spawnprotection 
        /// </summary>
        [Description("The message displayed to show the countdown. To display the time use {time}. To display the name of the player spectated use {player}")]
        public string ProtectionCountdownMessageSpectator { get; set; } = "<size=15><color=#00FF00>{player} hat noch {time} Sekunden Spawn-Schutz</color></size>";
        /// <summary>
        ///     The message when a player lost his protection 
        /// </summary>
        [Description("The message for the deactivation hint")]
        public string ProtectionDisabledMessage { get; set; } = "<size=15><color=#FF0000>Spawn-Schutz vorbei</color></size>";
        /// <summary>
        ///     The duration of the message when a player lost his protection 
        /// </summary>
        [Description("The time for the deactivation hint")]
        public int ProtectionDisabledMessageDuration { get; set; } = 5;
        /// <summary>
        ///     The message displayed to an attacker that the target has Spawnprotection 
        /// </summary>
        [Description("The message dispalyed to the attacker when a player has spawnprotection. To display the name of the target user {target}.")]
        public string TargetHasProtectionMessage { get; set; } = "<size=15><color=#FF0000>{target} hat Spawnschutzt!!!</color></size>";
        /// <summary>
        ///     The duration of the message dispalyed to an attacker that the target has Spawnprotection 
        /// </summary>
        [Description("The duration of the message dispalyed to the attacker when a player has spawnprotection.")]
        public int TargetHasProtectionMessageTime { get; set; } = 1;
        /// <summary>
        ///     Whether shy guy rages because of players with Spawnprotection 
        /// </summary>
        [Description("Should SCP-096 be enraged by players with spawnprotection?")]
        public bool Enrage096 { get; set; } = true;
                
        /// <summary>
        ///     Whether peanut gets stopped by players with Spawnprotection 
        /// </summary>
        [Description("Should SCP-173 be stopped when observed by player with spawnprotection?")]
        public bool Stop173 { get; set; } = true;
                
        /// <summary>
        ///     Whether ADHSL is enabled or not 
        /// </summary>
        [Description("Is ADHSL enabled?")]
        public bool IsADHSLEnabled { get; set; } = false;

        public bool IsAutoFFEnabled { get; set; } = true;
        
        [Description("Ob in der Lobby (Warten auf Spieler) Musik abgespielt wird.")]
        public bool WarteMusikEnabled { get; set; } = true;

        [Description("Der Ordner mit den Musik-Dateien (.wav oder .mp3). Pro Lobby wird ein zufälliger Track gespielt. Relative Pfade werden relativ zum LabAPI-Config-Ordner aufgelöst.")]
        public string WarteMusikFolder { get; set; } = "Music";

        [Description("Maximale Lautstärke der WarteMusik (0.0 bis 1.0). Der Regler in den Einstellungen skaliert diesen Wert.")]
        public float WarteMusikVolume { get; set; } = 0.5f;

        [Description("Wie die Lautstärke pro Spieler umgesetzt wird. 'Segmente' = feste Anzahl Lautsprecher, jeder Spieler wird auf die nächste Stufe gerundet. 'Spieler' = ein eigener Lautsprecher pro Zuhörer, dadurch exakt 0-100%.")]
        public Systems.WarteMusik.VolumeMode WarteMusikVolumeMode { get; set; } = Systems.WarteMusik.VolumeMode.Segmente;

        [Description("Nur im Modus 'Segmente': Anzahl der wählbaren Lautstärke-Stufen (1 bis 32). Jede Stufe kostet einen zusätzlichen Audio-Stream (CPU).")]
        public int WarteMusikVolumeSteps { get; set; } = 8;

        [Description("Nur im Modus 'Spieler': Maximale Anzahl gleichzeitiger Audio-Streams (1 bis 32). Sind mehr Zuhörer da als Streams, hören die überzähligen Spieler keine Musik.")]
        public int WarteMusikMaxStreams { get; set; } = 32;

        [Description("Basis-ControllerId der Lautsprecher. Nur ändern, wenn ein anderes Plugin dieselben IDs benutzt.")]
        public byte WarteMusikControllerId { get; set; } = 200;

        [Description("Ob die Musik geloopt wird, bis die Runde startet.")]
        public bool WarteMusikLoop { get; set; } = true;
    }
}
