using ASC.Web.Api.Routing;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;

namespace ASC.Mail.Controllers
{
    public partial class MailController : ControllerBase
    {
        /// <summary>
        ///    Returns list of all trusted addresses for image displaying.
        /// </summary>
        /// <returns>Addresses list. Email adresses represented as string name@domain.</returns>
        /// <short>Get trusted addresses</short> 
        /// <category>Images</category>
        [Read("display_images/addresses")]
        public IEnumerable<string> GetDisplayImagesAddresses()
        {
            return DisplayImagesAddressEngine.Get();
        }

        /// <summary>
        ///    Add the address to trusted addresses.
        /// </summary>
        /// <param name="address">Address for adding. </param>
        /// <returns>Added address</returns>
        /// <short>Add trusted address</short> 
        /// <exception cref="ArgumentException">Exception happens when in parameters is invalid. Text description contains parameter name and text description.</exception>
        /// <category>Images</category>
        [Create("display_images/address")]
        public string AddDisplayImagesAddress(string address)
        {
            DisplayImagesAddressEngine.Add(address);

            return address;
        }

        /// <summary>
        ///    Remove the address from trusted addresses.
        /// </summary>
        /// <param name="address">Address for removing</param>
        /// <returns>Removed address</returns>
        /// <short>Remove from trusted addresses</short> 
        /// <exception cref="ArgumentException">Exception happens when in parameters is invalid. Text description contains parameter name and text description.</exception>
        /// <category>Images</category>
        [Delete("display_images/address")]
        public string RemovevDisplayImagesAddress(string address)
        {
            DisplayImagesAddressEngine.Remove(address);

            return address;
        }
    }
}
