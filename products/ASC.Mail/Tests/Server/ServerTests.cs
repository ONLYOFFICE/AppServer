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


//using System.Linq;
//using ASC.Core;
//using ASC.Mail.Core;
//using NUnit.Framework;

//namespace ASC.Mail.Aggregator.Tests.Server
//{
//    [TestFixture]
//    public class ServerTests
//    {
//        private const int TENANT = 1;
//        private EngineFactory _engineFactory;

//        private int _serverId;

//        [SetUp]
//        public void Init()
//        {
//            CoreContext.TenantManager.SetCurrentTenant(TENANT);
//            SecurityContext.AuthenticateMe(ASC.Core.Configuration.Constants.CoreSystem);

//            _engineFactory = new EngineFactory(TENANT);

//            var serverData = new Core.Entities.Server
//            {
//                Id = 0,
//                ConnectionString = "{" +
//                                   "\"DbConnection\" : \"Server=teamlab-4testing.cyxlgbdbuyvm.us-east-1.rds.amazonaws.com;Database=teamlab_mailserver;User ID=tm-info;Password=tm-info;Pooling=True;Character Set=utf8\", " +
//                                   "\"Api\":" +
//                                       "{" +
//                                           "\"Protocol\":\"http\", " +
//                                           "\"Server\":\"34.206.220.122\", " +
//                                           "\"Port\":\"8081\", " +
//                                           "\"Version\":\"v1\"," +
//                                           "\"Token\":\"d59bf8489d92c22d238b2051b6afe37f\"" +
//                                       "}" +
//                                   "}",
//                MxRecord = "test mx",
//                Type = 2,
//                ImapSettingsId = 1511,
//                SmtpSettingsId = 1510
//            };

//            _serverId = _engineFactory.ServerEngine.Save(serverData);
//        }

//        [Test]
//        public void SaveTest()
//        {
//            Assert.Greater(_serverId, 0);
//        }

//        [Test]
//        public void DeleteTest()
//        {
//            var servers = _engineFactory.ServerEngine.GetAllServers();

//            Assert.IsTrue(servers.Any(s => s.Id == _serverId));

//            _engineFactory.ServerEngine.Delete(_serverId);

//            servers = _engineFactory.ServerEngine.GetAllServers();

//            Assert.IsFalse(servers.Any(s => s.Id == _serverId));

//            Assert.DoesNotThrow(() => _engineFactory.ServerEngine.Delete(_serverId));
//        }

//        [Test]
//        public void GetListTest()
//        {
//            var servers = _engineFactory.ServerEngine.GetAllServers();

//            Assert.Greater(servers.Count, 0);
//            Assert.IsTrue(servers.Any(s => s.Id == _serverId));
//        }

//        [Test]
//        public void LinkTest()
//        {
//             var linkedServer = _engineFactory.ServerEngine.GetLinkedServer();

//            Assert.IsNull(linkedServer);

//            var servers = _engineFactory.ServerEngine.GetAllServers();

//            var newServer = servers.SingleOrDefault(s => s.Id == _serverId);

//            Assert.IsNotNull(newServer);

//            _engineFactory.ServerEngine.Link(newServer, TENANT);

//            linkedServer = _engineFactory.ServerEngine.GetLinkedServer();

//            Assert.IsNotNull(linkedServer);

//            _engineFactory.ServerEngine.UnLink(linkedServer, TENANT);

//            linkedServer = _engineFactory.ServerEngine.GetLinkedServer();

//            Assert.IsNull(linkedServer);
//        }

//        [Test]
//        public void UnLinkNotLinkedTest()
//        {
//            var linkedServer = _engineFactory.ServerEngine.GetLinkedServer();

//            Assert.IsNull(linkedServer);

//            var servers = _engineFactory.ServerEngine.GetAllServers();

//            var newServer = servers.SingleOrDefault(s => s.Id == _serverId);

//           Assert.DoesNotThrow(() => _engineFactory.ServerEngine.UnLink(newServer, TENANT));
//        }

//        [TearDown]
//        public void Clean()
//        {
//            _engineFactory.ServerEngine.Delete(_serverId);
//        }
//    }
//}
