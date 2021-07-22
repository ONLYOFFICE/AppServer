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

namespace ASC.Mail
{
    public class DefineConstants
    {
        public const string MODULE_NAME = "mailaggregator";
        public const string MD5_EMPTY = "d41d8cd98f00b204e9800998ecf8427e";
        public const int SHARED_TENANT_ID = -1;
        public const int UNUSED_DNS_SETTING_DOMAIN_ID = -1;

        public const string ICAL_REQUEST = "REQUEST";
        public const string ICAL_REPLY = "REPLY";
        public const string ICAL_CANCEL = "CANCEL";

        public const string GOOGLE_HOST = "gmail.com";

        public const int HARDCODED_LOGIN_TIME_FOR_MS_MAIL = 900;

        public const string IMAP = "imap";
        public const string POP3 = "pop3";
        public const string SMTP = "smtp";

        public const string SSL = "ssl";
        public const string START_TLS = "starttls";
        public const string PLAIN = "plain";

        public const string OAUTH2 = "oauth2";
        public const string PASSWORD_CLEARTEXT = "password-cleartext";
        public const string PASSWORD_ENCRYPTED = "password-encrypted";
        public const string NONE = "none";

        public const string GET_IMAP_POP_SETTINGS = "get_imap_pop_settings";
        public const string GET_IMAP_SERVER = "get_imap_server";
        public const string GET_IMAP_SERVER_FULL = "get_imap_server_full";
        public const string GET_POP_SERVER = "get_pop_server";
        public const string GET_POP_SERVER_FULL = "get_pop_server_full";

        public const string MAIL_QUOTA_TAG = "666ceac1-4532-4f8c-9cba-8f510eca2fd1";

        public const long ATTACHMENTS_TOTAL_SIZE_LIMIT = 25 * 1024 * 1024; // 25 megabytes

        public const string ASCENDING = "ascending";
        public const string DESCENDING = "descending";

        public const string ORDER_BY_DATE_SENT = "datesent";
        public const string ORDER_BY_DATE_CHAIN = "chaindate";
        public const string ORDER_BY_SENDER = "sender";
        public const string ORDER_BY_SUBJECT = "subject";

        public const string CONNECTION_STRING_NAME = "mail";

        public const string DNS_DEFAULT_ORIGIN = "@";

        public const string ARCHIVE_NAME = "download.zip";

        public static readonly DateTime BaseJsDateTime = new DateTime(1970, 1, 1);

        public static readonly DateTime MinBeginDate = new DateTime(1975, 1, 1, 0, 0, 0);

        public enum TariffType
        {
            Active = 0,
            Overdue,
            LongDead
        };
    }
}
