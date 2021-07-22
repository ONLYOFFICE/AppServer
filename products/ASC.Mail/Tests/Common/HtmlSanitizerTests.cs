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


using ASC.Mail.Utils;
using NUnit.Framework;

namespace ASC.Mail.Aggregator.Tests.Common
{
    [TestFixture]
    internal class HtmlSanitizerTests
    {
        [Test]
        public void TestForScriptTagRemove()
        {
            const string html = "<html><head></head><body>ter\"><script>alert(document.cookie);</script> <scr<script>ipt>alert();</scr<script>ipt></body></html>";
            var res = HtmlSanitizer.SanitizeHtmlForEditor(HtmlSanitizer.Sanitize(html, new HtmlSanitizer.Options(false)));
            Assert.AreEqual("<div>ter\"> </div>", res);
        }

        [Test]
        public void TestForStyleTagWithEmptyValue()
        {
            const string html = "<html><head></head><body><style><!-- #some-id div.SomeSection {} --></style><div id=\"some-id\"><div class=\"SomeSection\">text</div></div></body></html>";
            HtmlSanitizer.Sanitize(html, new HtmlSanitizer.Options(false));
        }

        [Test]
        public void TestForStyleTagWithMultiAttributesAndUrl()
        {
            const string html = "<html><head></head><body><div><a style=\"text-decoration: none; height: 95px; width: 630px; margin: 0; padding: 35px 0 0 50px; display: block; background-image: url(http://example.com/media/newsletters/images/header_07.jpg);\" href=\"http://www.example.com/\"><img style=\"border: 0; margin: 0; padding: 0; color: #fff; font-size: 26px; font-weight: 700; display: block;\" alt=\"example™\" src=\"http://example.com/media/newsletters/images/header_08.png\" /></a></div></body></html>";
            var res = HtmlSanitizer.Sanitize(html, new HtmlSanitizer.Options(false));
            Assert.AreEqual("<html><head></head><body><div><a style=\"text-decoration:none;height:95px;width:630px;margin:0;padding:35px 0 0 50px;display:block;tl_disabled_background-image:url(http://example.com/media/newsletters/images/header_07.jpg);\" href=\"http://www.example.com/\"><img style=\"border: 0; margin: 0; padding: 0; color: #fff; font-size: 26px; font-weight: 700; display: block;\" alt=\"example™\" tl_disabled_src=\"http://example.com/media/newsletters/images/header_08.png\"></a></div></body></html>", res);
        }

        [Test]
        public void TestProxyImageUrls()
        {
            const string html = "<html><head></head><body><div><a style=\"text-decoration:none;height:95px;width:630px;margin:0;padding:35px 0 0 50px;display:block;background-image:url(http://example.com/media/newsletters/images/header_07.jpg);\" href=\"http://www.example.com/\"><img style=\"border: 0; margin: 0; padding: 0; color: #fff; font-size: 26px; font-weight: 700; display: block;\" alt=\"example™\" src=\"http://example.com/media/newsletters/images/header_08.png\"></a></div><table width=\"600\"><tr><td background=\"http://example.com/media/newsletters/images/header_07.jpg\">Ячейка с фоновым рисунком</td></tr></table></body></html>";

            var res = HtmlSanitizer.Sanitize(html, new HtmlSanitizer.Options
            {
                LoadImages = true,
                NeedProxyHttp = true
            });

            Assert.AreEqual("<html><head></head><body><div><a style=\"text-decoration:none;height:95px;width:630px;margin:0;padding:35px 0 0 50px;display:block;background-image:url(/httphandlers/urlProxy.ashx?url=aHR0cDovL2V4YW1wbGUuY29tL21lZGlhL25ld3NsZXR0ZXJzL2ltYWdlcy9oZWFkZXJfMDcuanBn);\" href=\"http://www.example.com/\"><img style=\"border: 0; margin: 0; padding: 0; color: #fff; font-size: 26px; font-weight: 700; display: block;\" alt=\"example™\" src=\"/httphandlers/urlProxy.ashx?url=aHR0cDovL2V4YW1wbGUuY29tL21lZGlhL25ld3NsZXR0ZXJzL2ltYWdlcy9oZWFkZXJfMDgucG5n\"></a></div><table width=\"600\"><tr><td background=\"/httphandlers/urlProxy.ashx?url=aHR0cDovL2V4YW1wbGUuY29tL21lZGlhL25ld3NsZXR0ZXJzL2ltYWdlcy9oZWFkZXJfMDcuanBn\">Ячейка с фоновым рисунком</td></tr></table></body></html>", res);
        }
        
        [Test]
        public void TestReplaceProxyUrlsBackToUrls()
        {
            const string html = "<html><head></head><body><div><a style=\"text-decoration:none;height:95px;width:630px;margin:0;padding:35px 0 0 50px;display:block;background-image:url(/httphandlers/urlProxy.ashx?url=aHR0cDovL2V4YW1wbGUuY29tL21lZGlhL25ld3NsZXR0ZXJzL2ltYWdlcy9oZWFkZXJfMDcuanBn);\" href=\"http://www.example.com/\"><img style=\"border: 0; margin: 0; padding: 0; color: #fff; font-size: 26px; font-weight: 700; display: block;\" alt=\"example™\" src=\"/httphandlers/urlProxy.ashx?url=aHR0cDovL2V4YW1wbGUuY29tL21lZGlhL25ld3NsZXR0ZXJzL2ltYWdlcy9oZWFkZXJfMDgucG5n\"></a></div><table width=\"600\"><tr><td background=\"/httphandlers/urlProxy.ashx?url=aHR0cDovL2V4YW1wbGUuY29tL21lZGlhL25ld3NsZXR0ZXJzL2ltYWdlcy9oZWFkZXJfMDcuanBn\">Ячейка с фоновым рисунком</td></tr></table></body></html>";

            var res = HtmlSanitizer.RemoveProxyHttpUrls(html);

            Assert.AreEqual("<html><head></head><body><div><a style=\"text-decoration:none;height:95px;width:630px;margin:0;padding:35px 0 0 50px;display:block;background-image:url(http://example.com/media/newsletters/images/header_07.jpg);\" href=\"http://www.example.com/\"><img style=\"border: 0; margin: 0; padding: 0; color: #fff; font-size: 26px; font-weight: 700; display: block;\" alt=\"example™\" src=\"http://example.com/media/newsletters/images/header_08.png\"></a></div><table width=\"600\"><tr><td background=\"http://example.com/media/newsletters/images/header_07.jpg\">Ячейка с фоновым рисунком</td></tr></table></body></html>", res);
        }
    }
}