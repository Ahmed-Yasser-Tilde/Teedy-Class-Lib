namespace TeedyPackage.Models.Comment
{
    public class GetComments
    {
        public int Id { get; set; }
        public string Share { get; set; }
        List<Comment> Comments { get; set; }
    }
}
