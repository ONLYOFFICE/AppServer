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


//using ASC.Core;
//using ASC.Core.Users;
//using ASC.Mail.Core;
//using ASC.Mail.Core.Engine.Operations.Base;
//using ASC.Mail.Models;
//using ASC.Mail.Enums;
//using ASC.Mail.Utils;
//using NUnit.Framework;
//using System;
//using System.Collections.Generic;
//using System.IO;
//using System.IO.Compression;
//using System.Linq;
//using System.Net;
//using System.Threading;
//using ASC.Mail.Aggregator.Tests.Common.Utils;

//namespace ASC.Mail.Aggregator.Tests.Common.Engine
//{
//    [TestFixture]
//    internal class OperationEngineTests
//    {
//        private const int CURRENT_TENANT = 0;
//        public const string PASSWORD = "123456";
//        public const string DOMAIN = "gmail.com";

//        public UserInfo TestUser { get; private set; }
//        public EngineFactory Factory { get; private set; }
//        public MailBoxData TestMailbox { get; private set; }

//        private static readonly string TestFolderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
//           @"..\..\Data\");
//        private const string EML1_FILE_NAME = @"with_attachments.eml";
//        private const string EML2_FILE_NAME = @"embed_image.eml";
//        private const string EML3_FILE_NAME = @"medium_sample.eml";
//        private static readonly string Eml1Path = TestFolderPath + EML1_FILE_NAME;
//        private static readonly string Eml2Path = TestFolderPath + EML2_FILE_NAME;
//        private static readonly string Eml3Path = TestFolderPath + EML3_FILE_NAME;
//        private const string NOATTACHMENTS_ERROR_STRING = "No attachments in message";
//        private const string DOWNLOAD_LINK = "http://localhost/products/files/httphandlers/filehandler.ashx?action=bulk";
//        private const string STATUS_FINISHED = "Finished";

//        [SetUp]
//        public void SetUp()
//        {
//            CoreContext.TenantManager.SetCurrentTenant(CURRENT_TENANT);

//            SecurityContext.AuthenticateMe(ASC.Core.Configuration.Constants.CoreSystem);

//            TestUser = TestHelper.CreateNewRandomEmployee();

//            SecurityContext.AuthenticateMe(TestUser.ID);

//            Factory = new EngineFactory(CURRENT_TENANT, TestUser.ID.ToString());

//            var mailboxSettings = Factory.MailBoxSettingEngine.GetMailBoxSettings(DOMAIN);

//            var testMailboxes = mailboxSettings.ToMailboxList(TestUser.Email, PASSWORD, CURRENT_TENANT, TestUser.ID.ToString());

//            TestMailbox = testMailboxes.FirstOrDefault();

//            if (TestMailbox == null || !Factory.MailboxEngine.SaveMailBox(TestMailbox))
//            {
//                throw new Exception(string.Format("Can't create mailbox with email: {0}", TestUser.Email));
//            }
//        }

//        [TearDown]
//        public void CleanUp()
//        {
//            if (TestUser == null || TestUser.ID == Guid.Empty)
//                return;

//            CoreContext.TenantManager.SetCurrentTenant(CURRENT_TENANT);

//            SecurityContext.AuthenticateMe(ASC.Core.Configuration.Constants.CoreSystem);

//            CoreContext.UserManager.DeleteUser(TestUser.ID);

//            var eraser = Factory.MailGarbageEngine;

//            eraser.ClearUserMail(TestUser.ID, CoreContext.TenantManager.GetCurrentTenant());
//        }

//        private HttpWebRequest CreateWebRequest(string url)
//        {
//            var authenticationCookie = SecurityContext.AuthenticateMe(TestUser.ID);

//            var httpRequest = (HttpWebRequest)WebRequest.Create(url);
//            httpRequest.AllowAutoRedirect = true;

//            var cookie = new Cookie("asc_auth_key", authenticationCookie, "/", "localhost");

//            httpRequest.CookieContainer = new CookieContainer();

//            httpRequest.CookieContainer.Add(cookie);

//            if (!string.IsNullOrWhiteSpace(authenticationCookie))
//            {
//                httpRequest.Headers["Authorization"] = authenticationCookie;
//            }

//            return httpRequest;
//        }

//        [Test]
//        public void DownloadAllAttachmentsOperationNoAttachmentsTest()
//        {
//            var draftItem = new MailDraftData(0, TestMailbox, "test@gmail.com", new List<string>(),
//                new List<string>(), new List<string>(), "Test subject", MailUtil.CreateMessageId(),
//                null, false, null, "Test body", MailUtil.CreateStreamId(), new List<MailAttachmentData>());

//            var draftData = Factory.DraftEngine.Save(draftItem);

//            var operation = Factory.OperationEngine.DownloadAllAttachments(draftData.Id);

//            MailOperationStatus operationStatus;

//            do
//            {
//                Thread.Sleep(100);

//                operationStatus = Factory.OperationEngine.GetMailOperationStatus(operation.Id);

//            } while (!operationStatus.Completed);

//            Assert.IsTrue(operationStatus.Completed);
//            Assert.AreEqual(NOATTACHMENTS_ERROR_STRING, operationStatus.Error);
//        }

//        [Test]
//        public void DownloadAllAttachmentsOperationOneAttachmentTest()
//        {
//            var simpleMessage = Factory.TestEngine.CreateSampleMessage((int?)FolderType.Inbox,
//                TestMailbox.MailBoxId, new List<string> { TestMailbox.EMailView }, new List<string>(),
//                new List<string>(), false, true, "Test subject", "Test body");

//            Assert.Greater(simpleMessage, 0);

//            MailAttachmentData attachmentData;

//            using (var fs = new FileStream(Eml1Path, FileMode.Open, FileAccess.Read))
//            {
//                attachmentData = Factory.TestEngine.AppendAttachmentsToSampleMessage(simpleMessage,
//                    EML1_FILE_NAME, fs, "message/eml");
//            }

//            Assert.IsNotNull(attachmentData);
//            Assert.Greater(attachmentData.fileId, 0);

//            var message = Factory.MessageEngine.GetMessage(simpleMessage, new MailMessageData.Options());

//            Assert.AreEqual(1, message.Attachments.Count);

//            var operation = Factory.OperationEngine.DownloadAllAttachments(message.Id);

//            MailOperationStatus operstionStatus;

//            do
//            {
//                Thread.Sleep(100);

//                operstionStatus = Factory.OperationEngine.GetMailOperationStatus(operation.Id);

//            } while (!operstionStatus.Completed);

//            Assert.IsTrue(operstionStatus.Completed);
//            Assert.AreEqual(operstionStatus.Status, STATUS_FINISHED);
//            Assert.AreEqual(operstionStatus.Source, DOWNLOAD_LINK);

//            var request = CreateWebRequest(DOWNLOAD_LINK);

//            using (var zipStream = new MemoryStream())
//            {
//                using (var response = (HttpWebResponse)request.GetResponse())
//                {
//                    using (var stream = response.GetResponseStream())
//                    {
//                        if (stream != null)
//                            stream.CopyTo(zipStream);
//                    }
//                }

//                using (var archive = new ZipArchive(zipStream,
//                    ZipArchiveMode.Update))
//                {
//                    Assert.AreEqual(archive.Entries.Count, 1);
//                    Assert.AreEqual(17448499, archive.Entries[0].Length);
//                    Assert.AreEqual(EML1_FILE_NAME, archive.Entries[0].Name);
//                }

//            }
//        }

//        [Test]
//        public void DownloadAllAttachmentsOperationManyAttachmentTest()
//        {
//            var simpleMessage = Factory.TestEngine.CreateSampleMessage((int?)FolderType.Inbox,
//                TestMailbox.MailBoxId, new List<string> { TestMailbox.EMailView }, new List<string>(),
//                new List<string>(), false, true, "Test subject", "Test body");

//            Assert.Greater(simpleMessage, 0);

//            MailAttachmentData attachmentData;

//            using (var fs = new FileStream(Eml1Path, FileMode.Open, FileAccess.Read))
//            {
//                attachmentData = Factory.TestEngine.AppendAttachmentsToSampleMessage(simpleMessage,
//                    EML1_FILE_NAME, fs, "message/eml");
//            }

//            Assert.IsNotNull(attachmentData);
//            Assert.Greater(attachmentData.fileId, 0);

//            using (var fs = new FileStream(Eml2Path, FileMode.Open, FileAccess.Read))
//            {
//                attachmentData = Factory.TestEngine.AppendAttachmentsToSampleMessage(simpleMessage,
//                    EML2_FILE_NAME, fs, "message/eml");
//            }

//            Assert.IsNotNull(attachmentData);
//            Assert.Greater(attachmentData.fileId, 1);

//            using (var fs = new FileStream(Eml3Path, FileMode.Open, FileAccess.Read))
//            {
//                attachmentData = Factory.TestEngine.AppendAttachmentsToSampleMessage(simpleMessage,
//                    EML3_FILE_NAME, fs, "message/eml");
//            }

//            Assert.IsNotNull(attachmentData);
//            Assert.Greater(attachmentData.fileId, 2);

//            var message = Factory.MessageEngine.GetMessage(simpleMessage, new MailMessageData.Options());

//            Assert.AreEqual(3, message.Attachments.Count);

//            var operation = Factory.OperationEngine.DownloadAllAttachments(message.Id);

//            MailOperationStatus operstionStatus;

//            do
//            {
//                Thread.Sleep(100);

//                operstionStatus = Factory.OperationEngine.GetMailOperationStatus(operation.Id);

//            } while (!operstionStatus.Completed);

//            Assert.IsTrue(operstionStatus.Completed);
//            Assert.AreEqual(operstionStatus.Status, STATUS_FINISHED);
//            Assert.AreEqual(operstionStatus.Source, DOWNLOAD_LINK);

//            var request = CreateWebRequest(DOWNLOAD_LINK);

//            using (var zipStream = new MemoryStream())
//            {
//                using (var response = (HttpWebResponse)request.GetResponse())
//                {
//                    using (var stream = response.GetResponseStream())
//                    {
//                        if (stream != null)
//                            stream.CopyTo(zipStream);
//                    }
//                }

//                using (var archive = new ZipArchive(zipStream,
//                    ZipArchiveMode.Update))
//                {
//                    Assert.AreEqual(archive.Entries.Count, 3);
//                    Assert.AreEqual(17448499, archive.Entries[0].Length);
//                    Assert.AreEqual(EML1_FILE_NAME, archive.Entries[0].Name);
//                    Assert.AreEqual(11457, archive.Entries[1].Length);
//                    Assert.AreEqual(EML2_FILE_NAME, archive.Entries[1].Name);
//                    Assert.AreEqual(70530, archive.Entries[2].Length);
//                    Assert.AreEqual(EML3_FILE_NAME, archive.Entries[2].Name);
//                }

//            }
//        }

//        [Test]
//        public void DownloadAllAttachmentsOperationOneDamagedTest()
//        {
//            var simpleMessage = Factory.TestEngine.CreateSampleMessage((int?)FolderType.Inbox,
//                TestMailbox.MailBoxId, new List<string> { TestMailbox.EMailView }, new List<string>(),
//                new List<string>(), false, true, "Test subject", "Test body");

//            Assert.Greater(simpleMessage, 0);

//            MailAttachmentData attachmentData;

//            using (var fs = new FileStream(Eml1Path, FileMode.Open, FileAccess.Read))
//            {
//                attachmentData = Factory.TestEngine.AppendAttachmentsToSampleMessage(simpleMessage,
//                    EML1_FILE_NAME, fs, "message/eml");
//            }

//            Assert.IsNotNull(attachmentData);
//            Assert.Greater(attachmentData.fileId, 0);

//            attachmentData = Factory.TestEngine.AppendAttachmentsToSampleMessage(simpleMessage,
//                EML2_FILE_NAME, Stream.Null, "message/eml");

//            Assert.IsNotNull(attachmentData);
//            Assert.Greater(attachmentData.fileId, 0);

//            using (var fs = new FileStream(Eml3Path, FileMode.Open, FileAccess.Read))
//            {
//                attachmentData = Factory.TestEngine.AppendAttachmentsToSampleMessage(simpleMessage,
//                    EML3_FILE_NAME, fs, "message/eml");
//            }

//            Assert.IsNotNull(attachmentData);
//            Assert.Greater(attachmentData.fileId, 0);

//            var message = Factory.MessageEngine.GetMessage(simpleMessage, new MailMessageData.Options());

//            Assert.AreEqual(3, message.Attachments.Count);

//            var operation = Factory.OperationEngine.DownloadAllAttachments(message.Id);

//            MailOperationStatus operstionStatus;

//            do
//            {
//                Thread.Sleep(100);

//                operstionStatus = Factory.OperationEngine.GetMailOperationStatus(operation.Id);

//            } while (!operstionStatus.Completed);

//            Assert.IsTrue(operstionStatus.Completed);
//            Assert.AreEqual(operstionStatus.Status, STATUS_FINISHED);
//            Assert.AreEqual(operstionStatus.Source, DOWNLOAD_LINK);

//            var request = CreateWebRequest(DOWNLOAD_LINK);

//            using (var zipStream = new MemoryStream())
//            {
//                using (var response = (HttpWebResponse)request.GetResponse())
//                {
//                    using (var stream = response.GetResponseStream())
//                    {
//                        if (stream != null)
//                            stream.CopyTo(zipStream);
//                    }
//                }

//                using (var archive = new ZipArchive(zipStream,
//                    ZipArchiveMode.Update))
//                {
//                    Assert.AreEqual(archive.Entries.Count, 3);
//                    Assert.AreEqual(17448499, archive.Entries[0].Length);
//                    Assert.AreEqual(EML1_FILE_NAME, archive.Entries[0].Name);
//                    Assert.AreEqual(0, archive.Entries[1].Length);
//                    Assert.AreEqual(EML2_FILE_NAME, archive.Entries[1].Name);
//                    Assert.AreEqual(70530, archive.Entries[2].Length);
//                    Assert.AreEqual(EML3_FILE_NAME, archive.Entries[2].Name);
//                }
//            }
//        }

//        [Test]
//        public void DownloadAllAttachmentsOperationManyDamagedTest()
//        {
//            var simpleMessage = Factory.TestEngine.CreateSampleMessage((int?)FolderType.Inbox,
//                TestMailbox.MailBoxId, new List<string> { TestMailbox.EMailView }, new List<string>(),
//                new List<string>(), false, true, "Test subject", "Test body");

//            Assert.Greater(simpleMessage, 0);

//            var attachmentData = Factory.TestEngine.AppendAttachmentsToSampleMessage(simpleMessage,
//                EML1_FILE_NAME, Stream.Null, "message/eml");

//            Assert.IsNotNull(attachmentData);
//            Assert.Greater(attachmentData.fileId, 0);

//            attachmentData = Factory.TestEngine.AppendAttachmentsToSampleMessage(simpleMessage,
//                EML2_FILE_NAME, Stream.Null, "message/eml");

//            Assert.IsNotNull(attachmentData);
//            Assert.Greater(attachmentData.fileId, 0);

//            using (var fs = new FileStream(Eml3Path, FileMode.Open, FileAccess.Read))
//            {
//                attachmentData = Factory.TestEngine.AppendAttachmentsToSampleMessage(simpleMessage,
//                    EML3_FILE_NAME, fs, "message/eml");
//            }

//            Assert.IsNotNull(attachmentData);
//            Assert.Greater(attachmentData.fileId, 0);

//            var message = Factory.MessageEngine.GetMessage(simpleMessage, new MailMessageData.Options());

//            Assert.AreEqual(3, message.Attachments.Count);

//            var operation = Factory.OperationEngine.DownloadAllAttachments(message.Id);

//            MailOperationStatus operstionStatus;

//            do
//            {
//                Thread.Sleep(100);

//                operstionStatus = Factory.OperationEngine.GetMailOperationStatus(operation.Id);

//            } while (!operstionStatus.Completed);

//            Assert.IsTrue(operstionStatus.Completed);
//            Assert.AreEqual(operstionStatus.Status, STATUS_FINISHED);
//            Assert.AreEqual(operstionStatus.Source, DOWNLOAD_LINK);

//            var request = CreateWebRequest(DOWNLOAD_LINK);

//            using (var zipStream = new MemoryStream())
//            {
//                using (var response = (HttpWebResponse)request.GetResponse())
//                {
//                    using (var stream = response.GetResponseStream())
//                    {
//                        if (stream != null)
//                            stream.CopyTo(zipStream);
//                    }
//                }

//                using (var archive = new ZipArchive(zipStream, ZipArchiveMode.Update))
//                {
//                    Assert.AreEqual(archive.Entries.Count, 3);
//                    Assert.AreEqual(0, archive.Entries[0].Length);
//                    Assert.AreEqual(EML1_FILE_NAME, archive.Entries[0].Name);
//                    Assert.AreEqual(0, archive.Entries[1].Length);
//                    Assert.AreEqual(EML2_FILE_NAME, archive.Entries[1].Name);
//                    Assert.AreEqual(70530, archive.Entries[2].Length);
//                    Assert.AreEqual(EML3_FILE_NAME, archive.Entries[2].Name);
//                }
//            }
//        }
//    }
//}
