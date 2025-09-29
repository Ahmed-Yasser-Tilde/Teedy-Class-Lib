namespace TeedyPackage.Models.Files
{
    public class AddFile
    {
        public string FileData { get; set; }
        public string FileID { get; set; }
        public string Status { get; set; }
        public long? Size { get; set; }
        public string PreviousFileId { get; set; }
        public string DocumentID { get; set; }
    }
}
