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
using System.Runtime.Serialization;
using ASC.Web.Core.Calendars;
using ASC.Calendar.BusinessObjects;
using System.Collections.Generic;
using ASC.Common;

namespace ASC.Calendar.Models
{
    [DataContract(Name = "subscription", Namespace = "")]
    [Scope]
    public class SubscriptionWrapper : CalendarWrapper
    {
        [DataMember(Name = "isSubscribed", Order = 100)]
        public bool IsAccepted { get; set; }

        [DataMember(Name = "isNew", Order = 140)]
        public bool IsNew { get; set; }

        [DataMember(Name = "group", Order = 130)]
        public string Group { get; set; }

        [DataMember(IsRequired = false)]
        public override CalendarPermissions Permissions { get; set; }

        public new static object GetSample()
        {
            return new
            {
                canEditTimeZone = false,
                timeZone = TimeZoneWrapper.GetSample(),
                defaultAlert = EventAlertWrapper.GetSample(),
                events = new List<object>() { EventWrapper.GetSample() },
                owner = UserParams.GetSample(),
                objectId = "1",
                title = "Calendar Name",
                description = "Calendar Description",
                backgroundColor = "#000000",
                textColor = "#ffffff",
                isEditable = true,
                permissions = CalendarPermissions.GetSample(),
                isShared = true,
                canAlertModify = true,
                isHidden = false,
                isiCalStream = false,
                isSubscription = false,
                group = "Personal Calendars",
                isNew = true,
                isSubscribed = false,

            };
        }
    }

    [Scope]
    public class SubscriptionWrapperHelper
    {

        public CalendarWrapperHelper CalendarWrapperHelper;
        public SubscriptionWrapperHelper(
            CalendarWrapperHelper calendarWrapperHelper)
        {
            CalendarWrapperHelper = calendarWrapperHelper;
        }

        public SubscriptionWrapper Get(BaseCalendar calendar)
        {
            var subscriptionWrapper = (SubscriptionWrapper)CalendarWrapperHelper.Get(calendar);
            var _userViewSettings = subscriptionWrapper._userViewSettings;

            //isSubscribed
            if (subscriptionWrapper.UserCalendar is Calendar.BusinessObjects.Calendar)
                subscriptionWrapper.IsAccepted = _userViewSettings != null && _userViewSettings.IsAccepted;
            else
                subscriptionWrapper.IsAccepted = subscriptionWrapper.IsAcceptedSubscription;

            //isNew
            subscriptionWrapper.IsNew = _userViewSettings == null;

            //group
            if (subscriptionWrapper.UserCalendar.IsiCalStream())
                subscriptionWrapper.Group = Resources.CalendarApiResource.iCalCalendarsGroup;
            else
                subscriptionWrapper.Group = String.IsNullOrEmpty(subscriptionWrapper.UserCalendar.Context.Group) ? Resources.CalendarApiResource.SharedCalendarsGroup : subscriptionWrapper.UserCalendar.Context.Group;

            return subscriptionWrapper;
        }
        public SubscriptionWrapper Get(BaseCalendar calendar, UserViewSettings userViewSettings)
        {
            var subscriptionWrapper = (SubscriptionWrapper)CalendarWrapperHelper.Get(calendar, userViewSettings);
            var _userViewSettings = subscriptionWrapper._userViewSettings;

            //isSubscribed
            if (subscriptionWrapper.UserCalendar is Calendar.BusinessObjects.Calendar)
                subscriptionWrapper.IsAccepted = _userViewSettings != null && _userViewSettings.IsAccepted;
            else
                subscriptionWrapper.IsAccepted = subscriptionWrapper.IsAcceptedSubscription;

            //isNew
            subscriptionWrapper.IsNew = _userViewSettings == null;

            //group
            if (subscriptionWrapper.UserCalendar.IsiCalStream())
                subscriptionWrapper.Group = Resources.CalendarApiResource.iCalCalendarsGroup;
            else
                subscriptionWrapper.Group = String.IsNullOrEmpty(subscriptionWrapper.UserCalendar.Context.Group) ? Resources.CalendarApiResource.SharedCalendarsGroup : subscriptionWrapper.UserCalendar.Context.Group;
            return subscriptionWrapper;
        }
    }
}
