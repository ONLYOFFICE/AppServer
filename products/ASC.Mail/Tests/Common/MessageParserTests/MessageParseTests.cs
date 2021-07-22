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
//using ASC.Mail.Clients;
//using ASC.Mail.Models;
//using ASC.Mail.Extensions;
//using NUnit.Framework;

//namespace ASC.Mail.Aggregator.Tests.Common.MessageParserTests
//{
//    [TestFixture]
//    internal class MessageParseTests
//    {
//        private static readonly string TestFolderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\Data\");
//        private const string EML_FILE_NAME = @"nct_attachment_not_parsed.eml";
//        private static readonly string FilePath = TestFolderPath + EML_FILE_NAME;

//        [Test]
//        public void ParseMessageTest()
//        {
//            Assert.IsTrue(File.Exists(FilePath));

//            var emlMessage = MailClient.ParseMimeMessage(FilePath);

//            Assert.IsNotNull(emlMessage);
//        }

//        [Test]
//        public void ConvertMessageToInternalFormatTest()
//        {
//            var message = MailClient.ParseMimeMessage(FilePath);

//            var mailMessageItem = message.CreateMailMessage();

//            Assert.IsNotNull(mailMessageItem);
//        }

//        [Test]
//        public void BodyReplaceEmbeddedImages()
//        {
//            var message = MailClient.ParseMimeMessage(FilePath);

//            var mailMessageItem = message.CreateMailMessage();

//            mailMessageItem.ReplaceEmbeddedImages();

//            Assert.IsNotNull(mailMessageItem.HtmlBodyStream);
//        }

//        [Test]
//        public void BodyWithSubmessages()
//        {
//            var message = MailClient.ParseMimeMessage(TestFolderPath + "message_with_submessages.eml");

//            var mailMessageItem = message.CreateMailMessage();

//            Assert.IsNotNull(mailMessageItem);
//        }

//        [Test]
//        public void BodyWithAttachments()
//        {
//            var message = MailClient.ParseMimeMessage(TestFolderPath + "with_attachments.eml");

//            var mailMessageItem = message.CreateMailMessage();

//            Assert.IsNotNull(mailMessageItem);

//            Assert.IsTrue(message.Attachments.Any());
//        }

//        [Test]
//        public void CorruptedBodyMessage()
//        {
//            var message = MailClient.ParseMimeMessage(TestFolderPath + "bad_content_type.eml");

//            Assert.IsNull(message.TextBody);

//            Assert.IsNull(message.HtmlBody);

//            var mailMessageItem = message.CreateMailMessage();

//            Assert.IsNotNull(mailMessageItem);

//            Assert.IsNotNull(mailMessageItem.HtmlBodyStream);
//        }

//        [Test]
//        public void InvalidAddressesMessage()
//        {
//            var message = MailClient.ParseMimeMessage(TestFolderPath + "dostavka_ru_bad_content_type.eml");

//            var mailMessageItem = message.CreateMailMessage();

//            Assert.IsNotNull(mailMessageItem);

//            Assert.AreEqual("", mailMessageItem.From);

//            Assert.AreEqual("", mailMessageItem.To);
//        }

//        [Test]
//        public void InvalidAttachmentsFilenames()
//        {
//            var message = MailClient.ParseMimeMessage(TestFolderPath + "invalid_attachments_filenames.eml");

//            var mailMessageItem = message.CreateMailMessage();

//            Assert.IsNotNull(mailMessageItem);

//            Assert.IsTrue(message.Attachments.Any());

//            Assert.AreEqual(12, mailMessageItem.Attachments.Count);

//            var filenames = new[]
//            {
//                "_.txt", "_.bin", "_.bin", ".txt", "☼.bin", "♫ .txt", "копия _.bin", "noname.bin", "копия ☼.bin",
//                "копия _  (копия).txt", "(копия).txt", "копия ☼  (копия).txt"
//            };

//            int i, len;
//            for (i = 0, len = mailMessageItem.Attachments.Count; i < len; i++)
//            {
//                var afn = mailMessageItem.Attachments[i].fileName;
//                Assert.AreEqual(filenames[i], afn);
//            }
//        }

//        [Test]
//        public void MailRuSpam1()
//        {
//            var message = MailClient.ParseMimeMessage(TestFolderPath + "Message14612877700000000705.eml");

//            var mailMessageItem = message.CreateMailMessage();

//            Assert.IsNotNull(mailMessageItem);

//            Assert.IsTrue(!mailMessageItem.Attachments.Any());
//        }

//        [Test]
//        public void MailRuSpam2()
//        {
//            var message = MailClient.ParseMimeMessage(TestFolderPath + "Message14634459670000000606.eml");

//            var mailMessageItem = message.CreateMailMessage();

//            Assert.IsNotNull(mailMessageItem);

//            Assert.IsFalse(message.Attachments.Any());
//        }

//        [Test]
//        public void IphoneMultipartMixed()
//        {
//            var message = MailClient.ParseMimeMessage(TestFolderPath + "iphone_yandex_with_inline_image.eml");

//            var mailMessageItem = message.CreateMailMessage();

//            Assert.IsNotNull(mailMessageItem);

//            Assert.IsTrue(mailMessageItem.Attachments.Count() == 1);

//            var contentId = mailMessageItem.Attachments.First().contentId;

//            Assert.IsNotEmpty(contentId);

//            Assert.IsNotNull(contentId);
//        }

//        [Test]
//        public void WithMhtAttachment()
//        {
//            var message = MailClient.ParseMimeMessage(TestFolderPath + "with_mht.eml");

//            var mailMessageItem = message.CreateMailMessage();

//            Assert.IsNotNull(mailMessageItem);

//            Assert.IsNotNull(mailMessageItem.HtmlBodyStream);

//            Assert.IsTrue(mailMessageItem.Attachments.Count(t => !t.isEmbedded) == 46);
//        }

//        [Test]
//        public void WithoutBadAttachments()
//        {
//            var message = MailClient.ParseMimeMessage(TestFolderPath + "Undeliverable_iwdvs_atn_pay_thisbill_01652567.eml");

//            var mailMessageItem = message.CreateMailMessage();

//            Assert.IsNotNull(mailMessageItem);

//            Assert.IsNotNull(mailMessageItem.HtmlBodyStream);

//            Assert.IsNotEmpty(mailMessageItem.Attachments.Where(a => !a.isEmbedded));

//            Assert.IsTrue(mailMessageItem.Attachments.Exists(t => t.fileName == "message.eml"));
//        }

//        [Test]
//        public void WithoutBadAttachments2()
//        {
//            var message = MailClient.ParseMimeMessage(TestFolderPath + "Undelivered_Mail_Returned_to_Sender.eml");

//            var mailMessageItem = message.CreateMailMessage();

//            Assert.IsNotNull(mailMessageItem);

//            Assert.IsNotNull(mailMessageItem.HtmlBodyStream);

//            Assert.IsTrue(!mailMessageItem.Attachments.Any());
//        }

//        [Test]
//        public void UberMailTest()
//        {
//            var message = MailClient.ParseMimeMessage(TestFolderPath + "Uber.eml");

//            var mailMessageItem = message.CreateMailMessage();

//            Assert.IsNotNull(mailMessageItem);

//            Assert.IsNotNull(mailMessageItem.HtmlBodyStream);

//            Assert.IsTrue(mailMessageItem.Attachments.Count(t => !t.isEmbedded) == 0);

//            Assert.IsTrue(mailMessageItem.Attachments.Count(t => t.isEmbedded) == 1);
//        }

//        [Test]
//        public void CorruptedMessage()
//        {
//            var message = MailClient.ParseMimeMessage(TestFolderPath + "corrupted_mailMessage.eml");

//            MailMessageData mailMessageItem = null;

//            Assert.DoesNotThrow(() => mailMessageItem = message.CreateCorruptedMesage());

//            Assert.IsNotNull(mailMessageItem);

//            Assert.IsNotNull(mailMessageItem.HtmlBodyStream);

//            Assert.AreEqual(true, mailMessageItem.HasParseError);

//            Assert.AreEqual(1, mailMessageItem.Attachments.Count);

//            Assert.IsTrue(mailMessageItem.Attachments.Count(t => t.isEmbedded) == 0);
//        }

//        [Test]
//        public void EmbeddedImageMessageTest()
//        {
//            var message = MailClient.ParseMimeMessage(TestFolderPath + "embedded_smile_not_parsed.eml");

//            var mailMessageItem = message.CreateMailMessage();

//            Assert.IsNotEmpty(mailMessageItem.Attachments);

//            mailMessageItem.Attachments.ForEach(a => a.storedFileUrl = string.Format("http://localhost/fake/files/{0}", a.fileName));

//            var htmlBefore = mailMessageItem.HtmlBody;

//            mailMessageItem.ReplaceEmbeddedImages();

//            var htmlAfter = mailMessageItem.HtmlBody;

//            Assert.AreNotEqual(htmlBefore, htmlAfter);
//        }

//        [Test]
//        public void MessageWithOneEmlAttachmentTest()
//        {
//            var message = MailClient.ParseMimeMessage(TestFolderPath + "message-with-eml-attachment.eml");

//            var mailMessageItem = message.CreateMailMessage();

//            Assert.IsNotEmpty(mailMessageItem.Attachments);

//            Assert.AreEqual(1, mailMessageItem.Attachments.Count);
//        }

//        [Test]
//        public void MessageWithThreeEmlAttachmentTest()
//        {
//            var message = MailClient.ParseMimeMessage(TestFolderPath + "message-with-3-eml-attachments.eml");

//            var mailMessageItem = message.CreateMailMessage();

//            Assert.IsNotEmpty(mailMessageItem.Attachments);

//            Assert.AreEqual(3, mailMessageItem.Attachments.Count);
//        }

//        [Test]
//        public void MessageWithMessageDeliferyFailureReportTest()
//        {
//            var message = MailClient.ParseMimeMessage(TestFolderPath + "MAILER-DAEMON.eml");

//            var mailMessageItem = message.CreateMailMessage();

//            Assert.IsNotEmpty(mailMessageItem.Attachments);

//            Assert.AreEqual(1, mailMessageItem.Attachments.Count);
//        }
//    }
//}
