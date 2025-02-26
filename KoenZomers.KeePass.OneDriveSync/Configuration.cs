﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Windows.Forms;
using KoenZomers.KeePass.OneDriveSync.Enums;
using KoenZomersKeePassOneDriveSync;
using Newtonsoft.Json;

namespace KoenZomers.KeePass.OneDriveSync
{
    /// <summary>
    /// Plugin configuration class. Contains functions to serialize/deserialize to/from JSON.
    /// </summary>
    [DataContract]
    public class Configuration : ICloneable
    {
        #region Constants

        /// <summary>
        /// Name under which to store these settings in the KeePass configuration store
        /// </summary>
        private const string ConfigurationKey = "KeeOneDrive";

        #endregion

        #region Non serializable Properties

        /// <summary>
        /// Dictionary with configuration settings for all password databases. Key is the local database path, value is the configuration belonging to it.
        /// </summary>
        public static IDictionary<string, Configuration> PasswordDatabases = new Dictionary<string, Configuration>();

        /// <summary>
        /// Boolean indicating if the syncing of this database is allowed
        /// </summary>
        public bool SyncingEnabled = true;

        /// <summary>
        /// The KeePass database to which these settings belong
        /// </summary>
        public KeePassLib.PwDatabase KeePassDatabase { get; set; }

        #endregion

        #region Serializable Properties

        /// <summary>
        /// Gets or sets refresh token that can be used to get an Access Token for OneDrive access
        /// </summary>
        [DataMember]
        public string RefreshToken { get; set; }

        /// <summary>
        /// Gets or sets the location where the refresh token will be stored
        /// </summary>
        [DataMember]
        public Enums.OneDriveRefreshTokenStorage? RefreshTokenStorage { get; set; } 

        /// <summary>
        /// Gets or sets the name of the OneDrive the KeePass database is synchronized with
        /// </summary>
        [DataMember]
        public string OneDriveName { get; set; }

        /// <summary>
        /// Gets or sets database file path on OneDrive relative to the user 
        /// </summary>
        [DataMember]
        public string RemoteDatabasePath { get; set; }

        /// <summary>
        /// Gets or sets a boolean to indicate if the database should be synced with OneDrive
        /// </summary>
        [DataMember]
        public bool DoNotSync { get; set; }

        /// <summary>
        /// The SHA1 hash of the local KeePass database
        /// </summary>
        [DataMember]
        public string LocalFileHash { get; set; }

        /// <summary>
        /// Date and time at which the database last synced with OneDrive
        /// </summary>
        [DataMember]
        public DateTime? LastSyncedAt { get; set; }

        /// <summary>
        /// Date and time at which the database has last been compared with its equivallent on OneDrive
        /// </summary>
        [DataMember]
        public DateTime? LastCheckedAt { get; set; }

        #endregion

        #region Methods

        /// <summary>
        /// Gets the KeePass database configuration for KeePassOneDriveSync for the KeePass database of which the local path is provided
        /// </summary>
        /// <param name="localPasswordDatabasePath">Full path to where the KeePass database resides locally</param>
        /// <returns>KeePassOneDriveSync settings for the provided database</returns>
        public static Configuration GetPasswordDatabaseConfiguration(string localPasswordDatabasePath)
        {
            if (!PasswordDatabases.ContainsKey(localPasswordDatabasePath))
            {
                PasswordDatabases.Add(new KeyValuePair<string, Configuration>(localPasswordDatabasePath, new Configuration()));
            }
            return PasswordDatabases[localPasswordDatabasePath];
        }

        /// <summary>
        /// Loads the configuration stored in KeePass
        /// </summary>
        public static void Load()
        {
            // Retrieve the stored configuration from KeePass
            var value = KoenZomersKeePassOneDriveSyncExt.Host.CustomConfig.GetString(ConfigurationKey, null);

            if (string.IsNullOrEmpty(value))
            {
                return;
            }

            try
            {
                // Convert the retrieved JSON to a typed entity
                PasswordDatabases = JsonConvert.DeserializeObject<Dictionary<string, Configuration>>(value);
            }
            catch (JsonSerializationException)
            {                
                MessageBox.Show("Unable to parse the plugin configuration for the KeePass OneDriveSync plugin. If this happens again, please let me know. Sorry for the inconvinience. Koen Zomers <mail@koenzomers.nl>", "KeePass OneDriveSync Plugin", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);

                // Reset the configuration so at least it works again next time, all be it that all configuration will be lost
                PasswordDatabases = new Dictionary<string, Configuration>();
                Save();
            }

            // Get all database configurations which have their OneDrive Refresh Token stored in the Windows Credential Manager and retrieve them
            var windowsCredentialManagerDatabaseConfigs = PasswordDatabases.Where(pwdDb => pwdDb.Value.RefreshTokenStorage == OneDriveRefreshTokenStorage.WindowsCredentialManager);
            foreach (var windowsCredentialManagerDatabaseConfig in windowsCredentialManagerDatabaseConfigs)
            {
                windowsCredentialManagerDatabaseConfig.Value.RefreshToken = Utilities.GetRefreshTokenFromWindowsCredentialManager(windowsCredentialManagerDatabaseConfig.Key);
            }
        }

        /// <summary>
        /// Saves the current configuration
        /// </summary>
        public static void Save()
        {
            // Create a new dictionary with the information to store in KeePass.config.xml
            var passwordDatabasesForStoring = new Dictionary<string, Configuration>();

            // Loop through the entries to store
            foreach (var passwordDatabase in PasswordDatabases)
            {
                switch (passwordDatabase.Value.RefreshTokenStorage)
                {
                    case OneDriveRefreshTokenStorage.Disk:
                        // Refresh token will be stored on disk, we can store the complete configuration instance on disk in this case
                        passwordDatabasesForStoring.Add(passwordDatabase.Key, passwordDatabase.Value);
                        break;

                    case OneDriveRefreshTokenStorage.KeePassDatabase:
                    case OneDriveRefreshTokenStorage.WindowsCredentialManager:
                        // Refresh token will not be stored on disk, we create a copy of the configuration and remove the refresh token from it so it will not be stored on disk
                        var tempConfiguration = (Configuration) passwordDatabase.Value.Clone();
                        tempConfiguration.RefreshToken = null;
                        passwordDatabasesForStoring.Add(passwordDatabase.Key, tempConfiguration);

                        if (passwordDatabase.Value.RefreshTokenStorage == OneDriveRefreshTokenStorage.KeePassDatabase && passwordDatabase.Value.KeePassDatabase != null && !string.IsNullOrEmpty(passwordDatabase.Value.RefreshToken))
                        {
                            Utilities.SaveRefreshTokenInKeePassDatabase(passwordDatabase.Value.KeePassDatabase, passwordDatabase.Value.RefreshToken);
                        }
                        if (passwordDatabase.Value.RefreshTokenStorage == OneDriveRefreshTokenStorage.WindowsCredentialManager)
                        {
                            Utilities.SaveRefreshTokenInWindowsCredentialManager(passwordDatabase.Key, passwordDatabase.Value.RefreshToken);
                        }
                        break;
                }
            }

            // Serialize the configuration to JSON
            var json = JsonConvert.SerializeObject(passwordDatabasesForStoring);

            // Store the configuration in KeePass.config.xml
            KoenZomersKeePassOneDriveSyncExt.Host.CustomConfig.SetString(ConfigurationKey, json);
        }

        /// <summary>
        /// Returns a copy of the current instance
        /// </summary>
        public object Clone()
        {
            return MemberwiseClone();
        }

        /// <summary>
        /// Deletes the complete configuration of the KeePass database on the provided local path
        /// </summary>
        /// <param name="localPasswordDatabasePath">Full local path to a KeePass database of which to delete the configuration</param>
        public static void DeleteConfig(string localPasswordDatabasePath)
        {
            // Verify if we have configuration available of a KeePass database stored on the provided full local path
            if (!PasswordDatabases.ContainsKey(localPasswordDatabasePath)) return;
            
            // Retrieve the configuration we have available about the KeePass database
            var config = PasswordDatabases[localPasswordDatabasePath];
            
            // Take cleanup actions based on where the OneDrive Refresh Token is stored
            switch (config.RefreshTokenStorage)
            {
                case OneDriveRefreshTokenStorage.Disk:
                    // No action required as it will be removed as part of removing the complete configuration
                    break;

                case OneDriveRefreshTokenStorage.KeePassDatabase:
                    // There's no way to remove it from the KeePass database without having the database open, so we'll have to leave it there
                    break;

                case OneDriveRefreshTokenStorage.WindowsCredentialManager:
                    Utilities.DeleteRefreshTokenFromWindowsCredentialManager(localPasswordDatabasePath);
                    break;
            }

            // Remove the configuration we have available
            PasswordDatabases.Remove(localPasswordDatabasePath);

            // Initiate a save to write the results
            Save();
        }

        #endregion
    }
}

