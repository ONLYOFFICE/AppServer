using System;
using System.Linq;
using ASC.Calendar.Resources;
using ASC.Web.Core;

namespace ASC.Calendar
{
    public class CalendarProduct : Product
    {
        internal const string ProductPath = "/products/calendar/";

        private ProductContext _context;

        public static Guid ID
        {
            get { return new Guid("{32D24CB5-7ECE-4606-9C94-19216BA42086}"); }
        }

        public override bool Visible { get { return true; } }

        public override ProductContext Context
        {
            get { return _context; }
        }

        public override string Name
        {
            get { return CalendarResource.ProductName; }
        }

        public override string Description
        {
            get { return CalendarResource.ProductDescription; }
        }

        public override string ExtendedDescription
        {
            get { return CalendarResource.ProductDescription; }
        }

        public override Guid ProductID
        {
            get { return ID; }
        }

        public override string StartURL
        {
            get { return ProductPath; }
        }

        public override string HelpURL
        {
            get { return string.Concat(ProductPath, "help.aspx"); }
        }

        public override string ProductClassName
        {
            get { return "calendar"; }
        }

        public override string ApiURL
        {
            get
            {
                return "api/2.0/calendar/info.json";
            }
        }

        public override void Init()
        {
            _context = new ProductContext
            {
                DisabledIconFileName = "product_disabled_logo.png",
                IconFileName = "product_logo.png",
                LargeIconFileName = "images/calendar.svg",
                DefaultSortOrder = 50,
                AdminOpportunities = () => CalendarResource.ProductAdminOpportunities.Split('|').ToList(),
                UserOpportunities = () => CalendarResource.ProductUserOpportunities.Split('|').ToList()
            };

            //SearchHandlerManager.Registry(new SearchHandler());
        }
    }
}
