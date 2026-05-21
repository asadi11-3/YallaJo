namespace Finance.Infrastructure.Persistence;

public sealed class InvoiceNumberCounter
{
    private InvoiceNumberCounter() { } // EF Core

    public string YearMonth { get; private set; } = string.Empty;
    public int Seq { get; private set; }
    public byte[] RowVersion { get; private set; } = [];

    public static InvoiceNumberCounter Create(string yearMonth)
    {
        if (string.IsNullOrWhiteSpace(yearMonth) || yearMonth.Length != 6)
        {
            throw new ArgumentException("YearMonth must use yyyyMM format.", nameof(yearMonth));
        }

        return new InvoiceNumberCounter
        {
            YearMonth = yearMonth,
            Seq = 1,
        };
    }

    public int Increment()
    {
        Seq++;
        return Seq;
    }
}
