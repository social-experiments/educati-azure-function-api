namespace goOfflineE.Services
{
    using Azure.Storage;
    using Azure.Storage.Sas;
    using goOfflineE.Common.Constants;
    using goOfflineE.Models;
    using Microsoft.AspNetCore.Http;
    using Microsoft.WindowsAzure.Storage;
    using System;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using goOfflineE.Repository;

    /// <summary>
    /// Defines the <see cref="AzureBlobService" />.
    /// </summary>
    public class AzureBlobService : IAzureBlobService
    {
        /// <summary>
        /// Defines the connectionString.
        /// </summary>
        private string connectionString = SettingConfigurations.AzureWebJobsStorage;

        /// <summary>
        /// Defines the accountKey.
        /// </summary>
        private string accountKey = SettingConfigurations.AccountKey;

        internal readonly  IHttpContextAccessor _httpContextAccessor;

        private readonly ITableStorage _tableStorage;

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureBlobService"/> class.
        /// </summary>
        /// <param name="httpContextAccessor">The httpContextAccessor<see cref="IHttpContextAccessor"/>.</param>
        /// <param name="tenantService">The tenantService<see cref="ITenantService"/>.</param>
        public AzureBlobService(IHttpContextAccessor httpContextAccessor,
            ITableStorage tableStorage)
        {
            _tableStorage = tableStorage;
            _httpContextAccessor = httpContextAccessor;
           
        }

        /// <summary>
        /// The GetSasUri.
        /// </summary>
        /// <param name="containerName">The containerName<see cref="string"/>.</param>
        /// <returns>The <see cref="Task{BlobStorageRequest}"/>.</returns>
        public async Task<BlobStorageRequest> GetSassUri(string containerName)
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(connectionString);
            _tableStorage.Client = storageAccount.CreateCloudTableClient();

            var tenantService = _httpContextAccessor.HttpContext.RequestServices.GetService<ITenantService>();

            //, ITenantService tenantService
            var request = _httpContextAccessor.HttpContext.Request;
            if (request != null && request.Headers.ContainsKey("Tenant"))
            {
                string tenantId = request.Headers["Tenant"].ToString();

                if (!String.IsNullOrEmpty(tenantId))
                {
                    var tenants = tenantService.Get(tenantId).Result;
                    connectionString = tenants.AzureWebJobsStorage;
                    accountKey = tenants.AccountKey;
                }
            }
            // connect to our storage account and create a blob client
            var blobStorageAccount = CloudStorageAccount.Parse(connectionString);
            var blobClient = blobStorageAccount.CreateCloudBlobClient();
            StorageSharedKeyCredential credential = new StorageSharedKeyCredential(blobStorageAccount.Credentials.AccountName, accountKey);

            // get a reference to the container
            var blobContainer = blobClient.GetContainerReference(containerName);
            await blobContainer.CreateIfNotExistsAsync();

            var sasBuilder = new AccountSasBuilder()
            {
                StartsOn = DateTimeOffset.UtcNow,
                ExpiresOn = DateTimeOffset.UtcNow.AddDays(7),
                Services = AccountSasServices.Blobs,
                ResourceTypes = AccountSasResourceTypes.All,
                Protocol = SasProtocol.Https
            };
            sasBuilder.SetPermissions(AccountSasPermissions.All);

            var sasToken = sasBuilder.ToSasQueryParameters(credential).ToString();
            return new BlobStorageRequest { StorageAccessToken = sasToken, StorageUri = blobContainer.ServiceClient.StorageUri.PrimaryUri.AbsoluteUri };
        }
    }
}
