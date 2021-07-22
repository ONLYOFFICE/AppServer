/*
 *
 * (c) Copyright Ascensio System Limited 2010-2018
 *
 * This program is freeware. You can redistribute it and/or modify it under the terms of the GNU 
 * General Public License (GPL) version 3 as published by the Free Software Foundation (https://www.gnu.org/copyleft/gpl.html). 
 * In accordance with Section 7(a) of the GNU GPL its Section 15 shall be amended to the effect that 
 * Ascensio System SIA expressly excludes the warranty of non-infringement of any third-party rights.
 *
 * THIS PROGRAM IS DISTRIBUTED WITHOUT ANY WARRANTY; WITHOUT EVEN THE IMPLIED WARRANTY OF MERCHANTABILITY OR
 * FITNESS FOR A PARTICULAR PURPOSE. For more details, see GNU GPL at https://www.gnu.org/copyleft/gpl.html
 *
 * You can contact Ascensio System SIA by email at sales@onlyoffice.com
 *
 * The interactive user interfaces in modified source and object code versions of ONLYOFFICE must display 
 * Appropriate Legal Notices, as required under Section 5 of the GNU GPL version 3.
 *
 * Pursuant to Section 7 § 3(b) of the GNU GPL you must retain the original ONLYOFFICE logo which contains 
 * relevant author attributions when distributing the software. If the display of the logo in its graphic 
 * form is not reasonably feasible for technical reasons, you must include the words "Powered by ONLYOFFICE" 
 * in every copy of the program you distribute. 
 * Pursuant to Section 7 § 3(e) we decline to grant you any rights under trademark law for use of our trademarks.
 *
*/


using ASC.Mail.Core.Dao.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace ASC.Mail.Core.Dao.Expressions.Conversation
{
    public class SimpleConversationsExp : IConversationsExp
    {
        public int Tenant { get; private set; }
        public string User { get; private set; }

        public List<int> FoldersIds { get; set; }
        public List<string> ChainIds { get; set; }

        public int? Folder { get; set; }
        public int? MailboxId { get; set; }
        public bool? Unread { get; set; }
        public string ChainId { get; set; }

        public SimpleConversationsExp(int tenant, string user)
        {
            Tenant = tenant;
            User = user;
        }

        public static ConversationsExpBuilder CreateBuilder(int tenant, string user)
        {
            return new ConversationsExpBuilder(tenant, user);
        }
        
        public Expression<Func<MailChain, bool>> GetExpression()
        {
            Expression<Func<MailChain, bool>> exp = c => c.Tenant == Tenant;

            if (!string.IsNullOrEmpty(User))
            {
                exp = exp.And(c => c.IdUser == User);
            }

            if (FoldersIds != null && FoldersIds.Any())
            {
                exp = exp.And(c => FoldersIds.Contains((int)c.Folder));
            }

            if (Folder.HasValue)
            {
                exp = exp.And(c => c.Folder == Folder.Value);
            }

            if (ChainIds != null && ChainIds.Any())
            {
                exp = exp.And(c => ChainIds.Contains(c.Id));
            }

            if (MailboxId.HasValue)
            {
                exp = exp.And(c => c.IdMailbox == MailboxId.Value);
            }

            if (!string.IsNullOrEmpty(ChainId))
            {
                exp = exp.And(c => c.Id == ChainId);
            }

            if (Unread.HasValue)
            {
                exp = exp.And(c => c.Unread == Unread.Value);
            }

            return exp;
        }
    }
}