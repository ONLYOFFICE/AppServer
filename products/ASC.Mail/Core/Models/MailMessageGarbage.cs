using ASC.Mail.Storage;
using ASC.Mail.Models.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ASC.Mail.Models
{
    public class MailMessageGarbage : MailGarbage
    {
        private readonly int _id;
        public override int Id
        {
            get { return _id; }
        }

        private readonly string _path;
        public override string Path
        {
            get { return _path; }
        }

        public MailMessageGarbage(string user, int id, string stream)
        {
            _id = id;
            _path = MailStoragePathCombiner.GetBodyKey(user, stream);
        }
    }
}
