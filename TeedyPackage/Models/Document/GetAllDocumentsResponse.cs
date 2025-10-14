namespace TeedyPackage.Models.Document
{
    public class GetAllDocumentsResponse
    {
        public int total { get; set; }
        public List<GetDocument> documents { get; set; }
    }
}
