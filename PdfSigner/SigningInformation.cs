using iText.IO.Image;

namespace DocumentSigner
{
    public record SigningInformation(string PathToPdf, string PathToIntermediaryPdf, int PageNumber = 1, string Reason = "", string Location = "", ImageData? Logo = null);
}
