using ASC.Mail.Enums;
using ASC.Mail.Models;
using ASC.Web.Api.Routing;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;

namespace ASC.Mail.Controllers
{
    public partial class MailController : ControllerBase
    {
        /// <summary>
        ///    Returns the list of the contacts for auto complete feature.
        /// </summary>
        /// <param name="term">string part of contact name, lastname or email.</param>
        /// <returns>Strings list.  Strings format: "Name Lastname" email</returns>
        /// <short>Get contact list for auto complete</short> 
        /// <category>Contacts</category>
        /// <exception cref="ArgumentException">Exception happens when in parameters is invalid. Text description contains parameter name and text description.</exception>
        [Read(@"emails/search")]
        public IEnumerable<string> SearchEmails(string term)
        {
            if (string.IsNullOrEmpty(term))
                throw new ArgumentException(@"term parameter empty.", "term");

            return ContactEngine.SearchEmails(TenantId, UserId, term, 
                MailAutocompleteMaxCountPerSystem, MailAutocompleteTimeout);
        }

        /// <summary>
        ///    Returns lists of mail contacts.
        /// </summary>
        /// <param optional="true" name="search">Text to search in contacts name or emails.</param>
        /// <param optional="true" name="contactType">Type of contacts</param>
        /// <param optional="true" name="pageSize">Count of contacts on page</param>
        /// <param optional="true" name="fromIndex">Page number</param> 
        /// <param name="sortorder">Sort order by name. String parameter: "ascending" - ascended, "descending" - descended.</param> 
        /// <returns>List of filtered contacts</returns>
        /// <short>Gets filtered contacts</short> 
        /// <category>Contacts</category>
        [Read(@"contacts")]
        public IEnumerable<MailContactData> GetContacts(string search, int? contactType, int? pageSize, int fromIndex,
            string sortorder)
        {
            var contacts = ContactEngine
                .GetContacts(search, contactType, pageSize, fromIndex, sortorder, out int totalCount);

            ApiContext.SetTotalCount(totalCount);

            return contacts;
        }

        /// <summary>
        ///   Returns lists of mail contacts with contact information
        /// </summary>
        /// <param optional="false" name="infoType">infoType</param>
        /// <param optional="false" name="data">data</param>
        /// <param optional="true" name="isPrimary">isPrimary</param>
        /// <returns>List of filtered contacts</returns>
        /// <short>Gets filtered contacts</short> 
        /// <category>Contacts</category>
        [Read(@"contacts/bycontactinfo")]
        public IEnumerable<MailContactData> GetContactsByContactInfo(ContactInfoType infoType, string data, bool? isPrimary)
        {
            var contacts = ContactEngine.GetContactsByContactInfo(infoType, data, isPrimary);

            return contacts;
        }

        /// <summary>
        ///    Create mail contact
        /// </summary>
        /// <param name="model">instance of ContactModel</param>
        /// <returns>Information about created contact </returns>
        /// <short>Create mail contact</short>
        /// <category>Contacts</category>
        [Create(@"contact/add")]
        public MailContactData CreateContact(ContactModel model)
        {
            var newContact = ContactEngine.CreateContact(model);

            return newContact;
        }

        /// <summary>
        ///    Removes selected mail contacts
        /// </summary>
        /// <param name="ids">List of mail contact ids</param>
        /// <returns>List of removed mail contact ids </returns>
        /// <short>Remove mail contact </short> 
        /// <category>Contacts</category>
        [Update(@"contacts/remove")]
        public IEnumerable<int> RemoveContacts(List<int> ids)
        {
            ContactEngine.RemoveContacts(ids);

            return ids;
        }

        /// <summary>
        ///    Updates the existing mail contact
        /// </summary>
        /// <param name="id">id of mail contact</param>
        /// <param name="name">Contact's name</param>
        /// <param name="description">Description of contact</param>
        /// <param name="emails">List of emails</param>
        /// <param name="phoneNumbers">List of phone numbers</param>
        /// <returns>Information about updated contact </returns>
        /// <short>Update mail contact</short>
        /// <category>Contacts</category>
        [Update(@"contact/update")]
        public MailContactData UpdateContact(ContactModel model)
        {
            var contact = ContactEngine.UpdateContact(model);

            return contact;
        }

        /// <summary>
        ///    Returns list of crm entities linked with chain. Entity: contact, case or opportunity.
        /// </summary>
        /// <param name="messageId">Id of message included in the chain. It may be id any of messages included in the chain.</param>
        /// <returns>List of structures: {entity_id, entity_type, avatar_link, title}</returns>
        /// <short>Get crm linked entities</short> 
        /// <category>Contacts</category>
        ///<exception cref="ArgumentException">Exception happens when in parameters is invalid. Text description contains parameter name and text description.</exception>
        [Read(@"crm/linked/entities")]
        public IEnumerable<CrmContactData> GetLinkedCrmEntitiesInfo(int messageId)
        {
            if (messageId < 0)
                throw new ArgumentException(@"meesage_id must be positive integer", "message_id");

            return CrmLinkEngine.GetLinkedCrmEntitiesId(messageId);
        }
    }
}
