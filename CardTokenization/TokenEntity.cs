
internal class TokenEntity
{
    public long ID { get; set; }
    public string CardNumber { get; set; }
    public string Token { get; set; }
    private string Bin { get; set; }
    public DateTime CreatedUtc { get;  set; }
}