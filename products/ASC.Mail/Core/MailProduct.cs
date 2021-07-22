using System;
using ASC.Web.Core;

namespace ASC.Mail
{
    public class MailProduct : Product
    {
        internal const string ProductPath = "/products/mail/";

        private ProductContext _context;

        public static Guid ID
        {
            get { return new Guid("{2A923037-8B2D-487b-9A22-5AC0918ACF3F}"); }
        }

        public override bool Visible { get { return true; } }

        public override ProductContext Context
        {
            get { return _context; }
        }

        public override string Name
        {
            get { return "Mail"; }// MailResource.ProductName; }
        }

        public override string Description
        {
            get { return "Aggregator of mail"; } // MailResource.ProductDescription; }
        }

        public override string ExtendedDescription
        {
            get { return "Mail Aggregator Client"; }// MailResource.ProductDescription; }
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
            get { return "mail"; }
        }

        public override string ApiURL
        {
            get
            {
                return "api/2.0/mail/info.json";
            }
        }

        public override void Init()
        {
            _context = new ProductContext
            {
                DisabledIconFileName = "product_disabled_logo.png",
                IconFileName = "product_logo.png",
                LargeIconFileName = "images/mail.svg",
                DefaultSortOrder = 50,
                //AdminOpportunities = () => MailResource.ProductAdminOpportunities.Split('|').ToList(),
                //UserOpportunities = () => MailResource.ProductUserOpportunities.Split('|').ToList()
            };

            //SearchHandlerManager.Registry(new SearchHandler());
        }
    }
}
