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
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using ASC.Common.Utils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ASC.Common;

namespace ASC.Calendar.Models
{
    [DataContract(Name = "timeZone", Namespace = "")]
    [Scope]
    public class TimeZoneWrapper {
        [DataMember(Name = "name", Order = 0)]
        public string Name { get; set; }

        [DataMember(Name = "id", Order = 0)]
        public string Id { get; set; }

        [DataMember(Name = "offset", Order = 0)]
        public int Offset { get; set; }

        public static TimeZoneWrapper GetSample()
        {
            return new TimeZoneWrapper { Offset = 0, Id = "UTC", Name = "UTC" };
        }
    }

    [Scope]
    public class TimeZoneWrapperHelper
    {
        private TimeZoneInfo _timeZone;
        private TimeZoneConverter TimeZoneConverter;
        public TimeZoneWrapperHelper(TimeZoneConverter timeZoneConverter)
        {
            TimeZoneConverter = timeZoneConverter;
        }
        public TimeZoneWrapper Get(TimeZoneInfo timeZone)
        {
            _timeZone = timeZone;

            var result = new TimeZoneWrapper();

            result.Name = TimeZoneConverter.GetTimeZoneName(_timeZone);
            result.Id = _timeZone.Id;
            result.Offset = (int)_timeZone.GetOffset().TotalMinutes;

            return result;
        }
    }
  
    
    
    
    
    /*
    [DataContract(Name = "timeZone", Namespace = "")]
    public class TimeZoneWrapper
    {
        private TimeZoneInfo _timeZone;
        private TimeZoneConverter TimeZoneConverter;
        public TimeZoneWrapper(TimeZoneInfo timeZone, TimeZoneConverter timeZoneConverter)
        {
            _timeZone = timeZone;
            TimeZoneConverter = timeZoneConverter;
        }

        [DataMember(Name = "name", Order = 0)]
        public string Name
        {
            get
            {
                return TimeZoneConverter.GetTimeZoneName(_timeZone);
            }
            set { }
        }

        [DataMember(Name = "id", Order = 0)]
        public string Id
        {
            get
            {
                return _timeZone.Id;
            }
            set { }
        }

        [DataMember(Name = "offset", Order = 0)]
        public int Offset
        {
            get
            {
                return (int)_timeZone.GetOffset().TotalMinutes;
            }
            set { }
        }

        public static TimeZoneWrapper GetSample()
        {
            return new TimeZoneWrapper { Offset = 0, Id = "UTC", Name = "UTC" };
        }


    }
    */
}
