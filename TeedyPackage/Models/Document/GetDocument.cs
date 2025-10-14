namespace TeedyPackage.Models.Document
{
    public class GetDocument
    {
        public string id { get; set; }
        public string title { get; set; }
        public string description { get; set; }
        public List<TeedyPackage.Models.Tags.Tag> tags { get; set; }



    }
}
