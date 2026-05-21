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

    public virtual DbSet<Request> Requests { get; set; }

    public virtual DbSet<Schedule> Schedules { get; set; }

    public DbSet<StudentSchedule> Student_Schedules { get; set; }

    public virtual DbSet<Student> Students { get; set; }

    public virtual DbSet<Tutor> Tutors { get; set; }

    public virtual DbSet<TutorCourse> TutorCourses { get; set; }

    public virtual DbSet<StudentCourse> StudentCourses { get; set; }

    public virtual DbSet<User> Users { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {

        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Course>(entity =>
        {
            entity.HasKey(e => e.CourseId).HasName("PK__Course__8F1EF7AEB9F1622F");

            entity.ToTable("Course");

            entity.Property(e => e.CourseId).HasColumnName("course_id");
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

        modelBuilder.Entity<Request>(entity =>
        {
            entity.HasKey(e => e.RequestId).HasName("PK__Request__18D3B90F519F4A7A");

            entity.ToTable("Request");

            entity.Property(e => e.RequestId).HasColumnName("request_id");
            entity.Property(e => e.CourseId).HasColumnName("course_id");
            entity.Property(e => e.RequestDate)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("request_date");
            entity.Property(e => e.StudentId).HasColumnName("student_id");
            entity.Property(e => e.TutorId).HasColumnName("tutor_id");

            entity.HasOne(d => d.Course).WithMany(p => p.Requests)
                .HasForeignKey(d => d.CourseId)
                .HasConstraintName("FK__Request__course___5FB337D6");

            entity.HasOne(d => d.Student).WithMany(p => p.Requests)
                .HasForeignKey(d => d.StudentId)
                .HasConstraintName("FK__Request__student__5DCAEF64");

            entity.HasOne(d => d.Tutor).WithMany(p => p.Requests)
                .HasForeignKey(d => d.TutorId)
                .HasConstraintName("FK__Request__tutor_i__5EBF139D");
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
            entity.Property(e => e.Time)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("time");
            entity.Property(e => e.TutorId).HasColumnName("tutor_id");

            entity.HasOne(d => d.Tutor).WithMany(p => p.Schedules)
                .HasForeignKey(d => d.TutorId)
                .HasConstraintName("FK__Schedule__tutor___5629CD9C");
            // ✅ FIX: Add StartDate mapping
            entity.Property(e => e.StartDate)
                  .HasColumnType("datetime")
                  .HasColumnName("start_date");

            // ✅ FIX: Add EndDate mapping
            entity.Property(e => e.EndDate)
                  .HasColumnType("datetime")
                  .HasColumnName("end_date");
        });

        modelBuilder.Entity<Student>(entity =>
        {
            entity.HasKey(e => e.StudentId).HasName("PK__Student__2A33069A6C1D77D6");

            entity.ToTable("Student");

            entity.Property(e => e.StudentId).HasColumnName("student_id");
            entity.Property(e => e.Location)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("location");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.User).WithMany(p => p.Students)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("FK__Student__user_id__5165187F");
        });

        modelBuilder.Entity<Tutor>(entity =>
        {
            entity.HasKey(e => e.TutorId).HasName("PK__Tutor__50DE5D0142BF8F72");

            entity.ToTable("Tutor");

            entity.Property(e => e.TutorId).HasColumnName("tutor_id");
            entity.Property(e => e.Experience).HasColumnName("experience");
            entity.Property(e => e.Location)
                .HasMaxLength(100)
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
            entity.Property(e => e.Grade)
                .HasMaxLength(10)
                .IsUnicode(false)
                .HasColumnName("grade");

            entity.HasOne(d => d.Course).WithMany(p => p.TutorCourses)
                .HasForeignKey(d => d.CourseId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Tutor_Cou__cours__59FA5E80");

            entity.HasOne(d => d.Tutor).WithMany(p => p.TutorCourses)
                .HasForeignKey(d => d.TutorId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Tutor_Cou__tutor__59063A47");
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

        //modelBuilder.Entity<StudentSchedule>(entity =>
        //{
        //    entity.HasKey(e => e.ScheduleId);

        //    entity.ToTable("Student_Schedule");

        //    entity.Property(e => e.ScheduleId).HasColumnName("schedule_id");
        //    entity.Property(e => e.StudentId).HasColumnName("student_id");

        //    entity.Property(e => e.Day)
        //        .HasMaxLength(20)
        //        .IsUnicode(false)
        //        .HasColumnName("day");

        //    entity.Property(e => e.Time)
        //        .HasMaxLength(20)
        //        .IsUnicode(false)
        //        .HasColumnName("time");

        //    //// ✅ FIX: Add StartDate mapping
        //    //entity.Property(e => e.StartDate)
        //    //      .HasColumnType("datetime")
        //    //      .HasColumnName("start_date");

        //    //// ✅ FIX: Add EndDate mapping
        //    //entity.Property(e => e.EndDate)
        //    //      .HasColumnType("datetime")
        //    //      .HasColumnName("end_date");

        //    //// ✅ FIX: Add Type mapping
        //    //entity.Property(e => e.Type)
        //    //      .HasMaxLength(50)
        //    //      .HasColumnName("type");

        //    entity.HasOne(d => d.Student)
        //        .WithMany(p => p.StudentSchedules)
        //        .HasForeignKey(d => d.StudentId)
        //        .HasConstraintName("FK_StudentSchedule_Student");

        //    entity.Property(e => e.StartDate)
        //          .HasColumnType("datetime")
        //          .HasColumnName("StartDate");

        //    entity.Property(e => e.EndDate)
        //          .HasColumnType("datetime")
        //          .HasColumnName("EndDate");

        //    entity.Property(e => e.Type)
        //          .HasMaxLength(50)
        //          .HasColumnName("Type");
        //});
        modelBuilder.Entity<StudentSchedule>(entity =>
        {
            entity.HasKey(e => e.ScheduleId);

            entity.ToTable("Student_Schedule");

            entity.Property(e => e.ScheduleId)
                .HasColumnName("schedule_id");

            entity.Property(e => e.StudentId)
                .HasColumnName("student_id");

            entity.Property(e => e.Day)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("day");

            entity.Property(e => e.Time)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("time");

            // =========================================
            // FIXED MAPPINGS
            // =========================================
            entity.Property(e => e.StartDate)
                  .HasColumnType("date")
                  .HasColumnName("start_date");

            entity.Property(e => e.EndDate)
                  .HasColumnType("date")
                  .HasColumnName("end_date");

            entity.Property(e => e.Type)
                  .HasMaxLength(50)
                  .IsUnicode(false)
                  .HasColumnName("type");

            entity.HasOne(d => d.Student)
                .WithMany(p => p.StudentSchedules)
                .HasForeignKey(d => d.StudentId)
                .HasConstraintName("FK_StudentSchedule_Student");
        });

        modelBuilder.Entity<StudentCourse>(entity =>
        {
            entity.HasKey(e => new { e.StudentId, e.CourseId });

            entity.ToTable("Student_Course");

            entity.Property(e => e.StudentId).HasColumnName("student_id");
            entity.Property(e => e.CourseId).HasColumnName("course_id");

            entity.HasOne(d => d.Student)
                .WithMany(p => p.StudentCourses)
                .HasForeignKey(d => d.StudentId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_StudentCourse_Student");

            entity.HasOne(d => d.Course)
                .WithMany(p => p.StudentCourses)
                .HasForeignKey(d => d.CourseId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_StudentCourse_Course");
        });


        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
