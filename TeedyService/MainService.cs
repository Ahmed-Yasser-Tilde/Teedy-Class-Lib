using Microsoft.Extensions.Configuration;
using TeedyPackage.Models.AlmesreyaModel;
using TeedyPackage.Models.Document;
using TeedyPackage.Models.Tags;
using TeedyPackage.Services;
using TeedyPackage.Services.DatabaseService;
using TeedyPackage.Services.TeedyServices;

namespace TeedyService
{
    public class MainService
    {
        private IConfiguration _configuration;
        private CancellationTokenSource tokenSource;
        private Task workerTask;

        public MainService()
        {
            _configuration = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables()
            .Build();
        }
        public void Start()
        {
            tokenSource = new CancellationTokenSource();

            // Run background loop, don't block Start()
            workerTask = Task.Run(() => RunAsync(tokenSource.Token));
        }

        public void Stop()
        {
            if (tokenSource != null)
            {
                tokenSource.Cancel();

                try
                {
                    workerTask?.Wait(5000); // Wait max 5s for shutdown
                }
                catch (Exception) { }
            }
        }

        private async Task RunAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    await StartTeedyService();
                    await Task.Delay(TimeSpan.FromMinutes(1), token);
                }
                catch (TaskCanceledException)
                {
                    // normal during shutdown
                }
                catch (Exception)
                {
                    // TODO: log ex
                }
            }
        }

        public async Task StartTeedyService()
        {
            string teedyStorageFolderPath = _configuration["TeedySettings:StorageFolder"];
            try
            {
                if (Directory.Exists(teedyStorageFolderPath))
                {

                    foreach (string subFolderPath in Directory.GetDirectories(teedyStorageFolderPath))
                    {
                        string rec_id = Path.GetFileName(subFolderPath);

                        string[] filesPath = Directory.GetFiles(subFolderPath);

                        string metaDataFilePath = filesPath.FirstOrDefault(filePath => Path.GetExtension(filePath).TrimStart('.').ToLower().Equals("txt", StringComparison.OrdinalIgnoreCase)) ?? Path.Combine(subFolderPath, "metadata.txt");
                        string metaDataFileContent = string.Empty;

                        if (File.Exists(metaDataFilePath))
                        {
                            metaDataFileContent = File.ReadAllText(metaDataFilePath);
                        }

                        string[] uploadedFiles = metaDataFileContent.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);

                        string receiptJsonFilePath = filesPath.FirstOrDefault(filePath => Path.GetExtension(filePath).TrimStart('.').ToLower().Equals("json", StringComparison.OrdinalIgnoreCase)) ?? Path.Combine(subFolderPath, "receipt.json");
                        if (!File.Exists(receiptJsonFilePath))
                        {
                            LogService.LogInfo($"Recepit file not found for Folder: {rec_id}");
                            continue;
                        }

                        string receiptJsonFileContent = File.ReadAllText(receiptJsonFilePath);

                        var filesPathToBeUploaded = filesPath.Where((filePath) =>
                        {
                            string ext = Path.GetExtension(filePath).TrimStart('.').ToLower();
                            return !ext.Equals("json", StringComparison.OrdinalIgnoreCase) && !ext.Equals("txt", StringComparison.OrdinalIgnoreCase) && !uploadedFiles.Contains(filePath);
                        }).ToList();

                        LogService.LogInfo("Files to be uploaded: " + string.Join(", ", filesPathToBeUploaded) + " for rec_id: " + rec_id);

                        bool isSucess = await UploadDocumentsToTeedy(_configuration, filesPathToBeUploaded, rec_id, receiptJsonFileContent);

                        if (!isSucess)
                        {
                            LogService.LogInfo($"Error in Upload Documents to Teedy for Folder: {rec_id}");
                        }
                        else
                        {
                            string metaDataFileNewContent = string.Join(Environment.NewLine, filesPath);
                            File.WriteAllText(metaDataFilePath, metaDataFileNewContent);
                            LogService.LogInfo($"Success in Upload Documents to Teedy for Folder: {rec_id}");
                        }

                        Thread.Sleep(20000);
                    }
                }
                else
                {
                    throw new Exception("Teedy Storage Folder Path is not exist");
                }
            }
            catch (Exception ex)
            {
                LogService.LogError("Function: StartTeedyService" + ex.Message);
            }
        }

        public static async Task<bool> UploadDocumentsToTeedy(IConfiguration configuration, List<string> filesPath, string rec_id, string receiptJsonFileContent)
        {
            try
            {
                TeedyApiMethods apiMethods = new(configuration);

                DbHelper dbHelper = new(configuration["connectionDefualt:connection"]);

                #region Authnication
                string authToken = await TeedyApiMethods.Login(configuration["Teedy:Credentials:Username"], configuration["Teedy:Credentials:Password"]);
                if (authToken == default)
                {
                    return false;
                }
                LogService.LogInfo("Authntication to Teedy is done successfully.");
                #endregion

                #region Prepare Tags Names from Almasrya Form
                IEnumerable<HelperData> helperData = dbHelper.Query<HelperData>("select cb.cb_id, cb.cb_br_id, cb.cb_name, br.br_name, r.rec_date, r.rec_document_id\r\n" +
                                "from receipts as r\r\njoin cashboxes as cb on r.rec_from_cb_id = cb.cb_id\r\n" +
                                "join branch as br on cb.cb_br_id = br.br_id\r\nwhere rec_id = @RecId;", new { RecId = rec_id });

                if (helperData == null || string.IsNullOrEmpty(helperData.First().br_name) || string.IsNullOrEmpty(helperData.First().br_name))
                {
                    throw new Exception("Error in Get Helper Data from Database");
                }

                List<string> tagsNamesFromAlmasryaForm = new List<string>()
                {
                    $"({helperData.First().cb_br_id}){helperData.First().br_name.Trim().Replace(" ","_")}",
                    $"({helperData.First().cb_br_id}){helperData.First().cb_name.Trim().Replace(" ","_")}",
                    $"({helperData.First().cb_id})({helperData.First().rec_date.ToString("yyyy-MM-dd").Trim().Replace(" ","_")})"
                };

                LogService.LogInfo("Tags Names from Almasrya Form: " + string.Join(" -> ", tagsNamesFromAlmasryaForm));
                #endregion

                #region Handle Create Or Modify Document In Teedy

                if (!string.IsNullOrEmpty(helperData.First().rec_document_id) && (await TeedyApiMethods.GetDocument(helperData.First().rec_document_id, authToken))) // Recepit has valid document id
                {
                    List<string> uploadedFilesId = await AddFilesToExistingDocument(authToken, filesPath, helperData.First().rec_document_id);
                    if (uploadedFilesId == null)
                    {
                        throw new Exception("Error in Add Files to Existing Document");
                    }
                    LogService.LogInfo("Handle Modify Document In Teedy is done successfully.");
                    return true;
                }
                else
                {
                    AddDocumentWithFilesResponse addDocumentWithFilesResponse = await CreateNewDocumentAndAttachFiles(authToken, tagsNamesFromAlmasryaForm, filesPath, receiptJsonFileContent, rec_id);
                    if (addDocumentWithFilesResponse.IsSuccess)
                    {
                        int numOfEffectedRowes = dbHelper.Execute("UPDATE receipts SET rec_document_id = @RecDocumentId WHERE rec_id = @RecId;",
                            new { RecDocumentId = addDocumentWithFilesResponse.DocumentId, RecId = rec_id });

                        if (numOfEffectedRowes <= 0)
                        {
                            throw new Exception("Error in Update rec_document_id in receipts table");
                        }
                        LogService.LogInfo("Handle Create Document And Attach Files In Teedy is done successfully.");
                        return true;
                    }
                    throw new Exception("Error in Create New Document And Attach Files");
                }
                
                #endregion
            }
            catch (Exception ex)
            {
                LogService.LogError($"Function: UploadDocumentsToTeedy , rec_id = {rec_id}" + ex.Message);
                throw;
            }
        }

        public static async Task<List<string>> AddFilesToExistingDocument(string authToken, List<string> filesPath, string rec_document_id)
        {
            try
            {
                LogService.LogInfo($"Start Add Files to Existing Document: {rec_document_id} in Teedy.");
                List<string> uploadedFilesId = new List<string>();
                bool isSucessAttachFiles = true;
                foreach (string filePath in filesPath)
                {
                    string fileId = await TeedyApiMethods.PutFile(filePath, authToken);
                    LogService.LogInfo("Uploaded File Path: " + filePath);
                    uploadedFilesId.Add(fileId);

                    bool? isSucess = await TeedyApiMethods.AttachFileToDoc(fileId, rec_document_id, authToken);
                    LogService.LogInfo($"Attach File Path: {filePath} to Document Id: " + rec_document_id);
                    if (!isSucess.GetValueOrDefault())
                    {
                        isSucessAttachFiles = false;
                        break;
                    }
                }

                if (!isSucessAttachFiles)
                    throw new Exception("Error in Attach Files to Document");
                else
                    return uploadedFilesId;
            }
            catch (Exception ex)
            {
                LogService.LogError("Function: AddFilesToExistingDocument : " + ex.Message);
                throw;
            }
        }

        public static async Task<AddDocumentWithFilesResponse> CreateNewDocumentAndAttachFiles(string authToken, List<string> tagsNamesFromAlmasryaForm, List<string> filesPath, string receiptJson, string rec_id)
        {
            try
            {
                List<string> tagsId = await HandleCreateTages(authToken, tagsNamesFromAlmasryaForm);
                if (tagsId == null)
                {
                    return new AddDocumentWithFilesResponse { IsSuccess = false, DocumentId = string.Empty, FileIds = new List<string>() };
                }
                LogService.LogInfo("Handle Create Tages is done successfully.");

                Document document = new Document
                {
                    title = $"رقم الحركة :{rec_id} ",
                    language = "eng",
                    description = receiptJson,
                    tags = tagsId.Select(t => new Tag
                    {
                        Id = t
                    }).ToList(),
                };

                AddDocumentWithFilesResponse addDocumentWithFiles = await TeedyApiMethods.AddFilesToDocument(document, filesPath, authToken);
                return addDocumentWithFiles;
            }
            catch(Exception ex)
            {
                LogService.LogError("Function: CreateNewDocumentAndAttachFiles : " + ex.Message);
                return new AddDocumentWithFilesResponse { IsSuccess = false, DocumentId = string.Empty, FileIds = new List<string>() };
            }
        }

        private static async Task<List<string>> HandleCreateTages(string authToken, List<string> tagsNamesFromAlmasryaFormOrder)
        {
            try
            {
                List<Tag> teedyExistTags = await TeedyApiMethods.GetAllTags(authToken);
                List<(string tagId, string parentId)> tagsId = new List<(string, string)>();

                #region Check Exists Almesrya Tags In Teedy
                Tag branchTag = teedyExistTags.FirstOrDefault(tag => tag.Name == tagsNamesFromAlmasryaFormOrder[0]);
                if (branchTag == null)
                {
                    // Branch tag does not exist, so all tages should be created from scratch and ignore all tags with same name
                    for (int i = 0; i < tagsNamesFromAlmasryaFormOrder.Count; i++)
                    {
                        string tagId = string.Empty;
                        string parentId = string.Empty;
                        tagsId.Add((tagId, parentId));
                    }
                }
                else
                {
                    tagsId.Add((branchTag.Id, string.Empty)); // Add branch tag with no parent
                    string lastTagId = branchTag.Id;
                    for (int i = 1; i < tagsNamesFromAlmasryaFormOrder.Count; i++)
                    {
                        string tagId = string.Empty;
                        string parentId = string.Empty;
                        Tag tag = teedyExistTags.FirstOrDefault(t => t.Name == tagsNamesFromAlmasryaFormOrder[i] && t.Parent == lastTagId);
                        if (tag == null)
                        {
                            // Tag does not exist, so create it and all incoming tages should be created from scratch and ignore all tags with same name 
                            for (int j = i; j < tagsNamesFromAlmasryaFormOrder.Count; j++)
                            {
                                tagId = string.Empty;
                                parentId = string.Empty;
                                tagsId.Add((tagId, parentId));
                            }
                            break; // No need to continue checking for further tags
                        }
                        else
                        {
                            tagId = tag.Id;
                            parentId = tag.Parent;
                            lastTagId = tagId; // Update lastTagId for next iteration
                        }
                        tagsId.Add((tagId, parentId));
                    }
                }
                #endregion

                #region Handle Tags Tree
                for (int i = 0; i < tagsNamesFromAlmasryaFormOrder.Count; i++)
                {
                    if (tagsId[i].tagId != string.Empty)
                    {
                        if (i > 0 && tagsId[i].parentId != tagsId[i - 1].tagId)
                        {
                            CreateTag tag = new CreateTag()
                            {
                                Name = tagsNamesFromAlmasryaFormOrder[i],
                                Color = "#008000",
                                ParentId = i == 0 ? null : tagsId[i - 1].tagId
                            };
                            string Id = await TeedyApiMethods.CreateTag(tag, authToken);
                            tagsId[i] = (Id, tag.ParentId);
                        }
                        continue;
                    }
                    else
                    {
                        CreateTag createtag = new CreateTag()
                        {
                            Name = tagsNamesFromAlmasryaFormOrder[i],
                            Color = "#008000",
                            ParentId = i == 0 ? null : tagsId[i - 1].tagId
                        };
                        string tagId = await TeedyApiMethods.CreateTag(createtag, authToken);
                        tagsId[i] = (tagId, createtag.ParentId);
                    }
                }
                #endregion

                return tagsId.Select(x => x.tagId).ToList();
            }
            catch
            {
                return null;
            }
        }


    }
}
