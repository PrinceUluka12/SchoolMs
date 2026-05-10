namespace SchoolMS.Core.Entities;

public class Book : BaseEntity
{
    public string ISBN { get; set; } = null!;
    public string Title { get; set; } = null!;
    public string Author { get; set; } = null!;
    public string? Publisher { get; set; }
    public int? PublicationYear { get; set; }
    public string Category { get; set; } = null!;
    public int TotalQuantity { get; set; }
    public int AvailableQuantity { get; set; }
    public string? ShelfLocation { get; set; }
    public string? CoverUrl { get; set; }
    public string? Description { get; set; }
    public ICollection<BookLoan> Loans { get; set; } = new List<BookLoan>();
}