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
//using System.Security.Authentication;
//using ASC.Core;
//using ASC.Core.Users;
//using ASC.Mail.Utils;
//using NUnit.Framework;

//namespace ASC.Mail.Aggregator.Tests.Common.Utils
//{
//    [TestFixture]
//    internal class ApiHelperTests
//    {
//        private UserInfo TestUser { get; set; }

//        private const int CURRENT_TENANT = 0;
//        private const string PASSWORD = "123456";
//        private const string DOMAIN = "gmail.com";

//        private const string TEST_FILE_NAME = "icloud_ics.eml";
//        private static readonly string TestFolderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\Data\");
//        private static readonly string TestFilePath = Path.Combine(TestFolderPath, TEST_FILE_NAME);

//        [SetUp]
//        public void Init()
//        {
//            CoreContext.TenantManager.SetCurrentTenant(CURRENT_TENANT);

//            TestUser = TestHelper.CreateNewRandomEmployee();

//            SecurityContext.AuthenticateMe(TestUser.ID);

//            if (!SecurityContext.IsAuthenticated)
//                throw new AuthenticationException();
//        }

//        [TearDown]
//        public void Cleanup()
//        {
//            if (TestUser == null || TestUser.ID == Guid.Empty)
//                return;

//            SecurityContext.Logout();

//            SecurityContext.AuthenticateMe(ASC.Core.Configuration.Constants.CoreSystem);

//            CoreContext.UserManager.DeleteUser(TestUser.ID);

//            SecurityContext.Logout();
//        }

//        public const string MY_DOCS_FOLDER_ID = "@my";

//        [Test]
//        public void UploadToDocumentsTest()
//        {
//            var apiHelper = new ApiHelper(Defines.DefaultApiSchema);

//            using (var stream = File.OpenRead(TestFilePath))
//            {
//                var id = apiHelper.UploadToDocuments(stream, TEST_FILE_NAME, "message/rfc822", MY_DOCS_FOLDER_ID, false);

//                Assert.IsNotNull(id);
//            }
//        }

//        [Test]
//        public void CreateEmployeeTest()
//        {
//            var user = TestHelper.CreateNewRandomEmployee();

//            Assert.NotNull(user);
//            Assert.AreNotEqual(Guid.Empty, user.ID);
//        }
//    }
//}
