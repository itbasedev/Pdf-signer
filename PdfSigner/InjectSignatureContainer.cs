using iText.Kernel.Pdf;
using iText.Signatures;
using Org.BouncyCastle.X509;

namespace DocumentSigner
{
    public class InjectSignatureContainer : IExternalSignatureContainer
    {
        private readonly IEnumerable<X509Certificate> _certificates;
        private readonly IEnumerable<byte[]>? _crlBytesCollection;
        private readonly byte[] _documentHash;
        private readonly IEnumerable<byte[]>? _ocspBytes;
        private readonly byte[] _signature;
        private readonly ITSAClient? _tsaClient;
        private readonly bool _ignoreCRL;

        public InjectSignatureContainer(byte[] signature,
                                        IEnumerable<X509Certificate> certificates,
                                        byte[] documentHash,
                                        IEnumerable<byte[]>? crlBytesCollection,
                                        IEnumerable<byte[]>? ocspBytes,
                                        ITSAClient? tsaClient = null,
                                        bool ignoreCRL = false)
        {
            _signature = signature;
            _certificates = certificates;
            _documentHash = documentHash;
            _crlBytesCollection = crlBytesCollection;
            _ocspBytes = ocspBytes;
            _tsaClient = tsaClient;
            _ignoreCRL = ignoreCRL;
        }
        public byte[] Sign(Stream data)
        {

            var sgn = new PdfPKCS7(null,
                                   _certificates.ToArray(),
                                   DigestAlgorithms.SHA256,
                                   false);

            sgn.SetExternalDigest(_signature,
                                  null,
                                  "RSA");
            var encodedSig = sgn.GetEncodedPKCS7(_documentHash,
                                                 PdfSigner.CryptoStandard.CMS,
                                                 _tsaClient,
                                                 _ignoreCRL ? null : _ocspBytes?.ToList(),
                                                 _ignoreCRL ? null : _crlBytesCollection?.ToList());

            return encodedSig;
        }

        public void ModifySigningDictionary(PdfDictionary signDic)
        {
        }
    }
}
