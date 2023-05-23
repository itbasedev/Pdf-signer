namespace DocumentSigner
{
    public record SigningHashes(byte[] HashToBeSigned, byte[] NakedHash);
}
