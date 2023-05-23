using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.Signatures;
using Org.BouncyCastle.X509;
using System.Text;

namespace DocumentSigner
{
    public class PdfSigningManagerService
    {
        private readonly IEnumerable<ICrlClient>? _crlClients;
        private readonly IOcspClient? _ocspClient;
        private readonly ITSAClient? _tsaClient;
        private readonly IList<byte[]>? _crlBytesList;
        private readonly IList<byte[]>? _ocspBytesList;
        private readonly bool _ignoreCRL;


        private readonly IList<X509Certificate> _userCertificateChain;
        public PdfSigningManagerService(IEnumerable<X509Certificate> userCertificateChainChain,
                                 IOcspClient? ocspClient = null,
                                 IEnumerable<ICrlClient>? crlClients = null,
                                 ITSAClient? tsaClient = null)
        {
            _userCertificateChain = userCertificateChainChain.ToList();
            _ocspClient = ocspClient;
            _crlClients = crlClients;
            _tsaClient = tsaClient;
            _crlBytesList = GetCrlByteList();
            _ocspBytesList = GetOcspBytesList();
        }

        public SigningHashes CreateTemporaryPdfForSigning(SigningInformation signingInformation)
        {
            var pdfSigner = new PdfSigner(new PdfReader(signingInformation.PathToPdf),
                                          new FileStream(signingInformation.PathToIntermediaryPdf, FileMode.Create),
                                          new StampingProperties());

            pdfSigner.SetFieldName(Settings.SIGNATURE_NAME);


            var appearance = pdfSigner.GetSignatureAppearance();

            appearance.SetPageRect(new Rectangle(0,
                                                 0,
                                                 0,
                                                 0))
                      .SetPageNumber(signingInformation.PageNumber)
                      .SetLayer2FontSize(6f)
                      .SetReason(signingInformation.Reason)
                      .SetLocation(signingInformation.Location)
                      .SetLayer2Text(BuildVisibleInformation(signingInformation.Reason, signingInformation.Location))
                      .SetCertificate(_userCertificateChain[0]);

            if (signingInformation.Logo != null)
            {
                appearance.SetRenderingMode(PdfSignatureAppearance.RenderingMode.GRAPHIC_AND_DESCRIPTION)
                          .SetSignatureGraphic(signingInformation.Logo);
            }


            var container = new SignerContainerPreparation(_userCertificateChain,_ignoreCRL, _crlBytesList, _ocspBytesList);
            pdfSigner.SignExternalContainer(container, EstimateContainerSize(_crlBytesList));

            return new(container.HashToBeSigned, container.NakedHash);
        }

        public void SignIntermediatePdf(SignatureInformation signatureInformation)
        {
            var document = new PdfDocument(new PdfReader(signatureInformation.PathToIntermediaryPdf));
            using var writer = new FileStream(signatureInformation.pathToSignedPdf, FileMode.Create);

            var container = new InjectSignatureContainer(signatureInformation.Signature,
                                                                    _userCertificateChain,
                                                                    signatureInformation.NakedHashFromIntermediaryPdf,
                                                                    _crlBytesList,
                                                                    _ocspBytesList,
                                                                    _tsaClient,
                                                                    _ignoreCRL);

            PdfSigner.SignDeferred(document, Settings.SIGNATURE_NAME, writer, container);

            document.Close();
        }

        private string BuildVisibleInformation(string? reason = "null", string? location = null)
        {
            CertificateInfo.X500Name subjectFields = CertificateInfo.GetSubjectFields(_userCertificateChain[0]);

            var stringBuilder = new StringBuilder();
            stringBuilder.AppendLine($"Assinado por {subjectFields?.GetField("CN") ?? subjectFields?.GetField("E") ?? ""}");
            stringBuilder.AppendLine($"BI: {subjectFields?.GetField("SN") ?? ""}");
            stringBuilder.AppendLine($"Date: {DateTime.Now:yyyy.MM.dd HH:mm:ss}");
            if (!string.IsNullOrEmpty(location))
            {
                stringBuilder.AppendLine($"Local: {location ?? ""}");
            }
            if (!string.IsNullOrEmpty(reason))
            {
                stringBuilder.AppendLine($"Motivo: {reason ?? ""}");
            }

            return stringBuilder.ToString();
        }


        private IList<byte[]>? GetCrlByteList() => _crlClients == null
                                                       ? null
                                                       : _userCertificateChain.Select(x509 => GetCrlClientBytesList(x509))
                                                                              .SelectMany(crlBytes => crlBytes)
                                                                              .ToList();

        private IList<byte[]>? GetCrlClientBytesList(X509Certificate certificate)
        {
            if (_crlClients == null)
            {
                return null;
            }

            var crls = _crlClients.Select(crlClient => crlClient.GetEncoded(certificate, null))
                                  .Where(encoded => encoded != null)
                                  .SelectMany(bytes => bytes)
                                  .ToList();
            return crls;
        }

        private IList<byte[]>? GetOcspBytesList()
        {
            if (_userCertificateChain.Count <= 1 ||
               _ocspClient == null)
            {
                return null;
            }

            var list = new List<byte[]>();
            for (var i = 0; i < _userCertificateChain.Count - 1; i++)
            {
                var encoded = _ocspClient.GetEncoded(_userCertificateChain[i], _userCertificateChain[i + 1], null);
                if (encoded != null)
                {
                    list.Add(encoded);
                }
            }

            return list;
        }


        private int EstimateContainerSize(IEnumerable<byte[]>? crlBytesList)
        {
            if (_ocspClient == null)
                return 8192 * 2 + 2;

            int estimatedSize = 8192 +
                              (_ocspClient != null ? 4192 : 0) +
                              (_tsaClient != null ? 4600 : 0);
            if (crlBytesList != null)
            {
                estimatedSize += crlBytesList.Sum(crlBytes => crlBytes.Length + 10);
            }

            return estimatedSize;
        }

    }
}
