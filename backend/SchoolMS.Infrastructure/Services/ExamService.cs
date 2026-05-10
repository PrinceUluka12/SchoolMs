using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SchoolMS.Core.DTOs.Exams;
using SchoolMS.Core.Entities;
using SchoolMS.Core.Interfaces;
using SchoolMS.Infrastructure.Data;

namespace SchoolMS.Infrastructure.Services;

public class ExamService : IExamService
{
    private readonly AppDbContext _db;

    public ExamService(AppDbContext db) { _db = db; }

    // ── Exams ─────────────────────────────────────────────────────────────────
    public async Task<ExamResponseDto> CreateExamAsync(CreateExamDto dto)
    {
        var exam = new Exam
        {
            Name = dto.Name.Trim(),
            Type = dto.Type,
            TermId = dto.TermId,
            AcademicYearId = dto.AcademicYearId,
            Status = "Scheduled",
            Description = dto.Description
        };

        _db.Exams.Add(exam);
        await _db.SaveChangesAsync();
        return await MapExamAsync(exam.Id);
    }

    public async Task<ExamResponseDto> GetExamByIdAsync(Guid id)
    {
        var exists = await _db.Exams.AnyAsync(e => e.Id == id);
        if (!exists) throw new KeyNotFoundException($"Exam {id} not found.");
        return await MapExamAsync(id);
    }

    public async Task<IEnumerable<ExamResponseDto>> GetExamsByTermAsync(Guid termId)
    {
        var exams = await _db.Exams
            .Include(e => e.Term)
            .Include(e => e.AcademicYear)
            .Include(e => e.Schedules)
            .Where(e => e.TermId == termId)
            .OrderBy(e => e.Name)
            .ToListAsync();

        return exams.Select(MapExamToDto);
    }

    public async Task<ExamResponseDto> UpdateExamAsync(Guid id, UpdateExamDto dto)
    {
        var exam = await _db.Exams.FindAsync(id)
            ?? throw new KeyNotFoundException($"Exam {id} not found.");

        if (dto.Name != null) exam.Name = dto.Name.Trim();
        if (dto.Type != null) exam.Type = dto.Type;
        if (dto.Status != null) exam.Status = dto.Status;
        if (dto.Description != null) exam.Description = dto.Description;

        await _db.SaveChangesAsync();
        return await MapExamAsync(id);
    }

    public async Task DeleteExamAsync(Guid id)
    {
        var exam = await _db.Exams.FindAsync(id)
            ?? throw new KeyNotFoundException($"Exam {id} not found.");

        if (exam.Status == "Ongoing")
            throw new InvalidOperationException("Cannot delete an ongoing exam.");

        exam.IsDeleted = true;
        await _db.SaveChangesAsync();
    }

    // ── Schedules ─────────────────────────────────────────────────────────────
    public async Task<ExamScheduleResponseDto> AddScheduleAsync(CreateExamScheduleDto dto)
    {
        // Check for conflict: same class, same date, overlapping time
        var conflict = await _db.ExamSchedules
            .AnyAsync(es =>
                es.ClassId == dto.ClassId &&
                es.ExamDate.Date == dto.ExamDate.Date &&
                es.ExamId != dto.ExamId);

        if (conflict)
            throw new InvalidOperationException("A schedule already exists for this class on this date.");

        var schedule = new ExamSchedule
        {
            ExamId = dto.ExamId,
            SubjectId = dto.SubjectId,
            ClassId = dto.ClassId,
            ExamDate = dto.ExamDate,
            StartTime = TimeSpan.Parse(dto.StartTime),
            EndTime = TimeSpan.Parse(dto.EndTime),
            Venue = dto.Venue,
            TotalSeats = dto.TotalSeats
        };

        _db.ExamSchedules.Add(schedule);
        await _db.SaveChangesAsync();
        return await MapScheduleAsync(schedule.Id);
    }

    public async Task DeleteScheduleAsync(Guid scheduleId)
    {
        var schedule = await _db.ExamSchedules.FindAsync(scheduleId)
            ?? throw new KeyNotFoundException($"Schedule {scheduleId} not found.");

        _db.ExamSchedules.Remove(schedule);
        await _db.SaveChangesAsync();
    }

    public async Task<IEnumerable<ExamScheduleResponseDto>> GetSchedulesByExamAsync(Guid examId)
    {
        var schedules = await _db.ExamSchedules
            .Include(es => es.Exam)
            .Include(es => es.Subject)
            .Include(es => es.Class)
            .Include(es => es.SeatingArrangements)
            .Where(es => es.ExamId == examId)
            .OrderBy(es => es.ExamDate).ThenBy(es => es.StartTime)
            .ToListAsync();

        return schedules.Select(MapScheduleToDto);
    }

    // ── Seating ───────────────────────────────────────────────────────────────
    public async Task<List<SeatingArrangementResponseDto>> GenerateSeatingAsync(Guid examScheduleId)
    {
        var schedule = await _db.ExamSchedules
            .Include(es => es.Class).ThenInclude(c => c.Students)
            .Include(es => es.Subject)
            .Include(es => es.SeatingArrangements)
            .FirstOrDefaultAsync(es => es.Id == examScheduleId)
            ?? throw new KeyNotFoundException($"Exam schedule {examScheduleId} not found.");

        // Remove existing seating
        if (schedule.SeatingArrangements.Any())
            _db.ExamSeatingArrangements.RemoveRange(schedule.SeatingArrangements);

        var students = schedule.Class.Students
            .Where(s => s.Status == "Active")
            .OrderBy(s => s.LastName)
            .ToList();

        var arrangements = new List<ExamSeatingArrangement>();
        int seatNumber = 1;

        foreach (var student in students)
        {
            arrangements.Add(new ExamSeatingArrangement
            {
                ExamScheduleId = examScheduleId,
                StudentId = student.Id,
                SeatNumber = $"{schedule.Venue?.Replace(" ", "").ToUpper() ?? "HALL"}-{seatNumber:D3}"
            });
            seatNumber++;
        }

        _db.ExamSeatingArrangements.AddRange(arrangements);
        await _db.SaveChangesAsync();

        return await GetSeatingDtoAsync(examScheduleId, schedule);
    }

    public async Task<IEnumerable<SeatingArrangementResponseDto>> GetSeatingAsync(Guid examScheduleId)
    {
        var schedule = await _db.ExamSchedules
            .Include(es => es.Subject)
            .Include(es => es.Class)
            .FirstOrDefaultAsync(es => es.Id == examScheduleId)
            ?? throw new KeyNotFoundException($"Exam schedule {examScheduleId} not found.");

        return await GetSeatingDtoAsync(examScheduleId, schedule);
    }

    // ── Admit Card PDF ────────────────────────────────────────────────────────
    public async Task<byte[]> GenerateAdmitCardPdfAsync(Guid studentId, Guid examId)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        var student = await _db.Students
            .Include(s => s.Class)
            .FirstOrDefaultAsync(s => s.Id == studentId)
            ?? throw new KeyNotFoundException($"Student {studentId} not found.");

        var exam = await _db.Exams
            .Include(e => e.Term)
            .Include(e => e.Schedules).ThenInclude(es => es.Subject)
            .Include(e => e.Schedules).ThenInclude(es => es.SeatingArrangements
                .Where(sa => sa.StudentId == studentId))
            .FirstOrDefaultAsync(e => e.Id == examId)
            ?? throw new KeyNotFoundException($"Exam {examId} not found.");

        var studentSchedules = exam.Schedules
            .Where(es => es.ClassId == student.ClassId)
            .OrderBy(es => es.ExamDate)
            .ToList();

        var doc = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A5);
                page.Margin(25);
                page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial"));

                page.Content().Column(col =>
                {
                    // Header
                    col.Item().Border(2).BorderColor("#1F3864").Padding(0).Column(header =>
                    {
                        header.Item().Background("#1F3864").Padding(12).Column(h =>
                        {
                            h.Item().Text("SCHOOL MANAGEMENT SYSTEM")
                                .Bold().FontSize(14).FontColor("#FFFFFF").AlignCenter();
                            h.Item().Text("EXAMINATION ADMIT CARD")
                                .FontSize(11).FontColor("#BDD7EE").AlignCenter();
                        });

                        header.Item().Padding(12).Column(body =>
                        {
                            body.Item().Table(table =>
                            {
                                table.ColumnsDefinition(cols =>
                                {
                                    cols.RelativeColumn();
                                    cols.RelativeColumn();
                                });

                                void InfoRow(string label, string value)
                                {
                                    table.Cell().Padding(3).Text(label).FontSize(9).FontColor("#666666");
                                    table.Cell().Padding(3).Text(value).Bold().FontSize(10);
                                }

                                InfoRow("Student Name:", student.StudentNumber != null
                                    ? $"{student.FirstName} {student.LastName}" : "N/A");
                                InfoRow("Student Number:", student.StudentNumber);
                                InfoRow("Class:", student.Class?.Name ?? "—");
                                InfoRow("Exam:", exam.Name);
                                InfoRow("Term:", exam.Term.Name);
                            });
                        });
                    });

                    col.Item().Height(10);

                    // Schedule Table
                    col.Item().Text("Examination Schedule").Bold().FontSize(11).FontColor("#1F3864");
                    col.Item().Height(4);

                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(cols =>
                        {
                            cols.RelativeColumn(2);
                            cols.RelativeColumn(2);
                            cols.RelativeColumn(1);
                            cols.RelativeColumn(1);
                            cols.RelativeColumn(1);
                        });

                        static IContainer HeaderCell(IContainer c) =>
                            c.Background("#1F3864").Padding(5);

                        table.Header(header =>
                        {
                            header.Cell().Element(HeaderCell).Text("Subject").Bold().FontColor("#FFFFFF");
                            header.Cell().Element(HeaderCell).Text("Date").Bold().FontColor("#FFFFFF");
                            header.Cell().Element(HeaderCell).Text("Start").Bold().FontColor("#FFFFFF");
                            header.Cell().Element(HeaderCell).Text("End").Bold().FontColor("#FFFFFF");
                            header.Cell().Element(HeaderCell).Text("Seat").Bold().FontColor("#FFFFFF");
                        });

                        bool alt = false;
                        foreach (var sched in studentSchedules)
                        {
                            var bg = alt ? "#F2F2F2" : "#FFFFFF";
                            alt = !alt;
                            var seat = sched.SeatingArrangements.FirstOrDefault()?.SeatNumber ?? "TBA";

                            static IContainer DataCell(IContainer c, string bg) =>
                                c.Background(bg).Padding(5);

                            table.Cell().Element(c => DataCell(c, bg)).Text(sched.Subject.Name);
                            table.Cell().Element(c => DataCell(c, bg)).Text(sched.ExamDate.ToString("dd/MM/yyyy"));
                            table.Cell().Element(c => DataCell(c, bg)).Text(sched.StartTime.ToString(@"hh\:mm"));
                            table.Cell().Element(c => DataCell(c, bg)).Text(sched.EndTime.ToString(@"hh\:mm"));
                            table.Cell().Element(c => DataCell(c, bg)).Text(seat).Bold().FontColor("#1F3864");
                        }
                    });

                    col.Item().Height(12);

                    // Rules
                    col.Item().Background("#FEF3C7").Padding(8).Column(rules =>
                    {
                        rules.Item().Text("Rules & Regulations").Bold().FontSize(9).FontColor("#92400E");
                        rules.Item().Text("1. Arrive 15 minutes before the exam starts.").FontSize(8);
                        rules.Item().Text("2. Carry this admit card to every examination.").FontSize(8);
                        rules.Item().Text("3. Electronic devices are not permitted in the exam hall.").FontSize(8);
                        rules.Item().Text("4. Present your student ID along with this admit card.").FontSize(8);
                    });

                    col.Item().Height(16);

                    // Signature
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().LineHorizontal(1).LineColor("#CCCCCC");
                            c.Item().Text("Student Signature").FontSize(8).FontColor("#999999");
                        });
                        row.ConstantItem(40);
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().LineHorizontal(1).LineColor("#CCCCCC");
                            c.Item().Text("Principal's Signature").FontSize(8).FontColor("#999999");
                        });
                    });
                });
            });
        });

        return doc.GeneratePdf();
    }

    // ── Seating PDF ───────────────────────────────────────────────────────────
    public async Task<byte[]> GenerateSeatingPdfAsync(Guid examScheduleId)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        var schedule = await _db.ExamSchedules
            .Include(es => es.Exam).ThenInclude(e => e.Term)
            .Include(es => es.Subject)
            .Include(es => es.Class)
            .Include(es => es.SeatingArrangements).ThenInclude(sa => sa.Student)
            .FirstOrDefaultAsync(es => es.Id == examScheduleId)
            ?? throw new KeyNotFoundException($"Schedule {examScheduleId} not found.");

        var sorted = schedule.SeatingArrangements
            .OrderBy(sa => sa.SeatNumber)
            .ToList();

        var doc = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(30);
                page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial"));

                page.Header().Column(col =>
                {
                    col.Item().Text("SCHOOL MANAGEMENT SYSTEM — SEATING ARRANGEMENT")
                        .Bold().FontSize(14).FontColor("#1F3864").AlignCenter();
                    col.Item().Text(
                        $"{schedule.Exam.Name} · {schedule.Subject.Name} · {schedule.Class.Name}")
                        .FontSize(11).AlignCenter().FontColor("#2E75B6");
                    col.Item().Text(
                        $"{schedule.ExamDate:dd MMMM yyyy} · {schedule.StartTime:hh\\:mm}–{schedule.EndTime:hh\\:mm} · Venue: {schedule.Venue ?? "TBD"}")
                        .FontSize(10).AlignCenter().FontColor("#666666");
                    col.Item().LineHorizontal(1).LineColor("#2E75B6");
                    col.Item().Height(8);
                });

                page.Content().Table(table =>
                {
                    table.ColumnsDefinition(cols =>
                    {
                        cols.ConstantColumn(40);
                        cols.RelativeColumn(1);
                        cols.RelativeColumn(3);
                        cols.RelativeColumn(3);
                        cols.RelativeColumn(2);
                    });

                    static IContainer HCell(IContainer c) => c.Background("#1F3864").Padding(6);

                    table.Header(header =>
                    {
                        header.Cell().Element(HCell).Text("#").Bold().FontColor("#FFFFFF");
                        header.Cell().Element(HCell).Text("Seat No.").Bold().FontColor("#FFFFFF");
                        header.Cell().Element(HCell).Text("Student Name").Bold().FontColor("#FFFFFF");
                        header.Cell().Element(HCell).Text("Student Number").Bold().FontColor("#FFFFFF");
                        header.Cell().Element(HCell).Text("Signature").Bold().FontColor("#FFFFFF");
                    });

                    bool alt = false;
                    int idx = 1;
                    foreach (var sa in sorted)
                    {
                        var bg = alt ? "#F2F2F2" : "#FFFFFF";
                        alt = !alt;
                        static IContainer DCell(IContainer c, string bg) => c.Background(bg).Padding(6);

                        table.Cell().Element(c => DCell(c, bg)).Text(idx++.ToString());
                        table.Cell().Element(c => DCell(c, bg)).Text(sa.SeatNumber).Bold().FontColor("#1F3864");
                        table.Cell().Element(c => DCell(c, bg)).Text(
                            $"{sa.Student.FirstName} {sa.Student.LastName}");
                        table.Cell().Element(c => DCell(c, bg)).Text(sa.Student.StudentNumber).FontColor("#666666");
                        table.Cell().Element(c => DCell(c, bg)).Text("");
                    }
                });

                page.Footer().AlignCenter().Text(x =>
                {
                    x.Span("Generated by SchoolMS · ").FontSize(8).FontColor("#999999");
                    x.Span(DateTime.UtcNow.ToString("dd MMM yyyy HH:mm")).FontSize(8).FontColor("#999999");
                });
            });
        });

        return doc.GeneratePdf();
    }

    // ── Private Helpers ───────────────────────────────────────────────────────
    private async Task<List<SeatingArrangementResponseDto>> GetSeatingDtoAsync(
        Guid scheduleId, ExamSchedule schedule)
    {
        var arrangements = await _db.ExamSeatingArrangements
            .Include(sa => sa.Student)
            .Include(sa => sa.ExamSchedule).ThenInclude(es => es.Subject)
            .Include(sa => sa.ExamSchedule).ThenInclude(es => es.Class)
            .Where(sa => sa.ExamScheduleId == scheduleId)
            .OrderBy(sa => sa.SeatNumber)
            .ToListAsync();

        return arrangements.Select(sa => new SeatingArrangementResponseDto
        {
            Id = sa.Id,
            StudentId = sa.StudentId,
            StudentName = $"{sa.Student.FirstName} {sa.Student.LastName}",
            StudentNumber = sa.Student.StudentNumber,
            SeatNumber = sa.SeatNumber,
            SubjectName = sa.ExamSchedule.Subject.Name,
            ClassName = sa.ExamSchedule.Class.Name,
            ExamDate = sa.ExamSchedule.ExamDate,
            Venue = sa.ExamSchedule.Venue ?? "TBD"
        }).ToList();
    }

    private async Task<ExamResponseDto> MapExamAsync(Guid id)
    {
        var e = await _db.Exams
            .Include(x => x.Term)
            .Include(x => x.AcademicYear)
            .Include(x => x.Schedules).ThenInclude(es => es.Subject)
            .Include(x => x.Schedules).ThenInclude(es => es.Class)
            .Include(x => x.Schedules).ThenInclude(es => es.SeatingArrangements)
            .FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new KeyNotFoundException();
        return MapExamToDto(e);
    }

    private static ExamResponseDto MapExamToDto(Exam e) => new()
    {
        Id = e.Id,
        Name = e.Name,
        Type = e.Type,
        Status = e.Status,
        Description = e.Description,
        TermId = e.TermId,
        TermName = e.Term?.Name ?? "",
        AcademicYearId = e.AcademicYearId,
        AcademicYearName = e.AcademicYear?.Name ?? "",
        ScheduleCount = e.Schedules.Count,
        Schedules = e.Schedules.Select(MapScheduleToDto).ToList(),
        CreatedAt = e.CreatedAt
    };

    private async Task<ExamScheduleResponseDto> MapScheduleAsync(Guid id)
    {
        var es = await _db.ExamSchedules
            .Include(x => x.Exam)
            .Include(x => x.Subject)
            .Include(x => x.Class)
            .Include(x => x.SeatingArrangements)
            .FirstOrDefaultAsync(x => x.Id == id)
            ?? throw new KeyNotFoundException();
        return MapScheduleToDto(es);
    }

    private static ExamScheduleResponseDto MapScheduleToDto(ExamSchedule es) => new()
    {
        Id = es.Id,
        ExamId = es.ExamId,
        ExamName = es.Exam?.Name ?? "",
        SubjectId = es.SubjectId,
        SubjectName = es.Subject?.Name ?? "",
        SubjectCode = es.Subject?.Code ?? "",
        ClassId = es.ClassId,
        ClassName = es.Class?.Name ?? "",
        ExamDate = es.ExamDate,
        StartTime = es.StartTime.ToString(@"hh\:mm"),
        EndTime = es.EndTime.ToString(@"hh\:mm"),
        Venue = es.Venue,
        TotalSeats = es.TotalSeats,
        SeatedStudents = es.SeatingArrangements.Count,
        CreatedAt = es.CreatedAt
    };
}