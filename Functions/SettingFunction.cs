namespace goOfflineE.Functions
{
    using AzureFunctions.Extensions.Swashbuckle.Attribute;
    using goOfflineE.Helpers.Attributes;
    using goOfflineE.Models;
    using goOfflineE.Services;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Azure.WebJobs.Extensions.Http;
    using Newtonsoft.Json;
    using System.Collections.Generic;
    using System.IO;
    using System.Net;
    using System.Threading.Tasks;
    using System.Web.Http;

    /// <summary>
    /// Defines the <see cref="SettingFunction" />.
    /// </summary>
    public class SettingFunction : AuthenticationFilter
    {
        /// <summary>
        /// Defines the _settingService.
        /// </summary>
        private readonly ISettingService _settingService;

        /// <summary>
        /// Initializes a new instance of the <see cref="SettingFunction"/> class.
        /// </summary>
        /// <param name="settingService">The settingService<see cref="ISettingService"/>.</param>
        public SettingFunction(ISettingService settingService)
        {
            _settingService = settingService;
        }

        /// <summary>
        /// The AssociateMenu.
        /// </summary>
        /// <param name="request">The request<see cref="HttpRequest"/>.</param>
        /// <param name="roleName">The roleName<see cref="string"/>.</param>
        /// <returns>The <see cref="Task{IActionResult}"/>.</returns>
        [FunctionName("AssociateMenu")]
        public async Task<IActionResult> AssociateMenu(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post",  Route = "setting/menu/{roleName}")]
            [RequestBodyType(typeof(List<AssociateMenu>), "Update associate menu request")] HttpRequest request, string roleName)
        {
            var validateStatus = base.AuthorizationStatus(request);
            if (validateStatus != HttpStatusCode.Accepted)
            {
                return new BadRequestObjectResult(validateStatus);
            }

            string requestBody = await new StreamReader(request.Body).ReadToEndAsync();
            List<AssociateMenu> requestData = JsonConvert.DeserializeObject<List<AssociateMenu>>(requestBody);

            try
            {
                await _settingService.UpdateMenus(roleName, requestData);
            }
            catch (HttpResponseException ex)
            {

                return new BadRequestObjectResult(ex);

            }
            return new OkObjectResult(new { message = "Associate menu update successfully." });
        }

        /// <summary>
        /// The ApplicationSetting.
        /// </summary>
        /// <param name="request">The request<see cref="HttpRequest"/>.</param>
        /// <returns>The <see cref="Task{IActionResult}"/>.</returns>
        [FunctionName("ApplicationSetting")]
        public async Task<IActionResult> ApplicationSetting(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post",  Route = "setting/application")]
            [RequestBodyType(typeof(ApplicationSetting), "Application settings")] HttpRequest request)
        {
            var validateStatus = base.AuthorizationStatus(request);
            if (validateStatus != HttpStatusCode.Accepted)
            {
                return new BadRequestObjectResult(validateStatus);
            }

            string requestBody = await new StreamReader(request.Body).ReadToEndAsync();
            ApplicationSetting requestData = JsonConvert.DeserializeObject<ApplicationSetting>(requestBody);

            try
            {
                await _settingService.SaveApplicationSetting(requestData);
            }
            catch (HttpResponseException ex)
            {

                return new BadRequestObjectResult(ex);

            }
            return new OkObjectResult(new { message = "Application setting update successfully." });
        }

        [FunctionName("ApplicationSettingGet")]
        public async Task<IActionResult> GetApplicationSetting(
           [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "setting/application")] HttpRequest request)
        {
            var validateStatus = base.AuthorizationStatus(request);
            if (validateStatus != System.Net.HttpStatusCode.Accepted)
            {
                return new BadRequestObjectResult(validateStatus);
            }

            var response = await _settingService.GetApplicationSetting();

            return new OkObjectResult(response);
        }
    }
}
