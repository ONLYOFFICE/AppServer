using ASC.Mail.Models;
using ASC.Web.Api.Routing;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Linq;

namespace ASC.Mail.Controllers
{
    public partial class MailController : ControllerBase
    {
        /// <summary>
        /// This method needed for getting mailbox signature.
        /// </summary>
        /// <param name="mailbox_id"></param>
        /// <returns>Signature object</returns>
        [Read("signature/{mailbox_id}")]
        public MailSignatureData GetSignature(int mailbox_id)
        {
            var accounts = GetAccounts();

            var account = accounts.FirstOrDefault(a => a.MailboxId == mailbox_id);

            if (account == null)
                throw new ArgumentException("Mailbox not found");

            return account.Signature;
        }

        /// <summary>
        /// This method needed for update or create signature.
        /// </summary>
        /// <param name="mailbox_id">Id of updated mailbox.</param>
        /// <param name="html">New signature value.</param>
        /// <param name="is_active">New signature status.</param>
        [Create("signature/update/{mailbox_id}")]
        public MailSignatureData UpdateSignature(int mailbox_id, string html, bool is_active)
        {
            var accounts = GetAccounts();

            var account = accounts.FirstOrDefault(a => a.MailboxId == mailbox_id);

            if (account == null)
                throw new ArgumentException("Mailbox not found");

            return SignatureEngine.SaveSignature(mailbox_id, html, is_active);
        }
    }
}
