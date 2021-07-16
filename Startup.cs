using goOfflineE;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;

[assembly: FunctionsStartup(typeof(Startup))]
namespace goOfflineE
{
    using AutoMapper;
    using AzureFunctions.Extensions.Swashbuckle;
    using goOfflineE.Common.Constants;
    using goOfflineE.Helpers;
    using goOfflineE.Repository;
    using goOfflineE.Services;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Azure.CognitiveServices.Vision.Face;
    using Microsoft.Azure.Functions.Extensions.DependencyInjection;
    using Microsoft.Extensions.DependencyInjection;
    using System.Reflection;

    /// <summary>
    /// Defines the <see cref="Startup" />.
    /// </summary>
    public class Startup : FunctionsStartup
    {
        /// <summary>
        /// The Configure.
        /// </summary>
        /// <param name="builder">The builder<see cref="IFunctionsHostBuilder"/>.</param>
        public override void Configure(IFunctionsHostBuilder builder)
        {

            //This is to generate the Default UI of Swagger Documentation  
            builder.AddSwashBuckle(Assembly.GetExecutingAssembly(), option =>
            {
                option.ConfigureSwaggerGen = new System.Action<Swashbuckle.AspNetCore.SwaggerGen.SwaggerGenOptions>(swagger =>
                {
                    swagger.OperationFilter<AddRequiredHeaderParameter>();
                    SwaggerUIConfigurations.SwaggerUIConfig(swagger);
                });
            });
            builder.Services.AddHttpContextAccessor();
            ConfigureServices(builder.Services);
        }

        /// <summary>
        /// The ConfigureServices.
        /// </summary>
        /// <param name="services">The services<see cref="IServiceCollection"/>.</param>
        private void ConfigureServices(IServiceCollection services)
        {
            services.AddAutoMapper(typeof(Startup));
            services.AddTransient<IHttpContextAccessor, HttpContextAccessor>();

            services.AddTransient<IAccountService, AccountService>();
            services.AddTransient<ISchoolService, SchoolService>();
            services.AddTransient<IProfileService, ProfileService>();
            services.AddTransient<ITeacherService, TeacherService>();
            services.AddTransient<IEmailService, EmailService>();
            services.AddTransient<IPowerBIService, PowerBIService>();
            services.AddTransient<IClassService, ClassService>();
            services.AddTransient<IStudentService, StudentService>();
            services.AddTransient<IAzureBlobService, AzureBlobService>(); 
            services.AddTransient<ICognitiveService, CognitiveService>();
            services.AddTransient<IContentService, ContentService>();
            services.AddTransient<IAssignmentService, AssignmentService>();
            services.AddTransient<IAssessmentService, AssessmentService>();
            services.AddTransient<IPushNotificationService, PushNotificationService>();
            services.AddTransient<ISettingService, SettingService>();
            services.AddTransient<ITenantService, TenantService>();

            services.AddScoped<ITableStorage, AzureTableStorage>();
            services.AddSingleton<IFaceClient, FaceClient>(s => new FaceClient(new ApiKeyServiceClientCredentials(SettingConfigurations.CognitiveServiceKey),
            new System.Net.Http.DelegatingHandler[] { }));
        }
    }
}
