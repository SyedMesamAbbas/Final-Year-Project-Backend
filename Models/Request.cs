using System;
using System.Collections.Generic;

namespace HouseofTutorAPI.Models;

public partial class Request
{
    public int RequestId { get; set; }

    public int? StudentId { get; set; }

    public int? TutorId { get; set; }

    public int? CourseId { get; set; }

    public DateTime? RequestDate { get; set; }

    public string? Status { get; set; }
    public string? Time { get; set; }

    public virtual Course? Course { get; set; }

    public virtual Student? Student { get; set; }

    public virtual Tutor? Tutor { get; set; }
}
