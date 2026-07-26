using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Arguments.ServerEvents;
using LabApi.Events.Handlers;
using LabApi.Features.Console;
using LabApi.Features.Wrappers;
using NebMainPlugin.API;
using NebMainPlugin.API.Enums;
using NebMainPluginLabApi;

namespace NebMainPlugin.Systems.Database
{
    public static class PlayerDataCache
    {
        internal static readonly ConcurrentDictionary<string, PlayerData> Data = new ConcurrentDictionary<string, PlayerData>();

        public static PlayerData Get(string id)
            => Data.TryGetValue(id, out var result) ? result : null;

        public static void Set(string id, PlayerData playerData)
            => Data[id] = playerData;
    }

    // Player data model
    public class PlayerData
    {
        [BsonId]
        public string Id { get; set; }
        public string DiscordId { get; set; } = null;
        public bool Verified { get; set; } = false;
        public string VerificationToken { get; set; }
        public string Nickname { get; set; }
        public string CustomNick { get; set; }
        public Roles.DiscordRoles dcRole { get; set; } = Roles.DiscordRoles.None;
        public List<Roles.DiscordRoles> dcRoles { get; set; } = new List<Roles.DiscordRoles>();
        public string slRole { get; set; } = null;
        public bool NicknameChangable { get; set; } = true;
        public List<Warn> Warns { get; set; } = new List<Warn>();
        public List<Warn> Watchlists { get; set; } = new List<Warn>();
        public List<Ban> Bans { get; set; } = new List<Ban>();
        public double? Playtime { get; set; }
        public double? WeekStart { get; set; } = 0;
        public int XP { get; set; }
        public int RequiredXP { get; set; } = 230;
        public int Level { get; set; } = 1;
        public int Kills { get; set; }
        public int Deaths { get; set; }
    }

    //Warn data structure
    public class Warn
    {
        public ObjectId Id { get; set; } = ObjectId.GenerateNewId();
        public DateTime CreatedAt { get; set; }
        public string Reason { get; set; }
        public string Issuer { get; set; }
    }

    //Ban data structure
    public class Ban
    {
        public ObjectId Id { get; set; } = ObjectId.GenerateNewId();
        public DateTime CreatedAt { get; set; }
        public long ExpiresAt { get; set; }
        public string Reason { get; set; }
        public string Issuer { get; set; }
    }

    internal static class Database
    {
        private static IMongoCollection<PlayerData> _collection;

        internal static void InitDB()
        {
            //Config.LoadConfig();

            if (string.IsNullOrEmpty(Main.Instance.dbConnectionString) || Main.Instance.dbConnectionString.ToCharArray().Count() < 4)
            {
                Logger.Error("Verbindung zur DB kann nicht aufgebaut werden, System nicht aktiviert!");
                return;
            }

            var client = new MongoClient(Main.Instance.dbConnectionString);
            var db = client.GetDatabase("SL_NEBULA");
            _collection = db.GetCollection<PlayerData>("players");

            if (_collection == null)
            {
                Logger.Error("DB nicht verbunden, bitte config Prüfen!");
                return;
            }

            LoadAllDataFromDatabase();
            
            PlayerEvents.Left += OnPlayerLeft;
            ServerEvents.RoundEnded += OnRoundEnded;
            PlayerEvents.Joined += OnJoined;

            XP.Enable();
        }

        internal static void CloseDB()
        {
            if (_collection == null)
            {
                Logger.Error("DB nicht verbunden, bitte config Prüfen!");
                return;
            }

            foreach (var p in Player.List)
            {
                UpdateDataAsync(p);
            }

            SaveAllDataToDatabase();
            
            PlayerEvents.Left -= OnPlayerLeft;
            ServerEvents.RoundEnded -= OnRoundEnded;
            PlayerEvents.Joined -= OnJoined;

            XP.Disable();

            _collection = null;
        }

        private static void LoadAllDataFromDatabase()
        {
            Task.Run(async () =>
            {
                var allPlayers = await _collection.Find(_ => true).ToListAsync();
                foreach (var player in allPlayers)
                {
                    try
                    {
                        PlayerDataCache.Data[player.Id] = player;
                    }
                    catch
                    {
                        Logger.Error("Player with wrong atributes, skipping...");
                        continue;
                    }
                }

                Logger.Info($"Loaded {PlayerDataCache.Data.Count} player data entries into memory.");
            }).GetAwaiter().GetResult(); // Block here if you must to ensure data is ready.
        }

        private static async Task SaveAllDataToDatabase()
        {
            foreach (var playerData in PlayerDataCache.Data.Values)
            {
                await SavePlayerDataPartialAsync(playerData);
            }

            Logger.Info("Saved all in-memory player data to MongoDB.");
        }

        internal static async Task<PlayerData> GetPlayerInfoAsync(string uid)
        {
            Player player = Player.List.FirstOrDefault(p => p.UserId == uid);

            if (player == null)
            {
                Logger.Warn($"Player with UID {uid} not found.");
                return null;
            }

            return await GetPlayerDataAsync(player);
        }

        private static async Task<PlayerData> GetPlayerDataAsync(Player player)
        {
            Logger.Debug($"Getting {player.UserId}:{player.Nickname} data!");

            var result = await _collection.Find(p => p.Id == player.UserId).FirstOrDefaultAsync();

            return result ?? await CreatePlayerDataAsync(player);
        }

        public static async Task<PlayerData> GetPlayerDataByIdAsync(string steamId)
        {
            return await _collection.Find(p => p.Id == steamId).FirstOrDefaultAsync();
        }

        private static async Task<PlayerData> CreatePlayerDataAsync(Player player)
        {
            Logger.Debug($"Creating {player.UserId}:{player.Nickname} data!");

            var data = new PlayerData
            {
                Id = player.UserId,
                Nickname = player.Nickname,
            };

            await _collection.InsertOneAsync(data);
            PlayerDataCache.Data[player.UserId] = data;

            return data;
        }

        private static async Task SavePlayerDataPartialAsync(PlayerData data)
        {
            var filter = Builders<PlayerData>.Filter.Eq(p => p.Id, data.Id);

            var update = Builders<PlayerData>.Update
                .Set(p => p.Nickname, data.Nickname)
                .Set(p => p.CustomNick, data.CustomNick)
                .Set(p => p.NicknameChangable, data.NicknameChangable)
                .Set(p => p.Warns, data.Warns)
                .Set(p => p.Watchlists, data.Watchlists)
                .Set(p => p.XP, data.XP)
                .Set(p => p.Level, data.Level)
                .Set(p => p.RequiredXP, data.RequiredXP)
                .Set(p => p.Playtime, data.Playtime)
                .Set(p => p.WeekStart, data.WeekStart)
                .Set(p => p.Deaths, data.Deaths)
                .Set(p => p.Kills, data.Kills)
                .Set(p => p.Verified, data.Verified);

            await _collection.UpdateOneAsync(filter, update, new UpdateOptions { IsUpsert = true });
        }


        /// <summary>
        /// Add or remove an amount of Recons or XP from a Player
        /// </summary>
        /// <param name="player"></param>
        /// <param name="recons"></param>
        /// <param name="xp"></param>
        public static void UpdateReconsAndXP(Player player, int xp = 0, bool ShowHint = true)
        {
            var data = PlayerDataCache.Get(player.UserId);
            if (xp != 0)
                data.XP += xp;

            PlayerDataCache.Set(player.UserId, data);

            Levels.CheckLevel(data);

            if (xp > 0 && ShowHint)
                HintsAPI.AddHint(player,$"[{Main.Instance.serverName}]: Du hast {xp} XP erhalten.",3);
        }

        /// <summary>
        /// Updates if a player can change thier nick
        /// </summary>
        /// <param name="player"></param>
        /// <param name="state"></param>
        public static void UpdateAllowNickChange(Player player, bool state)
        {
            var data = PlayerDataCache.Get(player.UserId);
            data.NicknameChangable = state;

            PlayerDataCache.Set(player.UserId, data);
        }

        /// <summary>
        /// Update the Players Displayname in DB for later use
        /// </summary>
        /// <param name="player"></param>
        public static void UpdateCustomNick(Player player)
        {
            var data = PlayerDataCache.Get(player.UserId);
            data.CustomNick = player.DisplayName;

            PlayerDataCache.Set(player.UserId, data);
        }

        /// <summary>
        /// Adds a Kill to the Database entry for this player
        /// </summary>
        /// <param name="player"></param>
        public static void AddKill(Player player)
        {
            var data = PlayerDataCache.Get(player.UserId);
            data.Kills++;

            PlayerDataCache.Set(player.UserId, data);
        }

        /// <summary>
        /// Adds a Death to the Database entry for this player
        /// </summary>
        /// <param name="player"></param>
        public static void AddDeath(Player player)
        {
            var data = PlayerDataCache.Get(player.UserId);
            data.Deaths++;

            PlayerDataCache.Set(player.UserId, data);
        }

        /// <summary>
        /// Adds a Ban to the specified player with relevant context.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="issuer"></param>
        /// <param name="reson"></param>
        public static void AddBan(Player target, Player issuer, string reson, long Duration)
        {
            var data = PlayerDataCache.Get(target.UserId);

            if (data.Bans == null)
                data.Bans = new List<Ban>();

            data.Bans.Add(new Ban
            {
                CreatedAt = DateTime.UtcNow.Date,
                ExpiresAt = Duration,
                Reason = reson,
                Issuer = issuer.Nickname
            });

            PlayerDataCache.Set(target.UserId, data);
        }

        /// <summary>
        /// Adds a Ban to the specified player with relevant context.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="issuer"></param>
        /// <param name="reson"></param>
        public static void AddBan(PlayerData data, Player issuer, string reson, long Expires)
        {
            if (data.Bans == null)
                data.Bans = new List<Ban>();

            data.Bans.Add(new Ban
            {
                CreatedAt = DateTime.UtcNow.Date,
                ExpiresAt = Expires,
                Reason = reson,
                Issuer = issuer.Nickname
            });

            PlayerDataCache.Set(data.Id, data);
        }

        /// <summary>
        /// Adds a Warn to the specified player with relevant context.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="issuer"></param>
        /// <param name="reson"></param>
        public static void AddWarn(Player target, Player issuer, string reson)
        {
            var data = PlayerDataCache.Get(target.UserId);

            if (data.Warns == null)
                data.Warns = new List<Warn>();

            data.Warns.Add(new Warn
            {
                CreatedAt = DateTime.UtcNow.Date,
                Reason = reson,
                Issuer = issuer.Nickname
            });

            PlayerDataCache.Set(target.UserId, data);
        }

        /// <summary>
        /// Adds a Warn to the specified player with relevant context.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="issuer"></param>
        /// <param name="reson"></param>
        public static void AddWarn(PlayerData data, Player issuer, string reson)
        {
            if (data.Warns == null)
                data.Warns = new List<Warn>();

            data.Warns.Add(new Warn
            {
                CreatedAt = DateTime.UtcNow.Date,
                Reason = reson,
                Issuer = issuer.Nickname
            });

            PlayerDataCache.Set(data.Id, data);
        }

        /// <summary>
        /// Remove a warn using the string parsed ObjectId.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="warn"></param>
        /// <returns></returns>
        public static bool RemoveWarn(PlayerData data, ObjectId warn)
        {
            var TestWarn = data.Warns.FirstOrDefault(w => w.Id == warn);
            if (data.Warns.IsEmpty() || data.Warns == null || TestWarn == null)
            {
                return false;
            }

            try
            {
                data.Warns.RemoveAll(w => w.Id == warn);
            }
            catch
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Lsit all warns of the ID
        /// </summary>
        /// <param name="steamID"></param>
        /// <returns>A Task<Lsit<Warn>> with all warns of the specified UserId</returns>
        public static async Task<List<Warn>> ListWarns(string steamID)
        {
            PlayerData data = await _collection.Find(steamID).FirstOrDefaultAsync();

            return data == null ? new List<Warn>() : data.Warns;
        }

        /// <summary>
        /// Adds a Watchlist entry to the specified player with relevant context.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="issuer"></param>
        /// <param name="reson"></param>
        public static void AddWatchlist(Player target, Player issuer, string reson)
        {
            var data = PlayerDataCache.Get(target.UserId);

            if (data.Watchlists == null)
                data.Watchlists = new List<Warn>();

            data.Watchlists.Add(new Warn
            {
                CreatedAt = DateTime.UtcNow.Date,
                Reason = reson,
                Issuer = issuer.Nickname
            });

            PlayerDataCache.Set(target.UserId, data);
        }

        /// <summary>
        /// Adds a Watchlist entry to the specified player with relevant context.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="issuer"></param>
        /// <param name="reson"></param>
        public static void AddWatchlist(PlayerData data, Player issuer, string reson)
        {
            if (data.Watchlists == null)
                data.Watchlists = new List<Warn>();

            data.Watchlists.Add(new Warn
            {
                CreatedAt = DateTime.UtcNow.Date,
                Reason = reson,
                Issuer = issuer.Nickname
            });

            PlayerDataCache.Set(data.Id, data);
        }

        /// <summary>
        /// Remove a watchlists using the string parsed ObjectId.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="watchlist"></param>
        /// <returns>If the Watchlist entry was removed Succesfully</returns>
        public static bool RemoveWatchlist(PlayerData data, ObjectId watchlist)
        {
            var TestWatchlists = data.Watchlists.FirstOrDefault(w => w.Id == watchlist);

            if (data.Watchlists.IsEmpty() || data.Watchlists == null || TestWatchlists == null)
            {
                return false;
            }

            try
            {
                data.Watchlists.RemoveAll(w => w.Id == watchlist);
                PlayerDataCache.Set(data.Id, data);
            }
            catch (Exception ex)
            {
                Logger.Error(ex.Message);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Lsit all watchlists of the ID
        /// </summary>
        /// <param name="steamID"></param>
        /// <returns>A Task<Lsit<Warn>> with all watchlists of the specified UserId</returns>
        public static async Task<List<Warn>> ListWatchlist(string steamID)
        {
            PlayerData data = await _collection.Find(steamID).FirstOrDefaultAsync();

            return data == null ? new List<Warn>() : data.Warns;
        }

        /// <summary>
        /// Resets the weekly playtime of all players and directly puts the in the DB.
        /// </summary>
        /// <returns>Weather the operation was successful or not</returns>
        public static async Task<bool> ResetWeeklyPlaytime()
        {
            foreach (PlayerData ply in PlayerDataCache.Data.Values)
            {
                ply.WeekStart = ply.Playtime;
            }

            try
            {
                await SaveAllDataToDatabase();
            }
            catch (Exception ex)
            {
                Logger.Error($"Error while resetting the Weekly Playtime in DB:\n{ex.Message}");
                return false;
            }
            return true;
        }

        /// <summary>
        /// Updates the rank of the specified player based on their associated role and permissions.
        /// </summary>
        /// <remarks>This method retrieves the player's role information from the cache and updates their
        /// rank accordingly. The rank is determined by the player's associated role and its corresponding
        /// permissions.</remarks>
        /// <param name="ply">The player whose rank is to be updated. This parameter cannot be <see langword="null"/>.</param>
        /// <returns><see langword="true"/> if the player's rank was successfully updated; otherwise, <see langword="false"/>.</returns>
        public static bool UpdatePlayerRank(Player ply)
        {
            var data = PlayerDataCache.Get(ply.UserId);

            if (data.dcRole != Roles.DiscordRoles.None || data.dcRole != 0 && data.dcRoles.Contains(data.dcRole))
            {
                Logger.Debug($"{data.Nickname} has a discord role ({data.dcRole.ToRoleString()}), trying to set it ingame now...");

                try
                {
                    PermissionRegistry.RoleInfo RankData;
                    PermissionRegistry.TryGetRoleInfo(data.dcRole, out RankData);

                    if (RankData == null)
                        throw new NullReferenceException("RankData is Null");

                    Logger.Debug($"Rank data will be: {RankData.DisplayName}");

                    UserGroup group = new()
                    {
                        Name = RankData.InternalName,
                        BadgeColor = RankData.Color,
                        BadgeText = RankData.DisplayName,
                        Permissions = RankData.Permissions,
                        Cover = data.dcRole.ToRoleString() == "Team" ? true : false,
                        HiddenByDefault = data.dcRole.ToRoleString() == "Team" ? true : false,
                        Shared = false,
                        KickPower = RankData.KickPower,
                        RequiredKickPower = RankData.RequiredKickPower
                    };

                    Logger.Debug($"Trying to set rank for {data.Nickname} to {group.Name}");
                    ply.UserGroup = group;
                    Logger.Debug($"Rank for {data.Nickname} is now: {ply.GroupName}");
                    return ply.GroupName == group.Name;
                }
                catch (Exception ex)
                {
                    Logger.Error($"Error while setting rank for {data.Nickname}: {ex.StackTrace}\n{ex.Data}");
                    return false;
                }
            }
            return false;
        }

        internal static async void UpdateDataAsync(Player player)
        {
            if (player.DoNotTrack)
                return;
            
            var data = PlayerDataCache.Get(player.UserId);

            data.Id = player.UserId; // Just in case
            data.Nickname = player.Nickname;

            if (data.Playtime == null)
                data.Playtime = 0;
            if (SessionVariables.Get(player).ContainsKey("JoinTime"))
            {
                data.Playtime += (DateTime.Now - DateTime.FromBinary((long)SessionVariables.Get(player)["JoinTime"])).TotalSeconds;
                SessionVariables.Set(player, "JoinTime", DateTime.Now.ToBinary());
            }
            await SavePlayerDataPartialAsync(data);
            SessionVariables.Clear(player);
        }

        internal static async void UpdateDataAsync(PlayerData data)
        {
            Logger.Debug($"Updating {data.Id}:{data.Nickname} data!");

            await SavePlayerDataPartialAsync(data);
        }

        // Events
        private static async void OnJoined(PlayerJoinedEventArgs ev)
        {
            var player = ev.Player;

            if (player.DoNotTrack){
                HintsAPI.AddHint(player,"Du hast Do not Track an. Es werden für dich keine Statistiken gesammelt und du erhälst auch keinen Rang. Um da zu ändern stelle dies in den Einstellungen um.", 10);
                return;
            }
            // 1) Sofort aus dem Cache die Rolle vergeben, damit der Spieler ohne Wartezeit seinen Rang bekommt
            var cachedData = PlayerDataCache.Get(player.UserId);
            if (cachedData != null)
            {
                ApplyDiscordRole(player, cachedData);
            }

            SessionVariables.Set(player, "JoinTime", DateTime.Now.ToBinary());

            // 2) Danach frisch aus der DB laden (falls sich z.B. die Rolle über Discord geändert hat)
            var freshData = await _collection.Find(p => p.Id == player.UserId).FirstOrDefaultAsync()
                            ?? await CreatePlayerDataAsync(player);

            if (string.IsNullOrEmpty(freshData.Nickname))
                freshData.Nickname = player.Nickname;
            freshData.WeekStart ??= freshData.Playtime;

            // Cache aktualisieren -> spätestens beim nächsten Join ist die Änderung da
            PlayerDataCache.Set(player.UserId, freshData);
            player.DisplayName = freshData.CustomNick;

            Logger.Debug($"Player {player.Nickname}, {player.UserId} currently has the role: {player.GroupName}");

            // 3) Optional: falls sich die Rolle gerade durch den frischen Pull geändert hat,
            // direkt anwenden statt erst beim nächsten Join
            if (cachedData == null || cachedData.dcRole != freshData.dcRole)
            {
                ApplyDiscordRole(player, freshData);
            }
        }
        private static void ApplyDiscordRole(Player player, PlayerData data)
        {
            if (data.dcRole == Roles.DiscordRoles.None || !data.dcRoles.Contains(data.dcRole))
                return;

            Logger.Debug($"{data.Nickname} has a discord role ({data.dcRole.ToRoleString()}), trying to set it ingame now...");

            try
            {
                PermissionRegistry.TryGetRoleInfo(data.dcRole, out var rankData);

                if (rankData == null)
                    throw new NullReferenceException("RankData is Null");

                Logger.Debug($"Rank data will be: {rankData.DisplayName}");

                bool isTeam = data.dcRole.ToRoleString() == "Team";

                UserGroup group = new()
                {
                    Name = rankData.InternalName,
                    BadgeColor = rankData.Color,
                    BadgeText = rankData.DisplayName,
                    Permissions = rankData.Permissions,
                    Cover = isTeam,
                    HiddenByDefault = isTeam,
                    Shared = false,
                    KickPower = rankData.KickPower,
                    RequiredKickPower = rankData.RequiredKickPower
                };

                Logger.Debug($"Trying to set rank for {data.Nickname} to {group.Name}");
                player.UserGroup = group;
                Logger.Debug($"Rank for {data.Nickname} is now: {player.GroupName}");
            }
            catch (Exception ex)
            {
                Logger.Error($"Error while setting rank for {data.Nickname}: {ex.StackTrace}\n{ex.Data}");
            }
        }

        private static void OnPlayerLeft(PlayerLeftEventArgs ev)
        {
            if (ev.Player.DoNotTrack)
                return;
            
            UpdateDataAsync(ev.Player);
        }

        private static void OnRoundEnded(RoundEndedEventArgs ev)
        {
            foreach (var p in Player.List)
            {
                if (p.DoNotTrack)
                    continue;
                
                UpdateDataAsync(p);
            }
            SaveAllDataToDatabase();
        }
    }
}