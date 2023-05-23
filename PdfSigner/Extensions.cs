using Org.BouncyCastle.X509;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DocumentSigner
{
    public static class Extensions
    {
        public static List<X509Certificate> AsX509certificates(this List<string> certificates)
        {

            List<X509Certificate> x509CertificateList = new();

            foreach (string certificate in certificates)
            {
                byte[] bytes = Convert.FromBase64String(certificate);
                x509CertificateList.Add(new X509CertificateParser().ReadCertificate(bytes));
            }
            return x509CertificateList;
        }
    }
}
