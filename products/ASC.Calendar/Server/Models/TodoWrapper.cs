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
using System.Collections.Generic;
using System.Runtime.Serialization;
using ASC.Core;
using ASC.Core.Users;
using ASC.Api.Core;
using ASC.Web.Core.Users;
using ASC.Web.Core.Calendars;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ASC.Common;

namespace ASC.Calendar.Models
{
    [DataContract(Name = "todo", Namespace = "")]
    [Scope]
    public class TodoWrapper
    {
        public Guid UserId { get; set; }

        [DataMember(Name = "objectId", Order = 0)]
        public string Id { get; set; }

        [DataMember(Name = "uniqueId", Order = 140)]
        public string Uid { get; set; }

        public int TenantId { get; set; }

        [DataMember(Name = "sourceId", Order = 10)]
        public string CalendarId { get; set; }

        [DataMember(Name = "title", Order = 20)]
        public string Name { get; set; }

        [DataMember(Name = "description", Order = 30)]
        public string Description { get; set; }

        [DataMember(Name = "start", Order = 40)]
        public ApiDateTime Start { get; set; }

        [DataMember(Name = "completed", Order = 110)]
        public ApiDateTime Completed { get; set; }

        [DataMember(Name = "owner", Order = 120)]
        public UserParams Owner { get; set; }

        public List<TodoWrapper> GetList()
        {
            var list = new List<TodoWrapper>();

            list.Add(this);

            return list;
        }
        public static TodoWrapper GetSample()
        {
            return new TodoWrapper
            {
                Owner = UserParams.GetSample(),
                Start = new ApiDateTime(DateTime.Now.AddDays(1), TimeZoneInfo.Utc.GetOffset()),
                Description = "Todo Description",
                Name = "Todo Name",
                Id = "1",
                CalendarId = "calendarID",
                Completed = new ApiDateTime(DateTime.MinValue, TimeZoneInfo.Utc.GetOffset())
            };
        }
    }

    [Scope]
    public class TodoWrapperHelper
    {

        private DateTime _utcStartDate = DateTime.MinValue;
        private DateTime _utcCompletedDate = DateTime.MinValue;
        private TimeZoneInfo _timeZone;

        protected ITodo _baseTodo;

        public UserManager UserManager { get; }
        public DisplayUserSettingsHelper DisplayUserSettingsHelper { get; }
        public TodoWrapperHelper(
            UserManager userManager,
            DisplayUserSettingsHelper displayUserSettingsHelper)
        {
            UserManager = userManager;
            DisplayUserSettingsHelper = displayUserSettingsHelper;
        }
        public TodoWrapper Get(ITodo baseTodo, Guid userId, TimeZoneInfo timeZone)
        {
            var result = new TodoWrapper();

            _timeZone = timeZone;
            _baseTodo = baseTodo;

            result.Id = _baseTodo.Id;
            result.Uid = _baseTodo.Uid;
            result.CalendarId = _baseTodo.CalendarId;
            result.Name = _baseTodo.Name;
            result.Description = _baseTodo.Description;

            var startD = _utcStartDate != DateTime.MinValue ? _utcStartDate : _baseTodo.UtcStartDate;
            startD = new DateTime(startD.Ticks, DateTimeKind.Utc);

            result.Start = new ApiDateTime(startD, _timeZone.GetOffset());

            var completedD = _utcCompletedDate != DateTime.MinValue ? _utcCompletedDate : _baseTodo.Completed;
            completedD = new DateTime(completedD.Ticks, DateTimeKind.Utc);

            result.Completed = new ApiDateTime(completedD, _timeZone.GetOffset());

            var owner = new UserParams() { Id = _baseTodo.OwnerId, Name = "" };
            if (_baseTodo.OwnerId != Guid.Empty)
                owner.Name = UserManager.GetUsers(_baseTodo.OwnerId).DisplayUserName(DisplayUserSettingsHelper);

            result.Owner = owner;
            
            result.UserId = userId;

            return result;
        }
        public TodoWrapper Get(ITodo baseTodo, Guid userId, TimeZoneInfo timeZone, DateTime utcStartDate)
        {
            _utcStartDate = utcStartDate;
            return this.Get(baseTodo, userId, timeZone);
        }
    }
}
