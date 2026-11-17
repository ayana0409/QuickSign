namespace QuickSign.Domain.Shared.Documents;

/// <summary>
/// Channel or method used by a client to submit document verification requests.
/// </summary>
public enum VerificationMethodType
{
    /// <summary>
    /// Document uploaded directly through the web user interface.
    /// </summary>
    WebUpload = 1,

    /// <summary>
    /// Quick lookup by document code (e.g., scanned from a physical QR code).
    /// </summary>
    WebCode = 2,

    /// <summary>
    /// Automated machine-to-machine verification via API Key.
    /// </summary>
    Api = 3
}
