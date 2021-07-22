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
using System.Linq;
using System.Text;
using ASC.Core;
using ASC.Core.Users;
using ASC.ElasticSearch;
using ASC.Mail.Utils;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace ASC.Mail.Aggregator.Tests.Common.Utils
{
    public static class TestHelper
    {
        public const string DOMAIN = "gmail.com";

        public static string GetRandomString(int count)
        {
            var letters = new[]
            {
                'A', 'B', 'C', 'D', 'E', 'F', 'G',
                'H', 'I', 'J', 'K', 'L', 'M', 'N',
                'O', 'P', 'Q', 'R', 'S', 'T', 'U',
                'V', 'W', 'X', 'Y', 'Z'
            };

            var rnd = new Random();

            var sb = new StringBuilder(count);

            for (var i = 0; i < count; i++)
            {
                var index = rnd.Next(0, 26);

                sb.Append(letters[index]);
            }

            return sb.ToString();
        }

        public static UserInfo CreateNewRandomEmployee(UserManager userManager, 
            SecurityContext securityContext, TenantManager tenantManager, ApiHelper apiHelper)
        {
            var admin = userManager.GetUsers()
                .FirstOrDefault(u => u.IsAdmin(userManager) 
                                  || u.IsOwner(tenantManager.GetCurrentTenant()));

            if (admin == null)
                throw new Exception("Not found any db admin");

            securityContext.AuthenticateMe(admin.ID);

            var startDate = DateTime.Now;

            var email = string.Format("1test_{0}_{1}_{2}_{3}@{4}",
                startDate.Day, startDate.Month, startDate.Year,
                startDate.Ticks, DOMAIN);

            //var apiHelper = new ApiHelper(Defines.DefaultApiSchema);

            var user = apiHelper.CreateEmployee(false, email, 
                "FirstName" + GetRandomString(4), 
                "LastName" + GetRandomString(4), 
                "Isadmin123");

            return userManager.SaveUserInfo(user);
        }

        public static bool IgnoreIfFullTextSearch<T>(bool enabled, IServiceProvider serviceProvider) where T : class, ISearchItem
        {
            using var scope = serviceProvider.CreateScope();

            var t = serviceProvider.GetService<T>();
            var factoryIndexer = serviceProvider.GetService<FactoryIndexer<T>>();

            if (enabled == factoryIndexer.Support(t))
            {
                Assert.Ignore("Test is Ignored until FullTextSearch {0} ", !enabled ? "is not started" : "is started");

                return false;
            }

            return true;
        }
    }
}
