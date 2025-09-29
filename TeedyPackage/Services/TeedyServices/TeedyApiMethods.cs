using Microsoft.Extensions.Configuration;
using RestSharp;
using System.Security.Authentication;
using System.Text.Json;
using System.Text.RegularExpressions;
using TeedyPackage.Models.AlmesreyaModel;
using TeedyPackage.Models.Tags;

namespace TeedyPackage.Services.TeedyServices
{
    public class TeedyApiMethods
    {
        private static IConfiguration _configuration;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="configuration"></param>
        public TeedyApiMethods(IConfiguration configuration)
        {
            _configuration = configuration;
        }


        #region Authentication
        /// <summary>
        /// 
        /// </summary>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="AuthenticationException"></exception>
        /// <exception cref="ApplicationException"></exception>
        public static async Task<string> Login(string username, string password)
        {
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                throw new ArgumentException("Username and password must be provided.");
            }
            try
            {

                var _baseUrl = _configuration["Teedy:Credentials:URL"];

                RestClient _restClient = new RestClient(new RestClientOptions(_baseUrl));

                var enpoindurl = _configuration["Teedy:Actions:Login"];
                RestRequest _restRequest = new RestRequest(enpoindurl, Method.Post);


                _restRequest.AddHeader("Content-Type", _configuration["Teedy:Headers:Content-Type"]);

                _restRequest.AddParameter("username", username);

                _restRequest.AddParameter("password", password);


                // Execution
                RestResponse _restResponse = await _restClient.ExecuteAsync(_restRequest);


                // With this corrected code:
                var authtoken = _restResponse.Cookies?.Cast<System.Net.Cookie>().FirstOrDefault(a => a.Name == "auth_token")?.Value;
                if (string.IsNullOrEmpty(authtoken))
                {
                    throw new AuthenticationException("No auth token returned from the server.");
                }

                return authtoken;
            }
            catch (HttpRequestException e)
            {
                // Handle network-related exceptions (timeouts, unreachable server, etc.)
                throw new AuthenticationException("Network error while contacting the authentication server.", e);
            }
            catch (Exception e)
            {
                // Log any other errors and throw them
                Console.WriteLine($"Error: {e.ToString()}");
                throw new ApplicationException("An error occurred during the login process.", e);
            }

        }

        #endregion

        #region Documents and Files
        /// <summary>
        /// Uploads a file to the Teedy document management system
        /// </summary>
        /// <param name="filePath">The path to the file to upload</param>
        /// <param name="authToken">Authentication token for the API</param>
        /// <returns>The file ID from the response, or the raw response content if parsing fails</returns>
        /// <exception cref="ArgumentException">Thrown when file path is invalid or file does not exist</exception>
        /// <exception cref="ApplicationException">Thrown when an error occurs during the upload process</exception>
        public static async Task<string> PutFile(string filePath, string authToken)
        {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            {
                throw new ArgumentException("File path is invalid or file does not exist.");
            }

            if (string.IsNullOrEmpty(authToken))
            {
                throw new ArgumentException("Authentication token cannot be null or empty.", nameof(authToken));
            }

            try
            {
                byte[] fileData = File.ReadAllBytes(filePath);
                string originalFileName = Path.GetFileName(filePath);
                string encodedFileName = Uri.EscapeDataString(originalFileName);
                string contentType = GetMimeType(originalFileName);

                string baseUrl = _configuration["Teedy:Credentials:URL"];
                string addFileEndpoint = _configuration["Teedy:Actions:AddFile"];
                string cleanEndpoint = addFileEndpoint.Split('?')[0];

                RestClientOptions options = new RestClientOptions(baseUrl)
                {
                    Encoding = System.Text.Encoding.UTF8
                };

                RestClient restClient = new RestClient(options);
                RestRequest restRequest = new RestRequest(addFileEndpoint, Method.Put);

                restRequest.AddQueryParameter("auth_token", authToken);
                restRequest.AddHeader("Cookie", "auth_token=" + authToken);
                // Use encoded filename for upload
                restRequest.AddFile("file", fileData, encodedFileName, contentType);
                restRequest.AddHeader("Accept", "application/json");

                RestResponse restResponse = await restClient.ExecuteAsync(restRequest);

                if (restResponse.IsSuccessful && !string.IsNullOrEmpty(restResponse.Content))
                {
                    try
                    {
                        var jsonResponse = JsonSerializer.Deserialize<JsonElement>(restResponse.Content);
                        if (jsonResponse.TryGetProperty("id", out JsonElement idElement))
                        {
                            return idElement.GetString() ?? restResponse.Content;
                        }
                        return restResponse.Content;
                    }
                    catch (JsonException)
                    {
                        return restResponse.Content;
                    }
                }

                throw new ApplicationException($"File upload failed. Status: {restResponse.StatusCode}, Content: {restResponse.Content}");
            }
            catch (Exception ex) when (!(ex is ArgumentException || ex is ApplicationException))
            {
                throw new ApplicationException($"An error occurred during the file upload process: {ex.Message}", ex);
            }
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="document"></param>
        /// <param name="authToken"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public static async Task<string> AddDocument(Models.Document.Document document, string authToken)
        {
            try
            {

                // Validate required fields: Title and Language 'other optional'
                if (document == null || string.IsNullOrEmpty(document.Title) || string.IsNullOrEmpty(document.Language))
                {
                    throw new ArgumentException("Document title and language are required.");
                }

                // Set the base URL
                var _baseUrl = _configuration["Teedy:Credentials:URL"];

                // Initialize RestClient
                RestClient _restClient = new RestClient(new RestClientOptions(_baseUrl));

                // Create the RestRequest for the PUT method
                RestRequest _restRequest = new RestRequest("/api/document", Method.Put);

                // Set headers
                _restRequest.AddHeader("Cookie", "auth_token=" + authToken);
                _restRequest.AddHeader("Content-Type", _configuration["Teedy:Headers:Content-Type"]);

                // Add required fields (Title and Language)
                _restRequest.AddParameter("title", document.Title);
                _restRequest.AddParameter("language", document.Language);

                foreach (var tag in document.Tags)
                {
                    if (!string.IsNullOrEmpty(tag))
                    {
                        _restRequest.AddParameter("tags", tag);
                    }
                }
                ;
                _restRequest.AddParameter("description", document.Description);

                // Execute the request
                RestResponse _restResponse = await _restClient.ExecuteAsync(_restRequest);

                if (_restResponse.IsSuccessful && !string.IsNullOrEmpty(_restResponse.Content))
                {
                    try
                    {
                        // Extract file ID from JSON response
                        var jsonResponse = JsonSerializer.Deserialize<JsonElement>(_restResponse.Content);
                        string fileId = jsonResponse.GetProperty("id").GetString();

                        return fileId ?? _restResponse.Content;
                    }
                    catch
                    {
                        return _restResponse.Content; // If parsing fails, return raw response
                    }
                }

                return _restResponse.Content?.ToString();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="fileId"></param>
        /// <param name="docId"></param>
        /// <param name="authToken"></param>
        /// <returns></returns>
        public static async Task<bool?> AttachFileToDoc(string fileId, string docId, string authToken)
        {
            try
            {

                var _baseUrl = _configuration["Teedy:Credentials:URL"];

                // Initialize RestClient with base URL
                RestClient _restClient = new RestClient(new RestClientOptions(_baseUrl));

                // Build the endpoint URL for file attachment
                RestRequest _restRequest = new RestRequest($"/api/file/{fileId}/attach", Method.Post);

                // Set headers
                _restRequest.AddHeader("Cookie", "auth_token=" + authToken);
                _restRequest.AddHeader("Content-Type", _configuration["Teedy:Headers:Content-Type"]);

                // Add parameters to the request body
                _restRequest.AddParameter("id", docId);

                // Execute the request
                RestResponse _restResponse = await _restClient.ExecuteAsync(_restRequest);


                if (_restResponse.IsSuccessful && !string.IsNullOrEmpty(_restResponse.Content))
                {
                    try
                    {
                        // Deserialize the JSON response
                        var jsonResponse = JsonSerializer.Deserialize<JsonElement>(_restResponse.Content);

                        // Extract the status or any relevant information from the response
                        string status = jsonResponse.GetProperty("status").GetString();

                        // If status is successful, return true
                        if (status == "ok")
                        {
                            return true;
                        }

                        return false;

                    }
                    catch
                    {
                        return false;
                    }
                }

                return false;
            }
            catch 
            {
                throw;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="document"></param>
        /// <param name="filePaths"></param>
        /// <param name="authToken"></param>
        /// <returns></returns>
        public static async Task<AddDocumentWithFilesResponse> AddFilesToDocument
            (Models.Document.Document document, List<string> filePaths, string authToken)
        {
            try
            {
                // Step 1: Add the document and get the DocId
                var docId = await AddDocument(document, authToken);
                if (string.IsNullOrEmpty(docId))
                    return new AddDocumentWithFilesResponse
                    {
                        IsSuccess = false
                    };


                // Step 2: Initialize a list to hold file IDs
                List<string> FileIds = new List<string>();


                // Step 3: Loop through the byte arrays, upload each file and get the FileId
                foreach (string filePath in filePaths)
                {
                    var fileId = await PutFile(filePath, authToken);
                    if (string.IsNullOrEmpty(fileId))
                        return new AddDocumentWithFilesResponse
                        {
                            IsSuccess = false
                        };
                    FileIds.Add(fileId);
                }

                // Step 4: Loop through the FileIds and attach each file to the document
                foreach (var fileId in FileIds)
                {
                    var attachmentResult = await AttachFileToDoc(fileId, docId, authToken);
                    if (!attachmentResult.HasValue || !attachmentResult.Value)
                        return new AddDocumentWithFilesResponse { IsSuccess = false };
                }
                // Step 5: Return true if all files are successfully attached to the document
                return new AddDocumentWithFilesResponse
                {
                    IsSuccess = true,
                    DocumentId = docId,
                    FileIds = FileIds
                };
            }

            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                //ClsGlobal.LogError(ex);
                //ClsGlobal.LogError($"Error: {ex.Message}", "TeedyApiMethod");
                return new AddDocumentWithFilesResponse { IsSuccess = false }; // Return false in case of an exception
            }

        }

        public static async Task<bool> GetDocument(string documentId, string authToken)
        {
            try
            {
                // Validate required fields: Title and Language 'other optional'
                if (string.IsNullOrEmpty(documentId))
                {
                    throw new ArgumentException("Document id is required.");
                }

                // Set the base URL
                var _baseUrl = _configuration["Teedy:Credentials:URL"];

                // Initialize RestClient
                RestClient _restClient = new RestClient(new RestClientOptions(_baseUrl));

                // Create the RestRequest for the PUT method
                RestRequest _restRequest = new RestRequest($"/api/document/{documentId}", Method.Get);

                // Set headers
                _restRequest.AddHeader("Cookie", "auth_token=" + authToken);


                // Add required fields (Title and Language)
                //_restRequest.AddParameter("id", documentId);

                // Execute the request
                RestResponse _restResponse = await _restClient.ExecuteAsync(_restRequest);

                if (_restResponse.IsSuccessful && !string.IsNullOrEmpty(_restResponse.Content))
                {
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error: {ex.Message}");
            }
        }

        #endregion

        #region Tags

        /// <summary>
        /// 
        /// </summary>
        /// <param name="create"></param>
        /// <param name="authToken"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="ApplicationException"></exception>
        public static async Task<string> CreateTag(CreateTag create, string authToken)
        {
            try
            {
                // Validate the Name property
                if (string.IsNullOrEmpty(create.Name))
                {
                    throw new ArgumentException("Cannot add a tag without a name.");
                }
                // Validate the Color property
                if (string.IsNullOrEmpty(create.Color) || create.Color.Length > 7)
                {
                    throw new ArgumentException("Color must be less than 7 characters.");
                }
                // Ensure the Color is a valid hexadecimal color code
                var colorRegex = new Regex("^#([a-fA-F0-9]{3}|[a-fA-F0-9]{6})$");

                // If the color is not a valid hex code or looks like a name, throw an exception
                if (!colorRegex.IsMatch(create.Color))
                {
                    Console.WriteLine("Color must be a valid hexadecimal color code (e.g., #FF00FF or #F0F). Enter color as a code, not a name.");
                    throw new ArgumentException("Color must be a valid hexadecimal color code (e.g., #FF00FF or #F0F). Enter color as a code, not a name.");
                }
                // Set the base URL
                var _baseUrl = _configuration["Teedy:Credentials:URL"];

                // Initialize RestClient
                RestClient _restClient = new RestClient(new RestClientOptions(_baseUrl));

                var endpointurl = _configuration["Teedy:Actions:AddTag"];
                // Create the RestRequest for the PUT method
                RestRequest _restRequest = new RestRequest(endpointurl, Method.Put);

                // Set headers
                _restRequest.AddHeader("Cookie", "auth_token=" + authToken);
                _restRequest.AddHeader("Content-Type", _configuration["Teedy:Headers:Content-Type"]);


                _restRequest.AddParameter("name", create.Name);
                _restRequest.AddParameter("color", create.Color);
                if (!string.IsNullOrEmpty(create.ParentId))
                    _restRequest.AddParameter("parent", create.ParentId);
                // Execute the request
                RestResponse _restResponse = await _restClient.ExecuteAsync(_restRequest);

                if (_restResponse.IsSuccessful && !string.IsNullOrEmpty(_restResponse.Content))
                {
                    try
                    {
                        // Extract file ID from JSON response
                        var jsonResponse = JsonSerializer.Deserialize<JsonElement>(_restResponse.Content);
                        string fileId = jsonResponse.GetProperty("id").GetString();

                        return fileId ?? _restResponse.Content;
                    }
                    catch
                    {
                        return _restResponse.Content; // If parsing fails, return raw response
                    }
                }

                return _restResponse.Content?.ToString();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                throw new ApplicationException("An error occurred during Adding This Tag process.", ex);

            }
        }

        public static async Task<Tag> GetTagById(string tagId, string authToken)
        {
            try
            {
                if (string.IsNullOrEmpty(tagId))
                {
                    throw new ArgumentException("Tag ID cannot be null or empty.");
                }

                // Set the base URL
                var baseUrl = _configuration["Teedy:Credentials:URL"];

                // Initialize RestClient
                RestClient _restClient = new RestClient(new RestClientOptions(baseUrl));

                var endpointUrl = $"{_configuration["Teedy:Actions:AddTag"]}/{tagId}";
                RestRequest _restRequest = new RestRequest(endpointUrl, Method.Get);

                // Set headers
                _restRequest.AddHeader("Cookie", "auth_token=" + authToken);

                // Execute the request
                RestResponse _restResponse = await _restClient.ExecuteAsync(_restRequest);

                // Handle failure cases
                if (!_restResponse.IsSuccessful)
                {
                    if (_restResponse.StatusCode == System.Net.HttpStatusCode.NotFound)
                    {
                        throw new ApplicationException($"Tag with ID {tagId} does not exist.");
                    }

                    throw new ApplicationException($"Failed to get tag. HTTP {_restResponse.StatusCode}: {_restResponse.ErrorMessage}");
                }

                if (string.IsNullOrEmpty(_restResponse.Content))
                {
                    throw new ApplicationException($"Tag with ID {tagId} does not exist or has no data.");
                }

                try
                {
                    // Deserialize JSON response to Tag object
                    var tag = JsonSerializer.Deserialize<Tag>(_restResponse.Content, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true // Handles JSON property name casing differences
                    });

                    if (tag == null)
                    {
                        throw new ApplicationException($"Failed to parse tag data for ID {tagId}.");
                    }

                    return tag;
                }
                catch (Exception parseEx)
                {
                    Console.WriteLine($"JSON Parsing Error: {parseEx.Message}");
                    throw new ApplicationException("Failed to parse the tag response.", parseEx);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                throw new ApplicationException("An error occurred while retrieving the tag.", ex);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="authToken"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="ApplicationException"></exception>
        public static async Task<Tag> GetTagByName(string name, string authToken)
        {
            try
            {
                if (string.IsNullOrEmpty(name))
                {
                    throw new ArgumentException("Tag Name cannot be null or empty.");
                }

                // Fetch all tags
                var AllTags = await GetAllTags(authToken); // Ensure this method returns a List<Tag>

                // Search for the tag by name
                var foundTag = AllTags.FirstOrDefault(tag => tag.Name != null && tag.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

                return foundTag; // Return the found tag
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                throw new ApplicationException("An error occurred while retrieving the tag.", ex);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="authToken"></param>
        /// <returns></returns>
        /// <exception cref="ApplicationException"></exception>
        public static async Task<List<Tag>> GetAllTags(string authToken)
        {
            try
            {
                // Set the base URL
                var baseUrl = _configuration["Teedy:Credentials:URL"];

                // Initialize RestClient
                RestClient restClient = new RestClient(new RestClientOptions(baseUrl));

                var endpointUrl = $"{_configuration["Teedy:Actions:AddTag"]}/list";
                RestRequest restRequest = new RestRequest(endpointUrl, Method.Get);

                // Set headers (with the provided authToken)
                restRequest.AddHeader("Cookie", "auth_token=" + authToken);

                // Execute the request
                var restResponse = await restClient.ExecuteAsync(restRequest);

                // Handle failure cases
                if (!restResponse.IsSuccessful)
                {
                    if (restResponse.StatusCode == System.Net.HttpStatusCode.NotFound)
                    {
                        throw new ApplicationException("No tags found or the resource does not exist.");
                    }

                    throw new ApplicationException($"Failed to get tags. HTTP {restResponse.StatusCode}: {restResponse.ErrorMessage}");
                }

                if (string.IsNullOrEmpty(restResponse.Content))
                {
                    throw new ApplicationException("No data returned from the API.");
                }

                try
                {
                    // Log the raw response content for debugging
                    Console.WriteLine("Response Content:");
                    Console.WriteLine(restResponse.Content);

                    // Deserialize the response content into GetTags object
                    var tagsResponse = JsonSerializer.Deserialize<GetTags>(restResponse.Content);

                    if (tagsResponse == null)
                    {
                        throw new ApplicationException("Failed to deserialize the response into GetTags object.");
                    }



                    // Return the list of Tags
                    return tagsResponse.Tags ?? new List<Tag>(); // Return an empty list if Tags is null
                }
                catch (Exception parseEx)
                {
                    Console.WriteLine($"JSON Parsing Error: {parseEx.Message}");
                    throw new ApplicationException("An error occurred while parsing the response.", parseEx);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                throw new ApplicationException("An error occurred while retrieving the tags.", ex);
            }
        }

        #endregion


        /// <summary>
        /// Gets the MIME type based on file extension
        /// </summary>
        /// <param name="fileName">The file name with extension</param>
        /// <returns>The appropriate MIME type</returns>
        private static string GetMimeType(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return "application/octet-stream";

            string extension = Path.GetExtension(fileName).ToLowerInvariant();

            switch (extension)
            {
                // Image formats
                case ".jpg":
                case ".jpeg":
                    return "image/jpeg";
                case ".png":
                    return "image/png";
                case ".gif":
                    return "image/gif";
                case ".bmp":
                    return "image/bmp";
                case ".tiff":
                case ".tif":
                    return "image/tiff";
                case ".webp":
                    return "image/webp";
                case ".svg":
                    return "image/svg+xml";
                case ".ico":
                    return "image/x-icon";

                // Document formats
                case ".pdf":
                    return "application/pdf";
                case ".doc":
                    return "application/msword";
                case ".docx":
                    return "application/msword";
                case ".xls":
                    return "application/vnd.ms-excel";
                case ".xlsx":
                    return "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                case ".ppt":
                    return "application/vnd.ms-powerpoint";
                case ".pptx":
                    return "application/vnd.openxmlformats-officedocument.presentationml.presentation";

                // Text formats
                case ".txt":
                    return "text/plain";
                case ".csv":
                    return "text/csv";
                case ".html":
                case ".htm":
                    return "text/html";
                case ".css":
                    return "text/css";
                case ".js":
                    return "application/javascript";
                case ".json":
                    return "application/json";
                case ".xml":
                    return "application/xml";

                // Archive formats
                case ".zip":
                    return "application/zip";
                case ".rar":
                    return "application/x-rar-compressed";
                case ".7z":
                    return "application/x-7z-compressed";
                case ".tar":
                    return "application/x-tar";
                case ".gz":
                    return "application/gzip";

                // Audio/Video formats
                case ".mp3":
                    return "audio/mpeg";
                case ".wav":
                    return "audio/wav";
                case ".mp4":
                    return "video/mp4";
                case ".avi":
                    return "video/x-msvideo";
                case ".mov":
                    return "video/quicktime";

                // Default fallback
                default:
                    return "application/octet-stream";
            }
        }
    }
}
