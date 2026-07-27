# Nebula Main Plugin (NebMainPluginLabApi)

Das offizielle LabAPI-Plugin des **Nebula Development Teams** für SCP: Secret Laboratory Server. Es bündelt Rang-/XP-System, Discord-Integration, Spawnschutz, individuelles HUD und mehrere Server-Custom-Commands in einem Plugin.

- **Autoren:** Skorp 1.0, MisterT13, Gian
- **Version:** 1.0.1.1
- **Framework:** .NET Framework 4.8 (net48), LabAPI ≥ 1.0.0
- **Sprache der Ingame-Texte:** Deutsch

## Features im Überblick

- 🎖️ **Discord-Rang-System** – MongoDB-gestützte Verifizierung, die Discord-Rollen mit Ingame-Badges/Gruppen verknüpft
- 📈 **XP & Level-System** – Spieler sammeln XP durch Items, Kills, SCP-Aktionen etc.
- 🛡️ **Spawnschutz** – konfigurierbarer Godmode-Ersatz mit Countdown-Hint für frisch gespawnte Spieler
- 🔑 **Remote-Keycards** – Türen/Generatoren/Spinde/Warhead-Panel per Inventar-Keycard aus der Distanz bedienbar
- 🔊 **WarteMusik** – Lobby-Musik (MP3/WAV) mit pro-Spieler oder segmentierter Lautstärkeregelung
- 🖥️ **Custom HUD** – Uhrzeit, TPS, Rundenzeit, Kills, Zuschauerliste, SCP-Übersicht via RueI
- 🎲 **SCP-Swap** – Spieler können innerhalb eines Zeitfensters ihr SCP tauschen
- 📊 **Discord-Logging** – Server-Events (Joins, Kills, Cuffs, ...) und wöchentliche Team-Spielzeiten werden per Webhook gepostet
- 🍬 **Pink-Candy-Chance** – konfigurierbare Wahrscheinlichkeit für SCP-330

## Voraussetzungen

- SCP:SL Server mit installiertem **LabAPI**
- MongoDB-Instanz (für Rang-/XP-/Ban-/Warn-Speicherung)
- Optional: Discord-Webhook-URLs für Logging & Team-Reports

## Installation

1. Kompilierte `NebMainPluginLabApi.dll` (+ `RueI.dll`) in den globalen Plugin-Ordner von LabAPI legen.
2. Restliche Abhängigkeiten (`MongoDB.*`, `Discord.Net.*`, `NLayer`, `0Harmony`, ...) in den `dependencies/global`-Ordner legen.
3. Server einmal starten, damit `config.yml` generiert wird.
4. In der Config mindestens folgendes setzen:
    - `dbConnectionString` – MongoDB-Connection-String
    - `WebHookLogs` – Discord-Webhook für Server-Logs (optional)
    - `TeamTimeControllWebhook` – Discord-Webhook für Team-Spielzeiten (optional)
5. Server neu starten.

> ⚠️ Die Config enthält Zugangsdaten (DB-Passwort, Webhook-URLs). Zugriff auf die Config-Datei entsprechend einschränken.

## Wichtige Commands (Kurzfassung)

| Command | Handler | Kurzbeschreibung |
|---|---|---|
| `.level` / `.lvl` | Client | Zeigt eigene Level-/XP-/Spielzeit-Stats |
| `.scpswap <nummer>` / `.scps` | Client | SCP innerhalb des Zeitfensters wechseln |
| `.verify <token>` | Client | Discord-Account verknüpfen |
| `spawnprotection` / `sp` | RA | Verwaltung des Spawnschutzes |
| `togglexp` / `txp` | RA | XP-System an/aus |
| `toggleadhsl` / `tadhsl` | RA | Movementboost-Event an/aus |
| `playtimectl` / `ptctl` | RA | Spielzeiten anderer Spieler einsehen |
| `tptpost` / `tptp` | Konsole | Team-Spielzeiten-Report manuell posten |

Details, Rechte und Konfigurationsoptionen: siehe [DOCUMENTATION.md](./DOCUMENTATION.md).

## Lizenz / Nutzung

Internes Plugin des Nebula Development Teams. Keine öffentliche Lizenz angegeben – bei Fragen an das Team wenden.