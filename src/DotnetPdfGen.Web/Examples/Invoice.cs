namespace DotnetPdfGen.Web.Examples;

/// <summary>
/// A strongly typed model bound into <c>Templates/invoice/template.html</c>. Computed properties
/// such as <see cref="Total"/> are evaluated in C#, so templates stay free of business logic.
/// </summary>
public sealed record Invoice(
    string Number,
    DateOnly IssuedOn,
    DateOnly DueOn,
    string Currency,
    Party Seller,
    Party Customer,
    IReadOnlyList<InvoiceLine> Lines,
    decimal TaxRate,
    bool Paid,
    string? Notes = null)
{
    public decimal Subtotal => Lines.Sum(line => line.Amount);

    public decimal Tax => Math.Round(Subtotal * TaxRate, 2, MidpointRounding.AwayFromZero);

    public decimal Total => Subtotal + Tax;

    public static Invoice CreateSample(bool paid) => new(
        Number: "INV-2026-0107",
        IssuedOn: new DateOnly(2026, 10, 16),
        DueOn: new DateOnly(2026, 11, 15),
        Currency: "GBP",
        Seller: new Party("Superducke Ltd", "12 Quay Street, Bristol BS1 4DJ"),
        Customer: new Party("Charles Babbage", "Difference Engine Works, London", "charles@example.com"),
        Lines:
        [
            new InvoiceLine("Typed C# model binding", 1, 1800m),
            new InvoiceLine("Custom report templates", 3, 420m),
            new InvoiceLine("Rendering performance review", 4, 110m),
        ],
        TaxRate: 0.2m,
        Paid: paid,
        Notes: "Generated from a C# Invoice record. Totals are computed properties on the model.");
}

public sealed record Party(string Name, string Address, string? Email = null);

public sealed record InvoiceLine(string Description, int Quantity, decimal UnitPrice)
{
    public decimal Amount => Quantity * UnitPrice;
}
