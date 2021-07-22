using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;

using ASC.Common;
using ASC.Mail.Core;
using ASC.Mail.Models;

namespace ASC.Mail.Aggregator.CollectionService.Queue
{
    [Scope]
    public class MailQueueItemSettings
    {
        public Dictionary<string, int> ImapFlags { get; private set; }
        public string[] SkipImapFlags { get; private set; }
        public Dictionary<string, Dictionary<string, MailBoxData.MailboxInfo>> SpecialDomainFolders { get; private set; }
        public Dictionary<string, int> DefaultFolders { get; private set; }

        public MailQueueItemSettings(IMailDaoFactory mailDaoFactory)
        {
            var imapFlags = mailDaoFactory.GetImapFlagsDao().GetImapFlags();

            SkipImapFlags = imapFlags.FindAll(i => i.Skip).ConvertAll(i => i.Name).ToArray();

            ImapFlags = new Dictionary<string, int>();
            imapFlags.FindAll(i => !i.Skip).ForEach(i => { ImapFlags[i.Name] = i.FolderId; });

            var serverFolderAccessInfos = mailDaoFactory.GetImapSpecialMailboxDao().GetServerFolderAccessInfoList();

            SpecialDomainFolders = new Dictionary<string, Dictionary<string, MailBoxData.MailboxInfo>>();

            foreach (var specialMB in serverFolderAccessInfos)
            {
                foreach (var boxInfoPair in specialMB.FolderAccessList)
                {
                    var mb = new MailBoxData.MailboxInfo
                    {
                        folder_id = boxInfoPair.Value.folder_id,
                        skip = boxInfoPair.Value.skip
                    };

                    if (SpecialDomainFolders.Keys.Contains(specialMB.Server))
                    {
                        SpecialDomainFolders[specialMB.Server][boxInfoPair.Key] = mb;
                    }
                    else
                    {
                        SpecialDomainFolders[specialMB.Server] = new Dictionary<string, MailBoxData.MailboxInfo>
                        {
                            { boxInfoPair.Key, mb }
                        };
                    }

                }
            }

            DefaultFolders = GetDefaultFolders();
        }

        private static Dictionary<string, int> GetDefaultFolders()
        {
            var list = new Dictionary<string, int>
            {
                {"inbox", 1},
                {"sent", 2},
                {"sent items", 2},
                {"drafts", 3},
                {"trash", 4},
                {"spam", 5},
                {"junk", 5},
                {"bulk", 5}
            };

            try
            {
                if (!string.IsNullOrEmpty(ConfigurationManager.AppSettings["mail.folders-mapping"]))
                {
                    list = ConfigurationManager.AppSettings["mail.folders-mapping"]
                        .Split('|')
                        .Select(s => s.Split(':'))
                        .ToDictionary(s => s[0].ToLower(), s => Convert.ToInt32(s[1]));
                }
            }
            catch
            {
                //ignore
            }
            return list;
        }
    }
}
