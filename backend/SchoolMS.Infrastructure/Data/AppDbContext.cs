using Microsoft.EntityFrameworkCore;
using SchoolMS.Core.Entities;
using SchoolMS.Infrastructure.Interceptors;

namespace SchoolMS.Infrastructure.Data;

public class AppDbContext : DbContext
{
    private readonly AuditInterceptor _auditInterceptor;

    public AppDbContext(DbContextOptions<AppDbContext> options, AuditInterceptor auditInterceptor)
        : base(options)
    {
        _auditInterceptor = auditInterceptor;
    }

    // Sprint 1
    public DbSet<User> Users => Set<User>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    // Sprint 2
    public DbSet<Student> Students => Set<Student>();
    public DbSet<Guardian> Guardians => Set<Guardian>();
    public DbSet<StudentDocument> StudentDocuments => Set<StudentDocument>();
    public DbSet<StaffDocument> StaffDocuments => Set<StaffDocument>();

    // Sprint 3
    public DbSet<Staff> Staff => Set<Staff>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<AcademicYear> AcademicYears => Set<AcademicYear>();
    public DbSet<Term> Terms => Set<Term>();
    public DbSet<Class> Classes => Set<Class>();
    public DbSet<Subject> Subjects => Set<Subject>();
    public DbSet<ClassSubject> ClassSubjects => Set<ClassSubject>();

    // Sprint 5
    public DbSet<Attendance> Attendances => Set<Attendance>();
    public DbSet<TimetableSlot> TimetableSlots => Set<TimetableSlot>();
    public DbSet<Grade> Grades => Set<Grade>();
    public DbSet<GradingScale> GradingScales => Set<GradingScale>();
    public DbSet<AssessmentWeight> AssessmentWeights => Set<AssessmentWeight>();

    // Sprint 6
    public DbSet<FeeCategory> FeeCategories => Set<FeeCategory>();
    public DbSet<FeeStructure> FeeStructures => Set<FeeStructure>();
    public DbSet<FeeInvoice> FeeInvoices => Set<FeeInvoice>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<Message> Messages => Set<Message>();
    public DbSet<Announcement> Announcements => Set<Announcement>();

    // Sprint 7
    public DbSet<Book> Books => Set<Book>();
    public DbSet<BookLoan> BookLoans => Set<BookLoan>();
    public DbSet<Vehicle> Vehicles => Set<Vehicle>();
    public DbSet<TransportRoute> TransportRoutes => Set<TransportRoute>();
    public DbSet<StudentTransport> StudentTransports => Set<StudentTransport>();
    public DbSet<HostelBuilding> HostelBuildings => Set<HostelBuilding>();
    public DbSet<HostelRoom> HostelRooms => Set<HostelRoom>();
    public DbSet<HostelAllocation> HostelAllocations => Set<HostelAllocation>();
    public DbSet<Exam> Exams => Set<Exam>();
    public DbSet<ExamSchedule> ExamSchedules => Set<ExamSchedule>();
    public DbSet<ExamSeatingArrangement> ExamSeatingArrangements => Set<ExamSeatingArrangement>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.AddInterceptors(_auditInterceptor);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ── Soft delete filters ─────────────────────────────────────────────
        modelBuilder.Entity<Student>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Staff>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Guardian>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Class>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Subject>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Department>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<User>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<AcademicYear>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Term>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<FeeCategory>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<FeeStructure>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<FeeInvoice>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Announcement>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Book>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Vehicle>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<TransportRoute>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<HostelBuilding>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<HostelRoom>().HasQueryFilter(e => !e.IsDeleted);
        modelBuilder.Entity<Exam>().HasQueryFilter(e => !e.IsDeleted);

        // ── Unique constraints ──────────────────────────────────────────────
        modelBuilder.Entity<User>().HasIndex(u => u.Email).IsUnique();
        modelBuilder.Entity<Student>().HasIndex(s => s.StudentNumber).IsUnique();
        modelBuilder.Entity<Staff>().HasIndex(s => s.StaffNumber).IsUnique();
        modelBuilder.Entity<Subject>().HasIndex(s => s.Code).IsUnique();
        modelBuilder.Entity<FeeInvoice>().HasIndex(i => i.InvoiceNumber).IsUnique();
        modelBuilder.Entity<Payment>().HasIndex(p => p.ReceiptNumber).IsUnique();
        modelBuilder.Entity<Book>().HasIndex(b => b.ISBN).IsUnique();
        modelBuilder.Entity<Vehicle>().HasIndex(v => v.PlateNumber).IsUnique();

        // ── AuditLog ────────────────────────────────────────────────────────
        modelBuilder.Entity<AuditLog>().HasKey(a => a.Id);

        // ── ClassSubject ────────────────────────────────────────────────────
        modelBuilder.Entity<ClassSubject>()
            .HasOne(cs => cs.Class).WithMany(c => c.ClassSubjects).HasForeignKey(cs => cs.ClassId);
        modelBuilder.Entity<ClassSubject>()
            .HasOne(cs => cs.Subject).WithMany(s => s.ClassSubjects).HasForeignKey(cs => cs.SubjectId);
        modelBuilder.Entity<ClassSubject>()
            .HasOne(cs => cs.Teacher).WithMany().HasForeignKey(cs => cs.TeacherId).IsRequired(false);

        // ── Department HoD ──────────────────────────────────────────────────
        modelBuilder.Entity<Department>()
            .HasOne(d => d.HeadOfDepartment).WithMany().HasForeignKey(d => d.HeadOfDepartmentId)
            .IsRequired(false).OnDelete(DeleteBehavior.SetNull);

        // ── Class → ClassTeacher ────────────────────────────────────────────
        modelBuilder.Entity<Class>()
            .HasOne(c => c.ClassTeacher).WithMany(s => s.ClassesAsTeacher)
            .HasForeignKey(c => c.ClassTeacherId).IsRequired(false).OnDelete(DeleteBehavior.SetNull);

        // ── Attendance ──────────────────────────────────────────────────────
        modelBuilder.Entity<Attendance>()
            .HasOne(a => a.Student).WithMany().HasForeignKey(a => a.StudentId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Attendance>()
            .HasOne(a => a.Class).WithMany().HasForeignKey(a => a.ClassId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Attendance>()
            .HasOne(a => a.MarkedBy).WithMany().HasForeignKey(a => a.MarkedById)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Attendance>()
            .HasIndex(a => new { a.StudentId, a.Date, a.Period }).IsUnique();

        // ── TimetableSlot ───────────────────────────────────────────────────
        modelBuilder.Entity<TimetableSlot>()
            .HasOne(t => t.Class).WithMany().HasForeignKey(t => t.ClassId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<TimetableSlot>()
            .HasOne(t => t.Subject).WithMany().HasForeignKey(t => t.SubjectId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<TimetableSlot>()
            .HasOne(t => t.Teacher).WithMany().HasForeignKey(t => t.TeacherId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<TimetableSlot>()
            .HasOne(t => t.AcademicYear).WithMany().HasForeignKey(t => t.AcademicYearId)
            .OnDelete(DeleteBehavior.Restrict);

        // ── Grade ───────────────────────────────────────────────────────────
        modelBuilder.Entity<Grade>()
            .HasOne(g => g.Student).WithMany().HasForeignKey(g => g.StudentId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Grade>()
            .HasOne(g => g.Subject).WithMany().HasForeignKey(g => g.SubjectId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Grade>()
            .HasOne(g => g.Term).WithMany().HasForeignKey(g => g.TermId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Grade>()
            .HasOne(g => g.Class).WithMany().HasForeignKey(g => g.ClassId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Grade>()
            .HasOne(g => g.EnteredBy).WithMany().HasForeignKey(g => g.EnteredById)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Grade>()
            .HasIndex(g => new { g.StudentId, g.SubjectId, g.TermId, g.AssessmentType }).IsUnique();

        // ── AssessmentWeight ────────────────────────────────────────────────
        modelBuilder.Entity<AssessmentWeight>()
            .HasOne(a => a.Class).WithMany().HasForeignKey(a => a.ClassId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<AssessmentWeight>()
            .HasOne(a => a.Term).WithMany().HasForeignKey(a => a.TermId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<AssessmentWeight>()
            .HasIndex(a => new { a.ClassId, a.TermId, a.AssessmentType }).IsUnique();

        // ── FeeStructure ────────────────────────────────────────────────────
        modelBuilder.Entity<FeeStructure>()
            .HasOne(f => f.FeeCategory).WithMany(c => c.FeeStructures).HasForeignKey(f => f.FeeCategoryId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<FeeStructure>()
            .HasOne(f => f.Term).WithMany().HasForeignKey(f => f.TermId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<FeeStructure>()
            .HasOne(f => f.Class).WithMany().HasForeignKey(f => f.ClassId)
            .IsRequired(false).OnDelete(DeleteBehavior.SetNull);
        modelBuilder.Entity<FeeStructure>()
            .Property(f => f.Amount).HasPrecision(18, 2);

        // ── FeeInvoice ──────────────────────────────────────────────────────
        modelBuilder.Entity<FeeInvoice>()
            .HasOne(i => i.Student).WithMany().HasForeignKey(i => i.StudentId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<FeeInvoice>()
            .HasOne(i => i.FeeStructure).WithMany(f => f.Invoices).HasForeignKey(i => i.FeeStructureId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<FeeInvoice>()
            .HasOne(i => i.Term).WithMany().HasForeignKey(i => i.TermId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<FeeInvoice>()
            .Property(i => i.Amount).HasPrecision(18, 2);
        modelBuilder.Entity<FeeInvoice>()
            .Property(i => i.PaidAmount).HasPrecision(18, 2);
        modelBuilder.Entity<FeeInvoice>()
            .Property(i => i.DiscountAmount).HasPrecision(18, 2);

        // ── Payment ─────────────────────────────────────────────────────────
        modelBuilder.Entity<Payment>()
            .HasOne(p => p.FeeInvoice).WithMany(i => i.Payments).HasForeignKey(p => p.FeeInvoiceId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Payment>()
            .HasOne(p => p.RecordedBy).WithMany().HasForeignKey(p => p.RecordedById)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Payment>()
            .Property(p => p.Amount).HasPrecision(18, 2);

        // ── Message ─────────────────────────────────────────────────────────
        modelBuilder.Entity<Message>()
            .HasOne(m => m.Sender).WithMany().HasForeignKey(m => m.SenderId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Message>()
            .HasOne(m => m.Recipient).WithMany().HasForeignKey(m => m.RecipientId)
            .IsRequired(false).OnDelete(DeleteBehavior.SetNull);

        // ── Announcement ────────────────────────────────────────────────────
        modelBuilder.Entity<Announcement>()
            .HasOne(a => a.CreatedBy).WithMany().HasForeignKey(a => a.CreatedById)
            .OnDelete(DeleteBehavior.Restrict);

        // ── BookLoan ────────────────────────────────────────────────────────
        modelBuilder.Entity<BookLoan>()
            .HasOne(l => l.Book).WithMany(b => b.Loans).HasForeignKey(l => l.BookId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<BookLoan>()
            .HasOne(l => l.Borrower).WithMany().HasForeignKey(l => l.BorrowerId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<BookLoan>()
            .HasOne(l => l.IssuedBy).WithMany().HasForeignKey(l => l.IssuedById)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<BookLoan>()
            .Property(l => l.FineAmount).HasPrecision(18, 2);

        // ── Vehicle ─────────────────────────────────────────────────────────
        modelBuilder.Entity<Vehicle>()
            .HasOne(v => v.Driver).WithMany().HasForeignKey(v => v.DriverId)
            .IsRequired(false).OnDelete(DeleteBehavior.SetNull);

        // ── TransportRoute ──────────────────────────────────────────────────
        modelBuilder.Entity<TransportRoute>()
            .HasOne(r => r.Vehicle).WithMany(v => v.Routes).HasForeignKey(r => r.VehicleId)
            .IsRequired(false).OnDelete(DeleteBehavior.SetNull);
        modelBuilder.Entity<TransportRoute>()
            .Property(r => r.MorningPickupFee).HasPrecision(18, 2);
        modelBuilder.Entity<TransportRoute>()
            .Property(r => r.AfternoonDropFee).HasPrecision(18, 2);

        // ── StudentTransport ─────────────────────────────────────────────────
        modelBuilder.Entity<StudentTransport>()
            .HasOne(st => st.Student).WithMany().HasForeignKey(st => st.StudentId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<StudentTransport>()
            .HasOne(st => st.Route).WithMany(r => r.StudentSubscriptions).HasForeignKey(st => st.RouteId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<StudentTransport>()
            .HasOne(st => st.Term).WithMany().HasForeignKey(st => st.TermId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<StudentTransport>()
            .HasIndex(st => new { st.StudentId, st.RouteId, st.TermId }).IsUnique();

        // ── HostelBuilding ───────────────────────────────────────────────────
        modelBuilder.Entity<HostelBuilding>()
            .HasOne(b => b.Warden).WithMany().HasForeignKey(b => b.WardenId)
            .IsRequired(false).OnDelete(DeleteBehavior.SetNull);

        // ── HostelRoom ───────────────────────────────────────────────────────
        modelBuilder.Entity<HostelRoom>()
            .HasOne(r => r.Building).WithMany(b => b.Rooms).HasForeignKey(r => r.BuildingId)
            .OnDelete(DeleteBehavior.Restrict);

        // ── HostelAllocation ─────────────────────────────────────────────────
        modelBuilder.Entity<HostelAllocation>()
            .HasOne(a => a.Student).WithMany().HasForeignKey(a => a.StudentId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<HostelAllocation>()
            .HasOne(a => a.Room).WithMany(r => r.Allocations).HasForeignKey(a => a.RoomId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<HostelAllocation>()
            .HasOne(a => a.Term).WithMany().HasForeignKey(a => a.TermId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<HostelAllocation>()
            .HasIndex(a => new { a.StudentId, a.TermId }).IsUnique();

        // ── Exam ────────────────────────────────────────────────────────────
        modelBuilder.Entity<Exam>()
            .HasOne(e => e.Term).WithMany().HasForeignKey(e => e.TermId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Exam>()
            .HasOne(e => e.AcademicYear).WithMany().HasForeignKey(e => e.AcademicYearId)
            .OnDelete(DeleteBehavior.Restrict);

        // ── ExamSchedule ─────────────────────────────────────────────────────
        modelBuilder.Entity<ExamSchedule>()
            .HasOne(es => es.Exam).WithMany(e => e.Schedules).HasForeignKey(es => es.ExamId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<ExamSchedule>()
            .HasOne(es => es.Subject).WithMany().HasForeignKey(es => es.SubjectId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<ExamSchedule>()
            .HasOne(es => es.Class).WithMany().HasForeignKey(es => es.ClassId)
            .OnDelete(DeleteBehavior.Restrict);

        // ── ExamSeatingArrangement ───────────────────────────────────────────
        modelBuilder.Entity<ExamSeatingArrangement>()
            .HasOne(sa => sa.ExamSchedule).WithMany(es => es.SeatingArrangements)
            .HasForeignKey(sa => sa.ExamScheduleId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<ExamSeatingArrangement>()
            .HasOne(sa => sa.Student).WithMany().HasForeignKey(sa => sa.StudentId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<ExamSeatingArrangement>()
            .HasIndex(sa => new { sa.ExamScheduleId, sa.StudentId }).IsUnique();
    }
}