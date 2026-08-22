using System;
using System.Collections.Generic;

namespace HouseofTutorAPI.Models;

public partial class Course
{
    public int CourseId { get; set; }

    public string? CourseTitle { get; set; }

    public decimal? AdminSetMinHourlyRate { get; set; }

    public decimal? AdminSetMaxHourlyRate { get; set; }

    public virtual ICollection<Feedback> Feedbacks { get; set; } = new List<Feedback>();

    public virtual ICollection<Request> Requests { get; set; } = new List<Request>();

    public virtual ICollection<StudentCourseFee> StudentCourseFees { get; set; } = new List<StudentCourseFee>();

    public virtual ICollection<StudentCourse> StudentCourses { get; set; } = new List<StudentCourse>();

    public virtual ICollection<TutorCourseRate> TutorCourseRates { get; set; } = new List<TutorCourseRate>();

    public virtual ICollection<TutorCourse> TutorCourses { get; set; } = new List<TutorCourse>();
}
