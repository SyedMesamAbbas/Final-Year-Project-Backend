using System;
using System.Collections.Generic;

namespace HouseofTutorAPI.Models;

public partial class Course
{
    public int CourseId { get; set; }

    public string? CourseTitle { get; set; }

    public decimal? AdminSetMinHourlyRate { get; set; }

    public decimal? AdminSetMaxHourlyRate { get; set; }

    public bool HasLabComponent { get; set; }

    public int StandardDurationWeeks { get; set; }

    public int StandardClassesPerWeek { get; set; }

    public int? TotalStandardClasses { get; set; }

    public virtual ICollection<CourseCode> CourseCodes { get; set; } = new List<CourseCode>();

    public virtual ICollection<Feedback> Feedbacks { get; set; } = new List<Feedback>();

    public virtual ICollection<LtRoomBooking> LtRoomBookings { get; set; } = new List<LtRoomBooking>();

    public virtual ICollection<RequestGroup> RequestGroups { get; set; } = new List<RequestGroup>();

    public virtual ICollection<Request> Requests { get; set; } = new List<Request>();

    public virtual ICollection<StudentCourseContent> StudentCourseContents { get; set; } = new List<StudentCourseContent>();

    public virtual ICollection<StudentCourseFee> StudentCourseFees { get; set; } = new List<StudentCourseFee>();

    public virtual ICollection<StudentCourse> StudentCourses { get; set; } = new List<StudentCourse>();

    public virtual ICollection<StudyGroup> StudyGroups { get; set; } = new List<StudyGroup>();

    public virtual ICollection<TutorCourseRate> TutorCourseRates { get; set; } = new List<TutorCourseRate>();

    public virtual ICollection<TutorCourse> TutorCourses { get; set; } = new List<TutorCourse>();

    public virtual ICollection<TutorPreviousTeacher> TutorPreviousTeachers { get; set; } = new List<TutorPreviousTeacher>();
}
