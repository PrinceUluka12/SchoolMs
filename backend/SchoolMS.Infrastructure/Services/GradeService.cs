using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SchoolMS.Core.DTOs.Grades;
using SchoolMS.Core.Entities;
using SchoolMS.Core.Interfaces;
using SchoolMS.Infrastructure.Data;

namespace SchoolMS.Infrastructure.Services;

public class GradeService : IGradeService
{
    private readonly AppDbContext _db;

    public GradeService(AppDbContext db)
    {
        _db = db;
    }

    // ── Enter Single Grade ────────────────────────────────────────────────────
    public async Task<GradeResponseDto> EnterGradeAsync(EnterGradeDto dto, Guid enteredById)
    {
        await ValidateNotLocked(dto.ClassId, dto.TermId);

        var existing = await _db.Grades.FirstOrDefaultAsync(g =>
            g.StudentId == dto.StudentId &&
            g.SubjectId == dto.SubjectId &&
            g.TermId == dto.TermId &&
            g.AssessmentType == dto.AssessmentType);

        if (existing != null)
        {
            existing.Score = dto.Score;
            existing.MaxScore = dto.MaxScore;
            existing.TeacherRemark = dto.TeacherRemark;
        }
        else
        {
            var grade = new Grade
            {
                StudentId = dto.StudentId,
                SubjectId = dto.SubjectId,
                TermId = dto.TermId,
                ClassId = dto.ClassId,
                EnteredById = enteredById,
                AssessmentType = dto.AssessmentType,
                Score = dto.Score,
                MaxScore = dto.MaxScore,
                TeacherRemark = dto.TeacherRemark,
                IsLocked = false
            };
            _db.Grades.Add(grade);
        }

        await _db.SaveChangesAsync();

        var saved = await _db.Grades
            .Include(g => g.Student)
            .Include(g => g.Subject)
            .Include(g => g.Term)
            .Include(g => g.EnteredBy)
            .FirstOrDefaultAsync(g =>
                g.StudentId == dto.StudentId &&
                g.SubjectId == dto.SubjectId &&
                g.TermId == dto.TermId &&
                g.AssessmentType == dto.AssessmentType);

        return MapToDto(saved!);
    }

    // ── Bulk Enter Grades ─────────────────────────────────────────────────────
    public async Task<List<GradeResponseDto>> BulkEnterGradesAsync(BulkEnterGradesDto dto, Guid enteredById)
    {
        await ValidateNotLocked(dto.ClassId, dto.TermId);
        var results = new List<GradeResponseDto>();

        foreach (var entry in dto.Entries)
        {
            var result = await EnterGradeAsync(new EnterGradeDto
            {
                StudentId = entry.StudentId,
                SubjectId = dto.SubjectId,
                TermId = dto.TermId,
                ClassId = dto.ClassId,
                AssessmentType = dto.AssessmentType,
                Score = entry.Score,
                MaxScore = dto.MaxScore,
                TeacherRemark = entry.TeacherRemark
            }, enteredById);
            results.Add(result);
        }

        return results;
    }

    // ── Update Grade ──────────────────────────────────────────────────────────
    public async Task<GradeResponseDto> UpdateGradeAsync(Guid gradeId, decimal score, string? remark, Guid updatedById)
    {
        var grade = await _db.Grades
            .Include(g => g.Student)
            .Include(g => g.Subject)
            .Include(g => g.Term)
            .Include(g => g.EnteredBy)
            .FirstOrDefaultAsync(g => g.Id == gradeId)
            ?? throw new KeyNotFoundException($"Grade {gradeId} not found.");

        if (grade.IsLocked)
            throw new InvalidOperationException("Cannot update a locked grade.");

        grade.Score = score;
        grade.TeacherRemark = remark;
        await _db.SaveChangesAsync();
        return MapToDto(grade);
    }

    // ── Delete Grade ──────────────────────────────────────────────────────────
    public async Task DeleteGradeAsync(Guid gradeId)
    {
        var grade = await _db.Grades.FindAsync(gradeId)
            ?? throw new KeyNotFoundException($"Grade {gradeId} not found.");

        if (grade.IsLocked)
            throw new InvalidOperationException("Cannot delete a locked grade.");

        _db.Grades.Remove(grade);
        await _db.SaveChangesAsync();
    }

    // ── Lock / Unlock ─────────────────────────────────────────────────────────
    public async Task LockGradesAsync(Guid classId, Guid termId)
    {
        var grades = await _db.Grades
            .Where(g => g.ClassId == classId && g.TermId == termId)
            .ToListAsync();
        grades.ForEach(g => g.IsLocked = true);
        await _db.SaveChangesAsync();
    }

    public async Task UnlockGradesAsync(Guid classId, Guid termId)
    {
        var grades = await _db.Grades
            .Where(g => g.ClassId == classId && g.TermId == termId)
            .ToListAsync();
        grades.ForEach(g => g.IsLocked = false);
        await _db.SaveChangesAsync();
    }

    // ── Class Grade Sheet ─────────────────────────────────────────────────────
    public async Task<ClassGradeSheetDto> GetClassGradeSheetAsync(
        Guid classId, Guid termId, Guid subjectId, string assessmentType)
    {
        var cls = await _db.Classes.FindAsync(classId)
            ?? throw new KeyNotFoundException($"Class {classId} not found.");
        var subject = await _db.Subjects.FindAsync(subjectId)
            ?? throw new KeyNotFoundException($"Subject {subjectId} not found.");
        var term = await _db.Terms.FindAsync(termId)
            ?? throw new KeyNotFoundException($"Term {termId} not found.");

        var grades = await _db.Grades
            .Include(g => g.Student)
            .Include(g => g.Subject)
            .Include(g => g.Term)
            .Include(g => g.EnteredBy)
            .Where(g =>
                g.ClassId == classId &&
                g.TermId == termId &&
                g.SubjectId == subjectId &&
                g.AssessmentType == assessmentType)
            .OrderBy(g => g.Student.LastName)
            .ToListAsync();

        return new ClassGradeSheetDto
        {
            ClassName = cls.Name,
            SubjectName = subject.Name,
            TermName = term.Name,
            AssessmentType = assessmentType,
            Grades = grades.Select(MapToDto).ToList()
        };
    }

    // ── Student Term Report ───────────────────────────────────────────────────
    public async Task<StudentTermReportDto> GetStudentTermReportAsync(Guid studentId, Guid termId)
    {
        var student = await _db.Students
            .Include(s => s.Class).ThenInclude(c => c!.AcademicYear)
            .FirstOrDefaultAsync(s => s.Id == studentId)
            ?? throw new KeyNotFoundException($"Student {studentId} not found.");

        var term = await _db.Terms
            .Include(t => t.AcademicYear)
            .FirstOrDefaultAsync(t => t.Id == termId)
            ?? throw new KeyNotFoundException($"Term {termId} not found.");

        var grades = await _db.Grades
            .Include(g => g.Subject)
            .Where(g => g.StudentId == studentId && g.TermId == termId)
            .ToListAsync();

        var weights = await _db.AssessmentWeights
            .Where(w => w.ClassId == student.ClassId && w.TermId == termId)
            .ToListAsync();

        var scalingScale = await _db.GradingScales.OrderByDescending(s => s.MinScore).ToListAsync();

        // Group by subject
        var subjectGroups = grades.GroupBy(g => g.SubjectId);
        var subjectReports = new List<SubjectReportDto>();

        foreach (var group in subjectGroups)
        {
            var subject = group.First().Subject;
            var assessments = new List<AssessmentScoreDto>();
            decimal weightedTotal = 0;

            foreach (var g in group)
            {
                var weight = weights.FirstOrDefault(w => w.AssessmentType == g.AssessmentType)?.Weight ?? 0;
                var percentage = g.MaxScore > 0 ? g.Score / g.MaxScore * 100 : 0;
                var weightedScore = percentage * weight / 100;
                weightedTotal += weightedScore;

                assessments.Add(new AssessmentScoreDto
                {
                    AssessmentType = g.AssessmentType,
                    Score = g.Score,
                    MaxScore = g.MaxScore,
                    Weight = weight
                });
            }

            var grade = GetGrade(scalingScale, weightedTotal);
            var remark = GetRemark(scalingScale, weightedTotal);
            var teacherRemark = group.LastOrDefault()?.TeacherRemark;

            subjectReports.Add(new SubjectReportDto
            {
                SubjectName = subject.Name,
                SubjectCode = subject.Code,
                Assessments = assessments,
                WeightedTotal = Math.Round(weightedTotal, 1),
                Grade = grade,
                Remark = remark,
                TeacherRemark = teacherRemark
            });
        }

        var overallAvg = subjectReports.Any()
            ? Math.Round(subjectReports.Average(s => s.WeightedTotal), 1)
            : 0;

        // Attendance rate for this term
        var attRecords = await _db.Attendances
            .Where(a => a.StudentId == studentId && a.Date >= term.StartDate && a.Date <= term.EndDate)
            .ToListAsync();
        var totalDays = attRecords.Select(a => a.Date.Date).Distinct().Count();
        var presentDays = attRecords.Count(a => a.Status == "Present" || a.Status == "Late" || a.Status == "Excused");
        var attRate = totalDays > 0 ? Math.Round((decimal)presentDays / totalDays * 100, 1) : 0;

        return new StudentTermReportDto
        {
            StudentId = studentId,
            StudentName = $"{student.FirstName} {student.LastName}",
            StudentNumber = student.StudentNumber,
            ClassName = student.Class?.Name ?? "",
            TermName = term.Name,
            AcademicYear = term.AcademicYear?.Name ?? "",
            OverallAverage = overallAvg,
            OverallGrade = GetGrade(scalingScale, overallAvg),
            OverallRemark = GetRemark(scalingScale, overallAvg),
            ClassRank = 0, // set below in class report
            TotalStudents = 0,
            AttendanceRate = attRate,
            Subjects = subjectReports.OrderBy(s => s.SubjectName).ToList()
        };
    }

    // ── Class Term Reports (with ranking) ─────────────────────────────────────
    public async Task<List<StudentTermReportDto>> GetClassTermReportsAsync(Guid classId, Guid termId)
    {
        var cls = await _db.Classes
            .Include(c => c.Students)
            .FirstOrDefaultAsync(c => c.Id == classId)
            ?? throw new KeyNotFoundException($"Class {classId} not found.");

        var reports = new List<StudentTermReportDto>();

        foreach (var student in cls.Students.Where(s => s.Status == "Active"))
        {
            var report = await GetStudentTermReportAsync(student.Id, termId);
            reports.Add(report);
        }

        // Assign ranks
        var ranked = reports.OrderByDescending(r => r.OverallAverage).ToList();
        for (int i = 0; i < ranked.Count; i++)
        {
            ranked[i].ClassRank = i + 1;
            ranked[i].TotalStudents = ranked.Count;
        }

        return ranked;
    }

    // ── Assessment Weights ────────────────────────────────────────────────────
    public async Task SetAssessmentWeightsAsync(SetAssessmentWeightDto dto)
    {
        var totalWeight = dto.Weights.Sum(w => w.Weight);
        if (totalWeight != 100)
            throw new ArgumentException($"Assessment weights must sum to 100. Current sum: {totalWeight}");

        var existing = await _db.AssessmentWeights
            .Where(w => w.ClassId == dto.ClassId && w.TermId == dto.TermId)
            .ToListAsync();
        _db.AssessmentWeights.RemoveRange(existing);

        foreach (var weight in dto.Weights)
        {
            _db.AssessmentWeights.Add(new AssessmentWeight
            {
                ClassId = dto.ClassId,
                TermId = dto.TermId,
                AssessmentType = weight.AssessmentType,
                Weight = weight.Weight
            });
        }

        await _db.SaveChangesAsync();
    }

    public async Task<List<AssessmentWeightEntryDto>> GetAssessmentWeightsAsync(Guid classId, Guid termId)
    {
        var weights = await _db.AssessmentWeights
            .Where(w => w.ClassId == classId && w.TermId == termId)
            .ToListAsync();

        return weights.Select(w => new AssessmentWeightEntryDto
        {
            AssessmentType = w.AssessmentType,
            Weight = w.Weight
        }).ToList();
    }

    // ── Generate Report Card PDF (single student) ─────────────────────────────
    public async Task<byte[]> GenerateReportCardPdfAsync(Guid studentId, Guid termId)
    {
        QuestPDF.Settings.License = LicenseType.Community;
        var report = await GetStudentTermReportAsync(studentId, termId);

        var doc = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(30);
                page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial"));

                page.Header().Column(col =>
                {
                    col.Item().Text("SCHOOL MANAGEMENT SYSTEM")
                        .Bold().FontSize(18).FontColor("#1F3864").AlignCenter();
                    col.Item().Text("Student Report Card")
                        .FontSize(12).FontColor("#2E75B6").AlignCenter();
                    col.Item().LineHorizontal(1).LineColor("#2E75B6");
                    col.Item().Height(8);
                });

                page.Content().Column(col =>
                {
                    // Student Info
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(cols =>
                        {
                            cols.RelativeColumn();
                            cols.RelativeColumn();
                            cols.RelativeColumn();
                            cols.RelativeColumn();
                        });

                        void InfoCell(string label, string value)
                        {
                            table.Cell().Padding(4).Column(c =>
                            {
                                c.Item().Text(label).FontSize(8).FontColor("#666666");
                                c.Item().Text(value).Bold().FontSize(10);
                            });
                        }

                        InfoCell("Student Name", report.StudentName);
                        InfoCell("Student Number", report.StudentNumber);
                        InfoCell("Class", report.ClassName);
                        InfoCell("Academic Year", report.AcademicYear);
                        InfoCell("Term", report.TermName);
                        InfoCell("Attendance Rate", $"{report.AttendanceRate}%");
                        InfoCell("Overall Average", $"{report.OverallAverage}%");
                        InfoCell("Class Rank", report.ClassRank > 0 ? $"{report.ClassRank} / {report.TotalStudents}" : "—");
                    });

                    col.Item().Height(12);

                    // Grades Table
                    col.Item().Text("Academic Performance").Bold().FontSize(12).FontColor("#1F3864");
                    col.Item().Height(4);

                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(cols =>
                        {
                            cols.RelativeColumn(3);   // Subject
                            cols.RelativeColumn(2);   // Assessments
                            cols.RelativeColumn(1);   // Total
                            cols.RelativeColumn(1);   // Grade
                            cols.RelativeColumn(2);   // Remark
                        });

                        // Header
                        static IContainer HeaderCell(IContainer c) =>
                            c.Background("#1F3864").Padding(5);

                        table.Header(header =>
                        {
                            header.Cell().Element(HeaderCell).Text("Subject").Bold().FontColor("#FFFFFF");
                            header.Cell().Element(HeaderCell).Text("Assessments").Bold().FontColor("#FFFFFF");
                            header.Cell().Element(HeaderCell).Text("Total").Bold().FontColor("#FFFFFF");
                            header.Cell().Element(HeaderCell).Text("Grade").Bold().FontColor("#FFFFFF");
                            header.Cell().Element(HeaderCell).Text("Remark").Bold().FontColor("#FFFFFF");
                        });

                        // Rows
                        bool alt = false;
                        foreach (var subject in report.Subjects)
                        {
                            var bg = alt ? "#F2F2F2" : "#FFFFFF";
                            alt = !alt;

                            static IContainer DataCell(IContainer c, string bg) =>
                                c.Background(bg).Padding(5);

                            var assessmentText = string.Join(", ",
                                subject.Assessments.Select(a => $"{a.AssessmentType}: {a.Score}/{a.MaxScore}"));

                            table.Cell().Element(c => DataCell(c, bg)).Text(subject.SubjectName);
                            table.Cell().Element(c => DataCell(c, bg)).Text(assessmentText).FontSize(8);
                            table.Cell().Element(c => DataCell(c, bg)).Text($"{subject.WeightedTotal}%").Bold();
                            table.Cell().Element(c => DataCell(c, bg)).Text(subject.Grade).Bold().FontColor("#1F3864");
                            table.Cell().Element(c => DataCell(c, bg)).Text(subject.Remark);
                        }
                    });

                    col.Item().Height(16);

                    // Summary
                    col.Item().Background("#BDD7EE").Padding(8).Row(row =>
                    {
                        row.RelativeItem().Text($"Overall Average: {report.OverallAverage}%").Bold().FontSize(12);
                        row.RelativeItem().Text($"Grade: {report.OverallGrade}").Bold().FontSize(12).AlignCenter();
                        row.RelativeItem().Text($"Remark: {report.OverallRemark}").Bold().FontSize(12).AlignRight();
                    });

                    col.Item().Height(20);

                    // Signatures
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().LineHorizontal(1).LineColor("#CCCCCC");
                            c.Item().Text("Class Teacher").FontSize(9).FontColor("#666666");
                        });
                        row.ConstantItem(40);
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().LineHorizontal(1).LineColor("#CCCCCC");
                            c.Item().Text("Principal").FontSize(9).FontColor("#666666");
                        });
                        row.ConstantItem(40);
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().LineHorizontal(1).LineColor("#CCCCCC");
                            c.Item().Text("Parent / Guardian").FontSize(9).FontColor("#666666");
                        });
                    });
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

    // ── Generate Class Report Cards PDF ───────────────────────────────────────
    public async Task<byte[]> GenerateClassReportCardsPdfAsync(Guid classId, Guid termId)
    {
        QuestPDF.Settings.License = LicenseType.Community;
        var reports = await GetClassTermReportsAsync(classId, termId);

        var doc = Document.Create(container =>
        {
            foreach (var report in reports)
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(30);
                    page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial"));

                    page.Header().Column(col =>
                    {
                        col.Item().Text("SCHOOL MANAGEMENT SYSTEM")
                            .Bold().FontSize(18).FontColor("#1F3864").AlignCenter();
                        col.Item().Text("Student Report Card")
                            .FontSize(12).FontColor("#2E75B6").AlignCenter();
                        col.Item().LineHorizontal(1).LineColor("#2E75B6");
                        col.Item().Height(8);
                    });

                    page.Content().Column(col =>
                    {
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(cols =>
                            {
                                cols.RelativeColumn();
                                cols.RelativeColumn();
                                cols.RelativeColumn();
                                cols.RelativeColumn();
                            });

                            void InfoCell(string label, string value)
                            {
                                table.Cell().Padding(4).Column(c =>
                                {
                                    c.Item().Text(label).FontSize(8).FontColor("#666666");
                                    c.Item().Text(value).Bold().FontSize(10);
                                });
                            }

                            InfoCell("Student Name", report.StudentName);
                            InfoCell("Student Number", report.StudentNumber);
                            InfoCell("Class", report.ClassName);
                            InfoCell("Academic Year", report.AcademicYear);
                            InfoCell("Term", report.TermName);
                            InfoCell("Attendance Rate", $"{report.AttendanceRate}%");
                            InfoCell("Overall Average", $"{report.OverallAverage}%");
                            InfoCell("Class Rank", $"{report.ClassRank} / {report.TotalStudents}");
                        });

                        col.Item().Height(12);
                        col.Item().Text("Academic Performance").Bold().FontSize(12).FontColor("#1F3864");
                        col.Item().Height(4);

                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(cols =>
                            {
                                cols.RelativeColumn(3);
                                cols.RelativeColumn(2);
                                cols.RelativeColumn(1);
                                cols.RelativeColumn(1);
                                cols.RelativeColumn(2);
                            });

                            static IContainer HeaderCell(IContainer c) =>
                                c.Background("#1F3864").Padding(5);

                            table.Header(header =>
                            {
                                header.Cell().Element(HeaderCell).Text("Subject").Bold().FontColor("#FFFFFF");
                                header.Cell().Element(HeaderCell).Text("Assessments").Bold().FontColor("#FFFFFF");
                                header.Cell().Element(HeaderCell).Text("Total").Bold().FontColor("#FFFFFF");
                                header.Cell().Element(HeaderCell).Text("Grade").Bold().FontColor("#FFFFFF");
                                header.Cell().Element(HeaderCell).Text("Remark").Bold().FontColor("#FFFFFF");
                            });

                            bool alt = false;
                            foreach (var subject in report.Subjects)
                            {
                                var bg = alt ? "#F2F2F2" : "#FFFFFF";
                                alt = !alt;

                                static IContainer DataCell(IContainer c, string bg) =>
                                    c.Background(bg).Padding(5);

                                var assessmentText = string.Join(", ",
                                    subject.Assessments.Select(a => $"{a.AssessmentType}: {a.Score}/{a.MaxScore}"));

                                table.Cell().Element(c => DataCell(c, bg)).Text(subject.SubjectName);
                                table.Cell().Element(c => DataCell(c, bg)).Text(assessmentText).FontSize(8);
                                table.Cell().Element(c => DataCell(c, bg)).Text($"{subject.WeightedTotal}%").Bold();
                                table.Cell().Element(c => DataCell(c, bg)).Text(subject.Grade).Bold().FontColor("#1F3864");
                                table.Cell().Element(c => DataCell(c, bg)).Text(subject.Remark);
                            }
                        });

                        col.Item().Height(16);
                        col.Item().Background("#BDD7EE").Padding(8).Row(row =>
                        {
                            row.RelativeItem().Text($"Overall Average: {report.OverallAverage}%").Bold().FontSize(12);
                            row.RelativeItem().Text($"Grade: {report.OverallGrade}").Bold().FontSize(12).AlignCenter();
                            row.RelativeItem().Text($"Remark: {report.OverallRemark}").Bold().FontSize(12).AlignRight();
                        });

                        col.Item().Height(20);
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().LineHorizontal(1).LineColor("#CCCCCC");
                                c.Item().Text("Class Teacher").FontSize(9).FontColor("#666666");
                            });
                            row.ConstantItem(40);
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().LineHorizontal(1).LineColor("#CCCCCC");
                                c.Item().Text("Principal").FontSize(9).FontColor("#666666");
                            });
                            row.ConstantItem(40);
                            row.RelativeItem().Column(c =>
                            {
                                c.Item().LineHorizontal(1).LineColor("#CCCCCC");
                                c.Item().Text("Parent / Guardian").FontSize(9).FontColor("#666666");
                            });
                        });
                    });

                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.Span("Generated by SchoolMS · ").FontSize(8).FontColor("#999999");
                        x.Span(DateTime.UtcNow.ToString("dd MMM yyyy HH:mm")).FontSize(8).FontColor("#999999");
                    });
                });
            }
        });

        return doc.GeneratePdf();
    }

    // ── Private Helpers ───────────────────────────────────────────────────────
    private async Task ValidateNotLocked(Guid classId, Guid termId)
    {
        var locked = await _db.Grades
            .AnyAsync(g => g.ClassId == classId && g.TermId == termId && g.IsLocked);
        if (locked)
            throw new InvalidOperationException("Grades for this class/term are locked. Unlock before editing.");
    }

    private static string GetGrade(List<GradingScale> scale, decimal score)
    {
        var match = scale.FirstOrDefault(s => score >= s.MinScore && score <= s.MaxScore);
        return match?.Name ?? "F";
    }

    private static string GetRemark(List<GradingScale> scale, decimal score)
    {
        var match = scale.FirstOrDefault(s => score >= s.MinScore && score <= s.MaxScore);
        return match?.Remark ?? "Fail";
    }

    private static GradeResponseDto MapToDto(Grade g) => new()
    {
        Id = g.Id,
        StudentId = g.StudentId,
        StudentName = $"{g.Student.FirstName} {g.Student.LastName}",
        StudentNumber = g.Student.StudentNumber,
        SubjectId = g.SubjectId,
        SubjectName = g.Subject.Name,
        TermId = g.TermId,
        TermName = g.Term.Name,
        AssessmentType = g.AssessmentType,
        Score = g.Score,
        MaxScore = g.MaxScore,
        IsLocked = g.IsLocked,
        TeacherRemark = g.TeacherRemark,
        EnteredByName = $"{g.EnteredBy.FirstName} {g.EnteredBy.LastName}",
        CreatedAt = g.CreatedAt
    };
}