namespace QuickSign.Application.Contracts.DTOs.Certificates;

public sealed class GeneratedKeyMaterialDto
{
    public string SerialNumber { get; set; } = default!;
    public string PublicKeyPem { get; set; } = default!;
    public string VaultKeyPath { get; set; } = default!;
}
