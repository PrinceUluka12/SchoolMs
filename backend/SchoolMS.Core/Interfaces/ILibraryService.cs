using SchoolMS.Core.DTOs.Library;

namespace SchoolMS.Core.Interfaces;

public interface ILibraryService
{
    Task<BookResponseDto> AddBookAsync(CreateBookDto dto);
    Task<BookResponseDto> GetBookByIdAsync(Guid id);
    Task<IEnumerable<BookResponseDto>> SearchBooksAsync(string? search, string? category);
    Task<BookResponseDto> UpdateBookAsync(Guid id, UpdateBookDto dto);
    Task DeleteBookAsync(Guid id);
    Task<BookLoanResponseDto> IssueLoanAsync(IssueLoanDto dto, Guid issuedByStaffId);
    Task<BookLoanResponseDto> ReturnLoanAsync(Guid loanId, ReturnLoanDto dto);
    Task<IEnumerable<BookLoanResponseDto>> GetActiveLoansByBorrowerAsync(Guid borrowerUserId);
    Task<IEnumerable<BookLoanResponseDto>> GetAllActiveLoansAsync();
    Task<IEnumerable<BookLoanResponseDto>> GetOverdueLoansAsync();
    Task<LibraryStatsDto> GetStatsAsync();
}