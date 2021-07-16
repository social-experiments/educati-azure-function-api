namespace goOfflineE.Services
{
    using goOfflineE.Models;
    using System.Threading.Tasks;

    /// <summary>
    /// Defines the <see cref="IAzureBlobService" />.
    /// </summary>
    public interface IAzureBlobService
    {
        /// <summary>
        /// The GetSasUri.
        /// </summary>
        /// <param name="containerName">The containerName<see cref="string"/>.</param>
        /// <returns>The <see cref="Task{BlobStorageRequest}"/>.</returns>
        Task<BlobStorageRequest> GetSassUri(string containerName);
    }
}
