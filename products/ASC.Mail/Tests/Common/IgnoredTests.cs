///*
// *
// * (c) Copyright Ascensio System Limited 2010-2020
// *
// * This program is freeware. You can redistribute it and/or modify it under the terms of the GNU 
// * General Public License (GPL) version 3 as published by the Free Software Foundation (https://www.gnu.org/copyleft/gpl.html). 
// * In accordance with Section 7(a) of the GNU GPL its Section 15 shall be amended to the effect that 
// * Ascensio System SIA expressly excludes the warranty of non-infringement of any third-party rights.
// *
// * THIS PROGRAM IS DISTRIBUTED WITHOUT ANY WARRANTY; WITHOUT EVEN THE IMPLIED WARRANTY OF MERCHANTABILITY OR
// * FITNESS FOR A PARTICULAR PURPOSE. For more details, see GNU GPL at https://www.gnu.org/copyleft/gpl.html
// *
// * You can contact Ascensio System SIA by email at sales@onlyoffice.com
// *
// * The interactive user interfaces in modified source and object code versions of ONLYOFFICE must display 
// * Appropriate Legal Notices, as required under Section 5 of the GNU GPL version 3.
// *
// * Pursuant to Section 7 § 3(b) of the GNU GPL you must retain the original ONLYOFFICE logo which contains 
// * relevant author attributions when distributing the software. If the display of the logo in its graphic 
// * form is not reasonably feasible for technical reasons, you must include the words "Powered by ONLYOFFICE" 
// * in every copy of the program you distribute. 
// * Pursuant to Section 7 § 3(e) we decline to grant you any rights under trademark law for use of our trademarks.
// *
//*/


//using System;
//using System.IO;
//using System.Linq;
//using ASC.Common.Logging;
//using ASC.Core;
//using ASC.Mail.Clients;
//using ASC.Mail.Core;
//using ASC.Mail.Core.Dao.Expressions.Mailbox;
//using ASC.Mail.Core.Engine;
//using ASC.Mail.Enums;
//using ASC.Mail.Extensions;
//using NUnit.Framework;

//namespace ASC.Mail.Aggregator.Tests.Common
//{
//    [TestFixture, Explicit]
//    internal class IgnoredTests
//    {
//        private static readonly string TestFolderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\Data\");

//        [Test, Explicit]
//        public void SaveMessageToMailbox()
//        {
//            const int mailbox_id = 1;

//            var message = MailClient.ParseMimeMessage(TestFolderPath + "message_emails_with_quotas.eml");

//            var mailMessageItem = message.CreateMailMessage();

//            var engine = new EngineFactory(-1);

//            var mbox = engine.MailboxEngine.GetMailboxData(new ConcreteSimpleMailboxExp(mailbox_id));

//            Assert.IsNotNull(mbox);

//            CoreContext.TenantManager.SetCurrentTenant(mbox.TenantId);

//            SecurityContext.AuthenticateMe(new Guid(mbox.UserId));

//            MessageEngine.StoreMailBody(mbox, mailMessageItem, new NullLog());

//            if (mailMessageItem.Attachments.Any())
//            {
//                engine.AttachmentEngine.StoreAttachments(mbox, mailMessageItem.Attachments, mailMessageItem.StreamId);
//            }

//            var id = engine.MessageEngine.MailSave(mbox, mailMessageItem, 0,
//                FolderType.Inbox, FolderType.Inbox, null, "1-777", "", true);

//            Assert.IsFalse(id < 0);
//        }

//    }
//}
