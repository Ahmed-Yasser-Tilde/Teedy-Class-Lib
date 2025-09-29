namespace TeedyPackage.Models.Document
{
    public class GetDocument
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public long CreateDate { get; set; }
        public long UpdateDate { get; set; }
        public string Language { get; set; }
        public bool Shared { get; set; }
        public int FileCount { get; set; }
        public List<TeedyPackage.Models.Tags.Tag> Tags { get; set; }
        public string Subject { get; set; }
        public string Identifier { get; set; }
        public string Publisher { get; set; }
        public string Format { get; set; }
        public string Source { get; set; }
        public string Type { get; set; }
        public string Coverage { get; set; }
        public string Rights { get; set; }
        public string Creator { get; set; }
        public string FileId { get; set; }
        public bool Writable { get; set; }


    }
}
