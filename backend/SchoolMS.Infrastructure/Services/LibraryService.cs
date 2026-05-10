using Microsoft.EntityFrameworkCore;
using SchoolMS.Core.DTOs.Library;
using SchoolMS.Core.Entities;
using SchoolMS.Core.Interfaces;
using SchoolMS.Infrastructure.Data;

namespace SchoolMS.Infrastructure.Services;

public class LibraryService : ILibraryService
{
    private readonly AppDbContext _db;
    private const decimal FinePerDay = 0.50m;

    public LibraryService(AppDbContext db) { _db = db; }

    public async Task<BookResponseDto> AddBookAsync(CreateBookDto dto)
    {
        var exists = await _db.Books.AnyAsync(b => b.ISBN == dto.ISBN);
        if (exists) throw new InvalidOperationException($"Book with ISBN '{dto.ISBN}' already exists.");

        var book = new Book
        {
            ISBN = dto.ISBN.Trim(),
            Title = dto.Title.Trim(),
            Author = dto.Author.Trim(),
            Publisher = dto.Publisher,
            PublicationYear = dto.PublicationYear,
            Category = dto.Category.Trim(),
            TotalQuantity = dto.TotalQuantity,
            AvailableQuantity = dto.TotalQuantity,
            ShelfLocation = dto.ShelfLocation,
            Description = dto.Description
        };

        _db.Books.Add(book);
        await _db.SaveChangesAsync();
        return MapBookToDto(book);
    }

    public async Task<BookResponseDto> GetBookByIdAsync(Guid id)
    {
        var book = await _db.Books.FindAsync(id)
            ?? throw new KeyNotFoundException($"Book {id} not found.");
        return MapBookToDto(book);
    }

    public async Task<IEnumerable<BookResponseDto>> SearchBooksAsync(string? search, string? category)
    {
        var query = _db.Books.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.ToLower();
            query = query.Where(b =>
                b.Title.ToLower().Contains(s) ||
                b.Author.ToLower().Contains(s) ||
                b.ISBN.ToLower().Contains(s));
        }

        if (!string.IsNullOrWhiteSpace(category))
            query = query.Where(b => b.Category == category);

        var books = await query.OrderBy(b => b.Title).ToListAsync();
        return books.Select(MapBookToDto);
    }

    public async Task<BookResponseDto> UpdateBookAsync(Guid id, UpdateBookDto dto)
    {
        var book = await _db.Books.FindAsync(id)
            ?? throw new KeyNotFoundException($"Book {id} not found.");

        if (dto.Title != null) book.Title = dto.Title.Trim();
        if (dto.Author != null) book.Author = dto.Author.Trim();
        if (dto.Publisher != null) book.Publisher = dto.Publisher;
        if (dto.Category != null) book.Category = dto.Category;
        if (dto.Description != null) book.Description = dto.Description;
        if (dto.ShelfLocation != null) book.ShelfLocation = dto.ShelfLocation;
        if (dto.TotalQuantity.HasValue)
        {
            var diff = dto.TotalQuantity.Value - book.TotalQuantity;
            book.TotalQuantity = dto.TotalQuantity.Value;
            book.AvailableQuantity = Math.Max(0, book.AvailableQuantity + diff);
        }

        await _db.SaveChangesAsync();
        return MapBookToDto(book);
    }

    public async Task DeleteBookAsync(Guid id)
    {
        var book = await _db.Books
            .Include(b => b.Loans)
            .FirstOrDefaultAsync(b => b.Id == id)
            ?? throw new KeyNotFoundException($"Book {id} not found.");

        if (book.Loans.Any(l => l.Status == "Active"))
            throw new InvalidOperationException("Cannot delete a book with active loans.");

        book.IsDeleted = true;
        await _db.SaveChangesAsync();
    }

    public async Task<BookLoanResponseDto> IssueLoanAsync(IssueLoanDto dto, Guid issuedByStaffId)
    {
        var book = await _db.Books.FindAsync(dto.BookId)
            ?? throw new KeyNotFoundException($"Book {dto.BookId} not found.");

        if (book.AvailableQuantity <= 0)
            throw new InvalidOperationException("No copies available for this book.");

        // Check borrower doesn't already have this book
        var alreadyBorrowed = await _db.BookLoans.AnyAsync(l =>
            l.BookId == dto.BookId &&
            l.BorrowerId == dto.BorrowerId &&
            l.Status == "Active");

        if (alreadyBorrowed)
            throw new InvalidOperationException("This borrower already has a copy of this book.");

        var loan = new BookLoan
        {
            BookId = dto.BookId,
            BorrowerId = dto.BorrowerId,
            BorrowerType = dto.BorrowerType,
            IssuedById = issuedByStaffId,
            IssueDate = DateTime.UtcNow,
            DueDate = dto.DueDate,
            Status = "Active",
            Notes = dto.Notes
        };

        book.AvailableQuantity--;

        _db.BookLoans.Add(loan);
        await _db.SaveChangesAsync();
        return await MapLoanAsync(loan.Id);
    }

    public async Task<BookLoanResponseDto> ReturnLoanAsync(Guid loanId, ReturnLoanDto dto)
    {
        var loan = await _db.BookLoans
            .Include(l => l.Book)
            .FirstOrDefaultAsync(l => l.Id == loanId)
            ?? throw new KeyNotFoundException($"Loan {loanId} not found.");

        if (loan.Status != "Active")
            throw new InvalidOperationException("This loan is not active.");

        loan.ReturnDate = DateTime.UtcNow;
        loan.Status = "Returned";
        loan.Notes = dto.Notes ?? loan.Notes;

        // Calculate fine
        if (DateTime.UtcNow > loan.DueDate)
        {
            var daysOverdue = (int)(DateTime.UtcNow - loan.DueDate).TotalDays;
            loan.FineAmount = daysOverdue * FinePerDay;
        }

        loan.Book.AvailableQuantity++;
        await _db.SaveChangesAsync();
        return await MapLoanAsync(loanId);
    }

    public async Task<IEnumerable<BookLoanResponseDto>> GetActiveLoansByBorrowerAsync(Guid borrowerUserId)
    {
        var loans = await _db.BookLoans
            .Include(l => l.Book)
            .Include(l => l.Borrower)
            .Include(l => l.IssuedBy)
            .Where(l => l.BorrowerId == borrowerUserId && l.Status == "Active")
            .OrderByDescending(l => l.IssueDate)
            .ToListAsync();

        return loans.Select(MapLoanToDto);
    }

    public async Task<IEnumerable<BookLoanResponseDto>> GetAllActiveLoansAsync()
    {
        var loans = await _db.BookLoans
            .Include(l => l.Book)
            .Include(l => l.Borrower)
            .Include(l => l.IssuedBy)
            .Where(l => l.Status == "Active")
            .OrderBy(l => l.DueDate)
            .ToListAsync();

        return loans.Select(MapLoanToDto);
    }

    public async Task<IEnumerable<BookLoanResponseDto>> GetOverdueLoansAsync()
    {
        var loans = await _db.BookLoans
            .Include(l => l.Book)
            .Include(l => l.Borrower)
            .Include(l => l.IssuedBy)
            .Where(l => l.Status == "Active" && l.DueDate < DateTime.UtcNow)
            .OrderBy(l => l.DueDate)
            .ToListAsync();

        return loans.Select(MapLoanToDto);
    }

    public async Task<LibraryStatsDto> GetStatsAsync()
    {
        var books = await _db.Books.Include(b => b.Loans).ToListAsync();
        var activeLoans = books.SelectMany(b => b.Loans).Where(l => l.Status == "Active").ToList();
        var overdueLoans = activeLoans.Where(l => l.DueDate < DateTime.UtcNow).ToList();

        var mostBorrowed = books
            .OrderByDescending(b => b.Loans.Count)
            .Take(5)
            .Select(MapBookToDto)
            .ToList();

        return new LibraryStatsDto
        {
            TotalBooks = books.Count,
            TotalCopies = books.Sum(b => b.TotalQuantity),
            AvailableCopies = books.Sum(b => b.AvailableQuantity),
            ActiveLoans = activeLoans.Count,
            OverdueLoans = overdueLoans.Count,
            TotalFinesOutstanding = activeLoans.Sum(l => l.FineAmount),
            MostBorrowed = mostBorrowed
        };
    }

    private async Task<BookLoanResponseDto> MapLoanAsync(Guid id)
    {
        var l = await _db.BookLoans
            .Include(x => x.Book)
            .Include(x => x.Borrower)
            .Include(x => x.IssuedBy)
            .FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new KeyNotFoundException();
        return MapLoanToDto(l);
    }

    private static BookLoanResponseDto MapLoanToDto(BookLoan l) => new()
    {
        Id = l.Id,
        BookId = l.BookId,
        BookTitle = l.Book.Title,
        BookISBN = l.Book.ISBN,
        BookAuthor = l.Book.Author,
        BorrowerId = l.BorrowerId,
        BorrowerName = l.Borrower.Email,
        BorrowerType = l.BorrowerType,
        IssuedByName = $"{l.IssuedBy.FirstName} {l.IssuedBy.LastName}",
        IssueDate = l.IssueDate,
        DueDate = l.DueDate,
        ReturnDate = l.ReturnDate,
        Status = l.Status,
        FineAmount = l.FineAmount,
        FinePaid = l.FinePaid,
        Notes = l.Notes,
        CreatedAt = l.CreatedAt
    };

    private static BookResponseDto MapBookToDto(Book b) => new()
    {
        Id = b.Id,
        ISBN = b.ISBN,
        Title = b.Title,
        Author = b.Author,
        Publisher = b.Publisher,
        PublicationYear = b.PublicationYear,
        Category = b.Category,
        TotalQuantity = b.TotalQuantity,
        AvailableQuantity = b.AvailableQuantity,
        ShelfLocation = b.ShelfLocation,
        CoverUrl = b.CoverUrl,
        Description = b.Description,
        CreatedAt = b.CreatedAt
    };
}