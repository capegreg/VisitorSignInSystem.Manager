using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VisitorSignInSystem.Manager.Helpers
{
    public static class ValidateEmail
    {
        /// <summary>
        /// Checks a string of delimited email addresses.
        /// Return true if all the email addresses are valid,
        /// otherwise, returns false
        /// </summary>
        /// <param name="email"></param>
        /// <returns></returns>
        public static string IsValidEmail(string email)
        {
            string last_email = "";
            try
            {
                string[] subs = email.Split(';');

                foreach (var item in subs)
                {
                    last_email = item;
                    var addr = new System.Net.Mail.MailAddress(item);
                }
                return "";
            }
            catch
            {
                return last_email;
            }
        }
    }
}
