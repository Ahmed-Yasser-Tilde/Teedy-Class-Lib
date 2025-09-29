namespace TeedyPackage.Models.Comment
{
    public class Comment
    {
        public int ID { get; set; }
        public string Content { get; set; }
        public string Creator { get; set; }
        public string CreatorGravatar { get; set; }
        public long CreateDate { get; set; }
    }
}
