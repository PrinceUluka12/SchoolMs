namespace SchoolMS.Core.DTOs.Library;

// ── Book ──────────────────────────────────────────────────────────────────────
public class CreateBookDto
{
    public string ISBN { get; set; } = null!;
    public string Title { get; set; } = null!;
    public string Author { get; set; } = null!;
    public string? Publisher { get; set; }
    public int? PublicationYear { get; set; }
    public string Category { get; set; } = null!;
    public int TotalQuantity { get; set; }
    public string? ShelfLocation { get; set; }
    public string? Description { get; set; }
}

public class UpdateBookDto
{
    public string? Title { get; set; }
    public string? Author { get; set; }
    public string? Publisher { get; set; }
    public string? Category { get; set; }
    public int? TotalQuantity { get; set; }
    public string? ShelfLocation { get; set; }
    public string? Description { get; set; }
}

public class BookResponseDto
{
    public Guid Id { get; set; }
    public string ISBN { get; set; } = null!;
    public string Title { get; set; } = null!;
    public string Author { get; set; } = null!;
    public string? Publisher { get; set; }
    public int? PublicationYear { get; set; }
    public string Category { get; set; } = null!;
    public int TotalQuantity { get; set; }
    public int AvailableQuantity { get; set; }
    public int CheckedOutQuantity => TotalQuantity - AvailableQuantity;
    public string? ShelfLocation { get; set; }
    public string? CoverUrl { get; set; }
    public string? Description { get; set; }
    public bool IsAvailable => AvailableQuantity > 0;
    public DateTime CreatedAt { get; set; }
}

// ── Book Loan ─────────────────────────────────────────────────────────────────
public class IssueLoanDto
{
    public Guid BookId { get; set; }
    public Guid BorrowerId { get; set; }   // UserId
    public string BorrowerType { get; set; } = "Student";
    public DateTime DueDate { get; set; }
    public string? Notes { get; set; }
}

public class ReturnLoanDto
{
    public string? Notes { get; set; }
}

public class BookLoanResponseDto
{
    public Guid Id { get; set; }
    public Guid BookId { get; set; }
    public string BookTitle { get; set; } = null!;
    public string BookISBN { get; set; } = null!;
    public string BookAuthor { get; set; } = null!;
    public Guid BorrowerId { get; set; }
    public string BorrowerName { get; set; } = null!;
    public string BorrowerType { get; set; } = null!;
    public string IssuedByName { get; set; } = null!;
    public DateTime IssueDate { get; set; }
    public DateTime DueDate { get; set; }
    public DateTime? ReturnDate { get; set; }
    public string Status { get; set; } = null!;
    public decimal FineAmount { get; set; }
    public bool FinePaid { get; set; }
    public bool IsOverdue => ReturnDate == null && DueDate < DateTime.UtcNow;
    public int DaysOverdue => ReturnDate == null && DueDate < DateTime.UtcNow
        ? (int)(DateTime.UtcNow - DueDate).TotalDays : 0;
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class LibraryStatsDto
{
    public int TotalBooks { get; set; }
    public int TotalCopies { get; set; }
    public int AvailableCopies { get; set; }
    public int ActiveLoans { get; set; }
    public int OverdueLoans { get; set; }
    public decimal TotalFinesOutstanding { get; set; }
    public List<BookResponseDto> MostBorrowed { get; set; } = new();
}