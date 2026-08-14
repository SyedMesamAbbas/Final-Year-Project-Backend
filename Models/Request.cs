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

    public DateOnly? ClassDate { get; set; }

    public string? Day { get; set; }

    public string? RequestType { get; set; }

    public int? ParentRequestId { get; set; }

    public string? LearningMode { get; set; }          // FullTime / SpecificTime

    public int? LearningDuration { get; set; }         // e.g. 2

    public string? LearningDurationUnit { get; set; }  // Days / Weeks / Months

    public virtual Course? Course { get; set; }

    public virtual Student? Student { get; set; }

    public virtual Tutor? Tutor { get; set; }
}
