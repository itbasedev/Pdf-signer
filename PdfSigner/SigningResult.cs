using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DocumentSigner
{
    public class SigningResult
    {
        public int CorrelationID { get; set; } = default!;
        public string ApplicationCode { get; set; }
        public string DocumentType { get; set; }
        public string DocumentNumber { get; set; }
        public int DocumentYear { get; set; }
        public DateTime EmissionDate { get; set; }
        public string Signature { get; set; }
        public string PdfContent { get; set; }
        public bool IsSigned { get; set; }
        public string? Error { get; set; }

        public SigningResult(
            int correlationID,
            string applicationCode,
            string documentType,
            string documentNumber,
            int documentYear,
            DateTime emissionDate,
            string signature,
            string pdfContent,
            bool isSigned,
            string? error = null)
        {
            CorrelationID = correlationID;
            ApplicationCode = applicationCode;
            DocumentType = documentType;
            DocumentNumber = documentNumber;
            DocumentYear = documentYear;
            EmissionDate = emissionDate;
            Signature = signature;
            PdfContent = pdfContent;
            IsSigned = isSigned;
            Error = error;
        }
    }
}
