namespace goOfflineE.Models
{
    /// <summary>
    /// Defines the <see cref="Tenant" />.
    /// </summary>
    public class Tenant
    {
        /// <summary>
        /// Gets or sets the Id.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the Name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the Email.
        /// </summary>
        public string Email { get; set; }

        /// <summary>
        /// Gets or sets the AzureWebJobsStorage.
        /// </summary>
        public string AzureWebJobsStorage { get; set; }

        /// <summary>
        /// Gets or sets the AccountKey.
        /// </summary>
        public string AccountKey { get; set; }

        /// <summary>
        /// Gets or sets the CognitiveServiceKey.
        /// </summary>
        public string CognitiveServiceKey { get; set; }

        /// <summary>
        /// Gets or sets the CognitiveServiceEndPoint.
        /// </summary>
        public string CognitiveServiceEndPoint { get; set; }

        /// <summary>
        /// Gets or sets the AzureBlobURL.
        /// </summary>
        public string AzureBlobURL { get; set; }

        /// <summary>
        /// Gets or sets the OfflineWebsiteURL.
        /// </summary>
        public string OfflineWebsiteURL { get; set; }

        /// <summary>
        /// Gets or sets the ApplicationSettings.
        /// </summary>
        public string ApplicationSettings { get; set; }
    }
}
