using iText.IO.Image;
using iText.Kernel.Pdf;
using iText.Signatures;
using Newtonsoft.Json;
using Org.BouncyCastle.X509;
using System.ComponentModel;
using System.Linq;

namespace DocumentSigner
{
    public class Signer
    {
        public List<string> CertificateChain { get; set; }
        public List<SigningDocument> DocumentsToSign { get; set; }
        public string Reason { get; set; }
        public string Location { get; set; }
        public bool IgnoreCRL { get; set; } = true;
        private DirectoryInfo _repository { get; set; }
        private readonly List<X509Certificate> _certificateChain;
        private List<SigningHashes> _signingHashes = new();
        private TSAClientBouncyCastle? _tsaClient = GetTSAClient();
        private List<ICrlClient> _crlClients;
        private OcspClientBouncyCastle _ocspClient = new(null);
        private PdfSigningManagerService _pdfSigner;
        private List<PdfCreationPath> _pdfCreationPathList = new();
        public Signer(
            List<string> certificateChain,
            List<SigningDocument> documentsToSign,
            string reason,
            string location)
        {
            //--
            CertificateChain = certificateChain;
            Reason = reason;
            Location = location;
            DocumentsToSign = documentsToSign;
            _certificateChain = certificateChain.AsX509certificates();
            _crlClients = new List<ICrlClient> { new CrlClientOnline(_certificateChain.ToArray()) };
            _pdfSigner = new PdfSigningManagerService(_certificateChain,
                                                         crlClients: IgnoreCRL ? null:_crlClients,
                                                         ocspClient: IgnoreCRL ? null:_ocspClient,
                                                         tsaClient: _tsaClient);
            _repository = GetPdfRepository();
        }
        //key = correlationID, value = signed hash
        public async Task<Dictionary<int, string>> GetHashesToBeSigned()
        {
            Dictionary<int, string> hashesTobeSigned = new();

            foreach (var document in DocumentsToSign)
            {
                string ID = Guid.NewGuid().ToString();
                var documentBytes = Convert.FromBase64String(document.Content);

                PdfCreationPath pdfCreationPath = new(_repository.FullName + $"\\ToBeSigned_{ID}.pdf",
                                                      _repository.FullName + $"\\ToBeSigned_TEMP_{ID}.pdf",
                                                      _repository.FullName + $"\\{BuildDocumentName(document)}_{ID}.pdf");


                await File.WriteAllBytesAsync(pdfCreationPath.PdfTobeSigned, documentBytes);

                SigningHashes signingHashes = _pdfSigner.CreateTemporaryPdfForSigning(new SigningInformation(pdfCreationPath.PdfTobeSigned,
                                                                                pdfCreationPath.TemporaryPdf,
                                                                                Reason: Reason,
                                                                                Location: Location,
                                                                                Logo: null));

                if (signingHashes != null)
                {
                    _pdfCreationPathList.Add(pdfCreationPath);
                    _signingHashes?.Add(signingHashes);

                    hashesTobeSigned.Add(document.CorrelationID,
                                         Convert.ToBase64String(signingHashes.HashToBeSigned));
                }
            }
            return hashesTobeSigned;
        }
        public async Task<List<SigningResult>> SignPdf(List<string> Signatures)
        {
            List<SigningResult> signingResultList = new();
            for (int i = 0; i < Signatures.Count; i++)
            {
                (bool isSigned, string? errors) signingValidation = (false,null);
                try
                {
                    _pdfSigner.SignIntermediatePdf(new SignatureInformation(_pdfCreationPathList[i].TemporaryPdf,
                                                                            _pdfCreationPathList[i].SignedPdf,
                                                                            Convert.FromBase64String(Signatures[i]),
                                                                            _signingHashes[i].NakedHash,
                                                                            null));

                    byte[] signedPDf = await File.ReadAllBytesAsync(_pdfCreationPathList[i].SignedPdf);

                    signingValidation = IsSignatureGenuineAndNotModified(_pdfCreationPathList[i].SignedPdf);

                    signingResultList.Add(new(DocumentsToSign[i].CorrelationID,
                                              signature: Signatures[i],
                                              pdfContent: Convert.ToBase64String(signedPDf),
                                              isSigned: signingValidation.isSigned,
                                              applicationCode: DocumentsToSign[i].ApplicationCode,
                                              emissionDate: DocumentsToSign[i].EmissionDate,
                                              documentType: DocumentsToSign[i].DocumentType,
                                              documentNumber: DocumentsToSign[i].DocumentNumber,
                                              documentYear: DocumentsToSign[i].DocumentYear,
                                              error: signingValidation.errors
                                              ));
                }
                catch (Exception e)
                {
                    signingResultList.Add(new(DocumentsToSign[i].CorrelationID,
                                              signature: Signatures[i],
                                              pdfContent: string.Empty,
                                              isSigned: signingValidation.isSigned,
                                              applicationCode: DocumentsToSign[i].ApplicationCode,
                                              emissionDate: DocumentsToSign[i].EmissionDate,
                                              documentType: DocumentsToSign[i].DocumentType,
                                              documentNumber: DocumentsToSign[i].DocumentNumber,
                                              documentYear: DocumentsToSign[i].DocumentYear,
                                              error: e.Message + (signingValidation.errors?.Length > 0 ? " - "+signingValidation.errors: string.Empty )));
                }
            }
            await Task.Run(() => CleanRepository());

            return signingResultList;
        }
        private (bool isSigned,string? errors) IsSignatureGenuineAndNotModified(string pdfPath)
        {
            bool isValid = false;
            string? errors = null;
            try
            {
                PdfDocument pdfDocument = new PdfDocument(new PdfReader(pdfPath));
                //--
                SignatureUtil signatureUtil = new SignatureUtil(pdfDocument);
                //--
                PdfPKCS7 signature1 = signatureUtil.ReadSignatureData(Settings.SIGNATURE_NAME);
                //--
                if (signature1 != null)
                {
                    isValid = signature1.VerifySignatureIntegrityAndAuthenticity();
                }

                pdfDocument.Close();
            }
            catch(Exception e)
            {
                isValid = false;
                errors = e.Message;
            }
            return (isValid, errors);
        }
        private string BuildDocumentName(SigningDocument document)
        {
            return document.ApplicationCode + "_" +
                   document.DocumentType + "_" +
                   document.DocumentNumber.Replace(@"\", ".").Replace(@"/", ".") + "_" +
                   document.DocumentYear.ToString();
        }
        private void CleanRepository()
        {
            foreach (var pdf in _pdfCreationPathList)
            {
                try
                {
                    var files = _repository.GetFiles().Where(x => x.FullName == pdf.PdfTobeSigned ||
                                                                  x.FullName == pdf.TemporaryPdf ||
                                                                  x.FullName == pdf.SignedPdf);

                    foreach (var file in files)
                    {
                        file.Delete();
                    }
                }
                catch
                {

                }
            }
        }
        private static TSAClientBouncyCastle? GetTSAClient()
        {
            TSAClientBouncyCastle? tsaClient = null;
            if (File.Exists("PdfSigner\\Config.json"))
            {
                using (StreamReader r = new StreamReader("PdfSigner\\Config.json"))
                {
                    string json = r.ReadToEnd();
                    //--
                    var config = JsonConvert.DeserializeObject<Config>(json);
                    //--
                    if (!string.IsNullOrEmpty(config?.TSAClient))
                    {
                        tsaClient = new(config.TSAClient);
                    }
                }
            }
            return tsaClient;
        }
        private DirectoryInfo GetPdfRepository()
        {
            DirectoryInfo folder;
            if (!Directory.Exists("PdfTemporaryCreationRepository"))
                folder = Directory.CreateDirectory("PdfTemporaryCreationRepository");
            else
                folder = new DirectoryInfo("PdfTemporaryCreationRepository");

            return folder;
        }
    }
}
