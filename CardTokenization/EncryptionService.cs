using System.Security.Cryptography;
using System.Text;

internal interface IEncryptionService
{
    TokenEntity EncryptAsync(string cardNumber);
    TokenEntity DecryptAsync(string token);
}

internal class EncryptionService : IEncryptionService
{
    public TokenEntity EncryptAsync(string cardNumber)
    {
        throw new NotImplementedException();
    }

    public TokenEntity DecryptAsync(string token)
    {
        throw new NotImplementedException();
    }


}