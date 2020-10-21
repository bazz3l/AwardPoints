using System.Collections.Generic;
using Oxide.Core;
using Oxide.Core.Plugins;

namespace Oxide.Plugins
{
    [Info("AwardPoints", "Bazz3l", "1.0.4")]
    [Description("Reward players with points for activity and tasks")]
    public class AwardPoints : RustPlugin
    {
        [PluginReference] Plugin ServerRewards;

        #region Fields
        
        private BasePlayer _heliLastAttacker;

        private PluginConfig _config;

        private StoredData _stored;
        
        #endregion

        #region Config
        
        private PluginConfig GetDefaultConfig()
        {
            return new PluginConfig
            {
                BarrelEnabled = true,
                HeliEnabled = true,
                BradleyEnabled = true,
                PlayerEnabled = true,

                BarrelPoints = 1,
                HeliPoints = 10,
                BradleyPoints = 10,
                PlayerPoints = 2,

                PlayerAwardTime = 900f
            };
        }

        private class PluginConfig
        {
            public bool BarrelEnabled;
            public bool HeliEnabled;
            public bool BradleyEnabled;
            public bool PlayerEnabled;
            public int BarrelPoints;
            public int HeliPoints;
            public int BradleyPoints;
            public int PlayerPoints;
            public float PlayerAwardTime;
        }
        
        #endregion

        #region Storage

        private class StoredData
        {
            public List<string> Players = new List<string>();
        }

        private void SaveData() => Interface.Oxide.DataFileSystem.WriteObject(Name, this);

        #endregion

        #region Oxide
        
        protected override void LoadDefaultConfig() => Config.WriteObject(GetDefaultConfig(), true);

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                {"TogglePoints", "<color=#DC143C>Award Points</color>: notifications are now {0}"},
                {"AwardPoints",  "<color=#DC143C>Award Points</color>: You gained +{0} points."},
                {"Enabled", "<color=#21BF4D>Enabled</color>"},
                {"Disabled", "<color=#DC143C>Disabled</color>"}
            }, this);
        }

        private void OnServerInitialized()
        {
            AwardPlayers();

            if (!_config.PlayerEnabled)
            {
                return;
            }
            
            timer.Every(_config.PlayerAwardTime, AwardPlayers);
        }

        private void Init()
        {
            _config = Config.ReadObject<PluginConfig>();
            _stored = Interface.Oxide.DataFileSystem.ReadObject<StoredData>(Name);
        }

        private void OnEntityTakeDamage(BaseHelicopter heli, HitInfo info)
        {
            if (heli.GetComponent<PatrolHelicopterAI>() == null || info?.InitiatorPlayer == null) return;

            _heliLastAttacker = info?.InitiatorPlayer;
        }

        private void OnEntityDeath(BaseCombatEntity entity, HitInfo info)
        {
            if (string.IsNullOrEmpty(entity.ShortPrefabName))
            {
                return;
            }
            
            string prefabName = entity.ShortPrefabName;

            if (prefabName.StartsWith("loot-barrel") || prefabName.StartsWith("loot_barrel") || prefabName == "oil_barrel")
            {
                AwardBarrel(info?.InitiatorPlayer, _config.BarrelPoints);
                return;
            }

            if (prefabName.Contains("bradleyapc") && !prefabName.Contains("gibs"))
            {
                AwardBradley(info?.InitiatorPlayer, _config.BradleyPoints);
                return;
            }

            if (prefabName.Contains("patrolhelicopter") && !prefabName.Contains("gibs"))
            {
                AwardHeli(_heliLastAttacker, _config.HeliPoints);
            }
        }
        
        #endregion

        #region Core
        
        private void AwardPlayers()
        {
            foreach (BasePlayer player in BasePlayer.activePlayerList)
            {
                AddPoints(player.UserIDString, _config.PlayerPoints);
                
                player.ChatMessage(Lang("AwardPoints", player.UserIDString, _config.PlayerPoints));
            }
        }

        private void AwardBarrel(BasePlayer player, int points) 
        {
            if (player == null || !_config.BarrelEnabled)
            {
                return;
            }

            AddPoints(player.UserIDString, points);
            
            if (_stored.Players.Contains(player.UserIDString))
            {
                return;
            }

            player.ChatMessage(Lang("AwardPoints", player.UserIDString, points));
        }

        private void AwardHeli(BasePlayer player, int points)
        {
            if (player == null || !_config.HeliEnabled)
            {
                return;
            }

            AddPoints(player.UserIDString, points);
            
            if (_stored.Players.Contains(player.UserIDString))
            {
                return;
            }

            player.ChatMessage(Lang("AwardPoints", player.UserIDString, points));
        }

        private void AwardBradley(BasePlayer player, int points)
        {
            if (player == null || !_config.BradleyEnabled)
            {
                return;
            }

            AddPoints(player.UserIDString, points);

            if (_stored.Players.Contains(player.UserIDString))
            {
                return;
            }

            player.ChatMessage(Lang("AwardPoints", player.UserIDString, points));
        }
        
        #endregion

        #region Command

        [ChatCommand("ap")]
        private void AwardPointsCommand(BasePlayer player, string command, string[] args)
        {
            if (!_stored.Players.Remove(player.UserIDString))
            {
                _stored.Players.Add(player.UserIDString);
            }
            
            player.ChatMessage(Lang("TogglePoints", player.UserIDString, _stored.Players.Contains(player.UserIDString) ? Lang("Disabled") : Lang("Enabled")));
        }

        #endregion

        #region Helpers
        
        private string Lang(string key, string id = null, params object[] args) => string.Format(lang.GetMessage(key, this, id), args);

        private object AddPoints(string userID, int points) => ServerRewards?.Call("AddPoints", userID, points);

        #endregion
    }
}
