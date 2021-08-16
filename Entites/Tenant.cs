namespace goOfflineE.Entites
{
    /// <summary>
    /// Defines the <see cref="Tenant" />.
    /// </summary>
    public class Tenant : BaseEntity
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Tenant"/> class.
        /// </summary>
        public Tenant()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Tenant"/> class.
        /// </summary>
        /// <param name="Name">The Name<see cref="string"/>.</param>
        /// <param name="Id">The Id<see cref="string"/>.</param>
        public Tenant(string Name, string Id)
        {
            this.PartitionKey = Name;
            this.RowKey = Id;
        }

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
        /// Gets or sets the AppInsightsKey.
        /// </summary>
        public string AppInsightsKey { get; set; }

        /// <summary>
        /// Gets or sets the ApplicationSettings.
        /// </summary>
        public string ApplicationSettings { get; set; }
    }
}
