using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using ASC.Common;
using ASC.Core;

namespace ASC.Mail.Core.Engine
{
    [Scope]
    public class MailEnginesFactory
    {
        private TenantManager _tenantManager;
        private SecurityContext _securityContext;

        public int Tenant => _tenantManager.GetCurrentTenant().TenantId;
        public string UserId => _securityContext.CurrentAccount.ID.ToString();

        private AutoreplyEngine _autoreplyEngine;
        private CalendarEngine _calendarEngine;
        private IndexEngine _indexEngine;
        private TagEngine _tagEngine;
        private CrmLinkEngine _crmLinkEngine;
        private EmailInEngine _emailInEngine;
        private FilterEngine _filterEngine;
        private MailboxEngine _mailboxEngine;
        private MessageEngine _messageEngine;

        public AutoreplyEngine AutoreplyEngine => _autoreplyEngine;
        public CalendarEngine CalendarEngine => _calendarEngine;
        public IndexEngine IndexEngine => _indexEngine;
        public TagEngine TagEngine => _tagEngine;
        public CrmLinkEngine CrmLinkEngine => _crmLinkEngine;
        public EmailInEngine EmailInEngine => _emailInEngine;
        public FilterEngine FilterEngine => _filterEngine;
        public MailboxEngine MailboxEngine => _mailboxEngine;
        public MessageEngine MessageEngine => _messageEngine;

        public MailEnginesFactory(
            AutoreplyEngine autoreplyEngine, 
            CalendarEngine calendarEngine, 
            IndexEngine indexEngine, 
            TagEngine tagEngine, 
            CrmLinkEngine crmLinkEngine, 
            EmailInEngine emailInEngine, 
            FilterEngine filterEngine, 
            MailboxEngine mailboxEngine,
            MessageEngine messageEngine,
            TenantManager tenantManager,
            SecurityContext securityContext)
        {
            _autoreplyEngine = autoreplyEngine;
            _calendarEngine = calendarEngine;
            _indexEngine = indexEngine;
            _tagEngine = tagEngine;
            _crmLinkEngine = crmLinkEngine;
            _emailInEngine = emailInEngine;
            _filterEngine = filterEngine;
            _mailboxEngine = mailboxEngine;
            _messageEngine = messageEngine;

            _tenantManager = tenantManager;
            _securityContext = securityContext;
        }
    }
}
