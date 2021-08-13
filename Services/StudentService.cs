namespace goOfflineE.Services
{
    using AutoMapper;
    using goOfflineE.Common.Constants;
    using goOfflineE.Common.Enums;
    using goOfflineE.Helpers;
    using goOfflineE.Models;
    using goOfflineE.Repository;
    using Microsoft.WindowsAzure.Storage;
    using Microsoft.WindowsAzure.Storage.Table;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    /// <summary>
    /// Defines the <see cref="StudentService" />.
    /// </summary>
    public class StudentService : IStudentService
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
        /// Defines the _emailService.
        /// </summary>
        private readonly IEmailService _emailService;

        /// <summary>
        /// Defines the _azureBlobService.
        /// </summary>
        private readonly IAzureBlobService _azureBlobService;

        /// <summary>
        /// Initializes a new instance of the <see cref="StudentService"/> class.
        /// </summary>
        /// <param name="emailService">The emailService<see cref="IEmailService"/>.</param>
        /// <param name="tableStorage">The tableStorage<see cref="ITableStorage"/>.</param>
        /// <param name="mapper">The mapper<see cref="IMapper"/>.</param>
        /// <param name="azureBlobService">The azureBlobService<see cref="IAzureBlobService"/>.</param>
        public StudentService(IEmailService emailService, ITableStorage tableStorage, IMapper mapper, IAzureBlobService azureBlobService)
        {
            _tableStorage = tableStorage;
            _mapper = mapper;
            _emailService = emailService;
            _azureBlobService = azureBlobService;
        }

        /// <summary>
        /// The CreateUpdate.
        /// </summary>
        /// <param name="model">The model<see cref="StudentRequest"/>.</param>
        /// <returns>The <see cref="Task"/>.</returns>
        public async Task CreateUpdate(StudentRequest model)
        {
            TableQuery<Entites.Student> query = new TableQuery<Entites.Student>()
                 .Where(TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, model.Id));
            var studentQuery = await _tableStorage.QueryAsync<Entites.Student>("Student", query);
            var student = studentQuery.SingleOrDefault();

            if (student != null)
            {

                //Update student information
                student.EnrolmentNo = model.EnrolmentNo;
                student.FirstName = model.FirstName;
                student.LastName = model.LastName;
                student.Gender = model.Gender;
                student.ClassId = model.ClassId;
                student.Address1 = model.Address1;
                student.Address2 = model.Address2;
                student.Country = model.Country;
                student.State = model.State;
                student.City = model.City;
                student.Zip = model.Zip;
                student.Latitude = model.Latitude;
                student.Longitude = model.Longitude;
                student.Active = true;
                student.CreatedBy = model.CreatedBy;
                student.UpdatedOn = DateTime.UtcNow;
                student.UpdatedBy = model.CreatedBy;

                try
                {
                    await _tableStorage.UpdateAsync("Student", student);
                }
                catch (Exception ex)
                {
                    throw new AppException("update student error: ", ex.InnerException);
                }
            }
            else
            {
                var studentId = String.IsNullOrEmpty(model.Id) ? Guid.NewGuid().ToString() : model.Id;

                var newStudent = new Entites.Student(model.SchoolId, studentId)
                {

                    TenantId = model.TenantId,
                    EnrolmentNo = model.EnrolmentNo,
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    Gender = model.Gender,
                    ClassId = model.ClassId,
                    Address1 = model.Address1,
                    Address2 = model.Address2,
                    Country = model.Country,
                    State = model.State,
                    City = model.City,
                    Zip = model.Zip,
                    Latitude = model.Latitude,
                    Longitude = model.Longitude,
                    Active = true,
                    CreatedBy = model.CreatedBy,
                    UpdatedOn = DateTime.UtcNow,
                    UpdatedBy = model.CreatedBy,
                };
                try
                {
                    await _tableStorage.AddAsync("Student", newStudent);

                    if (!String.IsNullOrEmpty(model.TenantId))
                    {
                        CloudStorageAccount storageAccount = CloudStorageAccount.Parse(SettingConfigurations.AzureWebJobsStorage);
                        _tableStorage.Client = storageAccount.CreateCloudTableClient();

                        var userId = Guid.NewGuid().ToString();
                        var newUser = new Entites.User(model.SchoolId, userId)
                        {
                            EnrolmentNo = model.EnrolmentNo,
                            TenantId = model.TenantId,
                            Role = Role.Student.ToString(),
                            Active = true,
                            Verified = DateTime.UtcNow,
                            CreatedBy = userId,
                            UpdatedOn = DateTime.UtcNow,
                            UpdatedBy = userId
                        };

                        await _tableStorage.AddAsync("User", newUser);
                    }
                }
                catch (Exception ex)
                {
                    throw new AppException("Create student error: ", ex.InnerException);
                }
            }
        }

        /// <summary>
        /// The Delete.
        /// </summary>
        /// <param name="studentId">The studentId<see cref="string"/>.</param>
        /// <returns>The <see cref="Task"/>.</returns>
        public async Task Delete(string studentId)
        {
            TableQuery<Entites.Student> studentQuery = new TableQuery<Entites.Student>()
                  .Where(TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, studentId));
            var students = await _tableStorage.QueryAsync<Entites.Student>("Student", studentQuery);

            var student = students.FirstOrDefault();

            student.Active = false;

            try
            {
                await _tableStorage.UpdateAsync("Student", student);
            }
            catch (Exception ex)
            {
                throw new AppException("update student error: ", ex.InnerException);
            }
        }

        /// <summary>
        /// The GetAll.
        /// </summary>
        /// <param name="schoolId">The schoolId<see cref="string"/>.</param>
        /// <param name="classId">The classId<see cref="string"/>.</param>
        /// <returns>The <see cref="Task{IEnumerable{StudentResponse}}"/>.</returns>
        public async Task<IEnumerable<StudentResponse>> GetAll(string schoolId, string classId)
        {
            var filterString = TableQuery.CombineFilters(
                                TableQuery.GenerateFilterCondition("PartitionKey",
                                    QueryComparisons.Equal,
                                    schoolId),
                                TableOperators.And,
                                TableQuery.GenerateFilterCondition("ClassId",
                                    QueryComparisons.Equal,
                                    classId)
                                );

            TableQuery<Entites.Student> studentQuery = new TableQuery<Entites.Student>().Where(filterString);

            var students = await _tableStorage.QueryAsync<Entites.Student>("Student", studentQuery);

            var studentList = from student in students
                              where student.Active.GetValueOrDefault(false)
                              orderby student.UpdatedOn descending
                              select new StudentResponse
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
                                  Gender = student.Gender,
                                  ProfileStoragePath = student.ProfileStoragePath,
                                  TrainStudentModel = student.TrainStudentModel
                              };

            return studentList;
        }

        /// <summary>
        /// The GetAll.
        /// </summary>
        /// <returns>The <see cref="Task{IEnumerable{StudentResponse}}"/>.</returns>
        public async Task<IEnumerable<StudentResponse>> GetAll()
        {
            var students = await _tableStorage.GetAllAsync<Entites.Student>("Student");

            var studentList = from student in students
                              where student.Active.GetValueOrDefault(false)
                              orderby student.UpdatedOn descending
                              select new StudentResponse
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
                                  Gender = student.Gender,
                                  ProfileStoragePath = student.ProfileStoragePath,
                                  TrainStudentModel = student.TrainStudentModel,
                                  Timestamp = student.Timestamp.DateTime,
                                  UpdatedOn = student.UpdatedOn
                              };

            return studentList;
        }

        /// <summary>
        /// The UpdateStatusToTrainStudentModel.
        /// </summary>
        /// <param name="studentId">The studentId<see cref="string"/>.</param>
        /// <param name="studentPhotos">The studentPhotos<see cref="List{string}"/>.</param>
        /// <returns>The <see cref="Task"/>.</returns>
        public async Task UpdateStudentProfile(string studentId, List<string> studentPhotos)
        {
            TableQuery<Entites.Student> query = new TableQuery<Entites.Student>()
                 .Where(TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, studentId));
            var studentQuery = await _tableStorage.QueryAsync<Entites.Student>("Student", query);
            var student = studentQuery.SingleOrDefault();
            student.TrainStudentModel = true;


            if (student.ProfileStoragePath != null)
            {
                var existingPhotURLs = JsonConvert.DeserializeObject<StudentPhotos>(student.ProfileStoragePath);
                studentPhotos = studentPhotos.Union(existingPhotURLs.Photos).ToList();
            }

            var studentPhotoBlobURLs = JsonConvert.SerializeObject(new StudentPhotos { Photos = studentPhotos });

            student.ProfileStoragePath = studentPhotoBlobURLs;

            try
            {
                await _tableStorage.UpdateAsync("Student", student);
            }
            catch (Exception ex)
            {
                throw new AppException("UpdateStatusToTrainStudentModel error: ", ex.InnerException);
            }
        }
    }
}
