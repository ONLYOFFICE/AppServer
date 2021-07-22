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


using ASC.Mail.Core.Dao.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace ASC.Mail.Core.Dao.Expressions.UserFolder
{
    public class SimpleUserFoldersTreeExp : IUserFoldersTreeExp
    {
        public List<int> Ids { get; set; }
        public int? ParentId { get; set; }
        public uint? Level { get; set; }

        public string OrderBy { get; set; }
        public bool? OrderAsc { get; set; }
        public int? StartIndex { get; set; }
        public int? Limit { get; set; }

        public static UserFoldersTreeExpBuilder CreateBuilder()
        {
            return new UserFoldersTreeExpBuilder();
        }

        public Expression<Func<MailUserFolderTree, bool>> GetExpression()
        {
            Expression<Func<MailUserFolderTree, bool>> exp = t => true;

            if (Ids != null && Ids.Any())
            {
                exp = exp.And(t => Ids.Contains(t.FolderId));
            }

            if (ParentId.HasValue)
            {
                exp = exp.And(t => t.ParentId == ParentId.Value);
            }

            if (Level.HasValue)
            {
                exp = exp.And(t => Level == Level.Value);
            }

            return exp;
        }
    }
}