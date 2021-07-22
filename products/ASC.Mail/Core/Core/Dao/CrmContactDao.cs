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


using System.Collections.Generic;
using System.Linq;
using ASC.Common;
using ASC.Core;
using ASC.Core.Common.EF;
using ASC.Mail.Core.Dao.Interfaces;
using ASC.Mail.Enums;

namespace ASC.Mail.Core.Dao
{
    [Scope]
    public class CrmContactDao : BaseMailDao, ICrmContactDao
    {
        public CrmContactDao(
             TenantManager tenantManager,
             SecurityContext securityContext,
             DbContextManager<MailDbContext> dbContext)
            : base(tenantManager, securityContext, dbContext)
        {
        }

        public List<int> GetCrmContactIds(string email)
        {
            var ids = new List<int>();

            if (string.IsNullOrEmpty(email))
                return ids;
            //try
            //{

            var contactList = MailDbContext.CrmContact.Join(MailDbContext.CrmContactInfo, c => c.Id, ci => ci.ContactId,
                (c, ci) => new { 
                    Contact = c,
                    Info = ci
                })
                .Where(o => o.Contact.TenantId == Tenant)
                .Where(o => o.Info.Type == (int)ContactInfoType.Email && o.Info.Data == email)
                .Select(o => new {
                    o.Contact.Id,
                    Company = o.Contact.IsCompany,
                    ShareType = (ShareType)o.Contact.IsShared
                })
                .ToList();

                if (!contactList.Any())
                    return ids;

            //TODO: Fix check access rights 
            ids.AddRange(contactList.Select(c => c.Id));

            //TODO: Fix
            //CoreContext.TenantManager.SetCurrentTenant(Tenant);
            //SecurityContext.AuthenticateMe(new Guid(CurrentUserId));

            //foreach (var info in contactList)
            //{
            //    var contact = info.Company
            //        ? new Company()
            //        : (Contact) new Person();

            //    contact.ID = info.Id;
            //    contact.ShareType = info.ShareType;

            //    if (CRMSecurity.CanAccessTo(contact))
            //    {
            //        ids.Add(info.Id);
            //    }
            //}
            //}
            //catch (Exception e)
            //{
            //    Log.WarnFormat("GetCrmContactsId(tenandId='{0}', userId='{1}', email='{2}') Exception:\r\n{3}\r\n",
            //        Tenant, CurrentUserId, email, e.ToString());
            //}

            return ids;
        }
    }
}