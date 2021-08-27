namespace goOfflineE.Models
{
    /// <summary>
    /// Defines the <see cref="StaticContent" />.
    /// </summary>
    public class StaticContent
    {
        public StaticContent()
        {

        }
        /// <summary>
        /// Gets or sets the Id.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the Name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the Type.
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// Gets or sets the Blob.
        /// </summary>
        public string Blob { get; set; }
    }
}
