namespace goOfflineE.Functions
{
    using goOfflineE.Helpers.Attributes;
    using goOfflineE.Services;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Azure.WebJobs.Extensions.Http;
    using NSwag.Annotations;
    using System.Threading.Tasks;

    /// <summary>
    /// Defines the <see cref="AzureBlobFunction" />.
    /// </summary>
    public class AzureBlobFunction : AuthenticationFilter
    {
        /// <summary>
        /// Defines the _blobService.
        /// </summary>
        private readonly IAzureBlobService _blobService;

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureBlobFunction"/> class.
        /// </summary>
        /// <param name="blobService">The blobService<see cref="IAzureBlobService"/>.</param>
        public AzureBlobFunction(IAzureBlobService blobService)
        {
            _blobService = blobService;
        }

        /// <summary>
        /// The GetSASAccessToken.
        /// </summary>
        /// <param name="req">The req<see cref="HttpRequest"/>.</param>
        /// <param name="container">The container<see cref="string"/>.</param>
        /// <returns>The <see cref="Task{IActionResult}"/>.</returns>
        [FunctionName("AzureSasURI")]
        [OpenApiOperation("List", "SAS")]
        public async Task<IActionResult> GetSASAccessToken(
          [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "azure/{container}/uri")] HttpRequest req, string container)
        {
            var validateStatus = base.AuthorizationStatus(req);
            if (validateStatus != System.Net.HttpStatusCode.Accepted)
            {
                return new BadRequestObjectResult(validateStatus);
            }

            var response = await _blobService.GetSassUri(container);

            return new OkObjectResult(response);
        }
    }
}
