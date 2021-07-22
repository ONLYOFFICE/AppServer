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
//using System.Collections.Generic;
//using System.IO;
//using System.Linq;
//using ASC.Core;
//using ASC.Core.Users;
//using ASC.Mail.Aggregator.Tests.Common.Utils;
//using ASC.Mail.Core;
//using ASC.Mail.Core.Dao.Expressions.Mailbox;
//using ASC.Mail.Core.Engine;
//using ASC.Mail.Models;
//using ASC.Mail.Enums;
//using ASC.Mail.Utils;
//using NUnit.Framework;

//namespace ASC.Mail.Aggregator.Tests.Garbage
//{
//    [TestFixture]
//    public class UserMailEraseTests
//    {
//        public UserInfo TestUser1 { get; private set; }
//        public UserInfo TestUser2 { get; private set; }

//        public List<MailBoxData> TestUser1Mailboxes { get; set; }
//        public List<MailBoxData> TestUser2Mailboxes { get; set; }

//        public const int CURRENT_TENANT = 0;
//        public const string PASSWORD = "123456";
//        public const string DOMAIN = "gmail.com";

//        private static readonly string TestFolderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
//            @"..\..\Data\");

//        private const string EML1_FILE_NAME = @"bad_encoding.eml";
//        private const string EML2_FILE_NAME = @"embed_image.eml";
//        private static readonly string Eml1Path = TestFolderPath + EML1_FILE_NAME;
//        private static readonly string Eml2Path = TestFolderPath + EML2_FILE_NAME;

//        [SetUp]
//        public void Init()
//        {
//            TestUser1 = TestHelper.CreateNewRandomEmployee();

//            TestUser2 = TestHelper.CreateNewRandomEmployee();

//            var engine = new EngineFactory(CURRENT_TENANT);

//            var mailboxSettings = engine.MailBoxSettingEngine.GetMailBoxSettings(DOMAIN);

//            var testMailboxes = mailboxSettings.ToMailboxList(TestUser1.Email, PASSWORD, CURRENT_TENANT, TestUser1.ID.ToString());

//            var mbox = testMailboxes.FirstOrDefault();

//            if (mbox == null || !engine.MailboxEngine.SaveMailBox(mbox))
//            {
//                throw new Exception(string.Format("Can't create mailbox with email: {0}", TestUser1.Email));
//            }

//            testMailboxes = mailboxSettings.ToMailboxList(TestUser2.Email, PASSWORD, CURRENT_TENANT, TestUser2.ID.ToString());

//            var mboxUser2 = testMailboxes.FirstOrDefault();

//            if (!engine.MailboxEngine.SaveMailBox(mboxUser2))
//            {
//                throw new Exception(string.Format("Can't create mailbox with email: {0}", TestUser2.Email));
//            }

//            TestUser2Mailboxes = new List<MailBoxData> {mboxUser2};

//            const string email2 = "test2@mail.ru";

//            var test2Mailboxes = mailboxSettings.ToMailboxList(email2, PASSWORD, CURRENT_TENANT, TestUser1.ID.ToString());

//            var mbox2 = test2Mailboxes.FirstOrDefault();

//            if (!engine.MailboxEngine.SaveMailBox(mbox2))
//            {
//                throw new Exception(string.Format("Can't create mailbox with email: {0}", email2));
//            }

//            const string email3 = "test3@mail.ru";

//            var test3Mailboxes = mailboxSettings.ToMailboxList(email3, PASSWORD, CURRENT_TENANT, TestUser1.ID.ToString());

//            var mbox3 = test3Mailboxes.FirstOrDefault();

//            if (!engine.MailboxEngine.SaveMailBox(mbox3))
//            {
//                throw new Exception(string.Format("Can't create mailbox with email: {0}", email3));
//            }

//            TestUser1Mailboxes = new List<MailBoxData> {mbox, mbox2, mbox3};

//            engine = new EngineFactory(CURRENT_TENANT, TestUser1.ID.ToString());

//            var baseFolder = engine.UserFolderEngine.Create("Folder 1");

//            if (baseFolder == null || baseFolder.Id < 0)
//            {
//                throw new Exception("Can\'t create User Folder");
//            }

//            int mailId1;

//            using (var fs = new FileStream(Eml1Path, FileMode.Open, FileAccess.Read))
//            {
//                mailId1 = engine.TestEngine.LoadSampleMessage((int)FolderType.UserFolder, baseFolder.Id,
//                    mbox.MailBoxId, true, fs);
//            }

//            if (mailId1 < 0)
//            {
//                throw new Exception(string.Format("Can't save test mail to mailbox: {0}", TestUser1.Email));
//            }
//        }

//        [TearDown]
//        public void Cleanup()
//        {
//            if (TestUser1 == null || TestUser1.ID == Guid.Empty) 
//                return;

//            CoreContext.TenantManager.SetCurrentTenant(CURRENT_TENANT);

//            SecurityContext.AuthenticateMe(ASC.Core.Configuration.Constants.CoreSystem);

//            CoreContext.UserManager.DeleteUser(TestUser1.ID);
//            CoreContext.UserManager.DeleteUser(TestUser2.ID);

//            // Clear TestUser1 mailboxes
//            var eraser = new MailGarbageEngine();
//            eraser.ClearUserMail(TestUser1.ID, CoreContext.TenantManager.GetCurrentTenant());

//            // Clear TestUser2 mailboxes
//            eraser = new MailGarbageEngine();
//            eraser.ClearUserMail(TestUser2.ID, CoreContext.TenantManager.GetCurrentTenant());
//        }

//        [Test]
//        public void ClearUserMailTest()
//        {
//            var eraser = new MailGarbageEngine();

//            var tenant = CoreContext.TenantManager.GetCurrentTenant();

//            eraser.ClearUserMail(TestUser1.ID, tenant);

//            var engine = new EngineFactory(tenant.TenantId, TestUser1.ID.ToString());

//            foreach (var mailbox in TestUser1Mailboxes)
//            {
//                var mb =
//                    engine.MailboxEngine.GetMailboxData(
//                        new СoncreteUserMailboxExp(mailbox.MailBoxId, tenant.TenantId, TestUser1.ID.ToString()));
//                Assert.Null(mb);
//            }

//            var userFolders = engine.UserFolderEngine.GetList();

//            Assert.IsEmpty(userFolders);

//            foreach (var mailbox in TestUser2Mailboxes)
//            {
//                var mb =
//                   engine.MailboxEngine.GetMailboxData(
//                       new СoncreteUserMailboxExp(mailbox.MailBoxId, tenant.TenantId, TestUser2.ID.ToString()));
//                Assert.NotNull(mb);
//            }
//        }

//        [Test]
//        public void ClearInvalidUserMailTest()
//        {
//            var eraser = new MailGarbageEngine();

//            var tenant = CoreContext.TenantManager.GetCurrentTenant();

//            eraser.ClearUserMail(Guid.NewGuid(), tenant);
//        }
//    }
//}
