using System;
using System.Collections.Generic;

namespace HouseofTutorAPI.Models;

public partial class RequestCoursePlan
{
    public int PlanId { get; set; }

    public int RequestGroupId { get; set; }

    public int OriginalDurationWeeks { get; set; }

    public int OriginalClassesPerWeek { get; set; }

    public int RequestedDurationWeeks { get; set; }

    public int RequestedClassesPerWeek { get; set; }

    public int TotalClasses { get; set; }

    public string PlanType { get; set; } = null!;

    public DateOnly? StartDate { get; set; }

    public DateOnly? TargetEndDate { get; set; }

    public string Status { get; set; } = null!;

    public DateTime? CreatedDate { get; set; }

    public string? TutorRemarks { get; set; }

    public DateTime? ApprovedDate { get; set; }

    public int? ApprovedByTutorId { get; set; }

    public int? ClassDurationMinutes { get; set; }

    public virtual Tutor? ApprovedByTutor { get; set; }

    public virtual ICollection<CoursePlanSession> CoursePlanSessions { get; set; } = new List<CoursePlanSession>();

    public virtual RequestGroup RequestGroup { get; set; } = null!;
}
