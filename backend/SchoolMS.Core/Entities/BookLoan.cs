namespace SchoolMS.Core.Entities;

public class BookLoan : BaseEntity
{
    public Guid BookId { get; set; }
    public Guid BorrowerId { get; set; }     // UserId
    public string BorrowerType { get; set; } = null!; // Student, Staff
    public Guid IssuedById { get; set; }     // StaffId
    public DateTime IssueDate { get; set; }
    public DateTime DueDate { get; set; }
    public DateTime? ReturnDate { get; set; }
    public string Status { get; set; } = "Active"; // Active, Returned, Overdue
    public decimal FineAmount { get; set; } = 0;
    public bool FinePaid { get; set; } = false;
    public string? Notes { get; set; }
    public Book Book { get; set; } = null!;
    public User Borrower { get; set; } = null!;
    public Staff IssuedBy { get; set; } = null!;
}