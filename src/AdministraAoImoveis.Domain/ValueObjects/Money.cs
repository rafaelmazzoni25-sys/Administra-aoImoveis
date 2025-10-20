namespace AdministraAoImoveis.Domain.ValueObjects;

public readonly record struct Money(decimal Amount, string Currency)
{
    public static Money Zero(string currency = "BRL") => new(0m, currency);

    public override string ToString() => $"{Amount:0.00} {Currency}";
}
