using iText.Signatures;

namespace DocumentSigner
{
    public record SignatureInformation(string PathToIntermediaryPdf,
                                       string pathToSignedPdf,
                                       byte[] Signature,
                                       byte[] NakedHashFromIntermediaryPdf,
                                       ITSAClient? tsaClient = null);
}
