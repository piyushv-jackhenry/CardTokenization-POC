internal interface ITokenizationService
{
    TokenEntity TokenizeAsync(string pan);
    TokenEntity DetokenizeAsync(string token);
}

internal class TokenizationService : ITokenizationService
{
    private readonly IFpeService _fpe;
    public TokenizationService(IFpeService fpe)
    {
        _fpe = fpe;
    }

    public TokenEntity TokenizeAsync(string pan)
    {
        var digits = new string([.. pan.Where(char.IsDigit)]);
        if (digits.Length < 12 || digits.Length > 19) throw new ArgumentException("PAN length must be between 12 and 19 digits.", nameof(pan));

        var opaqueToken = Guid.NewGuid().ToString("N");
        var fpeToken = _fpe.Encrypt(digits, opaqueToken);

        return new TokenEntity
        {
            FpeToken = fpeToken,
            OpaqueToken = opaqueToken,
            CreatedUtc = DateTime.UtcNow
        };
    }

    public TokenEntity DetokenizeAsync(string token)
    {
        TokenEntity entity = null;
        if (token.All(char.IsDigit))
        {
            entity = null; // lookup by FPE token
        }
        else
        {
            entity = null; // lookup by opaque token
        }

        if (entity == null)
            return null;

        var pan = _fpe.Decrypt(entity.Pan, "");

        return new TokenEntity
        {
            Pan = pan,
            FpeToken = entity.FpeToken,
            OpaqueToken = entity.OpaqueToken,
            CreatedUtc = entity.CreatedUtc
        };
    }
}