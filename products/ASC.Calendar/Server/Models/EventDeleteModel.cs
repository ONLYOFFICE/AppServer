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
    public class EventDeleteModel
    {

        [DataMember(Name = "event_id", Order = 1)]
        public int EventId { get; set; }

        [DataMember(Name = "date", Order = 0)]
        public ApiDateTime Date { get; set; }

        [DataMember(Name = "type", Order = 2)]
        public EventRemoveType Type { get; set; }

        [DataMember(Name = "uri", Order = 50)]
        public Uri Uri { get; set; }

        [DataMember(Name = "from_caldav_server", Order = 200)]
        public bool FromCaldavServer { get; set; }



        /*public static object GetSample()
        {
            return new
            {
                
            };
        }*/

    }
}
