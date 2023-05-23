using iText.Kernel.Pdf;
using iText.Signatures;
using Org.BouncyCastle.X509;

namespace DocumentSigner
{
    public class SignerContainerPreparation : ExternalBlankSignatureContainer
    {
        private readonly IEnumerable<X509Certificate> _certificates;
        private readonly IEnumerable<byte[]>? _crlBytesCollection;
        private readonly IEnumerable<byte[]>? _ocspBytes;
        private readonly bool _ignoreCRL;

        public byte[] NakedHash { get; private set; } = new byte[0];
        public byte[] HashToBeSigned { get; private set; } = new byte[0];

        public SignerContainerPreparation(IEnumerable<X509Certificate> certificates,
                                          bool ignoreCRL,
                                          IEnumerable<byte[]>? crlBytesCollection,
                                          IEnumerable<byte[]>? ocspBytes) : base(PdfName.Adobe_PPKLite, PdfName.Adbe_pkcs7_detached)
        {
            _certificates = certificates;
            _crlBytesCollection = crlBytesCollection;
            _ocspBytes = ocspBytes;
            _ignoreCRL = ignoreCRL;
        }
        public override byte[] Sign(Stream data)
        {

            var sgn = new PdfPKCS7(null,
                                    _certificates.ToArray(),
                                    DigestAlgorithms.SHA256,
                                    false);

            NakedHash = DigestAlgorithms.Digest(data, DigestAlgorithms.SHA256);

            var docBytes = sgn.GetAuthenticatedAttributeBytes(NakedHash,
                                                              PdfSigner.CryptoStandard.CMS,
                                                              _ignoreCRL? null:_ocspBytes?.ToList(),
                                                              _ignoreCRL ? null: _crlBytesCollection?.ToList());

            using var hashMemoryStream = new MemoryStream(docBytes, false);
            var docBytesHash = DigestAlgorithms.Digest(hashMemoryStream, DigestAlgorithms.SHA256);


            var totalHash = new byte[Settings.SHA256_HASH_PREFIX.Length + docBytesHash.Length];
            Settings.SHA256_HASH_PREFIX.CopyTo(totalHash, 0);
            docBytesHash.CopyTo(totalHash, Settings.SHA256_HASH_PREFIX.Length);
            HashToBeSigned = totalHash;

            return Array.Empty<byte>();
        }
    }
}
