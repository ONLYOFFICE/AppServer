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


using System.Collections.Generic;
using System.Linq;
using ASC.Common;
using ASC.Core;
using ASC.Core.Common.EF;
using ASC.Mail.Core.Dao.Entities;
using ASC.Mail.Core.Dao.Interfaces;

namespace ASC.Mail.Core.Dao
{
    [Scope]
    public class ServerDao : BaseMailDao, IServerDao
    {
        public ServerDao(
             TenantManager tenantManager,
             SecurityContext securityContext,
             DbContextManager<MailDbContext> dbContext)
            : base(tenantManager, securityContext, dbContext)
        {
        }

        private const string SERVER_ALIAS = "ms";
        private const string SERVER_X_TENANT_ALIAS = "st";

        public Core.Entities.Server Get(int tenant)
        {
            var server = MailDbContext.MailServerServer
                .Join(MailDbContext.MailServerServerXTenant, s => s.Id, x => x.IdServer, 
                    (s, x) => new { 
                        Server = s,
                        Xtenant = x
                    })
                .Where(o => o.Xtenant.IdTenant == tenant)
                .Select(o => ToServer(o.Server))
                .SingleOrDefault();

            return server;
        }

        public List<Core.Entities.Server> GetList()
        {
            var list = MailDbContext.MailServerServer.Select(ToServer).ToList();

            return list;
        }

        public int Link(Core.Entities.Server server, int tenant)
        {
            var xItem = new MailServerServerXTenant
            {
                IdServer = server.Id,
                IdTenant = tenant
            };

            MailDbContext.AddOrUpdate(t => t.MailServerServerXTenant, xItem);

            var result = MailDbContext.SaveChanges();

            return result;
        }

        public int UnLink(Core.Entities.Server server, int tenant)
        {
            var deleteItem = new MailServerServerXTenant
            {
                IdServer = server.Id,
                IdTenant = tenant
            };

            MailDbContext.MailServerServerXTenant.Remove(deleteItem);

            var result = MailDbContext.SaveChanges();

            return result;
        }

        public int Save(Core.Entities.Server server)
        {
            var mailServer = new MailServerServer
            {
                Id = server.Id,
                MxRecord = server.MxRecord,
                ConnectionString = server.ConnectionString,
                ServerType = server.Type,
                SmtpSettingsId = server.SmtpSettingsId,
                ImapSettingsId = server.ImapSettingsId
            };

            var entry = MailDbContext.AddOrUpdate(t => t.MailServerServer, mailServer);

            MailDbContext.SaveChanges();

            return entry.Id;
        }

        public int Delete(int id)
        {
            var deleteItem = new MailServerServerXTenant
            {
                IdServer = id
            };

            MailDbContext.MailServerServerXTenant.Remove(deleteItem);

            MailDbContext.SaveChanges();

            var mailServer = new MailServerServer
            {
                Id = id
            };

            MailDbContext.MailServerServer.Remove(mailServer);

            var result = MailDbContext.SaveChanges();

            return result;
        }

        protected Core.Entities.Server ToServer(MailServerServer r)
        {
            var s = new Core.Entities.Server
            {
                Id = r.Id,
                MxRecord = r.MxRecord,
                ConnectionString = r.ConnectionString,
                Type = r.ServerType,
                SmtpSettingsId = r.SmtpSettingsId,
                ImapSettingsId = r.ImapSettingsId
            };

            return s;
        }
    }
}