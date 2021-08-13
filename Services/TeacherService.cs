namespace goOfflineE.Services
{
    using AutoMapper;
    using goOfflineE.Common.Constants;
    using goOfflineE.Helpers;
    using goOfflineE.Models;
    using goOfflineE.Repository;
    using Microsoft.WindowsAzure.Storage;
    using Microsoft.WindowsAzure.Storage.Table;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    /// <summary>
    /// Defines the <see cref="TeacherService" />.
    /// </summary>
    public class TeacherService : ITeacherService
    {
        /// <summary>
        /// Defines the _tableStorage.
        /// </summary>
        private readonly ITableStorage _tableStorage;

        /// <summary>
        /// Defines the _mapper.
        /// </summary>
        private readonly IMapper _mapper;

        /// <summary>
        /// Defines the _profileService.
        /// </summary>
        private readonly IProfileService _profileService;

        /// <summary>
        /// Defines the _emailService.
        /// </summary>
        private readonly IEmailService _emailService;

        /// <summary>
        /// Initializes a new instance of the <see cref="TeacherService"/> class.
        /// </summary>
        /// <param name="emailService">The emailService<see cref="IEmailService"/>.</param>
        /// <param name="tableStorage">The tableStorage<see cref="ITableStorage"/>.</param>
        /// <param name="mapper">The mapper<see cref="IMapper"/>.</param>
        /// <param name="profileService">The profileService<see cref="IProfileService"/>.</param>
        public TeacherService(IEmailService emailService, ITableStorage tableStorage, IMapper mapper, IProfileService profileService)
        {
            _tableStorage = tableStorage;
            _mapper = mapper;
            _profileService = profileService;
            _emailService = emailService;
        }

        /// <summary>
        /// The CreateUpdate.
        /// </summary>
        /// <param name="model">The model<see cref="TeacherRequest"/>.</param>
        /// <returns>The <see cref="Task"/>.</returns>
        public async Task CreateUpdate(TeacherRequest model)
        {

            TableQuery<Entites.Teacher> query = new TableQuery<Entites.Teacher>()
                  .Where(TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, model.Id));
            var teacherQuery = await _tableStorage.QueryAsync<Entites.Teacher>("Teacher", query);
            var teacher = teacherQuery.SingleOrDefault();

            if (teacher != null)
            {
                // Update profile
                ProfileUpdateRequest profileUpdateRequest = new ProfileUpdateRequest
                {
                    Email = model.Email,
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    Id = model.Id
                };

                await _profileService.UpdateProfile(profileUpdateRequest);

                //Update teacher information
                teacher.Gender = model.Gender;
                teacher.Address1 = model.Address1;
                teacher.Address2 = model.Address2;
                teacher.Country = model.Country;
                teacher.State = model.State;
                teacher.City = model.City;
                teacher.Zip = model.Zip;
                teacher.Latitude = model.Latitude;
                teacher.Longitude = model.Longitude;
                teacher.Active = true;
                teacher.CreatedBy = model.CreatedBy;
                teacher.UpdatedOn = DateTime.UtcNow;
                teacher.UpdatedBy = model.CreatedBy;

                try
                {
                    await _tableStorage.UpdateAsync("Teacher", teacher);
                }
                catch (Exception ex)
                {
                    throw new AppException("update teacher error: ", ex.InnerException);
                }
            }
            else
            {
                // Register user as teacher
                var userId = String.IsNullOrEmpty(model.Id) ? Guid.NewGuid().ToString() : model.Id;
                var defaultPasswrod = "teacher@123"; //SettingConfigurations.GetRandomPassword(10);
                RegisterRequest registerRequest = new RegisterRequest
                {
                    Email = model.Email,
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    Role = model.Role,
                    AcceptTerms = true,
                    Id = userId,
                    SchoolId = model.SchoolId,
                    Password = defaultPasswrod,
                    ConfirmPassword = defaultPasswrod
                };

                var newTeacher = new Entites.Teacher(model.SchoolId, userId)
                {
                    Address1 = model.Address1,
                    Address2 = model.Address2,
                    Country = model.Country,
                    State = model.State,
                    City = model.City,
                    Zip = model.Zip,
                    Latitude = model.Latitude,
                    Longitude = model.Longitude,
                    Active = true,
                    Gender = model.Gender,
                    CreatedBy = model.CreatedBy,
                    UpdatedOn = DateTime.UtcNow,
                    UpdatedBy = model.CreatedBy,
                };
                try
                {

                    await _tableStorage.AddAsync("Teacher", newTeacher);

                    if (!String.IsNullOrEmpty(model.TenantId))
                    {
                        CloudStorageAccount storageAccount = CloudStorageAccount.Parse(SettingConfigurations.AzureWebJobsStorage);
                        _tableStorage.Client = storageAccount.CreateCloudTableClient();
                        await _profileService.Register(registerRequest);
                    }
                    await NewTeacherNotificationEmail(registerRequest);
                }
                catch (Exception ex)
                {
                    throw new AppException("Create teacher error: ", ex.InnerException);
                }
            }
        }

        /// <summary>
        /// The Delete.
        /// </summary>
        /// <param name="teacherId">The teacherId<see cref="string"/>.</param>
        /// <returns>The <see cref="Task"/>.</returns>
        public async Task Delete(string teacherId)
        {
            TableQuery<Entites.Teacher> teacherQuery = new TableQuery<Entites.Teacher>()
                  .Where(TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, teacherId));
            var teachers = await _tableStorage.QueryAsync<Entites.Teacher>("Teacher", teacherQuery);

            var teacher = teachers.FirstOrDefault();

            teacher.Active = false;

            try
            {
                await _tableStorage.UpdateAsync("Teacher", teacher);
            }
            catch (Exception ex)
            {
                throw new AppException("update teacher error: ", ex.InnerException);
            }
        }

        /// <summary>
        /// The GetAll.
        /// </summary>
        /// <param name="schoolId">The schoolId<see cref="string"/>.</param>
        /// <returns>The <see cref="Task{IEnumerable{TeacherResponse}}"/>.</returns>
        public async Task<IEnumerable<TeacherResponse>> GetAll(string schoolId)
        {
            TableQuery<Entites.User> userQuery = new TableQuery<Entites.User>()
                  .Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, schoolId));
            var users = await _tableStorage.QueryAsync<Entites.User>("User", userQuery);

            TableQuery<Entites.Teacher> teacherQuery = new TableQuery<Entites.Teacher>()
                  .Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, schoolId));
            var teachers = await _tableStorage.QueryAsync<Entites.Teacher>("Teacher", teacherQuery);
            var teachersList = from user in users
                               join teacher in teachers
                                    on user.RowKey equals teacher.RowKey
                               where teacher.Active.GetValueOrDefault(false)
                               orderby teacher.UpdatedOn descending
                               select new TeacherResponse
                               {
                                   Id = user.RowKey,
                                   FirstName = user.FirstName,
                                   LastName = user.LastName,
                                   Email = user.Email,
                                   Address1 = teacher.Address1,
                                   Address2 = teacher.Address2,
                                   Country = teacher.Country,
                                   State = teacher.State,
                                   City = teacher.City,
                                   Zip = teacher.Zip,
                                   Gender = teacher.Gender,
                                   SchoolId = teacher.PartitionKey
                               };

            return teachersList;
        }

        /// <summary>
        /// The NewTeacherNotificationEmail.
        /// </summary>
        /// <param name="registerRequest">The registerRequest<see cref="RegisterRequest"/>.</param>
        /// <returns>The <see cref="Task"/>.</returns>
        private async Task NewTeacherNotificationEmail(RegisterRequest registerRequest)
        {
            StringBuilder emailBody = new StringBuilder();
            emailBody.AppendFormat("<p>Hi, <br/>", Environment.NewLine);
            emailBody.AppendFormat("Congratulations! <br/>", Environment.NewLine);
            emailBody.AppendFormat("You have registerd with portal! Below are the login credentials <br/>", Environment.NewLine);
            emailBody.AppendFormat($"User Name: <b>{registerRequest.Email} </b> <br/>", Environment.NewLine);
            emailBody.AppendFormat($"Password:<b> {registerRequest.Password} </b></p>", Environment.NewLine);
            emailBody.AppendFormat($"<p>Click here to login: <a href=${SettingConfigurations.WebSiteUrl}>${SettingConfigurations.WebSiteUrl}</a> </p>");
            EmailRequest emailRequest = new EmailRequest
            {
                To = new List<string>
            {
                registerRequest.Email
            },
                Subject = "Your registration was successful!",
                HtmlContent = emailBody.ToString()
            };
            await _emailService.SendAsync(emailRequest);
        }
    }
}
