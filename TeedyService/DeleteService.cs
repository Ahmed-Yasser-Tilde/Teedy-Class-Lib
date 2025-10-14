using Microsoft.Extensions.Configuration;
using TeedyPackage.Models.Document;
using TeedyPackage.Models.Files;
using TeedyPackage.Models.Tags;
using TeedyPackage.Services;
using TeedyPackage.Services.TeedyServices;

namespace TeedyService
{
    public class DeleteService
    {
        private IConfiguration _configuration;
        private CancellationTokenSource tokenSource;
        private Task workerTask;

        public DeleteService()
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
                    await StartTeedyDeleteService();
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

        private async Task StartTeedyDeleteService()
        {
            try
            {
                TeedyApiMethods apiMethods = new TeedyApiMethods(_configuration);
                string teedyStorageFolderPath = _configuration["TeedySettings:StorageFolder"];

                string authToken = await TeedyApiMethods.Login(_configuration["Teedy:Credentials:Username"], _configuration["Teedy:Credentials:Password"]);
                if (authToken == default)
                {
                    throw new Exception("Authentication failed. Please check your credentials.");
                }

                int limit = 10;
                int offset = 0;
                int totalDocuments = 0;

                List<(string docId, string docTitle, string docDescription, int rec_id)> documentsToUpdate = new List<(string, string, string, int)>();
                do
                {
                    GetAllDocumentsResponse getAllDocumentsResponse = await TeedyApiMethods.GetDocuments(authToken, limit, offset);
                    totalDocuments = getAllDocumentsResponse.total;
                    foreach (GetDocument document in getAllDocumentsResponse.documents)
                    {
                        if (document.title.StartsWith("رقم الحركة"))
                        {
                            int rec_id = int.Parse(new string(document.title.Where(char.IsDigit).ToArray()));
                            foreach (Tag tag in document.tags)
                            {
                                await TeedyApiMethods.DeleteTag(authToken, tag.Id);
                            }

                            GetFiles getFiles = await TeedyApiMethods.GetDocumentFiles(authToken, document.id);
                            foreach (TeedyPackage.Models.Files.File file in getFiles.files)
                            {
                                await TeedyApiMethods.DeleteFile(authToken, file.id);
                            }

                            await TeedyApiMethods.DeleteDocument(authToken, document.id);

                            string recPath = Path.Combine(teedyStorageFolderPath, rec_id.ToString());
                            if (Directory.Exists(recPath))
                            {
                                string[] filesPath = Directory.GetFiles(recPath);
                                string metaDataFilePath = filesPath.FirstOrDefault(filePath => Path.GetExtension(filePath).TrimStart('.').ToLower().Equals("txt", StringComparison.OrdinalIgnoreCase))
                                    ?? Path.Combine(recPath, "metadata.txt");
                                try
                                {
                                    System.IO.File.Delete(metaDataFilePath);
                                }
                                catch
                                {
                                    LogService.LogError($"Failed to delete metadata file: {metaDataFilePath}");
                                }
                            }
                        }
                    }
                    offset += limit;
                } while (offset < totalDocuments);
            }
            catch (Exception ex)
            {
                LogService.LogError(ex.Message);
                throw;
            }
        }
    }
}
