namespace QuickSign.Application.Contracts.Infrastructure;

public interface IApiKeyCryptoService
{
    (string PlaintextKey, string Prefix, string Hash) GenerateApiKey();
    string ComputeHash(string plaintextKey);
}
