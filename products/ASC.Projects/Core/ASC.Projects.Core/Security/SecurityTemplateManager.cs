﻿#region License agreement statement

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

#endregion License agreement statement

using System;

using ASC.Core.Common.Utils;
using ASC.Projects.Core.BusinessLogic.Data;
using ASC.Projects.Core.DataAccess.Domain.Entities;
using ASC.Projects.Core.DataAccess.Domain.Enums;
using ASC.Projects.Core.Security;
using ASC.Projects.Core.Security.Interfaces;

namespace ASC.Projects.Core.BusinessLogic.Security
{
    public abstract class SecurityTemplateManager<TData> : IProjectSecurityTemplateManager<TData>
    {
        protected readonly ICommonSecurityManager CommonSecurityManager;

        protected SecurityTemplateManager(ICommonSecurityManager common)
        {
            CommonSecurityManager = common.NotNull(nameof(common));
        }

        public virtual bool CanCreateEntities(ProjectData project)
        {
            var result = project?.Status == ProjectStatus.Open && CommonSecurityManager.Can();

            return result;
        }

        public virtual bool CanReadEntities(ProjectData project, Guid userId)
        {
            return CommonSecurityManager.IsProjectsEnabled(userId);
        }

        public bool CanCreateEntities(TData project)
        {
            throw new NotImplementedException();
        }

        public virtual bool CanReadEntities(Guid userId)
        {
            var result = CommonSecurityManager.IsProjectsEnabled(userId);

            return result;
        }

        public bool CanReadEntities(TData project)
        {
            throw new NotImplementedException();
        }

        public bool CanReadEntities(ProjectData project)
        {
            var result = CanReadEntities(CommonSecurityManager.CurrentUserId);

            return result;
        }

        public bool CanReadEntity(TData entity)
        {
            var result = CanReadEntity(entity, CommonSecurityManager.CurrentUserId);

            return result;
        }

        public virtual bool CanReadEntity(TData entity, Guid userId)
        {
            var result = entity != null && CommonSecurityManager.Can(userId);

            return result;
        }

        public virtual bool CanUpdateEntity(TData entity)
        {
            var result = CommonSecurityManager.Can() && entity != null;

            return result;
        }

        public virtual bool CanDeleteEntity(TData entity)
        {
            var result = CommonSecurityManager.Can() && entity != null;

            return result;
        }

        public virtual bool CanCreateComment(TData entity)
        {
            return false;
        }

        public virtual bool CanEditFiles(TData entity)
        {
            return false;
        }

        public virtual bool CanEditComment(TData entity, CommentData comment)
        {
            return false;
        }

        public virtual bool CanGoToFeed(TData entity, Guid userId)
        {
            return false;
        }
    }
}
