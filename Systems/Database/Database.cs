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
using MEC;
using NebMainPluginLabApi;
using NebMainPluginLabApi.API;
using NebMainPluginLabApi.API.Enums;

namespace NebMainPluginLabApi.Systems.Database
{
    public static class PlayerDataCache
    {
        internal static readonly ConcurrentDictionary<string, PlayerData> Data = new ConcurrentDictionary<string, PlayerData>();

        public static PlayerData Get(string id)
            => Data.TryGetValue(id, out var result) ? result : null;

        public static PlayerData GetOrCreate(string id)
            => Data.GetOrAdd(id, key => new PlayerData { Id = key });

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
            PlayerEvents.ChangedBadgeVisibility += OnBadgeVisibilityChanged;

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
                UpdateCachedData(p);
            }

            try
            {
                SaveAllDataToDatabase().GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                Logger.Error($"Error while saving player data on shutdown: {ex}");
            }

            PlayerEvents.Left -= OnPlayerLeft;
            ServerEvents.RoundEnded -= OnRoundEnded;
            PlayerEvents.Joined -= OnJoined;
            PlayerEvents.ChangedBadgeVisibility -= OnBadgeVisibilityChanged;

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
            if (player.DoNotTrack)
                return;

            var data = PlayerDataCache.GetOrCreate(player.UserId);
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
            var data = PlayerDataCache.GetOrCreate(player.UserId);
            data.NicknameChangable = state;

            PlayerDataCache.Set(player.UserId, data);
        }

        /// <summary>
        /// Update the Players Displayname in DB for later use
        /// </summary>
        /// <param name="player"></param>
        public static void UpdateCustomNick(Player player)
        {
            var data = PlayerDataCache.GetOrCreate(player.UserId);
            data.CustomNick = player.DisplayName;

            PlayerDataCache.Set(player.UserId, data);
        }

        /// <summary>
        /// Adds a Kill to the Database entry for this player
        /// </summary>
        /// <param name="player"></param>
        public static void AddKill(Player player)
        {
            if (player.DoNotTrack)
                return;

            var data = PlayerDataCache.GetOrCreate(player.UserId);
            data.Kills++;

            PlayerDataCache.Set(player.UserId, data);
        }

        /// <summary>
        /// Adds a Death to the Database entry for this player
        /// </summary>
        /// <param name="player"></param>
        public static void AddDeath(Player player)
        {
            if (player.DoNotTrack)
                return;

            var data = PlayerDataCache.GetOrCreate(player.UserId);
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
            var data = PlayerDataCache.GetOrCreate(target.UserId);

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
            var data = PlayerDataCache.GetOrCreate(target.UserId);

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
            if (data?.Warns == null || data.Warns.All(w => w.Id != warn))
            {
                return false;
            }

            try
            {
                data.Warns.RemoveAll(w => w.Id == warn);
                PlayerDataCache.Set(data.Id, data);
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
            PlayerData data = await _collection.Find(p => p.Id == steamID).FirstOrDefaultAsync();

            return data?.Warns ?? new List<Warn>();
        }

        /// <summary>
        /// Adds a Watchlist entry to the specified player with relevant context.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="issuer"></param>
        /// <param name="reson"></param>
        public static void AddWatchlist(Player target, Player issuer, string reson)
        {
            var data = PlayerDataCache.GetOrCreate(target.UserId);

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
            if (data?.Watchlists == null || data.Watchlists.All(w => w.Id != watchlist))
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
            PlayerData data = await _collection.Find(p => p.Id == steamID).FirstOrDefaultAsync();

            return data?.Watchlists ?? new List<Warn>();
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
            if (ply?.UserId == null)
                return false;

            var data = PlayerDataCache.Get(ply.UserId);

            if (data == null)
                return false;

            return ApplyDiscordRole(ply, data, forceDisplay: true);
        }

        /// <summary>
        /// Returns every role the player is allowed to display, ordered by category.
        /// </summary>
        internal static List<Roles.DiscordRoles> GetSelectableRoles(Player player)
        {
            var result = new List<Roles.DiscordRoles>();

            if (player?.UserId == null)
                return result;

            var data = PlayerDataCache.Get(player.UserId);
            if (data?.dcRoles == null)
                return result;

            foreach (var role in data.dcRoles)
            {
                if (role == Roles.DiscordRoles.None || result.Contains(role))
                    continue;

                if (PermissionRegistry.Roles.ContainsKey(role))
                    result.Add(role);
            }

            return result
                .OrderBy(r => CategoryOrder(r.GetDiscordRoleType()))
                .ThenBy(r => r.ToRoleString())
                .ToList();
        }

        private static int CategoryOrder(string category)
        {
            switch (category)
            {
                case "Team": return 0;
                case "Rewards": return 1;
                case "Cosmetic": return 2;
                case "Playtime": return 3;
                default: return 4;
            }
        }

        /// <summary>
        /// Stores the role the player picked and applies it immediately.
        /// </summary>
        internal static bool SetSelectedRole(Player player, Roles.DiscordRoles role)
        {
            if (player?.UserId == null)
                return false;

            var data = PlayerDataCache.Get(player.UserId);
            if (data == null)
                return false;

            if (role != Roles.DiscordRoles.None && (data.dcRoles == null || !data.dcRoles.Contains(role)))
            {
                Logger.Warn($"{player.Nickname} tried to select role {role} without owning it.");
                return false;
            }

            data.dcRole = role;
            PlayerDataCache.Set(player.UserId, data);
            SaveSelectedRoleAsync(data);

            if (role == Roles.DiscordRoles.None)
            {
                player.ReferenceHub.serverRoles.SetGroup(null, false, true);
                return true;
            }

            return ApplyDiscordRole(player, data);
        }

        private static async void SaveSelectedRoleAsync(PlayerData data)
        {
            if (_collection == null)
                return;

            try
            {
                var filter = Builders<PlayerData>.Filter.Eq(p => p.Id, data.Id);
                var update = Builders<PlayerData>.Update.Set(p => p.dcRole, data.dcRole);

                await _collection.UpdateOneAsync(filter, update, new UpdateOptions { IsUpsert = true });
            }
            catch (Exception ex)
            {
                Logger.Error($"Error while saving selected role for {data.Id}: {ex.Message}");
            }
        }

        private static PlayerData UpdateCachedData(Player player)
        {
            if (player.DoNotTrack)
                return null;

            var data = PlayerDataCache.GetOrCreate(player.UserId);
            data.Nickname = player.Nickname;

            if (data.Playtime == null)
                data.Playtime = 0;
            if (SessionVariables.Get(player).ContainsKey("JoinTime"))
            {
                data.Playtime += (DateTime.Now - DateTime.FromBinary((long)SessionVariables.Get(player)["JoinTime"])).TotalSeconds;
                SessionVariables.Set(player, "JoinTime", DateTime.Now.ToBinary());
            }

            return data;
        }

        internal static async void UpdateDataAsync(Player player)
        {
            var data = UpdateCachedData(player);
            if (data == null)
                return;

            await SavePlayerDataPartialAsync(data);
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

            if (_collection == null)
            {
                Settings.EventHandles.SendRoleOptions(player);
                return;
            }

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

            Settings.EventHandles.SendRoleOptions(player);
        }
        private static bool ApplyDiscordRole(Player player, PlayerData data, bool forceDisplay = false)
        {
            if (player?.ReferenceHub == null || data == null)
                return false;

            if (data.dcRole == Roles.DiscordRoles.None)
                return false;

            if (data.dcRoles == null || !data.dcRoles.Contains(data.dcRole))
            {
                Logger.Debug($"{data.Nickname} has {data.dcRole} selected but does not own it, skipping.");
                return false;
            }

            Logger.Debug($"{data.Nickname} has a discord role ({data.dcRole.ToRoleString()}), trying to set it ingame now...");

            try
            {
                PermissionRegistry.TryGetRoleInfo(data.dcRole, out var rankData);

                if (rankData == null)
                    throw new NullReferenceException("RankData is Null");

                Logger.Debug($"Rank data will be: {rankData.DisplayName}");

                UserGroup group = new()
                {
                    Name = rankData.InternalName,
                    BadgeColor = rankData.Color,
                    BadgeText = rankData.DisplayName,
                    Permissions = rankData.Permissions,
                    Cover = rankData.Cover,
                    HiddenByDefault = rankData.Hidden,
                    Shared = false,
                    KickPower = rankData.KickPower,
                    RequiredKickPower = rankData.RequiredKickPower
                };

                Logger.Debug($"Trying to set rank for {data.Nickname} to {group.Name}");
                player.ReferenceHub.serverRoles.SetGroup(group, false, forceDisplay);
                Logger.Debug($"Rank for {data.Nickname} is now: {player.GroupName} ({player.ReferenceHub.serverRoles.Network_myColor})");

                AnnounceBadge(player, 0.5f);
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error($"Error while setting rank for {data.Nickname}: {ex}");
                return false;
            }
        }

        /// <summary>
        /// True when the player has a badge that SCP:SL is currently hiding (hidetag / Badge-Einstellung).
        /// </summary>
        internal static bool IsBadgeHidden(Player player)
        {
            var roles = player?.ReferenceHub?.serverRoles;
            if (roles == null)
                return false;

            return !string.IsNullOrEmpty(roles.HiddenBadge) || roles.GlobalHidden;
        }

        private static string BadgeHex(Player player, string colorName)
        {
            var named = player?.ReferenceHub?.serverRoles?.NamedColors?
                .FirstOrDefault(c => c != null && c.Name == colorName);

            return string.IsNullOrEmpty(named?.ColorHex) ? null : named.ColorHex;
        }

        /// <summary>
        /// Tells the player which badge is active and whether it is currently hidden.
        /// </summary>
        internal static void AnnounceBadge(Player player, float delay)
        {
            if (player?.UserId == null)
                return;

            string userId = player.UserId;

            Timing.CallDelayed(delay, () =>
            {
                try
                {
                    Player current = Player.List.FirstOrDefault(p => p.UserId == userId);
                    if (current?.ReferenceHub == null)
                        return;

                    var data = PlayerDataCache.Get(userId);
                    if (data == null || data.dcRole == Roles.DiscordRoles.None)
                        return;

                    if (!PermissionRegistry.TryGetRoleInfo(data.dcRole, out var rankData) || rankData == null)
                        return;

                    bool hidden = IsBadgeHidden(current);

                    if (BadgeStates.TryGetValue(userId, out var last)
                        && last.Hidden == hidden
                        && (DateTime.Now - last.At).TotalSeconds < 2)
                        return;

                    BadgeStates[userId] = (hidden, DateTime.Now);

                    string hex = BadgeHex(current, rankData.Color);
                    string name = hex == null ? rankData.DisplayName : $"<color=#{hex}>{rankData.DisplayName}</color>";

                    HintsAPI.AddHint(current, hidden
                        ? $"Dein Rang {name} ist <color=#FF0000>versteckt</color>. Tippe <b>showtag</b> in die Spielkonsole (Ö), um ihn anzuzeigen."
                        : $"Dein Rang: {name}", 5);
                }
                catch (Exception ex)
                {
                    Logger.Error($"Error while announcing badge: {ex.Message}");
                }
            });
        }

        private static readonly Dictionary<string, (bool Hidden, DateTime At)> BadgeStates =
            new Dictionary<string, (bool, DateTime)>();

        private static void OnBadgeVisibilityChanged(PlayerChangedBadgeVisibilityEventArgs ev)
        {
            var player = ev.Player;
            if (player?.UserId == null)
                return;

            RestoreBadgeIfWiped(player);
            AnnounceBadge(player, 0.1f);
        }

        /// <summary>
        /// showtag ruft RefreshLocalTag auf. Das leert HiddenBadge und laedt die Gruppe
        /// aus config_remoteadmin neu - dort stehen unsere Discord-Raenge aber nicht drin,
        /// also waere der Badge danach komplett weg. Hier setzen wir ihn wieder.
        /// </summary>
        private static void RestoreBadgeIfWiped(Player player)
        {
            var roles = player?.ReferenceHub?.serverRoles;
            if (roles == null)
                return;

            if (!string.IsNullOrEmpty(roles.HiddenBadge) || !string.IsNullOrEmpty(player.GroupName))
                return;

            var data = PlayerDataCache.Get(player.UserId);
            if (data == null || data.dcRole == Roles.DiscordRoles.None)
                return;

            Logger.Debug($"Restoring wiped badge for {data.Nickname} ({data.dcRole}).");
            ApplyDiscordRole(player, data, forceDisplay: true);
        }

        private static void OnPlayerLeft(PlayerLeftEventArgs ev)
        {
            if (ev.Player.DoNotTrack)
                return;

            UpdateDataAsync(ev.Player);
            SessionVariables.Clear(ev.Player);
            Settings.EventHandles.Forget(ev.Player.UserId);
            BadgeStates.Remove(ev.Player.UserId);
        }

        private static async void OnRoundEnded(RoundEndedEventArgs ev)
        {
            foreach (var p in Player.List)
            {
                UpdateCachedData(p);
            }

            try
            {
                await SaveAllDataToDatabase();
            }
            catch (Exception ex)
            {
                Logger.Error($"Error while saving player data on round end: {ex}");
            }
        }
    }
}