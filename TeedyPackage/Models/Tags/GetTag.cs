namespace TeedyPackage.Models.Tag
{
    public class GetTag
    {
        public string ID { get; set; }
        public string Name { get; set; }
        public string Creator { get; set; }
        public string Color { get; set; }
        public string Parent { get; set; }
        public object[] Tags { get; set; }
        public bool Writable { get; set; }
        public List<Acl> Acls { get; set; }


    }
    public class Acl
    {
        public string ID { get; set; }
        public string Permission { get; set; } // Allowed values: "READ", "WRITE"
        public string TargetName { get; set; }
        public string TargetType { get; set; } // Allowed values: "USER", "GROUP", "SHARE"
    }
}
