//-----------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------

namespace Internal.Azure.Tests.ProcessSimple.Common
{
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.Linq;
    using System.Web.Configuration;
    using System.Web;
    // using System.Reflection;
    // using Microsoft.WindowsAzure.ResourceStack.Common.Services;

    /// <summary>
    /// Class to add limited-scope usage of Configuration settings.
    /// </summary>
    /// <remarks>
    /// This should be used before any creation of front door instances.
    /// </remarks>
    public class AppConfigSettingScope : IDisposable
    {
        /// <summary>
        /// Gets or sets the Configuration value in this scope.
        /// </summary>
        private IDictionary<string, string> ConfigValues { get; set; }

        /// <summary>
        /// Gets or sets the original Configuration values in this scope.
        /// </summary>
        private IDictionary<string, string> ConfigOriginalValues { get; set; }

        /// <summary>
        /// Gets or sets the configuration.
        /// </summary>
        private Configuration Configuration { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="AppConfigSettingScope" /> class.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="value">The value.</param>
        public AppConfigSettingScope(string name, string value)
            : this(new Dictionary<string, string> { { name, value } })
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AppConfigSettingScope" /> class.
        /// </summary>
        /// <param name="configValues">The temporary config values to use.</param>
        public AppConfigSettingScope(IDictionary<string, string> configValues)
        {
            this.ConfigValues = configValues;
            this.Configuration = WebConfigurationManager.OpenWebConfiguration("~");

            this.ConfigOriginalValues = this.ConfigValues.Keys.ToDictionary(name => name, name => this.GetAppConfigSetting(name));

            this.UpdateAppConfigSettings(this.ConfigValues);
        }

        /// <summary>
        /// Disposes the scope and resets the Configuration setting
        /// </summary>
        public void Dispose()
        {
            this.UpdateAppConfigSettings(this.ConfigOriginalValues);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Get a value in the app Configuration.
        /// </summary>
        /// <param name="name">Name of value to get.</param>
        private string GetAppConfigSetting(string name)
        {
            return !this.ContainsAppConfigSetting(name: name) ? null : this.Configuration.AppSettings.Settings[name].Value;
        }

        /// <summary>
        /// Update values in the app Configuration.
        /// </summary>
        /// <param name="configSettings">The dictionary of values to update.</param>
        private void UpdateAppConfigSettings(IDictionary<string, string> configSettings)
        {
            foreach (var setting in configSettings)
            {
                this.UpdateAppConfigSetting(name: setting.Key, value: setting.Value);
            }

            // TODO [saban]: Fix after adding Internal.Azure.Tests.ProcessSimple.Common to ARM Common.Services/friends.cs
            ////CloudConfigurationManager.ResetCache();
            // typeof(CloudConfigurationManager).GetMethod(name: "ResetCache", bindingAttr: BindingFlags.Static | BindingFlags.Public).Invoke(null, null);
        }

        /// <summary>
        /// Update a value in the app Configuration.
        /// </summary>
        /// <param name="name">Name of value to update.</param>
        /// <param name="value">Value to update to.</param>
        private void UpdateAppConfigSetting(string name, string value)
        {
            if (this.ContainsAppConfigSetting(name: name))
            {
                this.Configuration.AppSettings.Settings.Remove(name);
            }

            if (value != null)
            {
                this.Configuration.AppSettings.Settings.Add(name, value);
            }

            this.Configuration.Save(ConfigurationSaveMode.Full);
            ConfigurationManager.RefreshSection("appSettings");
        }

        /// <summary>
        /// Indicates if the app config has the setting.
        /// </summary>
        /// <param name="name">Name of the setting to check.</param>
        private bool ContainsAppConfigSetting(string name)
        {
            return this.Configuration.AppSettings.Settings.AllKeys.Contains(value: name, comparer: StringComparer.InvariantCultureIgnoreCase);
        }
    }
}