using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;

using ASC.Mail.Core.Dao.Expressions.Mailbox;
using ASC.Mail.Core.Engine.Operations.Base;
using ASC.Mail.Core.Resources;
using ASC.Mail.Enums;
using ASC.Mail.Exceptions;
using ASC.Mail.Extensions;
using ASC.Mail.Models;
using ASC.Web.Api.Routing;

using Microsoft.AspNetCore.Mvc;

namespace ASC.Mail.Controllers
{
    public partial class MailController : ControllerBase
    {
        /// <summary>
        ///    Returns lists of all mailboxes, aliases and groups for user.
        /// </summary>
        /// <param name="username" visible="false">User id</param>
        /// <returns>Mailboxes, aliases and groups list</returns>
        /// <short>Get mailboxes, aliases and groups list</short> 
        /// <category>Accounts</category>
        [Read("accounts")]
        public IEnumerable<MailAccountData> GetAccounts()
        {
            var accounts = AccountEngine.GetAccountInfoList();
            return accounts.ToAccountData();
        }

        /// <summary>
        ///    Returns the information about the account.
        /// </summary>
        /// <param name="email">Account email address</param>
        /// <returns>Account with specified email</returns>
        /// <exception cref="ArgumentException">Exception happens when in parameters is invalid. Text description contains parameter name and text description.</exception>
        /// <exception cref="NullReferenceException">Exception happens when mailbox wasn't found by email.</exception>
        /// <short>Get account by email</short> 
        /// <category>Accounts</category>
        [Read(@"accounts/single")]
        public MailBoxData GetAccount(string email)
        {
            var account = AccountEngine.GetAccount(email);

            return account;
        }

        /// <summary>
        ///    Creates account using full information about mail servers.
        /// </summary>
        /// <param name="model">instance of AccountModel</param>
        /// <returns>Created account</returns>
        /// <exception cref="Exception">Exception contains text description of happened error.</exception>
        /// <short>Create account with custom mail servers.</short> 
        /// <category>Accounts</category>
        [Create("accounts")]
        public MailAccountData CreateAccount(AccountModel model)
        {
            var accountInfo = AccountEngine.TryCreateAccount(model, out LoginResult loginResult);

            if (accountInfo == null)
                throw new LoginException(loginResult);

            return accountInfo.ToAccountData().FirstOrDefault();
        }

        /// <summary>
        ///    Creates an account based on email and password.
        /// </summary>
        /// <param name="email">Account email in string format like: name@domain</param>
        /// <param name="password">Password as plain text.</param>
        /// <exception cref="ArgumentException">Exception happens when in parameters is invalid. Text description contains parameter name and text description.</exception>
        /// <exception cref="Exception">Exception contains text description of happened error.</exception>
        /// <returns>Created account</returns>
        /// <short>Create new account by email and password</short> 
        /// <category>Accounts</category>
        [Create(@"accounts/simple")]
        public MailAccountData CreateAccountSimple(string email, string password)
        {
            if (string.IsNullOrEmpty(email))
                throw new ArgumentException(@"Empty email", "email");

            //Thread.CurrentThread.CurrentCulture = CurrentCulture;
            //Thread.CurrentThread.CurrentUICulture = CurrentCulture;

            string errorText = null;

            try
            {
                var account = AccountEngine.CreateAccountSimple(email, password, out List<LoginResult> loginResults);

                if (account != null)
                    return account.ToAccountData().FirstOrDefault();

                var i = 0;

                foreach (var loginResult in loginResults)
                {
                    errorText += string.Format("#{0}:<br>", ++i);

                    if (!loginResult.IngoingSuccess)
                    {
                        errorText += GetFormattedTextError(loginResult.IngoingException,
                            loginResult.Imap ? ServerType.Imap : ServerType.Pop3, false) + "<br>";
                    }

                    if (!loginResult.OutgoingSuccess)
                    {
                        errorText += GetFormattedTextError(loginResult.OutgoingException, ServerType.Smtp, false) +
                                     "<br>";
                    }
                }

            }
            catch (Exception ex)
            {
                //TODO: change AttachmentsUnknownError to common unknown error text
                errorText = GetFormattedTextError(ex, MailApiResource.AttachmentsUnknownError);
            }

            throw new Exception(errorText);
        }

        /// <summary>
        ///    Creates Mail account with OAuth authentication. Only Google OAuth supported.
        /// </summary>
        /// <param name="code">Oauth code</param>
        /// <param name="type">Type of OAuth service. 0- Unknown, 1 - Google.</param>
        /// <exception cref="Exception">Exception contains text description of happened error.</exception>
        /// <returns>Created account</returns>
        /// <short>Create OAuth account</short> 
        /// <category>Accounts</category>
        [Create(@"accounts/oauth")]
        public MailAccountData CreateAccountOAuth(string code, byte type)
        {
            if (string.IsNullOrEmpty(code))
                throw new ArgumentException(@"Empty oauth code", "code");

            try
            {
                var account = AccountEngine.CreateAccountOAuth(code, type);
                return account.ToAccountData().FirstOrDefault();
            }
            catch (Exception imapException)
            {
                throw new Exception(GetFormattedTextError(imapException, ServerType.ImapOAuth,
                    imapException is ImapConnectionTimeoutException));
            }
        }

        /// <summary>
        ///    Update Mail account with OAuth authentication. Only Google OAuth supported.
        /// </summary>
        /// <param name="code">Oauth code</param>
        /// <param name="type">Type of OAuth service. 0- Unknown, 1 - Google.</param>
        /// <param name="mailboxId">Mailbox ID to update</param>
        /// <exception cref="Exception">Exception contains text description of happened error.</exception>
        /// <returns>Updated OAuth account</returns>
        /// <short>Update OAuth account</short> 
        /// <category>Accounts</category>
        [Update(@"accounts/oauth")]
        public MailAccountData UpdateAccountOAuth(string code, byte type, int mailboxId)
        {
            string errorText = null;

            try
            {
                var accountInfo = AccountEngine.UpdateAccountOAuth(mailboxId, code, type);

                if (accountInfo != null)
                {
                    return accountInfo.ToAccountData().FirstOrDefault();
                }
            }
            catch (Exception ex)
            {
                //TODO: change AttachmentsUnknownError to common unknown error text
                errorText = GetFormattedTextError(ex, MailApiResource.AttachmentsUnknownError);
            }

            throw new Exception(errorText ?? MailApiResource.AttachmentsUnknownError);
        }

        /// <summary>
        ///    Updates the existing account.
        /// </summary>
        /// <param name="model">instance of AccountModel</param>
        /// <returns>Updated account</returns>
        /// <exception cref="Exception">Exception contains text description of happened error.</exception>
        /// <short>Update account</short> 
        /// <category>Accounts</category>
        [Update(@"accounts")]
        public MailAccountData UpdateAccount(AccountModel accountModel)
        {
            if (accountModel == null || string.IsNullOrEmpty(accountModel.Email))
                throw new ArgumentException();

            string errorText = null;
            var mbox = new MailBoxData
            {
                Name = accountModel.Name,
                EMail = new MailAddress(accountModel.Email),
                Account = accountModel.Login,
                Password = accountModel.Password,
                Port = accountModel.Port,
                Server = accountModel.Server,
                SmtpAccount = accountModel.SmtpLogin,
                SmtpPassword = accountModel.SmtpPassword,
                SmtpPort = accountModel.SmtpPort,
                SmtpServer = accountModel.SmtpServer,
                TenantId = TenantId,
                UserId = UserId,
                BeginDate = accountModel.Restrict
                    ? DateTime.Now.Subtract(new TimeSpan(MailBoxData.DefaultMailLimitedTimeDelta))
                    : new DateTime(MailBoxData.DefaultMailBeginTimestamp),
                Encryption = accountModel.IncomingEncryptionType,
                SmtpEncryption = accountModel.OutcomingEncryptionType,
                Authentication = accountModel.IncomingAuthenticationType,
                SmtpAuthentication = accountModel.SmtpAuth ? accountModel.OutcomingAuthenticationType : SaslMechanism.None
            };

            try
            {
                var accountInfo = AccountEngine.UpdateAccount(mbox, out LoginResult loginResult);

                if (accountInfo != null)
                {
                    return accountInfo.ToAccountData().FirstOrDefault();
                }

                if (!loginResult.IngoingSuccess)
                {
                    errorText = GetFormattedTextError(loginResult.IngoingException,
                        mbox.Imap ? ServerType.Imap : ServerType.Pop3, false);
                    // exImap is ImapConnectionTimeoutException
                }

                if (!loginResult.OutgoingSuccess)
                {
                    if (!string.IsNullOrEmpty(errorText))
                        errorText += "\r\n";

                    errorText += GetFormattedTextError(loginResult.OutgoingException, ServerType.Smtp, false);
                    // exSmtp is SmtpConnectionTimeoutException);
                }
            }
            catch (Exception ex)
            {
                //TODO: change AttachmentsUnknownError to common unknown error text
                errorText = GetFormattedTextError(ex, MailApiResource.AttachmentsUnknownError);
            }

            throw new Exception(errorText ?? MailApiResource.AttachmentsUnknownError);
        }

        /// <summary>
        ///    Deletes account by email.
        /// </summary>
        /// <param name="email">Email the account to delete</param>
        /// <returns>MailOperationResult object</returns>
        /// <exception cref="ArgumentException">Exception happens when some parameters are invalid. Text description contains parameter name and text description.</exception>
        /// <exception cref="NullReferenceException">Exception happens when mailbox wasn't found.</exception>
        /// <short>Delete account</short> 
        /// <category>Accounts</category>
        [Delete(@"accounts")]
        public MailOperationStatus DeleteAccount(string email)
        {
            if (string.IsNullOrEmpty(email))
                throw new ArgumentException(@"Email empty", "email");

            var mailbox =
                MailboxEngine.GetMailboxData(new СoncreteUserMailboxExp(new MailAddress(email), TenantId,
                    UserId));

            if (mailbox == null)
                throw new NullReferenceException(string.Format("Account wasn't found by email: {0}", email));

            if (mailbox.IsTeamlab)
                throw new ArgumentException("Mailbox with specified email can't be deleted");

            //TODO: Fix return OperationEngine.RemoveMailbox(mailbox, TranslateMailOperationStatus);
            throw new NotImplementedException();
        }

        /// <summary>
        ///    Sets the state for the account specified in the request
        /// </summary>
        /// <param name="email">Email of the account</param>
        /// <param name="state">Account activity state. Value: true or false. True - enabled, False - disabled.</param>
        /// <returns>Account mailbox id</returns>
        /// <exception cref="ArgumentException">Exception happens when in parameters is invalid. Text description contains parameter name and text description.</exception>
        /// <exception cref="Exception">Exception happens when update operation failed.</exception>
        /// <short>Set account state</short> 
        /// <category>Accounts</category>
        [Update(@"accounts/state")]
        public int SetAccountEnable(string email, bool state)
        {
            if (string.IsNullOrEmpty(email))
                throw new ArgumentNullException("email");

            string errorText = null;

            var mailboxId = AccountEngine.SetAccountEnable(new MailAddress(email), state, out LoginResult loginResult);

            if (loginResult != null)
            {
                if (!loginResult.IngoingSuccess)
                {
                    errorText = GetFormattedTextError(loginResult.IngoingException,
                        loginResult.Imap ? ServerType.Imap : ServerType.Pop3, false);
                    // exImap is ImapConnectionTimeoutException
                }

                if (!loginResult.OutgoingSuccess)
                {
                    if (!string.IsNullOrEmpty(errorText))
                        errorText += "\r\n";

                    errorText += GetFormattedTextError(loginResult.OutgoingException, ServerType.Smtp, false);
                    // exSmtp is SmtpConnectionTimeoutException);
                }

                if (!string.IsNullOrEmpty(errorText))
                    throw new Exception(errorText);
            }

            if (mailboxId < 0)
                throw new Exception("EnableMaibox failed.");

            return mailboxId;
        }

        /// <summary>
        ///    Sets the default account specified in the request
        /// </summary>
        /// <param name="email">Email of the account</param>
        /// <param name="isDefault">Set or reset account as default</param>
        /// <returns>Account email address</returns>
        /// <exception cref="ArgumentException">Exception happens when in parameters is invalid. Text description contains parameter name and text description.</exception>
        /// <exception cref="Exception">Exception happens when update operation failed.</exception>
        /// <short>Set default account</short> 
        /// <category>Accounts</category>
        [Update(@"accounts/default")]
        public string SetDefaultAccount(string email, bool isDefault)
        {
            var result = AccountEngine.SetDefaultAccount(email, isDefault);

            return result;
        }

        /// <summary>
        ///    Gets the default settings for the account based on the email domain.
        /// </summary>
        /// <param name="email">Account email address</param>
        /// <param name="action">This string parameter specifies action for default settings. Values:
        /// "get_imap_pop_settings" - get imap or pop settings, imap settings are prior.
        /// "get_imap_server" | "get_imap_server_full" - get imap settings
        /// "get_pop_server" | "get_pop_server_full" - get pop settings
        /// By default returns default imap settings.
        /// </param>
        /// <returns>Account with default settings</returns>
        /// <short>Get default account settings</short> 
        /// <category>Accounts</category>
        [Read(@"accounts/setups")]
        public MailBoxData GetAccountDefaults(string email, string action)
        {
            var result = AccountEngine.GetAccountDefaults(email, action);

            return result;
        }

        /// <summary>
        ///    Sets the state for the account specified in the request
        /// </summary>
        /// <param name="mailbox_id">Id of the account</param>
        /// <param name="email_in_folder">Document's folder Id</param>
        /// <returns>Account email address</returns>
        /// <exception cref="ArgumentException">Exception happens when in parameters is invalid. Text description contains parameter name and text description.</exception>
        /// <exception cref="Exception">Exception happens when update operation failed.</exception>
        /// <short>Set account state</short> 
        /// <category>Accounts</category>
        [Update(@"accounts/emailinfolder")]
        public void SetAccountEMailInFolder(int mailbox_id, string email_in_folder)
        {
            if (mailbox_id < 0)
                throw new ArgumentNullException("mailbox_id");

            AccountEngine.SetAccountEmailInFolder(mailbox_id, email_in_folder);
        }

        /// <summary>
        /// UpdateUserActivity
        /// </summary>
        /// <param name="userOnline"></param>
        /// <category>Accounts</category>
        [Update(@"accounts/updateuseractivity")]
        public void UpdateUserActivity(bool userOnline)
        {
            AccountEngine.SetAccountsActivity(userOnline);
        }

        private static string GetFormattedTextError(Exception ex, ServerType mailServerType, bool timeoutFlag = true)
        {
            var headerText = string.Empty;
            var errorExplain = string.Empty;

            switch (mailServerType)
            {
                case ServerType.Imap:
                case ServerType.ImapOAuth:
                    headerText = MailApiResource.ImapResponse;
                    if (timeoutFlag)
                        errorExplain = MailApiResource.ImapConnectionTimeoutError;
                    break;
                case ServerType.Pop3:
                    headerText = MailApiResource.Pop3Response;
                    if (timeoutFlag)
                        errorExplain = MailApiResource.Pop3ConnectionTimeoutError;
                    break;
                case ServerType.Smtp:
                    headerText = MailApiResource.SmtRresponse;
                    if (timeoutFlag)
                        errorExplain = MailApiResource.SmtpConnectionTimeoutError;
                    break;
            }

            return GetFormattedTextError(ex, errorExplain, headerText);

        }

        //TODO: Remove HTML tags from response data
        private static string GetFormattedTextError(Exception ex, string errorExplain = "", string headerText = "")
        {
            if (!string.IsNullOrEmpty(headerText))
                headerText = string.Format("<span class=\"attempt_header\">{0}</span><br/>", headerText);

            if (string.IsNullOrEmpty(errorExplain))
                errorExplain = ex.InnerException == null ||
                                string.IsNullOrEmpty(ex.InnerException.Message)
                                    ? ex.Message
                                    : ex.InnerException.Message;

            var errorText = string.Format("{0}{1}",
                          headerText,
                          errorExplain);

            return errorText;
        }
    }
}
