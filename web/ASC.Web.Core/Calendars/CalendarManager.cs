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

using ASC.Common;
using ASC.Core;

namespace ASC.Web.Core.Calendars
{
    public delegate List<BaseCalendar> GetCalendarForUser(Guid userId);

    [Scope]
    public class CalendarManager
    {
        private readonly List<GetCalendarForUser> _calendarProviders = new List<GetCalendarForUser>();
        private readonly List<BaseCalendar> _calendars = new List<BaseCalendar>();

        public void RegistryCalendar(BaseCalendar calendar)
        {
            lock (this._calendars)
            {
                if (!this._calendars.Exists(c => string.Equals(c.Id, calendar.Id, StringComparison.InvariantCultureIgnoreCase)))
                    this._calendars.Add(calendar);
            }
        }

        public void UnRegistryCalendar(string calendarId)
        {
            lock (this._calendars)
            {
                this._calendars.RemoveAll(c => string.Equals(c.Id, calendarId, StringComparison.InvariantCultureIgnoreCase));
            }
        }

        public void RegistryCalendarProvider(GetCalendarForUser provider)
        {
            lock (this._calendarProviders)
            {
                if (!this._calendarProviders.Exists(p => p.Equals(provider)))
                    this._calendarProviders.Add(provider);
            }
        }

        public void UnRegistryCalendarProvider(GetCalendarForUser provider)
        {
            lock (this._calendarProviders)
            {
                this._calendarProviders.RemoveAll(p => p.Equals(provider));
            }
        }

        public BaseCalendar GetCalendarForUser(Guid userId, string calendarId, UserManager userManager)
        {
            return GetCalendarsForUser(userId, userManager).Find(c => string.Equals(c.Id, calendarId, StringComparison.InvariantCultureIgnoreCase));
        }

        public List<BaseCalendar> GetCalendarsForUser(Guid userId, UserManager userManager)
        {
            var cals = new List<BaseCalendar>();
            foreach (var h in _calendarProviders)
            {
                var list = h(userId);
                if (list != null)
                    cals.AddRange(list.FindAll(c => c.SharingOptions.PublicForItem(userId, userManager)));
            }

            cals.AddRange(_calendars.FindAll(c => c.SharingOptions.PublicForItem(userId, userManager)));
            return cals;
        }

    }
}
