/*
 *
 * (c) Copyright Ascensio System Limited 2010-2020
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


using System.Net.Mail;
using ASC.Mail.Utils;
using NUnit.Framework;

namespace ASC.Mail.Aggregator.Tests.Common
{
    [TestFixture]
    public class LoginFormatingTests
    {
        [Test]
        public void TestForAllName()
        {
            var result = new MailAddress("localpart@domain.ru").ToLoginFormat("localpart@domain.ru");
            Assert.AreEqual("%EMAILADDRESS%", result);
        }

        [Test]
        public void TestForAllNameCase()
        {
            var result = new MailAddress("LocaLpart@domain.ru").ToLoginFormat("lOcalPart@domain.ru");
            Assert.AreEqual("%EMAILADDRESS%", result);
        }

        [Test]
        public void TestForLocalPart()
        {
            var result = new MailAddress("localpart@domain.ru").ToLoginFormat("localpart");
            Assert.AreEqual("%EMAILLOCALPART%", result);
        }

        [Test]
        public void TestForLocalPartCase()
        {
            var result = new MailAddress("LocaLpart@domain.ru").ToLoginFormat("lOcalPart");
            Assert.AreEqual("%EMAILLOCALPART%", result);
        }

        [Test]
        public void TestForError()
        {
            var result = new MailAddress("localpart@domain.ru").ToLoginFormat("asdasd");
            Assert.IsNull(result);
        }

        [Test]
        public void TestForErrorCase()
        {
            var result = new MailAddress("localPart@domain.ru").ToLoginFormat("asDasD");
            Assert.IsNull(result);
        }

        [Test]
        public void TestForHost()
        {
            var result = new MailAddress("localpart@domain.ru").ToLoginFormat("domain");
            Assert.AreEqual("%EMAILHOSTNAME%", result);
        }

        [Test]
        public void TestForHostCase()
        {
            var result = new MailAddress("localpart@doMain.ru").ToLoginFormat("DomAin");
            Assert.AreEqual("%EMAILHOSTNAME%", result);
        }

        [Test]
        public void TestForDomain()
        {
            var result = new MailAddress("localpart@domain.ru").ToLoginFormat("domain.ru");
            Assert.AreEqual("%EMAILDOMAIN%", result);
        }

        [Test]
        public void TestForDomainCase()
        {
            var result = new MailAddress("localpart@domAin.rU").ToLoginFormat("doMain.Ru");
            Assert.AreEqual("%EMAILDOMAIN%", result);
        }

        [Test]
        public void TestForComplexFormat()
        {
            var result = new MailAddress("localpart@domain.ru").ToLoginFormat("localpart.domain");
            Assert.AreEqual("%EMAILLOCALPART%.%EMAILHOSTNAME%", result);
        }

        [Test]
        public void TestForComplexFormatCase()
        {
            var result = new MailAddress("LocalPart@doMain.ru").ToLoginFormat("lOcalpaRt.dOmaIn");
            Assert.AreEqual("%EMAILLOCALPART%.%EMAILHOSTNAME%", result);
        }

        [Test]
        public void TestForEqualLocalpartAndDomainName()
        {
            var result = new MailAddress("equal@equal.ru").ToLoginFormat("equal.equal");
            Assert.AreEqual("%EMAILLOCALPART%.%EMAILHOSTNAME%", result);
        }

        [Test]
        public void TestForEqualLocalpartAndDomainNameCase()
        {
            var result = new MailAddress("eQuaL@eqUAl.ru").ToLoginFormat("EquaL.EQual");
            Assert.AreEqual("%EMAILLOCALPART%.%EMAILHOSTNAME%", result);
        }

        [Test]
        public void TestForMultiplePointInDomain()
        {
            var result = new MailAddress("equal@notequal.co.uk").ToLoginFormat("equal.notequal");
            Assert.AreEqual("%EMAILLOCALPART%.%EMAILHOSTNAME%", result);
        }

        [Test]
        public void TestForMultiplePointInDomainCase()
        {
            var result = new MailAddress("EquaL@notEqual.co.uk").ToLoginFormat("eQUal.nOtequal");
            Assert.AreEqual("%EMAILLOCALPART%.%EMAILHOSTNAME%", result);
        }

        [Test]
        public void TestForMultiplePointInDomain2()
        {
            var result = new MailAddress("equal@notequal.mail.pala.jp").ToLoginFormat("equal.notequal.mail.pala");
            Assert.AreEqual("%EMAILLOCALPART%.%EMAILHOSTNAME%", result);
        }

        [Test]
        public void TestForMultiplePointInDomain2Case()
        {
            var result = new MailAddress("eqUal@noTequal.mail.pAlA.jp").ToLoginFormat("eQual.notequaL.mail.PaLa");
            Assert.AreEqual("%EMAILLOCALPART%.%EMAILHOSTNAME%", result);
        }

        [Test]
        public void TestForGmailAnalgues()
        {
            var result = new MailAddress("equal@equal.ru").ToLoginFormat("recent:equal@equal.ru");
            Assert.AreEqual("recent:%EMAILADDRESS%", result);
        }

        [Test]
        public void TestForGmailAnalguesCase()
        {
            var result = new MailAddress("equAl@equaL.ru").ToLoginFormat("recent:eqUal@eQual.ru");
            Assert.AreEqual("recent:%EMAILADDRESS%", result);
        }
    }
}
