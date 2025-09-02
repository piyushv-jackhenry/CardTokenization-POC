
internal class TokenEntity
{
    public long CardID { get; set; }
    private string Bin { get; set; }
    public string FpeCardNumber { get; set; }
    public string Token { get; set; }
    public DateTime CreatedUtc { get; set; }
}