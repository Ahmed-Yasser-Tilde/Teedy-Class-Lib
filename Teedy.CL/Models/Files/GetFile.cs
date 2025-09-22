namespace Teedy.CL.Models.Files
{
    public class GetFile
    {
        public string FileID { get; set; }
        public string ShareID { get; set; }
        public string SizeVariation { get; set; }
        public AddFile File { get; set; }
    }
}
