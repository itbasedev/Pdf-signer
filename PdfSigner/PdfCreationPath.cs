namespace DocumentSigner
{
    public class PdfCreationPath
    {
        public string PdfTobeSigned { get; set; }
        public string TemporaryPdf { get; set; }
        public string SignedPdf { get; set; }

        public PdfCreationPath(string pdfTobeSigned,string temporaryPdf,string signedPdf)
        {
            PdfTobeSigned = pdfTobeSigned;
            TemporaryPdf = temporaryPdf;
            SignedPdf = signedPdf;
        }
    }
}
