using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DocumentSigner
{
    public class SigningDocument
    {
        public int CorrelationID { get; set; } = default!; 
        public string ApplicationCode { get; set; }
        public string CompanyCode { get; set; }
        public string DocumentType { get; set; }
        public string DocumentNumber { get; set; }
        public DateTime EmissionDate { get; set; }
        public int DocumentYear { get; set; }
        public string Content { get; set; }

        public SigningDocument(
            int correlationID,
            string applicationCode,
            string companyCode,
            string documentType,
            string documentNumber,
            DateTime emissionDate,
            int documentYear,
            string content)
        {
            CorrelationID = correlationID;
            ApplicationCode = applicationCode;
            CompanyCode = companyCode;
            DocumentType = documentType;
            DocumentNumber = documentNumber;
            EmissionDate = emissionDate;
            DocumentYear = documentYear;
            Content = content;
        }
    }
}
