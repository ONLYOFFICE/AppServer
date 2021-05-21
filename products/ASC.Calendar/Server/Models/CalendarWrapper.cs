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
using ASC.Calendar.BusinessObjects;
using ASC.Core;
using ASC.Core.Users;
using ASC.Web.Core.Users;
using ASC.Web.Core.Calendars;
using ASC.Calendar.ExternalCalendars;
using ASC.Common.Security;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ASC.Common;

namespace ASC.Calendar.Models
{
    [DataContract(Name = "calendar", Namespace = "")]
    public class CalendarWrapper
    {
        internal UserViewSettings _userViewSettings;
        internal Guid _userId;
        public BaseCalendar UserCalendar { get; set; }

        [DataMember(Name = "isSubscription", Order = 80)]
        public bool IsSubscription { get; set; }

        [DataMember(Name = "iCalUrl", Order = 230)]
        public string iCalUrl { get; set; }

        [DataMember(Name = "isiCalStream", Order = 220)]
        public bool IsiCalStream { get; set; }

        [DataMember(Name = "isHidden", Order = 50)]
        public bool IsHidden { get; set; }

        [DataMember(Name = "canAlertModify", Order = 200)]
        public bool CanAlertModify { get; set; }

        [DataMember(Name = "isShared", Order = 60)]
        public bool IsShared { get; set; }

        [DataMember(Name = "permissions", Order = 70)]
        public virtual CalendarPermissions Permissions { get; set; }

        [DataMember(Name = "isEditable", Order = 90)]
        public bool IsEditable { get; set; }

        [DataMember(Name = "textColor", Order = 30)]
        public string TextColor { get; set; }

        [DataMember(Name = "backgroundColor", Order = 40)]
        public string BackgroundColor { get; set; }

        [DataMember(Name = "description", Order = 20)]
        public string Description { get; set; }

        [DataMember(Name = "title", Order = 30)]
        public string Title { get; set; }

        [DataMember(Name = "objectId", Order = 0)]
        public string Id { get; set; }

        [DataMember(Name = "isTodo", Order = 0)]
        public int IsTodo { get; set; }

        [DataMember(Name = "owner", Order = 120)]
        public UserParams Owner { get; set; }

        public bool IsAcceptedSubscription { get; set; }

        [DataMember(Name = "events", Order = 150)]
        public List<EventWrapper> Events { get; set; }

        [DataMember(Name = "todos", Order = 160)]
        public List<TodoWrapper> Todos { get; set; }

        [DataMember(Name = "defaultAlert", Order = 160)]
        public EventAlertWrapper DefaultAlertType { get; set; }

        [DataMember(Name = "timeZone", Order = 160)]
        public TimeZoneWrapper TimeZoneInfo { get; set; }

        [DataMember(Name = "canEditTimeZone", Order = 160)]
        public bool CanEditTimeZone { get; set; }

        public static CalendarWrapper GetSample()
        {
            return new CalendarWrapper
            {
                CanEditTimeZone = false,
                TimeZoneInfo = TimeZoneWrapper.GetSample(),
                DefaultAlertType = EventAlertWrapper.GetSample(),
                Events = new List<EventWrapper>() { EventWrapper.GetSample() },
                Owner = UserParams.GetSample(),
                Id = "1",
                Title = "Calendar Name",
                Description = "Calendar Description",
                BackgroundColor = "#000000",
                TextColor = "#ffffff",
                IsEditable = true,
                Permissions = CalendarPermissions.GetSample(),
                IsShared = true,
                CanAlertModify = true,
                IsHidden = false,
                IsiCalStream = false,
                IsSubscription = false
            };
        }
    }

    [Scope]
    public class CalendarWrapperHelper
    {

        public AuthContext AuthContext { get; }
        private AuthManager Authentication { get; }
        private PermissionContext PermissionContext { get; }
        public UserManager UserManager { get; }
        public DisplayUserSettingsHelper DisplayUserSettingsHelper { get; }
        private TenantManager TenantManager { get; }
        private PublicItemCollectionHelper PublicItemCollectionHelper { get; }
        public TimeZoneWrapperHelper TimeZoneWrapperHelper { get; }

        public CalendarWrapperHelper(
            AuthContext authContext,
            AuthManager authentication,
            TenantManager tenantManager,
            TimeZoneWrapperHelper timeZoneWrapperHelper,
            UserManager userManager,
            DisplayUserSettingsHelper dsplayUserSettingsHelper,
            PublicItemCollectionHelper publicItemCollectionHelper,
            PermissionContext permissionContext)
        {
            AuthContext = authContext;
            Authentication = authentication;
            TenantManager = tenantManager;
            UserManager = userManager;
            DisplayUserSettingsHelper = dsplayUserSettingsHelper;
            TimeZoneWrapperHelper = timeZoneWrapperHelper;
            PublicItemCollectionHelper = publicItemCollectionHelper;
            PermissionContext = permissionContext;
        }
        public CalendarWrapper Get(BaseCalendar calendar)
        {
            return this.Get(calendar, null);
        }
        public CalendarWrapper Get(BaseCalendar calendar, UserViewSettings userViewSettings)
        {
            var calendarWraper = new CalendarWrapper();

            calendarWraper._userViewSettings = userViewSettings;
            if (calendarWraper._userViewSettings == null && calendar is BusinessObjects.Calendar)
            {
                calendarWraper._userViewSettings = (calendar as BusinessObjects.Calendar)
                                    .ViewSettings.Find(s => s.UserId == AuthContext.CurrentAccount.ID);
            }

            if (calendarWraper._userViewSettings == null)
            {
                calendarWraper.UserCalendar = calendar;
                calendarWraper._userId = AuthContext.CurrentAccount.ID;
            }
            else
            {
                calendarWraper.UserCalendar = calendar.GetUserCalendar(calendarWraper._userViewSettings);
                calendarWraper._userId = calendarWraper._userViewSettings.UserId;
            }

            //---IsSubscription
            if (calendarWraper.UserCalendar.Id != null)
            {
                if (calendarWraper.UserCalendar.IsiCalStream())
                    calendarWraper.IsSubscription = true;
                else if (calendarWraper.UserCalendar.Id.Equals(SharedEventsCalendar.CalendarId, StringComparison.InvariantCultureIgnoreCase))
                    calendarWraper.IsSubscription = true;
                else if (calendarWraper.UserCalendar.OwnerId.Equals(calendarWraper._userId))
                    calendarWraper.IsSubscription = false;
                else
                    calendarWraper.IsSubscription = true;

                //---iCalUrl
                if (calendarWraper.UserCalendar.IsiCalStream())
                    calendarWraper.iCalUrl = (calendarWraper.UserCalendar as BusinessObjects.Calendar).iCalUrl;
                else
                    calendarWraper.iCalUrl = "";

                //---isiCalStream
                if (calendarWraper.UserCalendar.IsiCalStream())
                    calendarWraper.IsiCalStream = true;
                else
                    calendarWraper.IsiCalStream = false;

                //---IsHidden
                calendarWraper.IsHidden = calendarWraper._userViewSettings != null ? calendarWraper._userViewSettings.IsHideEvents : false;

                //---CanAlertModify
                calendarWraper.CanAlertModify = calendarWraper.UserCalendar.Context.CanChangeAlertType;

                //---IsShared
                calendarWraper.IsShared = calendarWraper.UserCalendar.SharingOptions.SharedForAll || calendarWraper.UserCalendar.SharingOptions.PublicItems.Count > 0;

                //---Permissions
                var p = new CalendarPermissions() { Data = PublicItemCollectionHelper.GetForCalendar(calendarWraper.UserCalendar) };
                foreach (var item in calendarWraper.UserCalendar.SharingOptions.PublicItems)
                {
                    if (item.IsGroup)
                        p.UserParams.Add(new UserParams() { Id = item.Id, Name = UserManager.GetGroupInfo(item.Id).Name });
                    else
                        p.UserParams.Add(new UserParams() { Id = item.Id, Name = UserManager.GetUsers(item.Id).DisplayUserName(DisplayUserSettingsHelper) });
                }
                calendarWraper.Permissions = p;

                //---IsEditable
                if (calendarWraper.UserCalendar.IsiCalStream())
                    calendarWraper.IsEditable = false;
                else if (calendarWraper.UserCalendar is ISecurityObject)
                    calendarWraper.IsEditable = PermissionContext.PermissionResolver.Check(Authentication.GetAccountByID(TenantManager.GetCurrentTenant().TenantId, calendarWraper._userId), (ISecurityObject)calendarWraper.UserCalendar as ISecurityObject, null, CalendarAccessRights.FullAccessAction);
                else
                    calendarWraper.IsEditable = false;

                //---TextColor
                calendarWraper.TextColor = String.IsNullOrEmpty(calendarWraper.UserCalendar.Context.HtmlTextColor) ? BusinessObjects.Calendar.DefaultTextColor :
                        calendarWraper.UserCalendar.Context.HtmlTextColor;

                //---BackgroundColor
                calendarWraper.BackgroundColor = String.IsNullOrEmpty(calendarWraper.UserCalendar.Context.HtmlBackgroundColor) ? BusinessObjects.Calendar.DefaultBackgroundColor :
                        calendarWraper.UserCalendar.Context.HtmlBackgroundColor;

                //---Description
                calendarWraper.Description = calendarWraper.UserCalendar.Description;

                //---Title
                calendarWraper.Title = calendarWraper.UserCalendar.Name;

                //---Id
                calendarWraper.Id = calendarWraper.UserCalendar.Id;

                //---IsTodo
                if (calendarWraper.UserCalendar.IsExistTodo())
                    calendarWraper.IsTodo = (calendarWraper.UserCalendar as BusinessObjects.Calendar).IsTodo;
                else
                    calendarWraper.IsTodo = 0;

                //---Owner
                var owner = new UserParams() { Id = calendarWraper.UserCalendar.OwnerId, Name = "" };
                if (calendarWraper.UserCalendar.OwnerId != Guid.Empty)
                    owner.Name = UserManager.GetUsers(calendarWraper.UserCalendar.OwnerId).DisplayUserName(DisplayUserSettingsHelper);

                calendarWraper.Owner = owner;

                //---IsAcceptedSubscription
                calendarWraper.IsAcceptedSubscription = calendarWraper._userViewSettings == null || calendarWraper._userViewSettings.IsAccepted;

                //---DefaultAlertType
                calendarWraper.DefaultAlertType = EventAlertWrapper.ConvertToTypeSurrogated(calendarWraper.UserCalendar.EventAlertType);

                //---TimeZoneInfo
                calendarWraper.TimeZoneInfo = TimeZoneWrapperHelper.Get(calendarWraper.UserCalendar.TimeZone);

                //---CanEditTimeZone
                calendarWraper.CanEditTimeZone = calendarWraper.UserCalendar.Context.CanChangeTimeZone;
            }

            return calendarWraper;
        }
    }



    /*
           [DataContract(Name = "calendar", Namespace = "")]
           public class CalendarWrapper
           {        
               public BaseCalendar UserCalendar{get; private set;}
               protected UserViewSettings _userViewSettings;
               protected Guid _userId;

               public CalendarWrapper(BaseCalendar calendar) : this(calendar, null){}
               public CalendarWrapper(BaseCalendar calendar, UserViewSettings userViewSettings)
               {
                   _userViewSettings = userViewSettings;
                   if (_userViewSettings == null && calendar is ASC.Calendar.BusinessObjects.Calendar)
                   { 
                       _userViewSettings = (calendar as ASC.Calendar.BusinessObjects.Calendar)
                                           .ViewSettings.Find(s=> s.UserId == AuthContext.CurrentAccount.ID);
                   }

                   if (_userViewSettings == null)
                   {
                       UserCalendar = calendar;
                       _userId = AuthContext.CurrentAccount.ID;
                   }
                   else
                   {
                       UserCalendar = calendar.GetUserCalendar(_userViewSettings);
                       _userId = _userViewSettings.UserId;
                   }
               }

               [DataMember(Name = "isSubscription", Order = 80)]
               public virtual bool IsSubscription
               {
                   get
                   {
                       if (UserCalendar.IsiCalStream())
                           return true;

                       if (UserCalendar.Id.Equals(SharedEventsCalendar.CalendarId, StringComparison.InvariantCultureIgnoreCase))
                           return true;

                       if (UserCalendar.OwnerId.Equals(_userId))
                           return false;

                       return true;
                   }
                   set { }
               }

               [DataMember(Name = "iCalUrl", Order = 230)]
               public virtual string iCalUrl
               {
                   get
                   {
                       if (UserCalendar.IsiCalStream())
                           return (UserCalendar as BusinessObjects.Calendar).iCalUrl;

                       return "";
                   }
                   set { }
               }

               [DataMember(Name = "isiCalStream", Order = 220)]
               public virtual bool IsiCalStream
               {
                   get
                   {
                       if (UserCalendar.IsiCalStream())
                           return true;

                       return false;
                   }
                   set { }
               }

               [DataMember(Name = "isHidden", Order = 50)]
               public virtual bool IsHidden
               {
                   get
                   {
                       return _userViewSettings != null ? _userViewSettings.IsHideEvents : false;
                   }
                   set { }
               }

               [DataMember(Name = "canAlertModify", Order = 200)]
               public virtual bool CanAlertModify
               {
                   get
                   {
                       return UserCalendar.Context.CanChangeAlertType;
                   }
                   set { }
               }


               [DataMember(Name = "isShared", Order = 60)]
               public virtual bool IsShared
               {
                   get
                   {
                       return UserCalendar.SharingOptions.SharedForAll || UserCalendar.SharingOptions.PublicItems.Count > 0;
                   }
                   set { }
               }

               [DataMember(Name = "permissions", Order = 70)]
               public virtual CalendarPermissions Permissions
               {
                   get
                   {
                       var p = new CalendarPermissions() { Data = PublicItemCollection.GetForCalendar(UserCalendar) };
                       foreach (var item in UserCalendar.SharingOptions.PublicItems)
                       {
                           if (item.IsGroup)
                               p.UserParams.Add(new UserParams() { Id = item.Id, Name = UserManager.GetGroupInfo(item.Id).Name });
                           else
                               p.UserParams.Add(new UserParams() { Id = item.Id, Name = UserManager.GetUsers(item.Id).DisplayUserName(DisplayUserSettingsHelper) });
                       }
                       return p;
                   }
                   set { }
               }

               [DataMember(Name = "isEditable", Order = 90)]
               public virtual bool IsEditable
               {
                   get
                   {
                       if (UserCalendar.IsiCalStream())
                           return false;

                       if (UserCalendar is ISecurityObject)
                           return PermissionContext.PermissionResolver.Check(Authentication.GetAccountByID(TenantManager.GetCurrentTenant().TenantId, _userId), (ISecurityObject)UserCalendar as ISecurityObject, null, CalendarAccessRights.FullAccessAction);

                       return false;
                   }
                   set { }
               }

               [DataMember(Name = "textColor", Order = 30)]
               public string TextColor
               {
                   get
                   {
                       return String.IsNullOrEmpty(UserCalendar.Context.HtmlTextColor)? BusinessObjects.Calendar.DefaultTextColor:
                           UserCalendar.Context.HtmlTextColor;
                   }
                   set { }
               }

               [DataMember(Name = "backgroundColor", Order = 40)]
               public string BackgroundColor
               {
                   get
                   {
                       return String.IsNullOrEmpty(UserCalendar.Context.HtmlBackgroundColor) ? BusinessObjects.Calendar.DefaultBackgroundColor :
                           UserCalendar.Context.HtmlBackgroundColor;
                   }
                   set { }
               }

               [DataMember(Name = "description", Order = 20)]
               public string Description { get { return UserCalendar.Description; } set { } }

               [DataMember(Name = "title", Order = 30)]
               public string Title
               {
                   get{return UserCalendar.Name;}
                   set{}
               } 

               [DataMember(Name = "objectId", Order = 0)]
               public string Id
               {
                   get{return UserCalendar.Id;}
                   set{}
               }

               [DataMember(Name = "isTodo", Order = 0)]
               public int IsTodo
               {

                   get
                   {
                       if (UserCalendar.IsExistTodo())
                           return (UserCalendar as BusinessObjects.Calendar).IsTodo;

                       return 0;
                   }
                   set { }
               }

               [DataMember(Name = "owner", Order = 120)]
               public UserParams Owner
               {
                   get
                   {
                       var owner = new UserParams() { Id = UserCalendar.OwnerId , Name = ""};
                       if (UserCalendar.OwnerId != Guid.Empty)
                           owner.Name = UserManager.GetUsers(UserCalendar.OwnerId).DisplayUserName(DisplayUserSettingsHelper);

                       return owner;
                   }
                   set { }
               }

               public bool IsAcceptedSubscription
               {
                   get
                   {
                       return  _userViewSettings == null || _userViewSettings.IsAccepted;
                   }
                   set { }
               }

               [DataMember(Name = "events", Order = 150)]
               public List<EventWrapper> Events { get; set; }

               [DataMember(Name = "todos", Order = 160)]
               public List<TodoWrapper> Todos { get; set; }

               [DataMember(Name = "defaultAlert", Order = 160)]
               public EventAlertWrapper DefaultAlertType
               {
                   get{
                       return EventAlertWrapper.ConvertToTypeSurrogated(UserCalendar.EventAlertType);            
                   }
                   set { }
               }

               [DataMember(Name = "timeZone", Order = 160)]
               public TimeZoneWrapper TimeZoneInfo
               {
                   get {
                       return new TimeZoneWrapper(UserCalendar.TimeZone);
                   }
                   set{}
               }

               [DataMember(Name = "canEditTimeZone", Order = 160)]
               public bool CanEditTimeZone
               {
                   get { return UserCalendar.Context.CanChangeTimeZone;}
                   set { }
               }

               public static object GetSample()
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
                       isSubscription = false
                   };
               }

           }
           */
}
