namespace goOfflineE.Services
{
    using goOfflineE.Common.Constants;
    using goOfflineE.Entites;
    using goOfflineE.Helpers;
    using goOfflineE.Models;
    using goOfflineE.Repository;
    using Microsoft.Azure.CognitiveServices.Vision.Face;
    using Microsoft.Azure.CognitiveServices.Vision.Face.Models;
    using Microsoft.Extensions.Logging;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using ILogger = Microsoft.Extensions.Logging.ILogger;

    /// <summary>
    /// Defines the <see cref="CognitiveService" />.
    /// </summary>
    public class CognitiveService : ICognitiveService
    {
        /// <summary>
        /// Defines the _faceClient.
        /// </summary>
        private readonly IFaceClient _faceClient;

        /// <summary>
        /// Defines the _azureBlobService.
        /// </summary>
        private readonly IAzureBlobService _azureBlobService;

        /// <summary>
        /// Defines the _tableStorage.
        /// </summary>
        private readonly ITableStorage _tableStorage;

        /// <summary>
        /// Defines the _studentService.
        /// </summary>
        private readonly IStudentService _studentService;

        /// <summary>
        /// Initializes a new instance of the <see cref="CognitiveService"/> class.
        /// </summary>
        /// <param name="faceClient">The faceClient<see cref="IFaceClient"/>.</param>
        /// <param name="azureBlobService">The azureBlobService<see cref="IAzureBlobService"/>.</param>
        /// <param name="tableStorage">The tableStorage<see cref="ITableStorage"/>.</param>
        /// <param name="studentService">The studentService<see cref="IStudentService"/>.</param>
        public CognitiveService(IFaceClient faceClient, IAzureBlobService azureBlobService, ITableStorage tableStorage, IStudentService studentService)
        {
            _faceClient = faceClient;
            _azureBlobService = azureBlobService;
            _tableStorage = tableStorage;
            _studentService = studentService;

            if (Uri.IsWellFormedUriString(SettingConfigurations.CognitiveServiceEndPoint, UriKind.Absolute))
            {
                faceClient.Endpoint = SettingConfigurations.CognitiveServiceEndPoint;
            }
        }

        /// <summary>
        /// The TrainStudentModel.
        /// </summary>
        /// <param name="queueDataMessage">The queueDataMessage<see cref="QueueDataMessage"/>.</param>
        /// <param name="_log">The _log<see cref="ILogger"/>.</param>
        /// <returns>The <see cref="Task"/>.</returns>
        public async Task TrainStudentModel(QueueDataMessage queueDataMessage, ILogger _log)
        {
            //Create school group
            _log.LogInformation($"TrainStudentModel: {queueDataMessage.StudentId}");
            CreateGroup(queueDataMessage.SchoolId, _log);

            var pictureURLs = new List<string>();

            try
            {

                // Define school specific student
                _log.LogInformation($"Define school specific student");
                Person student = await _faceClient.PersonGroupPerson.CreateAsync(
                    // Id of the PersonGroup that the person belonged to
                    queueDataMessage.SchoolId,
                    // studentId
                    queueDataMessage.StudentId
                ).ConfigureAwait(false);

                BlobStorageRequest blobStorage = await _azureBlobService.GetSassUri("students");

                _log.LogInformation($"BlobStorageRequest {blobStorage.StorageUri}");

                foreach (var blobUri in queueDataMessage.PictureURLs)
                {
                    var blobUriBuilder = new System.UriBuilder($"{blobStorage.StorageUri}students/{blobUri}")
                    {
                        Query = blobStorage.StorageAccessToken
                    };
                    _log.LogInformation($"blobUriBuilder { blobUriBuilder.Uri.AbsoluteUri}");

                    try
                    {
                        await _faceClient.PersonGroupPerson.AddFaceFromUrlAsync(queueDataMessage.SchoolId, student.PersonId, blobUriBuilder.Uri.AbsoluteUri, detectionModel: DetectionModel.Detection01);
                        pictureURLs.Add(blobUri);
                        _log.LogInformation($"AddFaceFromUrlAsync Done");
                    }
                    catch (Exception ex)
                    {
                        _log.LogError(ex, $"AddFaceFromUrlAsync APIErrorException: {queueDataMessage.StudentId} URI: {blobUriBuilder.Uri.AbsoluteUri}");
                    }
                }

                _log.LogInformation($"Train the PersonGroup Start {queueDataMessage.SchoolId}");

                // Train the PersonGroup
                await _faceClient.PersonGroup.TrainAsync(queueDataMessage.SchoolId).ConfigureAwait(false);

                //while (true)
                //{
                //    Task.Delay(1000).Wait();
                //    var status = await _faceClient.LargeFaceList.GetTrainingStatusAsync(queueDataMessage.SchoolId);

                //    if (status.Status == TrainingStatusType.Running)
                //    {
                //        _log.LogInformation($"Training Running status ({queueDataMessage.StudentId}): {status.Status}");
                //        continue;
                //    }
                //    else if (status.Status == TrainingStatusType.Succeeded)
                //    {
                //        _log.LogInformation($"Training Succeeded status ({queueDataMessage.StudentId}): {status.Status}");
                //        break;
                //    }
                //}

                Task.Delay(1000).Wait();

                await _studentService.UpdateStudentProfile(queueDataMessage.StudentId, pictureURLs);

                _log.LogInformation($"Train the PersonGroup Done {queueDataMessage.SchoolId}: {queueDataMessage.StudentId}");
            }
            // Catch and display Face API errors.
            catch (APIErrorException ex)
            {
                _log.LogError(ex, $"TrainStudentModel APIErrorException: {queueDataMessage.StudentId}");
                throw new AppException("APIErrorException: ", ex.InnerException);
            }
            // Catch and display all other errors.
            catch (Exception ex)
            {
                _log.LogError(ex, $"TrainStudentModel Error: {queueDataMessage.StudentId}");
                throw new AppException("Train Student Cognitive Service Error: ", ex.InnerException);
            }
        }

        /// <summary>
        /// The ProcessAttendance.
        /// </summary>
        /// <param name="queueDataMessage">The queueDataMessage<see cref="QueueDataMessage"/>.</param>
        /// <param name="_log">The _log<see cref="ILogger"/>.</param>
        /// <returns>The <see cref="Task"/>.</returns>
        public async Task ProcessAttendance(QueueDataMessage queueDataMessage, ILogger _log)
        {

            // Call the Face API.
            try
            {
                BlobStorageRequest blobStorage = await _azureBlobService.GetSassUri("attendance");
                _log.LogInformation($"APIErrorException BlobStorageRequest: {blobStorage.StorageUri}");

                var blobUriBuilder = new System.UriBuilder($"{blobStorage.StorageUri}attendance/{queueDataMessage.PictureURLs[0]}")
                {
                    Query = blobStorage.StorageAccessToken
                };

                _log.LogInformation($"APIErrorException blobUriBuilder: {blobUriBuilder.Uri.AbsoluteUri}");


                _log.LogInformation($"Start DetectWithUrlAsync");

                IList<DetectedFace> faceList =
                    await _faceClient.Face.DetectWithUrlAsync(blobUriBuilder.Uri.AbsoluteUri, true, true, recognitionModel: "recognition_01", returnRecognitionModel: true).ConfigureAwait(false);

                _log.LogInformation($"End DetectWithUrlAsync: { faceList}");


                var detectedFaceGroups = faceList
                              .Select((face, i) => new { DetectedFace = face.FaceId.Value, Index = i })
                              .GroupBy(x => x.Index / 10, x => x.DetectedFace);

                List<IdentifyResult> IdentifyResults = new List<IdentifyResult>();

                foreach (var face in detectedFaceGroups)
                {
                    List<Guid?> sourceFaceIds = new List<Guid?>();

                    foreach (var detectedFace in face)
                    {
                        sourceFaceIds.Add(detectedFace);
                    }

                    var identifiedResult = await _faceClient.Face.IdentifyAsync(sourceFaceIds, queueDataMessage.SchoolId).ConfigureAwait(false);

                    IdentifyResults.AddRange(identifiedResult);
                }

                IList<string> presentStudetns = new List<string>();

                foreach (var identifyResult in IdentifyResults)
                {
                    _log.LogInformation($"identifyResult = {identifyResult}");

                    if (identifyResult.Candidates.Count() == 0)
                    {
                        _log.LogInformation($"No one identified");
                    }
                    else
                    {
                        // Get top 1 among all candidates returned
                        _log.LogInformation($"Get top 1 among all candidates returned");
                        var candidateId = identifyResult.Candidates[0].PersonId;
                        _log.LogInformation($"candidateId: {candidateId}");

                        var person = await _faceClient.PersonGroupPerson.GetAsync(queueDataMessage.SchoolId, candidateId).ConfigureAwait(false);
                        _log.LogInformation($"Identified as: {person.Name}");

                        var studentId = person.Name;
                        presentStudetns.Add(studentId);

                    }
                }

                var allStudents = await _studentService.GetAll(queueDataMessage.SchoolId, queueDataMessage.ClassId);

                foreach (var student in allStudents)
                {
                    var attaintance = new Attentdance(queueDataMessage.SchoolId, Guid.NewGuid().ToString())
                    {
                        StudentId = student.Id,
                        ClassRoomId = queueDataMessage.ClassId,
                        TeacherId = queueDataMessage.TeacherId,
                        CreatedBy = queueDataMessage.TeacherId,
                        UpdatedBy = queueDataMessage.TeacherId,
                        UpdatedOn = DateTime.UtcNow,
                        Timestamp = queueDataMessage.PictureTimestamp,
                        Latitude = queueDataMessage.Latitude,
                        Longitude = queueDataMessage.Longitude,
                        Present = presentStudetns.Contains(student.Id)
                    };
                    _log.LogInformation($"Attentdance AddAsync: {attaintance}");

                    await _tableStorage.AddAsync("Attentdance", attaintance);

                    _log.LogInformation($"Done Attentdance AddAsync: {attaintance}");
                }
                _log.LogInformation($"Done Attentdance..");
            }
            // Catch and display Face API errors.
            catch (APIErrorException ex)
            {
                _log.LogError(ex, "ProcessAttendance Exception");

                throw new AppException("APIErrorException: ", ex.InnerException);
            }
            // Catch and display all other errors.
            catch (Exception ex)
            {
                _log.LogError(ex, "ProcessAttendance Exception");

                throw new AppException("Process Attendance Cognitive Service Error: ", ex.InnerException);
            }
        }

        /// <summary>
        /// The GetPictureData.
        /// </summary>
        /// <param name="jsonBlob">The jsonBlob<see cref="Stream"/>.</param>
        /// <param name="log">The log<see cref="ILogger"/>.</param>
        /// <returns>The <see cref="QueueDataMessage"/>.</returns>
        private QueueDataMessage GetPictureData(Stream jsonBlob, ILogger log)
        {
            QueueDataMessage result = new QueueDataMessage();
            var serializer = new JsonSerializer();
            using (StreamReader reader = new StreamReader(jsonBlob))
            {
                using (var jsonTextReader = new JsonTextReader(reader))
                {
                    result = serializer.Deserialize<QueueDataMessage>(jsonTextReader);
                }
            }

            log.LogInformation($"Successfully parsed JSON blob");
            return result;
        }

        /// <summary>
        /// The CreateGroup.
        /// </summary>
        /// <param name="personGroupId">The personGroupId<see cref="string"/>.</param>
        /// <param name="_log">The _log<see cref="ILogger"/>.</param>
        private void CreateGroup(string personGroupId, ILogger _log)
        {
            try
            {
                _log.LogInformation($"CreateGroup: {personGroupId}");
                //Create school group
                _faceClient.PersonGroup.CreateAsync(personGroupId, "School Group", recognitionModel: RecognitionModel.Recognition01).GetAwaiter().GetResult();
            }
            catch
            {
                _log.LogInformation($"Already Created : {personGroupId}");
            }
        }

        /// <summary>
        /// The GetAttentdance.
        /// </summary>
        /// <returns>The <see cref="Task{IEnumerable{AttentdanceResponse}}"/>.</returns>
        public async Task<IEnumerable<AttentdanceResponse>> GetAttentdance()
        {
            var attentdance = await _tableStorage.GetAllAsync<Entites.Attentdance>("Attentdance");

            var attentdanceList = from attent in attentdance
                                  orderby attent.UpdatedOn descending
                                  select new AttentdanceResponse
                                  {
                                      Id = attent.RowKey,
                                      SchoolId = attent.PartitionKey,
                                      StudentId = attent.StudentId,
                                      ClassRoomId = attent.ClassRoomId,
                                      TeacherId = attent.TeacherId,
                                      Latitude = attent.Latitude,
                                      Longitude = attent.Longitude,
                                      Present = attent.Present,
                                      Timestamp = attent.Timestamp.DateTime,
                                  };

            return attentdanceList;
        }

        /// <summary>
        /// The CheckIsAttendanceProcessed.
        /// </summary>
        /// <param name="schoolId">The schoolId<see cref="string"/>.</param>
        /// <param name="classId">The classId<see cref="string"/>.</param>
        /// <returns>The <see cref="Task{bool}"/>.</returns>
        public async Task<bool> VerifyIsAttendanceProcessed(string schoolId, string classId)
        {
            var attentdance = await _tableStorage.GetAllAsync<Entites.Attentdance>("Attentdance");
            return attentdance.Any((a) => a.PartitionKey == schoolId && a.ClassRoomId == classId && a.Timestamp.Date == DateTime.Today.Date);
        }
    }
}
