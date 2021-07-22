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


using ASC.Common;
using ASC.Core;
using ASC.Core.Common.EF;
using ASC.Mail.Core.Dao.Entities;
using ASC.Mail.Core.Dao.Expressions.Contact;
using ASC.Mail.Core.Dao.Interfaces;
using ASC.Mail.Core.Entities;
using ASC.Mail.Enums;
using System.Collections.Generic;
using System.Linq;

namespace ASC.Mail.Core.Dao
{
    [Scope]
    public class ContactCardDao : BaseMailDao, IContactCardDao
    {
        public ContactCardDao(
             TenantManager tenantManager,
             SecurityContext securityContext,
             DbContextManager<MailDbContext> dbContext)
            : base(tenantManager, securityContext, dbContext)
        {
        }

        public ContactCard GetContactCard(int id)
        {
            var contacts = MailDbContext.MailContacts
                .Where(c => c.TenantId == Tenant && c.IdUser == UserId && c.Id == id)
                .Select(ToContact)
                .ToList();

            var contactInfos = MailDbContext.MailContactInfo
                .Where(c => c.IdContact == id)
                .Select(ToContactInfo)
                .ToList();

            var result = ToContactCardList(contacts, contactInfos)
                .SingleOrDefault();

            return result;
        }

        public List<ContactCard> GetContactCards(IContactsExp exp)
        {
            var query = MailDbContext.MailContacts
                .Where(exp.GetExpression());

            if (exp.OrderAsc.HasValue)
            {
                if (exp.OrderAsc.Value)
                {
                    query.OrderBy(r => r.Name);
                }
                else {
                    query.OrderByDescending(r => r.Name);
                }
            }

            if (exp.StartIndex.HasValue)
            {
                query.Skip(exp.StartIndex.Value);
            }

            if (exp.Limit.HasValue)
            {
                query.Take(exp.Limit.Value);
            }

            var contacts = query
                .Select(ToContact)
                .ToList();

            var ids = contacts.Select(c => c.Id).ToList();

            var contactInfos = MailDbContext.MailContactInfo
                .Where(c => ids.Contains((int)c.IdContact))
                .Select(ToContactInfo)
                .ToList();

            return ToContactCardList(contacts, contactInfos);
        }

        public int GetContactCardsCount(IContactsExp exp)
        {
            var count = MailDbContext.MailContacts
                .Where(exp.GetExpression())
                .Join(MailDbContext.MailContactInfo.DefaultIfEmpty(), c => c.Id, ci => ci.IdContact,
                (c, ci) => new
                {
                    Contact = c,
                    Info = ci
                })
                .Select(fci => fci.Contact.Id)
                .Distinct()
                .Count();

            return count;
        }

        protected List<ContactCard> ToContactCardList(List<Contact> contacts, List<ContactInfo> contactInfos)
        {
            return
                contacts.Select(
                    contact => new ContactCard(contact, contactInfos.Where(ci => ci.ContactId == contact.Id).ToList()))
                    .ToList();
        }

        protected Contact ToContact(MailContact r)
        {
            var c = new Contact
            {
                Id = (int)r.Id,
                User = r.IdUser,
                Tenant = r.TenantId,
                ContactName = r.Name,
                Address = r.Address,
                Description = r.Description,
                Type = (ContactType) r.Type,
                HasPhoto = r.HasPhoto
            };

            return c;
        }

        protected ContactInfo ToContactInfo(MailContactInfo r)
        {
            var c = new ContactInfo
            {
                Id = (int)r.Id,
                Tenant = r.TenantId,
                User = r.IdUser,
                ContactId = (int)r.IdContact,
                Data = r.Data,
                Type = r.Type,
                IsPrimary = r.IsPrimary
            };

            return c;
        }
    }
}