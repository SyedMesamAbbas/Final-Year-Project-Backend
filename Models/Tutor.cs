using System;
using System.Collections.Generic;

namespace HouseofTutorAPI.Models;

public partial class Tutor
{
    public int TutorId { get; set; }

    public int? UserId { get; set; }

    public string? Qualification { get; set; }

    public int? Experience { get; set; }

    public string? Location { get; set; }
     
    public double? Longitude { get; set; }
    public double? Latitude { get; set; }

    public int? Radius { get; set; }

    public string? Status { get; set; }

    public virtual ICollection<Feedback> Feedbacks { get; set; } = new List<Feedback>();

    public virtual ICollection<Request> Requests { get; set; } = new List<Request>();

    public virtual ICollection<Schedule> Schedules { get; set; } = new List<Schedule>();

    public virtual ICollection<TutorCourse> TutorCourses { get; set; } = new List<TutorCourse>();

    public virtual User? User { get; set; }
}
