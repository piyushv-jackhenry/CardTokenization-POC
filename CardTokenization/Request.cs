internal class Request
{
    private List<string> _cardNumbers;
    public List<string> CardNumbers
    {
        get => _cardNumbers;
        set
        {
            _cardNumbers = [];
            if (value != null && value.Count > 0)
                _cardNumbers.AddRange(value.Where(x => !string.IsNullOrEmpty(x)).Distinct());
        }
    }

    private List<string> _tokens;
    public List<string> Tokens
    {
        get => _tokens;
        set
        {
            _tokens = [];
            if (value != null && value.Count > 0)
                _tokens.AddRange(value.Where(x => !string.IsNullOrEmpty(x)).Distinct());
        }
    }

    public bool CreateIfNotFound { get; set; } = true;
}