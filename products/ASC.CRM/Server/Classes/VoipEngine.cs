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


using System;
using System.Collections.Generic;
using System.Linq;

using ASC.Common;
using ASC.Common.Logging;
using ASC.Common.Threading.Workers;
using ASC.Core;
using ASC.Core.Tenants;
using ASC.CRM.Core;
using ASC.CRM.Core.Dao;
using ASC.CRM.Core.Entities;
using ASC.CRM.Core.Enums;
using ASC.CRM.Resources;
using ASC.VoipService;
using ASC.VoipService.Dao;

using Microsoft.Extensions.Options;

namespace ASC.Web.CRM.Classes
{
    [Scope]
    public class VoipEngine
    {
        private readonly WorkerQueue<QueueItem> Queue = new WorkerQueue<QueueItem>();

        private readonly object Locker = new object();

        public VoipEngine(DaoFactory daoFactory,
                         CrmSecurity crmSecurity,
                         TenantUtil tenantUtil,
                         SecurityContext securityContext,
                         IOptionsMonitor<ILog> logger,
                         TenantManager tenantManager,
                         VoipDao voipDao)
        {
            CRMSecurity = crmSecurity;
            TenantUtil = tenantUtil;
            SecurityContext = securityContext;
            Logger = logger.Get("ASC.CRM");
            TenantManager = tenantManager;
            VoipDao = voipDao;
            DaoFactory = daoFactory;
        }

        public DaoFactory DaoFactory { get; }

        public VoipDao VoipDao { get; }

        public TenantManager TenantManager { get; }

        public ILog Logger { get; }

        public int TenantId { get; }

        public SecurityContext SecurityContext { get; }

        public TenantUtil TenantUtil { get; }

        public CrmSecurity CRMSecurity { get; }

        public VoipCall SaveOrUpdateCall(VoipCall callHistory)
        {
            var dao = DaoFactory.GetVoipDao();
            var call = dao.GetCall(callHistory.Id) ?? callHistory;

            if (string.IsNullOrEmpty(call.ParentID))
            {
                GetContact(call);
            }

            if (!callHistory.AnsweredBy.Equals(Guid.Empty))
            {
                call.AnsweredBy = callHistory.AnsweredBy;
            }

            if (!callHistory.Date.Equals(default(DateTime)))
            {
                call.Date = callHistory.Date;
            }

            if (!callHistory.EndDialDate.Equals(default(DateTime)))
            {
                call.EndDialDate = callHistory.EndDialDate;
            }

            if (call.Price == 0 && callHistory.Price != default(decimal))
            {
                call.Price = callHistory.Price;
            }

            if (call.DialDuration == 0)
            {
                call.DialDuration = callHistory.DialDuration;
            }

            if (call.VoipRecord == null)
            {
                call.VoipRecord = new VoipRecord();
            }

            if (string.IsNullOrEmpty(call.VoipRecord.Id))
            {
                call.VoipRecord.Id = callHistory.VoipRecord.Id;
            }

            if (call.VoipRecord.Price == default(decimal))
            {
                call.VoipRecord.Price = callHistory.VoipRecord.Price;
            }

            if (string.IsNullOrEmpty(call.VoipRecord.Uri))
            {
                call.VoipRecord.Uri = callHistory.VoipRecord.Uri;
            }

            if (call.VoipRecord.Duration == 0)
            {
                call.VoipRecord.Duration = callHistory.VoipRecord.Duration;
            }

            if (callHistory.Status.HasValue)
            {
                call.Status = callHistory.Status;
            }

            return dao.SaveOrUpdateCall(call);
        }

        public void AddHistoryToCallContact(VoipCall call, DaoFactory daoFactory)
        {
            var listItemDao = daoFactory.GetListItemDao();

            if (call == null || call.ContactId == 0) return;

            var category = listItemDao.GetByTitle(ListType.HistoryCategory, CRMCommonResource.HistoryCategory_Call);
            if (category == null)
            {
                category = new ListItem(CRMCommonResource.HistoryCategory_Call, "event_category_call.png");
                category.ID = listItemDao.CreateItem(ListType.HistoryCategory, category);
            }
            var contact = daoFactory.GetContactDao().GetByID(call.ContactId);
            if (contact != null && CRMSecurity.CanAccessTo(contact))
            {
                var note = call.Status == VoipCallStatus.Incoming || call.Status == VoipCallStatus.Answered
                    ? CRMContactResource.HistoryVoipIncomingNote
                    : CRMContactResource.HistoryVoipOutcomingNote;
                var content = string.Format(note, call.DialDuration);

                var relationshipEvent = new RelationshipEvent
                {
                    CategoryID = category.ID,
                    EntityType = EntityType.Any,
                    EntityID = 0,
                    Content = content,
                    ContactID = contact.ID,
                    CreateOn = TenantUtil.DateTimeFromUtc(DateTime.UtcNow),
                    CreateBy = SecurityContext.CurrentAccount.ID
                };

                daoFactory.GetRelationshipEventDao().CreateItem(relationshipEvent);
            }
        }

        public Contact GetContact(VoipCall call)
        {
            if (call.ContactId != 0)
            {
                return null;
            }

            var contactPhone = call.Status == VoipCallStatus.Incoming || call.Status == VoipCallStatus.Answered ? call.From : call.To;

            var newContactIds = DaoFactory.GetContactDao().GetContactIDsByContactInfo(ContactInfoType.Phone, contactPhone.TrimStart('+'), null, true);

            foreach (var newContactId in newContactIds)
            {
                if (newContactId != 0)
                {
                    var existContact = DaoFactory.GetContactDao().GetByID(newContactId);
                    if (CRMSecurity.CanAccessTo(existContact))
                    {
                        call.ContactId = newContactId;
                        return existContact;
                    }
                }
            }

            return null;
        }

        public List<Contact> GetContacts(string contactPhone, DaoFactory daoFactory)
        {
            var dao = daoFactory.GetContactDao();
            var ids = dao.GetContactIDsByContactInfo(ContactInfoType.Phone, contactPhone.TrimStart('+'), null, true);
            return ids.Select(r => dao.GetByID(r)).ToList();
        }

        public void SaveAdditionalInfo(string callId)
        {
            lock (Locker)
            {
                if (!Queue.IsStarted)
                {
                    Queue.Start(SaveAdditionalInfoAction);
                }

                Queue.Add(new QueueItem { CallID = callId, TenantID = TenantId });
            }
        }

        private void SaveAdditionalInfoAction(QueueItem queueItem)
        {
            try
            {
                TenantManager.SetCurrentTenant(queueItem.TenantID);

                var voipEngine = new VoipEngine(DaoFactory,
                                                CRMSecurity,
                                                TenantUtil,
                                                SecurityContext,
                                                null,
                                                TenantManager,
                                                VoipDao);

                var dao = DaoFactory.GetVoipDao();

                var call = dao.GetCall(queueItem.CallID);

                GetPriceAndDuration(call);

                if (call.ChildCalls.Any())
                {
                    call.ChildCalls.ForEach(r =>
                    {
                        GetPriceAndDuration(r);
                        voipEngine.SaveOrUpdateCall(r);
                    });
                }

                call = voipEngine.SaveOrUpdateCall(call);

                if (!string.IsNullOrEmpty(call.VoipRecord.Id))
                {
                    call.VoipRecord = VoipDao.GetProvider().GetRecord((string)call.Id, (string)call.VoipRecord.Id);
                    voipEngine.SaveOrUpdateCall(call);
                }

                SecurityContext.AuthenticateMe(call.AnsweredBy);
                AddHistoryToCallContact(call, DaoFactory);

            }
            catch (Exception ex)
            {
                Logger.ErrorFormat("SaveAdditionalInfo {0}, {1}", ex, ex.StackTrace);
            }
        }

        private void GetPriceAndDuration(VoipCall call)
        {
            var provider = VoipDao.GetProvider();
            var twilioCall = provider.GetCall(call.Id);
            call.Price = twilioCall.Price;
            call.DialDuration = twilioCall.DialDuration;
        }

        public void AnswerCall(VoipCall call)
        {
            call.AnsweredBy = SecurityContext.CurrentAccount.ID;
            call.Status = VoipCallStatus.Answered;
            DaoFactory.GetVoipDao().SaveOrUpdateCall(call);
        }

        public Contact CreateContact(string contactPhone)
        {
            var contact = new Person
            {
                FirstName = contactPhone,
                LastName = TenantUtil.DateTimeFromUtc(DateTime.UtcNow).ToString("yyyy-MM-dd hh:mm"),
                ShareType = ShareType.None,
                CreateBy = SecurityContext.CurrentAccount.ID,
                CreateOn = DateTime.UtcNow
            };

            contact.ID = DaoFactory.GetContactDao().SaveContact(contact);

            DaoFactory.GetContactInfoDao()
                .Save(new ContactInfo
                {
                    ContactID = contact.ID,
                    IsPrimary = true,
                    InfoType = ContactInfoType.Phone,
                    Data = contactPhone
                });

            CRMSecurity.SetAccessTo(contact, new List<Guid> { SecurityContext.CurrentAccount.ID });

            return contact;
        }

        private class QueueItem
        {
            public int TenantID { get; set; }
            public string CallID { get; set; }
        }
    }
}