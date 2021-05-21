using ASC.Api.Core;
using ASC.Web.Core.Calendars;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using static ASC.Calendar.Controllers.CalendarController;

namespace ASC.Calendar.Models
{
    public class EventModel
    {
        [DataMember(Name = "event_id", Order = 1)]
        public int EventId { get; set; }

        [DataMember(Name = "calendar_id", Order = 0)]
        public string CalendarId { get; set; }

        [DataMember(Name = "ics", Order = 2)]
        public string Ics { get; set; }
        [DataMember(Name = "title", Order = 20)]
        public string Name { get; set; }

        [DataMember(Name = "description", Order = 30)]
        public string Description { get; set; }

        [DataMember(Name = "start", Order = 40)]
        public ApiDateTime Start { get; set; }

        [DataMember(Name = "end", Order = 50)]
        public ApiDateTime End { get; set; }

        [DataMember(Name = "repeatRule", Order = 70)]
        public string RepeatRule { get; set; }

        [DataMember(Name = "allDay", Order = 60)]
        public bool AllDayLong { get; set; }

        [DataMember(Name = "alert_type", Order = 50)]
        public EventAlertType AlertType { get; set; }

        [DataMember(Name = "sharing_options", Order = 200)]
        public List<SharingParam> SharingOptions { get; set; }

        [DataMember(Name = "event_uid", Order = 1)]
        public string EventUid { get; set; }

        [DataMember(Name = "from_caldav_server", Order = 1)]
        public bool FromCalDavServer { get; set; }

        [DataMember(Name = "owner_id", Order = 1)]
        public string OwnerId { get; set; }


        /*public static object GetSample()
        {
            return new
            {
                
            };
        }*/

    }
}
