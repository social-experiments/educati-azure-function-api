namespace goOfflineE.Services
{
    using AutoMapper;
    using goOfflineE.Common.Enums;
    using goOfflineE.Helpers;
    using goOfflineE.Models;
    using goOfflineE.Repository;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Threading.Tasks;
    using System.Web.Http;

    /// <summary>
    /// Defines the <see cref="TenantService" />.
    /// </summary>
    public class TenantService : ITenantService
    {
        /// <summary>
        /// Defines the _tableStorage.
        /// </summary>
        private readonly ITableStorage _tableStorage;

        /// <summary>
        /// Defines the _schoolService.
        /// </summary>
        private readonly ISchoolService _schoolService;

        /// <summary>
        /// Defines the _studentService.
        /// </summary>
        private readonly IStudentService _studentService;

        /// <summary>
        /// Defines the _classService.
        /// </summary>
        private readonly IClassService _classService;

        /// <summary>
        /// Defines the _mapper.
        /// </summary>
        private readonly IMapper _mapper;

        /// <summary>
        /// Defines the _contentService.
        /// </summary>
        private readonly IContentService _contentService;

        /// <summary>
        /// Defines the _assessmentService.
        /// </summary>
        private readonly IAssessmentService _assessmentService;

        /// <summary>
        /// Defines the _settingService.
        /// </summary>
        private readonly ISettingService _settingService;

        /// <summary>
        /// Defines the _profileService.
        /// </summary>
        private readonly IProfileService _profileService;

        /// <summary>
        /// Initializes a new instance of the <see cref="TenantService"/> class.
        /// </summary>
        /// <param name="tableStorage">The tableStorage<see cref="ITableStorage"/>.</param>
        /// <param name="schoolService">The schoolService<see cref="ISchoolService"/>.</param>
        /// <param name="mapper">The mapper<see cref="IMapper"/>.</param>
        /// <param name="studentService">The studentService<see cref="IStudentService"/>.</param>
        /// <param name="classService">The classService<see cref="IClassService"/>.</param>
        /// <param name="assessmentService">The assessmentService<see cref="IAssessmentService"/>.</param>
        /// <param name="contentService">The contentService<see cref="IContentService"/>.</param>
        /// <param name="profileService">The profileService<see cref="IProfileService"/>.</param>
        /// <param name="settingService">The settingService<see cref="ISettingService"/>.</param>
        public TenantService(ITableStorage tableStorage, ISchoolService schoolService,
            IMapper mapper,
            IStudentService studentService,
            IClassService classService,
            IAssessmentService assessmentService,
            IContentService contentService,
            IProfileService profileService,
            ISettingService settingService)
        {
            _tableStorage = tableStorage;
            _schoolService = schoolService;
            _mapper = mapper;
            _studentService = studentService;
            _classService = classService;
            _contentService = contentService;
            _assessmentService = assessmentService;
            _settingService = settingService;
            _profileService = profileService;
        }

        /// <summary>
        /// The CreateUpdate.
        /// </summary>
        /// <param name="tenant">The tenant<see cref="Tenant"/>.</param>
        /// <returns>The <see cref="Task"/>.</returns>
        public async Task CreateUpdate(Tenant tenant)
        {
            var tentants = await _tableStorage.GetAllAsync<Entites.Tenant>("Tenants");
            var tenantData = tentants.FirstOrDefault(t => t.RowKey == tenant.Id);

            if (tenantData is null)
            {
                try
                {
                    var rowKey = Guid.NewGuid().ToString();
                    var tenantDB = new Entites.Tenant(tenant.AccountKey, rowKey)
                    {
                        AccountKey = tenant.AccountKey,
                        AzureWebJobsStorage = tenant.AzureWebJobsStorage,
                        AzureBlobURL = tenant.AzureBlobURL,
                        Name = tenant.Name,
                        Email = tenant.Email,
                        Active = true,
                        ApplicationSettings = tenant.ApplicationSettings,
                        CognitiveServiceEndPoint = tenant.CognitiveServiceEndPoint,
                        CognitiveServiceKey = tenant.CognitiveServiceKey
                    };
                    await _tableStorage.AddAsync("Tenants", tenantDB);
                    await CreateTenantAdminUser(rowKey, tenant.Name, tenant.Email);
                }
                catch (Exception ex)
                {
                    throw new AppException("Tenants Create Error: ", ex.InnerException);
                }
            }
            else
            {
                try
                {
                    tenantData.AccountKey = tenant.AccountKey;
                    tenantData.AzureWebJobsStorage = tenant.AzureWebJobsStorage;
                    tenantData.AzureBlobURL = tenant.AzureBlobURL;
                    tenantData.Name = tenant.Name;
                    tenantData.Email = tenant.Email;
                    tenantData.ApplicationSettings = tenant.ApplicationSettings;
                    tenantData.CognitiveServiceEndPoint = tenant.CognitiveServiceEndPoint;
                    tenantData.CognitiveServiceKey = tenant.CognitiveServiceKey;

                    await _tableStorage.UpdateAsync("Tenants", tenantData);
                }
                catch (Exception ex)
                {
                    throw new AppException("Tenant Update Error: ", ex.InnerException);
                }
            }
        }

        /// <summary>
        /// The Get.
        /// </summary>
        /// <param name="tentantId">The tentantId<see cref="string"/>.</param>
        /// <returns>The <see cref="Task{Tenant}"/>.</returns>
        public async Task<Tenant> Get(string tentantId)
        {
            var tenants = await GetAll();
            return tenants.FirstOrDefault(m => m.Id == tentantId);
        }

        /// <summary>
        /// The GetAll.
        /// </summary>
        /// <returns>The <see cref="Task{IEnumerable{Tenant}}"/>.</returns>
        public async Task<IEnumerable<Tenant>> GetAll()
        {
            var dataTentants = await _tableStorage.GetAllAsync<Entites.Tenant>("Tenants");
            var tenants = dataTentants.ToList();
            return _mapper.Map<List<Tenant>>(tenants);
        }

        /// <summary>
        /// The GetDataResponse.
        /// </summary>
        /// <param name="requestData">The requestData<see cref="DataRequest"/>.</param>
        /// <returns>The <see cref="Task{DataResponse}"/>.</returns>
        public async Task<DataResponse> GetDataResponse(DataRequest requestData)
        {
            var result = new DataResponse();

            if (requestData.Role == Role.Student.ToString())
            {

                var associateMenu = await _settingService.GetMenus("Student");
                var courseContent = await _contentService.GetAll(requestData.SchoolId, requestData.ClassId);
                var school = await _schoolService.Get(requestData.SchoolId);
                var classRoom = await _classService.Get(requestData.ClassId);

                school.ClassRooms.Add(classRoom);

                result.Schools.Add(school);
                result.CourseContent = courseContent.ToList();
                result.AssociateMenu = associateMenu?.Select(menu => menu.Id).ToList();
                return result;

            }
            else
            {
                var schools = requestData.Role == Role.Teacher.ToString() ? await _schoolService.GetAll(requestData.SchoolId) : await _schoolService.GetAll();
                var courseContent = await _contentService.GetAll();
                var assessmentCategories = await _assessmentService.GetAssessmentSubjects();
                var associateMenu = await _settingService.GetMenus(requestData.Role);


                result.Schools = schools.ToList();
                result.CourseContent = courseContent.ToList();
                result.AssessmentCategory = assessmentCategories;
                result.AssociateMenu = associateMenu?.Select(menu => menu.Id).ToList();
                return result;
            }
        }

        /// <summary>
        /// The CreateTenantAdminUser.
        /// </summary>
        /// <param name="tenantId">The tenantId<see cref="string"/>.</param>
        /// <param name="name">The name<see cref="string"/>.</param>
        /// <param name="email">The email<see cref="string"/>.</param>
        /// <returns>The <see cref="Task"/>.</returns>
        private async Task CreateTenantAdminUser(string tenantId, string name, string email)
        {
            var newTenantAdminUser = new RegisterRequest()
            {
                Email = email,
                FirstName = name,
                Role = Role.SuperAdmin.ToString(),
                AcceptTerms = true,
                TenantId = tenantId

            };
            await _profileService.Register(newTenantAdminUser);
        }
    }
}
