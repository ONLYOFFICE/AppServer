using ASC.Mail.Enums;
using System.Collections.Generic;

namespace ASC.Mail.Models
{
    public class ServerFolderAccessInfo
    {
        public string Server { get; set; }

        public Dictionary<string, FolderInfo> FolderAccessList { get; set; }

        public class FolderInfo
        {
            public FolderType folder_id;
            public bool skip;
        }
    }
}
