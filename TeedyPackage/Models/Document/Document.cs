namespace TeedyPackage.Models.Document
{
    public class Document
    {
        public string id { get; set; }
        public string title { get; set; }
        public string description { get; set; }
        public List<TeedyPackage.Models.Tags.Tag> tags { get; set; }

        #region Unnecessary Properties
        //public int file_count { get; set; }
        //public string Subject { get; set; }
        //public string Identifier { get; set; }
        //public string Publisher { get; set; }
        //public string Format { get; set; }
        //public string Source { get; set; }
        //public string Type { get; set; }
        //public string Coverage { get; set; }
        //public string Rights { get; set; }
        //public string Creator { get; set; }
        //public string FileId { get; set; }
        //public bool Writable { get; set; }
        //public long CreateDate { get; set; } // Timestamp
        //public long UpdateDate { get; set; } // Timestamp
        public string language { get; set; }
        //public bool Shared { get; set; }
        #endregion


    }
}
