
internal class TokenEntity
{
    public long CardID { get; set; }
    public string Bin { get; set; }
    public string FpeCardNumber { get; set; }
    public string Token { get; set; }
    public DateTime CreatedUtc { get; set; }
}

internal class CardTokenEntity : TokenEntity
{
    public string CardNumber { get; set; }
}