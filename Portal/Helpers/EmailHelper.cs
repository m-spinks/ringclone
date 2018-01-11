using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Web;

namespace RingClone.Portal.Helpers
{
    public class EmailHelper
    {

        private static bool customCertValidation(object sender, X509Certificate cert, X509Chain chain, SslPolicyErrors errors)
        {

            return true;
        }
        public static string SendMail(string strTo, string strSubject, string strBody, string strCC, string strBCC)
        {

            System.Net.Mail.SmtpClient smtp = default(System.Net.Mail.SmtpClient);
            string username = "matt.spinks@northtechnologies.com";
            string @from = "matt.spinks@northtechnologies.com";
            string fromname = "Matt Spinks";
            string password = "123!@#qweQWE";
            string server = "mail.northtechnologies.com";
            string port = "587";
            bool usessl = false;
            System.Net.Mail.MailMessage mm = default(System.Net.Mail.MailMessage);

            try
            {
                mm = new System.Net.Mail.MailMessage("" + fromname + "<" + @from + ">", strTo);
                mm.Subject = strSubject;
                mm.Body = strBody;
                mm.IsBodyHtml = true;

                if ((strCC != null))
                {
                    if (strCC.Length > 0)
                    {
                        string[] arCC = strCC.Split(';');
                        foreach (string cc in arCC)
                        {
                            try
                            {
                                mm.CC.Add(cc);
                            }
                            catch (Exception ex)
                            {
                            }
                        }
                        arCC = null;
                    }
                }

                if ((strBCC != null))
                {
                    if (strBCC.Length > 0)
                    {
                        string[] arBCC = strBCC.Split(';');
                        foreach (string bcc in arBCC)
                        {
                            try
                            {
                                mm.Bcc.Add(bcc);
                            }
                            catch (Exception ex)
                            {
                            }
                        }
                    }
                }

                smtp = new System.Net.Mail.SmtpClient(server, Convert.ToInt16(port));
               
                smtp.EnableSsl = usessl;
                smtp.DeliveryMethod = System.Net.Mail.SmtpDeliveryMethod.Network;
                smtp.Credentials = new System.Net.NetworkCredential(username, password);

                ServicePointManager.ServerCertificateValidationCallback = new System.Net.Security.RemoteCertificateValidationCallback(customCertValidation);

                smtp.Send(mm);
            }
            catch (Exception ex)
            {
                try
                {
                    smtp.Send(mm);
                }
                catch (Exception ex2)
                {
                    return "Error - " + ex.Message;
                }
            }

            return "Success";

        }


    }
}