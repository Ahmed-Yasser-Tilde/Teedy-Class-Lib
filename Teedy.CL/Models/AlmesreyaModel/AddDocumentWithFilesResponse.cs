namespace Teedy.CL.Models.AlmesreyaModel
{
    public class AddDocumentWithFilesResponse
    {
        public bool IsSuccess { get; set; }
        public string DocumentId { get; set; }
        public List<string> FileIds { get; set; }
    }
}
