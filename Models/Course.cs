using System;
using System.Collections.Generic;

namespace HouseofTutorAPI.Models;

public partial class Course
{
    public int CourseId { get; set; }

    public string? CourseTitle { get; set; }

    public virtual ICollection<Feedback> Feedbacks { get; set; } = new List<Feedback>();

    public virtual ICollection<Request> Requests { get; set; } = new List<Request>();

    public virtual ICollection<TutorCourse> TutorCourses { get; set; } = new List<TutorCourse>();

    public virtual ICollection<StudentCourse> StudentCourses { get; set; } = new List<StudentCourse>();
}
