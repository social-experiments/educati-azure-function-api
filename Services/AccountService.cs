namespace goOfflineE.Services
{
    using AutoMapper;
    using goOfflineE.Common.Constants;
    using goOfflineE.Common.Enums;
    using goOfflineE.Entites;
    using goOfflineE.Helpers;
    using goOfflineE.Models;
    using goOfflineE.Repository;
    using Microsoft.IdentityModel.Tokens;
    using Microsoft.WindowsAzure.Storage;
    using Microsoft.WindowsAzure.Storage.Table;
    using System;
    using System.Collections.Generic;
    using System.IdentityModel.Tokens.Jwt;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Security.Claims;
    using System.Text;
    using System.Threading.Tasks;
    using System.Web.Http;
    using BC = BCrypt.Net.BCrypt;

    /// <summary>
    /// Defines the <see cref="AccountService" />.
    /// </summary>
    public class AccountService : IAccountService
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
        /// Defines the _emailService.
        /// </summary>
        private readonly IEmailService _emailService;

        /// <summary>
        /// Defines the _settingService.
        /// </summary>
        private readonly ISettingService _settingService;

        /// <summary>
        /// Initializes a new instance of the <see cref="AccountService"/> class.
        /// </summary>
        /// <param name="tableStorage">The tableStorage<see cref="ITableStorage"/>.</param>
        /// <param name="schoolService">The schoolService<see cref="ISchoolService"/>.</param>
        /// <param name="mapper">The mapper<see cref="IMapper"/>.</param>
        /// <param name="studentService">The studentService<see cref="IStudentService"/>.</param>
        /// <param name="classService">The classService<see cref="IClassService"/>.</param>
        /// <param name="assessmentService">The assessmentService<see cref="IAssessmentService"/>.</param>
        /// <param name="contentService">The contentService<see cref="IContentService"/>.</param>
        /// <param name="settingService">The settingService<see cref="ISettingService"/>.</param>
        /// <param name="emailService">The emailService<see cref="IEmailService"/>.</param>
        public AccountService(ITableStorage tableStorage,
            ISchoolService schoolService,
            IMapper mapper,
            IStudentService studentService,
            IClassService classService,
            IAssessmentService assessmentService,
            IContentService contentService,
            ISettingService settingService,
            IEmailService emailService)
        {
            _tableStorage = tableStorage;
            _schoolService = schoolService;
            _mapper = mapper;
            _studentService = studentService;
            _classService = classService;
            _contentService = contentService;
            _assessmentService = assessmentService;
            _emailService = emailService;
            _settingService = settingService;
        }

        /// <summary>
        /// The Authenticate.
        /// </summary>
        /// <param name="model">The model<see cref="AuthenticateRequest"/>.</param>
        /// <returns>The <see cref="Task{AuthenticateResponse}"/>.</returns>
        public async Task<AuthenticateResponse> Authenticate(AuthenticateRequest model)
        {
            // validate
            var users = await _tableStorage.GetAllAsync<User>("User");
            var account = users.SingleOrDefault(user => user.Email.ToLower() == model.Email.ToLower());

            if (account == null || !account.IsVerified || !BC.Verify(model.Password, account.PasswordHash))
            {
                var resp = new HttpResponseMessage(HttpStatusCode.BadRequest)
                {
                    Content = new StringContent(string.Format("Username or password is incorrect!"))
                };
                throw new HttpResponseException(resp);
            }

            var response = new AuthenticateResponse
            {
                Id = account.RowKey,
                Email = account.Email,
                FirstName = account.FirstName,
                LastName = account.LastName,
                Role = account.Role,
                ForceChangePasswordNextLogin = account.ForceChangePasswordNextLogin,
                TenantId = account.TenantId
            };

            // authentication successful so generate jwt
            response.Token = GenerateToken(account.RowKey, account.TenantId);
            if(!String.IsNullOrEmpty(account.TenantId))
            {
                var dataTentants = await _tableStorage.GetAllAsync<Entites.Tenant>("Tenants");
                var tenant = dataTentants.FirstOrDefault(t => t.RowKey == account.TenantId);
                response.Tenant = _mapper.Map<Models.Tenant>(tenant); ;
                CloudStorageAccount storageAccount = CloudStorageAccount.Parse(tenant.AzureWebJobsStorage);
                _tableStorage.Client = storageAccount.CreateCloudTableClient();
            }

            if (account.Role == Role.Teacher.ToString())
            {
                TableQuery<Entites.Teacher> query = new TableQuery<Entites.Teacher>()
                  .Where(TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, account.RowKey));
                var teacherQuery = await _tableStorage.QueryAsync<Entites.Teacher>("Teacher", query);
                var teacher = teacherQuery.SingleOrDefault();
                response.SchoolId = teacher.PartitionKey;
            }
            return response;
        }

        /// <summary>
        /// The VerifyEmail.
        /// </summary>
        /// <param name="email">The email<see cref="string"/>.</param>
        /// <returns>The <see cref="Task{bool}"/>.</returns>
        public async Task<bool> VerifyEmail(string email)
        {
            // validate
            TableQuery<User> query = new TableQuery<User>()
                   .Where(TableQuery.GenerateFilterCondition("Email", QueryComparisons.Equal, email));
            var users = await _tableStorage.QueryAsync<User>("User", query);
            var account = users.SingleOrDefault();

            if (account == null)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// The ForgotPassword.
        /// </summary>
        /// <param name="model">The model<see cref="ForgotPasswordRequest"/>.</param>
        /// <returns>The <see cref="Task"/>.</returns>
        public Task ForgotPassword(ForgotPasswordRequest model)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// The ResetPassword.
        /// </summary>
        /// <param name="model">The model<see cref="ResetPasswordRequest"/>.</param>
        /// <returns>The <see cref="Task"/>.</returns>
        public async Task ResetPassword(ResetPasswordRequest model)
        {
            // validate
            TableQuery<User> query = new TableQuery<User>()
                   .Where(TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, model.UserId));
            var users = await _tableStorage.QueryAsync<User>("User", query);
            var currentUser = users.SingleOrDefault();

            // Validate password and confirmed passwrd are same.
            if (!String.IsNullOrEmpty(model.Password) && !String.IsNullOrEmpty(model.Password))
            {
                if (model.Password != model.ConfirmPassword)
                {
                    var resp = new HttpResponseMessage(HttpStatusCode.NotAcceptable)
                    {
                        Content = new StringContent(string.Format("Password and confirm password are not same!")),

                    };
                    throw new HttpResponseException(resp);
                }
                currentUser.PasswordHash = BC.HashPassword(model.Password);
            }
            else
            {
                var resp = new HttpResponseMessage(HttpStatusCode.NotAcceptable)
                {
                    Content = new StringContent(string.Format("Password and confirm password are required to change!")),

                };
                throw new HttpResponseException(resp);
            }

            currentUser.ForceChangePasswordNextLogin = false;
            currentUser.PasswordReset = DateTime.UtcNow;
            currentUser.Verified = DateTime.UtcNow;
            currentUser.UpdatedBy = model.UserId;

            try
            {
                await _tableStorage.UpdateAsync("User", currentUser);
            }
            catch (Exception ex)
            {
                throw new AppException("User password reset error ", ex.InnerException);
            }
        }

        /// <summary>
        /// The GetAll.
        /// </summary>
        /// <returns>The <see cref="Task{IEnumerable{AccountResponse}}"/>.</returns>
        Task<IEnumerable<AccountResponse>> IAccountService.GetAll()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// The GetById.
        /// </summary>
        /// <param name="id">The id<see cref="string"/>.</param>
        /// <returns>The <see cref="Task{AccountResponse}"/>.</returns>
        public Task<AccountResponse> GetById(string id)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// The Create.
        /// </summary>
        /// <param name="model">The model<see cref="CreateRequest"/>.</param>
        /// <returns>The <see cref="Task{AccountResponse}"/>.</returns>
        public Task<AccountResponse> Create(CreateRequest model)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// The Update.
        /// </summary>
        /// <param name="id">The id<see cref="string"/>.</param>
        /// <param name="model">The model<see cref="UpdateRequest"/>.</param>
        /// <returns>The <see cref="Task{AccountResponse}"/>.</returns>
        public Task<AccountResponse> Update(string id, UpdateRequest model)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// The Delete.
        /// </summary>
        /// <param name="id">The id<see cref="string"/>.</param>
        /// <returns>The <see cref="Task"/>.</returns>
        public Task Delete(string id)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// The GenerateToken.
        /// </summary>
        /// <param name="userId">The userId<see cref="string"/>.</param>
        /// <param name="tenantId">The tenantId<see cref="string"/>.</param>
        /// <returns>The <see cref="string"/>.</returns>
        public string GenerateToken(string userId, string tenantId)
        {
            var mySecurityKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(SettingConfigurations.IssuerToken));
            var tokenHandler = new JwtSecurityTokenHandler();

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new Claim[]
                {
                    new Claim(ClaimTypes.NameIdentifier, userId),
                     new Claim(ClaimTypes.GroupSid, tenantId ?? ""),
                }),
                Expires = DateTime.UtcNow.AddDays(7),
                Issuer = SettingConfigurations.Issuer,
                Audience = SettingConfigurations.Audience,
                SigningCredentials = new SigningCredentials(mySecurityKey, SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        /// <summary>
        /// The Authenticate.
        /// </summary>
        /// <param name="model">The model<see cref="StudentAuthenticateRequest"/>.</param>
        /// <returns>The <see cref="Task{AuthenticateResponse}"/>.</returns>
        public async Task<AuthenticateResponse> Authenticate(StudentAuthenticateRequest model)
        {
            // validate
            var students = await _tableStorage.GetAllAsync<Student>("Student");
            var student = students.SingleOrDefault(stud => stud.EnrolmentNo != null && stud.EnrolmentNo.ToLower() == model.EnrolmentNo.ToLower());

            if (student == null)
            {
                var resp = new HttpResponseMessage(HttpStatusCode.BadRequest)
                {
                    Content = new StringContent(string.Format("Invalid Enrolment Number!"))
                };
                throw new HttpResponseException(resp);
            }

            // authentication successful so generate jwt
            var jwtToken = GenerateToken(student.RowKey, student.TenantId);

            var studentRes = new StudentResponse
            {
                Id = student.RowKey,
                FirstName = student.FirstName,
                LastName = student.LastName,
                EnrolmentNo = student.EnrolmentNo,
                Address1 = student.Address1,
                Address2 = student.Address2,
                Country = student.Country,
                State = student.State,
                City = student.City,
                Zip = student.Zip,
                SchoolId = student.PartitionKey,
                ClassId = student.ClassId,
                ProfileStoragePath = student.ProfileStoragePath,
                TrainStudentModel = student.TrainStudentModel,
                Gender = student.Gender
            };

            var associateMenu = await _settingService.GetMenus("Student");
            var courseContent = await _contentService.GetAll(student.PartitionKey, student.ClassId);
            var school = await _schoolService.Get(student.PartitionKey);
            var classRoom = await _classService.Get(student.ClassId);

            classRoom.Students.Add(studentRes);
            school.ClassRooms.Add(classRoom);

            var response = new AuthenticateResponse
            {
                Id = student.RowKey,
                EnrolmentNo = student.EnrolmentNo,
                FirstName = student.FirstName,
                LastName = student.LastName,
                Email = student.EnrolmentNo,
                SchoolId = school.Id,
                Role = Role.Student.ToString(),
                Token = jwtToken,
                TenantId = student.TenantId
            };

            return response;
        }

        /// <summary>
        /// The SyncData.
        /// </summary>
        /// <param name="schoolId">The schoolId<see cref="string"/>.</param>
        /// <param name="classId">The classId<see cref="string"/>.</param>
        /// <returns>The <see cref="Task{AuthenticateResponse}"/>.</returns>
        public Task<AuthenticateResponse> SyncData(string schoolId = null, string classId = null)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// The RefreshPushNotificationToken.
        /// </summary>
        /// <param name="token">The token<see cref="PushNotificationToken"/>.</param>
        /// <returns>The <see cref="Task"/>.</returns>
        public async Task RefreshPushNotificationToken(PushNotificationToken token)
        {
            try
            {
                if (token.Role == Role.Student.ToString())
                {
                    var students = await _tableStorage.GetAllAsync<Student>("Student");
                    var student = students.SingleOrDefault(s => s.RowKey == token.Id);

                    student.NotificationToken = token.RefreshToken;
                    student.UpdatedOn = DateTime.UtcNow;
                    student.UpdatedBy = token.Id;

                    await _tableStorage.UpdateAsync("Student", student);
                }
                else
                {
                    var users = await _tableStorage.GetAllAsync<User>("User");
                    var user = users.SingleOrDefault(u => u.RowKey == token.Id);

                    user.NotificationToken = token.RefreshToken;
                    user.UpdatedOn = DateTime.UtcNow;
                    user.UpdatedBy = token.Id;

                    await _tableStorage.UpdateAsync("User", user);
                }
            }
            catch (Exception ex)
            {
                throw new AppException("Error: Refresh push notification token ", ex.InnerException);
            }
        }

        /// <summary>
        /// The NonProfitAccountRegistration.
        /// </summary>
        /// <param name="nonProfitAccount">The nonProfitAccount<see cref="Models.NonProfitAccount"/>.</param>
        /// <returns>The <see cref="Task"/>.</returns>
        public async Task NonProfitAccountRegistration(Models.NonProfitAccount nonProfitAccount)
        {
            var id = String.IsNullOrEmpty(nonProfitAccount.Id) ? Guid.NewGuid().ToString() : nonProfitAccount.Id;
            var account = new Entites.NonProfitAccount(nonProfitAccount.RegistrationNo, id)
            {
                Address = nonProfitAccount.Address,
                Email = nonProfitAccount.Email,
                Location = nonProfitAccount.Location,
                NameOfNGO = nonProfitAccount.NameOfNGO,
                OperationalLocations = nonProfitAccount.OperationalLocations,
                PhoneNo = nonProfitAccount.PhoneNo,
                RegistrationNo = nonProfitAccount.RegistrationNo,
                TaxRegistrationNo = nonProfitAccount.TaxRegistrationNo,
                Active = false,
                CreatedBy = "system",
                UpdatedOn = DateTime.UtcNow,
                UpdatedBy = "system",
            };

            try
            {
                account.OTP = await GenerateTOPAndSend(account.Email);
                account.OTPDate = DateTime.UtcNow;
                await _tableStorage.AddAsync("NonProfitAccount", account);


            }
            catch (Exception ex)
            {
                throw new AppException("Create non profit account error: ", ex.InnerException);
            }
        }

        /// <summary>
        /// The GenerateTOPAndSend.
        /// </summary>
        /// <param name="emailId">The emailId<see cref="string"/>.</param>
        /// <returns>The <see cref="Task{string}"/>.</returns>


        /// <summary>
        /// The OTPNonProfitVerification.
        /// </summary>
        /// <param name="nonProfitAccount">The nonProfitAccount<see cref="Models.NonProfitAccount"/>.</param>
        /// <returns>The <see cref="Task{string}"/>.</returns>
        public async Task<bool> OTPNonProfitVerification(Models.NonProfitAccount nonProfitAccount)
        {
            var accounts = await _tableStorage.GetAllAsync<Entites.NonProfitAccount>("NonProfitAccount");
            var account = accounts.SingleOrDefault(s => s.RegistrationNo == nonProfitAccount.RegistrationNo && s.Email == nonProfitAccount.Email);
            if (account.OTP == nonProfitAccount.OTP)
            {
                try
                {
                    account.OTP = string.Empty;
                    account.OTPDate = DateTime.UtcNow;
                    await NewNonProfirAccountNotificationEmail(nonProfitAccount);
                    await _tableStorage.UpdateAsync("NonProfitAccount", account);
                    return true;
                }
                catch (Exception ex)
                {
                    throw new AppException("OTP verification non profit account error: ", ex.InnerException);
                }
            }

            return false;
        }

        /// <summary>
        /// The GenerateTOPAndSend.
        /// </summary>
        /// <param name="emailId">The emailId<see cref="string"/>.</param>
        /// <returns>The <see cref="Task{string}"/>.</returns>
        private async Task<string> GenerateTOPAndSend(string emailId)
        {
            var OTP = new Random().Next(0, 1000000).ToString("D6");
            StringBuilder emailBody = new StringBuilder();
            emailBody.AppendFormat("<p>Hi, <br/>", Environment.NewLine);
            emailBody.AppendFormat($"Your new non profit account verification code is: {OTP} </b></p>", Environment.NewLine);
            EmailRequest emailRequest = new EmailRequest
            {
                To = new List<string>
            {
               emailId
            },
                Subject = "Non profit account verification code!",
                HtmlContent = emailBody.ToString()
            };

            try
            {
                await _emailService.SendAsync(emailRequest);
            }
            catch (Exception ex)
            {

                throw new AppException("Create non profit account otp send error: ", ex.InnerException);
            }

            return OTP;
        }

        /// <summary>
        /// The NewTeacherNotificationEmail.
        /// </summary>
        /// <param name="nonProfitAccount">The nonProfitAccount<see cref="Models.NonProfitAccount"/>.</param>
        /// <returns>The <see cref="Task"/>.</returns>
        private async Task NewNonProfirAccountNotificationEmail(Models.NonProfitAccount nonProfitAccount)
        {
            StringBuilder emailBody = new StringBuilder();
            emailBody.AppendFormat("<p>Hi, <br/>", Environment.NewLine);
            emailBody.AppendFormat("Congratulations! <br/>", Environment.NewLine);
            emailBody.AppendFormat("New Non profit account, <br/>", Environment.NewLine);
            emailBody.AppendFormat($"Name Of NGO: <b>{nonProfitAccount.NameOfNGO} </b> <br/>", Environment.NewLine);
            emailBody.AppendFormat($"Registration No: <b>{nonProfitAccount.RegistrationNo} </b> <br/>", Environment.NewLine);
            emailBody.AppendFormat($"Email: <b>{nonProfitAccount.Email} </b> <br/>", Environment.NewLine);
            emailBody.AppendFormat($"Address: <b>{nonProfitAccount.Address} </b> <br/>", Environment.NewLine);
            emailBody.AppendFormat($"Phone No: <b>{nonProfitAccount.PhoneNo} </b> <br/>", Environment.NewLine);
            emailBody.AppendFormat($"Location: <b>{nonProfitAccount.Location} </b> <br/>", Environment.NewLine);
            emailBody.AppendFormat($"Operational Locations: <b>{nonProfitAccount.OperationalLocations} </b> <br/>", Environment.NewLine);
            emailBody.AppendFormat($"Tax Registration No:<b> {nonProfitAccount.TaxRegistrationNo} </b></p>", Environment.NewLine);

            var emails = SettingConfigurations.NonProfitAccountEmails.Split(';').ToList();

            EmailRequest emailRequest = new EmailRequest
            {
                To = emails,
                Subject = "New Non-Profit Account Registration!",
                HtmlContent = emailBody.ToString()
            };

            try
            {
                await _emailService.SendAsync(emailRequest);
            }
            catch (Exception ex)
            {

                throw new AppException("New Non-Profit account notification Email error: ", ex.InnerException);
            }
        }
    }
}
