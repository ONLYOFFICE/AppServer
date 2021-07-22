using ASC.Mail.Models;
using ASC.Web.Api.Routing;
using Microsoft.AspNetCore.Mvc;
using System;

namespace ASC.Mail.Controllers
{
    public partial class MailController : ControllerBase
    {
        /// <summary>
        /// This method needed for update or create autoreply.
        /// </summary>
        /// <param name="mailboxId">Id of updated mailbox.</param>
        /// <param name="turnOn">New autoreply status.</param>
        /// <param name="onlyContacts">If true then send autoreply only for contacts.</param>
        /// <param name="turnOnToDate">If true then field To is active.</param>
        /// <param name="fromDate">Start date of autoreply sending.</param>
        /// <param name="toDate">End date of autoreply sending.</param>
        /// <param name="subject">New autoreply subject.</param>
        /// <param name="html">New autoreply value.</param>
        [Create(@"autoreply/update/{mailboxId}")]
        public MailAutoreplyData UpdateAutoreply(int mailboxId, bool turnOn, bool onlyContacts,
            bool turnOnToDate, DateTime fromDate, DateTime toDate, string subject, string html)
        {
            var result = AutoreplyEngine
                .SaveAutoreply(mailboxId, turnOn, onlyContacts, turnOnToDate, fromDate, toDate, subject, html);

            return result;
        }
    }
}
