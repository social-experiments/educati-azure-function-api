using System.Collections.Generic;

namespace goOfflineE.Models
{
    /// <summary>
    /// Defines the <see cref="ApplicationSetting" />.
    /// </summary>
    public sealed class ApplicationSetting
    {
        /// <summary>
        /// Prevents a default instance of the <see cref="ApplicationSetting"/> class from being created.
        /// </summary>
        public ApplicationSetting()
        {
            StaticContent = new List<StaticContent>();
        }

        /// <summary>
        /// Gets or sets the Id.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the TenantId.
        /// </summary>
        public string TenantId { get; set; }

        /// <summary>
        /// Gets or sets the SettingName.
        /// </summary>
        public string SettingName { get; set; }

        /// <summary>
        /// Gets or sets the StaticContents.
        /// </summary>
        public List<StaticContent> StaticContent { get; set; }
    }
}
