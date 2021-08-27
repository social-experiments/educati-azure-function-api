namespace goOfflineE.Entites
{
    /// <summary>
    /// Defines the <see cref="ApplicationSettings" />.
    /// </summary>
    public class ApplicationSettings : BaseEntity
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ApplicationSettings"/> class.
        /// </summary>
        public ApplicationSettings()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ApplicationSettings"/> class.
        /// </summary>
        /// <param name="tenantId">The tenantId<see cref="string"/>.</param>
        /// <param name="applicationId">The applicationId<see cref="string"/>.</param>
        public ApplicationSettings(string tenantId, string settingId)
        {
            this.PartitionKey = tenantId;
            this.RowKey = settingId;
        }

        /// <summary>
        /// Gets or sets the SettingName.
        /// </summary>
        public string SettingName { get; set; }

        /// <summary>
        /// Gets or sets the Settings.
        /// </summary>
        public string StaticContent { get; set; }
    }
}
