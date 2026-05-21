using System;
using System.Collections.Generic;

namespace HouseofTutorAPI.Models;

public partial class Student
{
    public int StudentId { get; set; }

    public int? UserId { get; set; }

    public string? Location { get; set; }
    public double? Longitude { get; set; }
    public double? Latitude { get; set; }

    public virtual ICollection<Feedback> Feedbacks { get; set; } = new List<Feedback>();

    public virtual ICollection<Request> Requests { get; set; } = new List<Request>();
    public virtual ICollection<StudentCourse> StudentCourses { get; set; } = new List<StudentCourse>();

    public virtual ICollection<StudentSchedule> StudentSchedules { get; set; } = new List<StudentSchedule>();

    public virtual User? User { get; set; }
}
