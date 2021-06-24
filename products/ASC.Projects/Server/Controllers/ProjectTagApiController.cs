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

using System.Linq;
using ASC.Common;
using ASC.Core;
using ASC.Core.Common.Extensions;
using ASC.Core.Common.Utils;
using ASC.Projects.Configuration;
using ASC.Projects.Core.BusinessLogic.Data;
using ASC.Projects.Core.BusinessLogic.Managers.Interfaces;
using ASC.Projects.ViewModels;
using ASC.Web.Api.Routing;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;

namespace ASC.Projects.Controllers
{
    [Scope]
    [DefaultRoute]
    [ApiController]
    public class ProjectTagApiController : ProjectApiControllerBase
    {
        /// <summary>
        /// Project tag processing business logic manager.
        /// </summary>
        private readonly IProjectTagManager _projectTagManager;

        /// <summary>
        /// ViewModel-to-Data mapper.
        /// </summary>
        private readonly IMapper _mapper;

        public ProjectTagApiController(IProjectTagManager projectTagManager,
            IMapper mapper,
            ProductEntryPoint productEntryPoint,
            SecurityContext securityContext) : base(productEntryPoint, securityContext)
        {
            _projectTagManager = projectTagManager.NotNull(nameof(projectTagManager));
            _mapper = mapper.NotNull(nameof(mapper));
        }

        ///<summary>
        /// Returns the list of all available project tags.
        ///</summary>
        ///<returns>List of existing tags.</returns>
        [Read(@"tag")]
        public IActionResult GetAllTags()
        {
            var result = _projectTagManager.GetTags()
                .Select(pt => _mapper.Map<ProjectTagData, ProjectTagViewModel>(pt))
                .ToList();

            return Ok(result);
        }

        /// <summary>
        /// Creates new tag.
        /// </summary>
        /// <param name="title">Title of new tag.</param>
        /// <returns>Just created tag.</returns>
        [Create(@"tag")]
        public IActionResult CreateNewTag(string title)
        {
            if (title.IsNullOrWhiteSpace())
            {
                return BadRequest("Title of tag must be provided.");
            }

            var newTag = new ProjectTagData
            {
                Title = title
            };

            var createdTag = _projectTagManager.CreateTag(newTag);

            var result = _mapper.Map<ProjectTagData, ProjectTagViewModel>(createdTag);

            return Ok(result);
        }

        ///<summary>
        /// Returns the detailed list of all projects tagged with specified tag.
        ///</summary>
        ///<param name="tag">Tag name</param>
        ///<returns>List of projects tagged with specified tag.</returns>
        [Read(@"tag/{tag}")]
        public IActionResult GetTaggedProjects(string tag)
        {
            if (tag.IsNullOrWhiteSpace())
            {
                return BadRequest("Tag must be specified.");
            }

            var taggedProjects = _projectTagManager
                .GetTaggedProjects(tag)
                .Select(tp => _mapper.Map<ProjectData, ProjectViewModel>(tp))
                .ToList();

            return Ok(taggedProjects);
        }

        ///<summary>
        /// Returns the list of all tags with specified prefix in title.
        ///</summary>
        ///<param name="prefix">Prefix of needed tags</param>
        ///<returns>List of tags with specified prefix.</returns>
        [Read(@"tag/search")]
        public IActionResult GetTagsByTitle(string prefix)
        {
            if (prefix.IsNullOrWhiteSpace())
            {
                return BadRequest("A tag prefix must be specified");
            }

            var result = _projectTagManager.GetTagsWithPrefix(prefix)
                .Select(pt => _mapper.Map<ProjectTagData, ProjectTagViewModel>(pt));

            return Ok(result);
        }
    }
}
