using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ASC.Calendar.Models
{
    public class CaldavEventModel
    {
        public string CalendarId { get; set; }
        public string Uid { get; set; }
        public int Alert { get; set; }
        public List<string> Responsibles { get; set; } 
    }
}
