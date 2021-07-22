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


using System;
using ASC.Mail.Models;
using ASC.Mail.Utils;
using NUnit.Framework;

namespace ASC.Mail.Aggregator.Tests.Common.Utils
{
    [TestFixture]
    internal class ParserTests
    {

        // Tests are written based on data from the article: https://en.wikipedia.org/wiki/Email_address

        #region Must Pass
        [Test]
        public void ParseValidAddressTest()
        {
            Address address = null;

            Assert.DoesNotThrow(() => { address = Parser.ParseAddress("test@test.com"); }); 

            Assert.AreEqual("test", address.LocalPart);
            Assert.AreEqual("test.com", address.Domain);
            Assert.IsEmpty(address.Name);
        }

        [Test]
        public void ParseValidAddressWithNameTest()
        {
            Address address = null;

            Assert.DoesNotThrow(() => { address = Parser.ParseAddress("\"Test Name\" <test@test.com>"); });

            Assert.AreEqual("test", address.LocalPart);
            Assert.AreEqual("test.com", address.Domain);
            Assert.AreEqual("Test Name", address.Name);
        }

        [Test]
        public void ParseAddressWithPlusInLocalPartTest()
        {
            Address address = null;

            Assert.DoesNotThrow(() => { address = Parser.ParseAddress("te+st@test.com"); });

            Assert.AreEqual("te+st", address.LocalPart);
            Assert.AreEqual("test.com", address.Domain);
            Assert.IsEmpty(address.Name);
        }

        [Test]
        public void ParseAddressWithDotInLocalPartTest()
        {
            Address address = null;

            Assert.DoesNotThrow(() => { address = Parser.ParseAddress("te.st@test.com"); });

            Assert.AreEqual("te.st", address.LocalPart);
            Assert.AreEqual("test.com", address.Domain);
            Assert.IsEmpty(address.Name);
        }

        [Test]
        public void ParseAddressWithUnderscoreInLocalPartTest()
        {
            Address address = null;

            Assert.DoesNotThrow(() => { address = Parser.ParseAddress("te_st@test.com"); });

            Assert.AreEqual("te_st", address.LocalPart);
            Assert.AreEqual("test.com", address.Domain);
            Assert.IsEmpty(address.Name);
        }

        [Test]
        public void ParseAddressWithDashInLocalPartTest()
        {
            Address address = null;

            Assert.DoesNotThrow(() => { address = Parser.ParseAddress("te-st@test.com"); });

            Assert.AreEqual("te-st", address.LocalPart);
            Assert.AreEqual("test.com", address.Domain);
            Assert.IsEmpty(address.Name);
        }

        [Test]
        public void ParseAddressWithCommentFirstTest()
        {
            Address address = null;

            Assert.DoesNotThrow(() => { address = Parser.ParseAddress("(comment)test@test.com"); });

            Assert.AreEqual("test", address.LocalPart);
            Assert.AreEqual("test.com", address.Domain);
            Assert.IsEmpty(address.Name);
        }

        [Test]
        public void ParseAddressWithCommentLastTest()
        {
            Address address = null;

            Assert.DoesNotThrow(() => { address = Parser.ParseAddress("test(comment)@test.com"); });

            Assert.AreEqual("test", address.LocalPart);
            Assert.AreEqual("test.com", address.Domain);
            Assert.IsEmpty(address.Name);
        }

        [Test]
        public void ParseAddressWithSpaceTest()
        {
            Address address = null;

            Assert.DoesNotThrow(() => { address = Parser.ParseAddress("test @test.com"); });

            Assert.AreEqual("test", address.LocalPart);
            Assert.AreEqual("test.com", address.Domain);
            Assert.IsEmpty(address.Name);

            Assert.DoesNotThrow(() => { address = Parser.ParseAddress("test@ test.com"); });

            Assert.AreEqual("test", address.LocalPart);
            Assert.AreEqual("test.com", address.Domain);
            Assert.IsEmpty(address.Name);

            Assert.DoesNotThrow(() => { address = Parser.ParseAddress("test@test .com"); });

            Assert.AreEqual("test", address.LocalPart);
            Assert.AreEqual("test.com", address.Domain);
            Assert.IsEmpty(address.Name);

            Assert.DoesNotThrow(() => { address = Parser.ParseAddress("test@test. com"); });

            Assert.AreEqual("test", address.LocalPart);
            Assert.AreEqual("test.com", address.Domain);
            Assert.IsEmpty(address.Name);
        }

        #region Valid exapmles from article

        [Test]
        public void ExpampleValidSimpleEmailTest()
        {
            Address address = null;

            Assert.DoesNotThrow(() => { address = Parser.ParseAddress("simple@example.com"); });

            Assert.AreEqual("simple", address.LocalPart);
            Assert.AreEqual("example.com", address.Domain);
            Assert.IsEmpty(address.Name);
        }

        [Test]
        public void ExpampleValidEmailWithDotTest()
        {
            Address address = null;

            Assert.DoesNotThrow(() => { address = Parser.ParseAddress("very.common@example.com"); });

            Assert.AreEqual("very.common", address.LocalPart);
            Assert.AreEqual("example.com", address.Domain);
            Assert.IsEmpty(address.Name);
        }

        [Test]
        public void ExpampleValidEmailWithDotsAndPlusTest()
        {
            Address address = null;

            Assert.DoesNotThrow(() => { address = Parser.ParseAddress("disposable.style.email.with+symbol@example.com"); });

            Assert.AreEqual("disposable.style.email.with+symbol", address.LocalPart);
            Assert.AreEqual("example.com", address.Domain);
            Assert.IsEmpty(address.Name);
        }

        [Test]
        public void ExpampleValidEmailWithDotsAndDashesTest()
        {
            Address address = null;

            Assert.DoesNotThrow(() => { address = Parser.ParseAddress("other.email-with-hyphen@example.com"); });

            Assert.AreEqual("other.email-with-hyphen", address.LocalPart);
            Assert.AreEqual("example.com", address.Domain);
            Assert.IsEmpty(address.Name);
        }

        [Test]
        public void ExpampleValidEmailWithDashesTest()
        {
            Address address = null;

            Assert.DoesNotThrow(() => { address = Parser.ParseAddress("fully-qualified-domain@example.com"); });

            Assert.AreEqual("fully-qualified-domain", address.LocalPart);
            Assert.AreEqual("example.com", address.Domain);
            Assert.IsEmpty(address.Name);
        }

        [Test]
        public void ExpampleValidEmailWithTagsTest()
        {
            Address address = null;

            Assert.DoesNotThrow(() => { address = Parser.ParseAddress("user.name+tag+sorting@example.com"); });

            Assert.AreEqual("user.name+tag+sorting", address.LocalPart);
            Assert.AreEqual("example.com", address.Domain);
            Assert.IsEmpty(address.Name);
        }

        [Test]
        public void ExpampleValidEmailWithOneLetterLocalPartTest()
        {
            Address address = null;

            Assert.DoesNotThrow(() => { address = Parser.ParseAddress("x@example.com"); });

            Assert.AreEqual("x", address.LocalPart);
            Assert.AreEqual("example.com", address.Domain);
            Assert.IsEmpty(address.Name);
        }

        [Test]
        public void ExpampleValidEmailWithDashesInLocalAndDomainTest()
        {
            Address address = null;

            Assert.DoesNotThrow(() => { address = Parser.ParseAddress("example-indeed@strange-example.com"); });

            Assert.AreEqual("example-indeed", address.LocalPart);
            Assert.AreEqual("strange-example.com", address.Domain);
            Assert.IsEmpty(address.Name);
        }

        [Test]
        public void ExpampleValidEmailWithNonCommonTopLevelDomainTest()
        {
            Address address = null;

            Assert.DoesNotThrow(() => { address = Parser.ParseAddress("example@s.example"); });

            Assert.AreEqual("example", address.LocalPart);
            Assert.AreEqual("s.example", address.Domain);
            Assert.IsEmpty(address.Name);
        }

        [Test]
        public void ExampleInvalidLocalPartLonger64Test()
        {
            Assert.DoesNotThrow(() => { Parser.ParseAddress("1234567890123456789012345678901234567890123456789012345678901234+x@example.com"); });
        }
        #endregion

        #endregion

        #region Must trow exception
        [Test]
        public void ParseInvalidAddressTest()
        {
            Assert.Throws<Exception>(() => { Parser.ParseAddress("-@+._"); });
        }

        [Test]
        public void ParseInvalidAddressSingleCharTopDomainTest()
        {
            Assert.Throws<Exception>(() => { Parser.ParseAddress("single@a"); });
        }

        [Test]
        public void ParseInvalidAddressWithDotTest()
        {
            Assert.Throws<Exception>(() => { Parser.ParseAddress(".test@test.com"); });
            Assert.Throws<Exception>(() => { Parser.ParseAddress("test.@test.com"); });
            Assert.Throws<Exception>(() => { Parser.ParseAddress("test@.test.com"); });
        }

        [Test]
        public void ParseInvalidAddressWithUnderscoreTest()
        {
            Assert.Throws<Exception>(() => { Parser.ParseAddress("test_@test.com"); });
            Assert.Throws<Exception>(() => { Parser.ParseAddress("_test@test.com"); });
            Assert.Throws<Exception>(() => { Parser.ParseAddress("test@_test.com"); });
            Assert.Throws<Exception>(() => { Parser.ParseAddress("test@test.com_"); });
        }

        [Test]
        public void ParseInvalidAddressWithPlusTest()
        {
            Assert.Throws<Exception>(() => { Parser.ParseAddress("test+@test.com"); });
            Assert.Throws<Exception>(() => { Parser.ParseAddress("+test@test.com"); });
            Assert.Throws<Exception>(() => { Parser.ParseAddress("test@+test.com"); });
            Assert.Throws<Exception>(() => { Parser.ParseAddress("test@test.com+"); });
        }

        [Test]
        public void ParseInvalidAddressWithDashTest()
        {
            Assert.Throws<Exception>(() => { Parser.ParseAddress("test-@test.com"); });
            Assert.Throws<Exception>(() => { Parser.ParseAddress("-test@test.com"); });
            Assert.Throws<Exception>(() => { Parser.ParseAddress("test@test.com-"); });
            Assert.Throws<Exception>(() => { Parser.ParseAddress("test@-test.com"); });
        }

        [Test]
        public void ParseInvalidAddressWithSlashTest()
        {
            Assert.Throws<Exception>(() => { Parser.ParseAddress("test/@test.com"); });
            Assert.Throws<Exception>(() => { Parser.ParseAddress("/test@test.com"); });
            Assert.Throws<Exception>(() => { Parser.ParseAddress("test@/test.com"); });
            Assert.Throws<Exception>(() => { Parser.ParseAddress("test@test.com/"); });
        }

        [Test]
        public void ParseInvalidAddressWithEquallyTest()
        {
            Assert.Throws<Exception>(() => { Parser.ParseAddress("test=@test.com"); });
            Assert.Throws<Exception>(() => { Parser.ParseAddress("=test@test.com"); });
            Assert.Throws<Exception>(() => { Parser.ParseAddress("test@=test.com"); });
            Assert.Throws<Exception>(() => { Parser.ParseAddress("test@test.com="); });
        }

        [Test]
        public void ParseInvalidAddressWithCommentMiddleTest()
        {
            Assert.Throws<Exception>(() => { Parser.ParseAddress("test(comment)test@test.com"); });
        }

        [Test]
        public void ParseInvalidAddressWithIpV4InDomainTest()
        {
            Assert.Throws<Exception>(() => { Parser.ParseAddress("test@[192.168.1.1]"); });
        }

        [Test]
        public void ParseInvalidAddressWithIpV6InDomainTest()
        {
            Assert.Throws<Exception>(() => { Parser.ParseAddress("test@[2001:0db8:11a3:09d7:1f34:8a2e:07a0:765d]"); });
        }

        #region Invalid exapmles from article

        [Test]
        public void ExampleInvalidEmailNoAtTest()
        {
            Assert.Throws<Exception>(() => { Parser.ParseAddress("Abc.example.com"); });
        }

        [Test]
        public void ExampleInvalidEmailMoreThanOneAtTest()
        {
            Assert.Throws<Exception>(() => { Parser.ParseAddress("A@b@c@example.com"); });
        }

        [Test]
        public void ExampleInvalidEmailNotAllowCharsTest()
        {
            Assert.Throws<Exception>(() => { Parser.ParseAddress("a\"b(c)d,e:f;g<h>i[j\\k]l@example.com"); });
        }

        [Test]
        public void ExampleInvalidEmailNoSeparatedQuotesTest()
        {
            Assert.Throws<Exception>(() => { Parser.ParseAddress("just\"not\"right@example.com"); });
        }

        [Test]
        public void ExampleInvalidEmailSpaceWithoutQuotesTest()
        {
            Assert.Throws<Exception>(() => { Parser.ParseAddress("this is\"not\allowed@example.com"); });
        }

        [Test]
        public void ExampleInvalidEmailDoubleSlashesWithoutQuotesTest()
        {
            Assert.Throws<Exception>(() => { Parser.ParseAddress("this\\ still\"not\\allowed@example.com"); });
        }

        [Test]
        public void ExampleInvalidEmailDoubleDotBeforeAtTest()
        {
            Assert.Throws<Exception>(() => { Parser.ParseAddress("john..doe@example.com"); });
        }

        [Test]
        public void ExampleInvalidEmailDoubleDotAfterAtTest()
        {
            Assert.Throws<Exception>(() => { Parser.ParseAddress("john.doe@example..com"); });
        }

        [Test]
        public void ExampleInvalidEmailSymbolsOnlyTest()
        {
            Assert.Throws<Exception>(() => { Parser.ParseAddress("!@*.!"); });
        }

        #endregion

        #endregion
    }
}
