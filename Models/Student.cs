using System;
using System.Collections.Generic;

namespace HouseofTutorAPI.Models;

public partial class Student
{
    public int StudentId { get; set; }

    public int? UserId { get; set; }

    public string? Status { get; set; }
    public string? Location { get; set; }

    public double? Latitude { get; set; }

    public double? Longitude { get; set; }

    public string? FatherCnic { get; set; }

    public virtual ICollection<Feedback> Feedbacks { get; set; } = new List<Feedback>();

    public virtual ICollection<Request> Requests { get; set; } = new List<Request>();

    public virtual ICollection<StudentCourseFee> StudentCourseFees { get; set; } = new List<StudentCourseFee>();

    public virtual ICollection<StudentSchedule> StudentSchedules { get; set; } = new List<StudentSchedule>();

    public virtual User? User { get; set; }
}
