using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace HouseofTutorAPI.Models;

public partial class HouseofTutorContext : DbContext
{
    public HouseofTutorContext()
    {
    }

    public HouseofTutorContext(DbContextOptions<HouseofTutorContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Course> Courses { get; set; }

    public virtual DbSet<Feedback> Feedbacks { get; set; }

    public virtual DbSet<Payment> Payments { get; set; }

    public virtual DbSet<Request> Requests { get; set; }

    public virtual DbSet<RequestGroup> RequestGroups { get; set; }

    public virtual DbSet<Schedule> Schedules { get; set; }

    public virtual DbSet<Student> Students { get; set; }

    public virtual DbSet<StudentCourse> StudentCourses { get; set; }

    public virtual DbSet<StudentCourseContent> StudentCourseContents { get; set; }

    public virtual DbSet<StudentCourseFee> StudentCourseFees { get; set; }

    public virtual DbSet<StudentSchedule> StudentSchedules { get; set; }

    public virtual DbSet<Tutor> Tutors { get; set; }

    public virtual DbSet<TutorCourse> TutorCourses { get; set; }

    public virtual DbSet<TutorCourseRate> TutorCourseRates { get; set; }

    public virtual DbSet<User> Users { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlServer("Server=DESKTOP-FANR662\\SQLEXPRESS;Database=HouseofTutor;Trusted_Connection=True;TrustServerCertificate=True;");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Course>(entity =>
        {
            entity.HasKey(e => e.CourseId).HasName("PK__Course__8F1EF7AEB9F1622F");

            entity.ToTable("Course");

            entity.Property(e => e.CourseId).HasColumnName("course_id");
            entity.Property(e => e.AdminSetMaxHourlyRate)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("admin_set_max_hourly_rate");
            entity.Property(e => e.AdminSetMinHourlyRate)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("admin_set_min_hourly_rate");
            entity.Property(e => e.CourseTitle)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("course_title");
        });

        modelBuilder.Entity<Feedback>(entity =>
        {
            entity.HasKey(e => e.FeedbackId).HasName("PK__Feedback__7A6B2B8C35A2CDED");

            entity.ToTable("Feedback");

            entity.Property(e => e.FeedbackId).HasColumnName("feedback_id");
            entity.Property(e => e.Comment)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("comment");
            entity.Property(e => e.CourseId).HasColumnName("course_id");
            entity.Property(e => e.FeedbackBy)
                .HasMaxLength(20)
                .HasColumnName("feedback_by");
            entity.Property(e => e.FeedbackDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("feedback_date");
            entity.Property(e => e.Rating).HasColumnName("rating");
            entity.Property(e => e.StudentId).HasColumnName("student_id");
            entity.Property(e => e.TutorId).HasColumnName("tutor_id");

            entity.HasOne(d => d.Course).WithMany(p => p.Feedbacks)
                .HasForeignKey(d => d.CourseId)
                .HasConstraintName("FK__Feedback__course__66603565");

            entity.HasOne(d => d.Student).WithMany(p => p.Feedbacks)
                .HasForeignKey(d => d.StudentId)
                .HasConstraintName("FK__Feedback__studen__6477ECF3");

            entity.HasOne(d => d.Tutor).WithMany(p => p.Feedbacks)
                .HasForeignKey(d => d.TutorId)
                .HasConstraintName("FK__Feedback__tutor___656C112C");
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(e => e.PaymentId).HasName("PK__Payment__ED1FC9EA8811CF21");

            entity.ToTable("Payment");

            entity.Property(e => e.PaymentId).HasColumnName("payment_id");
            entity.Property(e => e.Amount)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("amount");
            entity.Property(e => e.FeeId).HasColumnName("fee_id");
            entity.Property(e => e.ParentStatus)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("parent_status");
            entity.Property(e => e.PaymentDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("payment_date");
            entity.Property(e => e.PaymentType)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("payment_type");
            entity.Property(e => e.Remarks)
                .HasMaxLength(200)
                .IsUnicode(false)
                .HasColumnName("remarks");
            entity.Property(e => e.TutorStatus)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("tutor_status");

            entity.HasOne(d => d.Fee).WithMany(p => p.Payments)
                .HasForeignKey(d => d.FeeId)
                .HasConstraintName("FK__Payment__fee_id__2645B050");
        });

        modelBuilder.Entity<Request>(entity =>
        {
            entity.HasKey(e => e.RequestId).HasName("PK__Request__18D3B90F519F4A7A");

            entity.ToTable("Request");

            entity.Property(e => e.RequestId).HasColumnName("request_id");
            entity.Property(e => e.ClassDate).HasColumnName("class_date");
            entity.Property(e => e.CourseId).HasColumnName("course_id");
            entity.Property(e => e.Day)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("day");
            entity.Property(e => e.LearningDuration).HasColumnName("learning_duration");
            entity.Property(e => e.LearningDurationUnit)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("learning_duration_unit");
            entity.Property(e => e.LearningMode)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("learning_mode");
            entity.Property(e => e.ParentRequestId).HasColumnName("parent_request_id");
            entity.Property(e => e.RequestDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("request_date");
            entity.Property(e => e.RequestGroupId).HasColumnName("request_group_id");
            entity.Property(e => e.RequestType)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("request_type");
            entity.Property(e => e.ResponseDeadline)
                .HasColumnType("datetime")
                .HasColumnName("response_deadline");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.StudentId).HasColumnName("student_id");
            entity.Property(e => e.Time)
                .HasMaxLength(100)
                .IsUnicode(false);
            entity.Property(e => e.TutorId).HasColumnName("tutor_id");
            entity.Property(e => e.TutorSequence).HasColumnName("tutor_sequence");

            entity.HasOne(d => d.Course).WithMany(p => p.Requests)
                .HasForeignKey(d => d.CourseId)
                .HasConstraintName("FK__Request__course___5FB337D6");

            entity.HasOne(d => d.RequestGroup).WithMany(p => p.Requests)
                .HasForeignKey(d => d.RequestGroupId)
                .HasConstraintName("FK_Request_RequestGroup");

            entity.HasOne(d => d.Student).WithMany(p => p.Requests)
                .HasForeignKey(d => d.StudentId)
                .HasConstraintName("FK__Request__student__5DCAEF64");

            entity.HasOne(d => d.Tutor).WithMany(p => p.Requests)
                .HasForeignKey(d => d.TutorId)
                .HasConstraintName("FK__Request__tutor_i__5EBF139D");
        });

        modelBuilder.Entity<RequestGroup>(entity =>
        {
            entity.HasKey(e => e.RequestGroupId).HasName("PK__Request___30D47C2981974292");

            entity.ToTable("Request_Group");

            entity.Property(e => e.RequestGroupId).HasColumnName("request_group_id");
            entity.Property(e => e.CourseId).HasColumnName("course_id");
            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("created_date");
            entity.Property(e => e.CurrentRequestId).HasColumnName("current_request_id");
            entity.Property(e => e.Status)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasColumnName("status");
            entity.Property(e => e.StudentId).HasColumnName("student_id");

            entity.HasOne(d => d.Course).WithMany(p => p.RequestGroups)
                .HasForeignKey(d => d.CourseId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Request_G__cours__51300E55");

            entity.HasOne(d => d.Student).WithMany(p => p.RequestGroups)
                .HasForeignKey(d => d.StudentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Request_G__stude__503BEA1C");
        });

        modelBuilder.Entity<Schedule>(entity =>
        {
            entity.HasKey(e => e.ScheduleId).HasName("PK__Schedule__C46A8A6FEF990444");

            entity.ToTable("Schedule");

            entity.Property(e => e.ScheduleId).HasColumnName("schedule_id");
            entity.Property(e => e.Day)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("day");
            entity.Property(e => e.EndDate).HasColumnName("end_date");
            entity.Property(e => e.StartDate).HasColumnName("start_date");
            entity.Property(e => e.Time)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("time");
            entity.Property(e => e.TutorId).HasColumnName("tutor_id");
            entity.Property(e => e.Type)
                .HasMaxLength(50)
                .IsUnicode(false);

            entity.HasOne(d => d.Tutor).WithMany(p => p.Schedules)
                .HasForeignKey(d => d.TutorId)
                .HasConstraintName("FK__Schedule__tutor___5629CD9C");
        });

        modelBuilder.Entity<Student>(entity =>
        {
            entity.HasKey(e => e.StudentId).HasName("PK__Student__2A33069A6C1D77D6");

            entity.ToTable("Student");

            entity.Property(e => e.StudentId).HasColumnName("student_id");
            entity.Property(e => e.FatherCnic)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("Father_Cnic");
            entity.Property(e => e.FeeResponsibility)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("fee_responsibility");
            entity.Property(e => e.Location)
                .IsUnicode(false)
                .HasColumnName("location");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("status");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.User).WithMany(p => p.Students)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK__Student__user_id__5165187F");
        });

        modelBuilder.Entity<StudentCourse>(entity =>
        {
            entity.HasKey(e => new { e.StudentId, e.CourseId }).HasName("PK__Student___D2C2E9E09DC4D622");

            entity.ToTable("Student_Course");

            entity.Property(e => e.StudentId).HasColumnName("student_id");
            entity.Property(e => e.CourseId).HasColumnName("course_id");
            entity.Property(e => e.Grade)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("grade");

            entity.HasOne(d => d.Course).WithMany(p => p.StudentCourses)
                .HasForeignKey(d => d.CourseId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Student_C__cours__72C60C4A");
        });

        modelBuilder.Entity<StudentCourseContent>(entity =>
        {
            entity.HasKey(e => e.ContentId).HasName("PK__Student___655FE5105D0E97CF");

            entity.ToTable("Student_Course_Content");

            entity.Property(e => e.ContentId).HasColumnName("content_id");
            entity.Property(e => e.CourseId).HasColumnName("course_id");
            entity.Property(e => e.Description)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("description");
            entity.Property(e => e.FileName)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("file_name");
            entity.Property(e => e.FilePath)
                .HasMaxLength(500)
                .IsUnicode(false)
                .HasColumnName("file_path");
            entity.Property(e => e.StudentId).HasColumnName("student_id");
            entity.Property(e => e.Title)
                .HasMaxLength(150)
                .IsUnicode(false)
                .HasColumnName("title");
            entity.Property(e => e.UploadedDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("uploaded_date");

            entity.HasOne(d => d.Course).WithMany(p => p.StudentCourseContents)
                .HasForeignKey(d => d.CourseId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Student_C__cours__56E8E7AB");

            entity.HasOne(d => d.Student).WithMany(p => p.StudentCourseContents)
                .HasForeignKey(d => d.StudentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Student_C__stude__55F4C372");
        });

        modelBuilder.Entity<StudentCourseFee>(entity =>
        {
            entity.HasKey(e => e.FeeId).HasName("PK__Student___A19C8AFB7090B923");

            entity.ToTable("Student_Course_Fee");

            entity.HasIndex(e => new { e.StudentId, e.TutorId, e.CourseId }, "UQ__Student___80B1FDBC799B4612").IsUnique();

            entity.Property(e => e.FeeId).HasColumnName("fee_id");
            entity.Property(e => e.CourseId).HasColumnName("course_id");
            entity.Property(e => e.CreatedDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("created_date");
            entity.Property(e => e.FeeResponsibility)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("Parent")
                .HasColumnName("fee_responsibility");
            entity.Property(e => e.StudentId).HasColumnName("student_id");
            entity.Property(e => e.TotalFee)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("total_fee");
            entity.Property(e => e.TutorId).HasColumnName("tutor_id");

            entity.HasOne(d => d.Course).WithMany(p => p.StudentCourseFees)
                .HasForeignKey(d => d.CourseId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Student_C__cours__22751F6C");

            entity.HasOne(d => d.Student).WithMany(p => p.StudentCourseFees)
                .HasForeignKey(d => d.StudentId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Student_C__stude__208CD6FA");

            entity.HasOne(d => d.Tutor).WithMany(p => p.StudentCourseFees)
                .HasForeignKey(d => d.TutorId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Student_C__tutor__2180FB33");
        });

        modelBuilder.Entity<StudentSchedule>(entity =>
        {
            entity.HasKey(e => e.ScheduleId).HasName("PK__Student___C46A8A6FA77E8ECE");

            entity.ToTable("Student_Schedule");

            entity.Property(e => e.ScheduleId).HasColumnName("schedule_id");
            entity.Property(e => e.Day)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("day");
            entity.Property(e => e.EndDate).HasColumnName("end_date");
            entity.Property(e => e.StartDate).HasColumnName("start_date");
            entity.Property(e => e.StudentId).HasColumnName("student_id");
            entity.Property(e => e.Time)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("time");
            entity.Property(e => e.Type)
                .HasMaxLength(50)
                .IsUnicode(false);

            entity.HasOne(d => d.Student).WithMany(p => p.StudentSchedules)
                .HasForeignKey(d => d.StudentId)
                .HasConstraintName("FK__Student_S__stude__6D0D32F4");
        });

        modelBuilder.Entity<Tutor>(entity =>
        {
            entity.HasKey(e => e.TutorId).HasName("PK__Tutor__50DE5D0142BF8F72");

            entity.ToTable("Tutor");

            entity.Property(e => e.TutorId).HasColumnName("tutor_id");
            entity.Property(e => e.Experience).HasColumnName("experience");
            entity.Property(e => e.Location)
                .IsUnicode(false)
                .HasColumnName("location");
            entity.Property(e => e.Qualification)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("qualification");
            entity.Property(e => e.Radius).HasColumnName("radius");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("status");
            entity.Property(e => e.TeachingMode)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("teaching_mode");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.User).WithMany(p => p.Tutors)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK__Tutor__user_id__4E88ABD4");
        });

        modelBuilder.Entity<TutorCourse>(entity =>
        {
            entity.HasKey(e => new { e.TutorId, e.CourseId }).HasName("PK__Tutor_Co__A82FB27BA4857372");

            entity.ToTable("Tutor_Course");

            entity.Property(e => e.TutorId).HasColumnName("tutor_id");
            entity.Property(e => e.CourseId).HasColumnName("course_id");
            entity.Property(e => e.CompletedDate)
                .HasColumnType("datetime")
                .HasColumnName("completed_date");
            entity.Property(e => e.Grade)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("grade");
            entity.Property(e => e.Institute)
                .HasMaxLength(150)
                .IsUnicode(false)
                .HasColumnName("institute");
            entity.Property(e => e.IsCompleted).HasColumnName("is_completed");

            entity.HasOne(d => d.Course).WithMany(p => p.TutorCourses)
                .HasForeignKey(d => d.CourseId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Tutor_Cou__cours__59FA5E80");

            entity.HasOne(d => d.Tutor).WithMany(p => p.TutorCourses)
                .HasForeignKey(d => d.TutorId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Tutor_Cou__tutor__59063A47");
        });

        modelBuilder.Entity<TutorCourseRate>(entity =>
        {
            entity.HasKey(e => e.RateId).HasName("PK__Tutor_Co__75920B42B74529C1");

            entity.ToTable("Tutor_Course_Rate");

            entity.Property(e => e.RateId).HasColumnName("rate_id");
            entity.Property(e => e.AdminSetMaxHourlyRate)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("admin_set_max_hourly_rate");
            entity.Property(e => e.AdminSetMinHourlyRate)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("admin_set_min_hourly_rate");
            entity.Property(e => e.CourseId).HasColumnName("course_id");
            entity.Property(e => e.HourlyRate)
                .HasColumnType("decimal(10, 2)")
                .HasColumnName("hourly_rate");
            entity.Property(e => e.TutorId).HasColumnName("tutor_id");

            entity.HasOne(d => d.Course).WithMany(p => p.TutorCourseRates)
                .HasForeignKey(d => d.CourseId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Tutor_Cou__cours__1BC821DD");

            entity.HasOne(d => d.Tutor).WithMany(p => p.TutorCourseRates)
                .HasForeignKey(d => d.TutorId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Tutor_Cou__tutor__1AD3FDA4");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PK__Users__B9BE370FAAD384A7");

            entity.HasIndex(e => e.Email, "UQ__Users__AB6E6164B2DCE846").IsUnique();

            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.Cnic)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("cnic");
            entity.Property(e => e.Email)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("email");
            entity.Property(e => e.FullName)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("full_name");
            entity.Property(e => e.Password)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("password");
            entity.Property(e => e.Phone)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("phone");
            entity.Property(e => e.Role)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("role");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
