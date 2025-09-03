using System.Security.Cryptography;
using System.Text;

internal interface ITokenService
{
    Task<TokenEntity> TokenizeAsync(string cardNumber);
    Task<CardTokenEntity> DetokenizeAsync(string token);
}

internal class TokenService : ITokenService
{
    private readonly IFpeService _fpe;
    private readonly IDbService _db;

    public TokenService(IFpeService fpe, IDbService db)
    {
        _fpe = fpe;
        _db = db;
    }

    public async Task<TokenEntity> TokenizeAsync(string cardNumber)
    {
        var digits = new string([.. cardNumber.Where(char.IsDigit)]);
        if (digits.Length < 12 || digits.Length > 19) throw new ArgumentException("Card number length must be between 12 and 19 digits.", nameof(cardNumber));

        var hash = cardNumber.ComputeHash();
        var tokens = await _db.ExecuteQueryAsync<TokenEntity>("GetToken", new() { { "Hash", hash } });

        if (tokens?.Any() ?? false)
            return tokens.First();

        var cardId = await _db.ExecuteQueryAsync<long>("CreateCard", new() { { "CardNumber", cardNumber } });

        var bin = cardNumber[..6];
        var opaquetoken = GenerateToken();
        var fpeCardNUmber = _fpe.Encrypt(digits, opaquetoken);
        var cardID = cardId.First();

        tokens = await _db.ExecuteQueryAsync<TokenEntity>("CreateToken",
            new()
            {
                { "CardId", cardID},
                { "Bin", bin},
                { "Hash", hash },
                { "Token", opaquetoken },
                { "FpeCardNumber", fpeCardNUmber },
            }, true);

        return tokens.First();
    }

    public async Task<CardTokenEntity> DetokenizeAsync(string token)
    {
        var tokens = token.All(char.IsDigit)
                        ? await _db.ExecuteQueryAsync<CardTokenEntity>("GetToken", new() { { "FpeCardNumber", token } })
                        : await _db.ExecuteQueryAsync<CardTokenEntity>("GetToken", new() { { "Token", token } });



        if (tokens?.Any() ?? false)
        {
            var tokenEntity = tokens.First();
            var cardNumber = await _db.ExecuteQueryAsync<string>("GetCard", new() { { "ID", tokenEntity.CardID } });
            tokenEntity.CardNumber = cardNumber.First();
            return tokenEntity;
        }

        return null;
    }

    private string GenerateToken()
    {
        byte[] hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(Guid.NewGuid().ToString()));
        string hashHex = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
        return "T" + hashHex[..18];
    }

    public static bool IsValidLuhn(string number)
    {
        int sum = 0;
        bool alternate = false;

        for (int i = number.Length - 1; i >= 0; i--)
        {
            int n = int.Parse(number[i].ToString());
            if (alternate)
            {
                n *= 2;
                if (n > 9) n -= 9;
            }
            sum += n;
            alternate = !alternate;
        }

        return (sum % 10 == 0);
    }
}