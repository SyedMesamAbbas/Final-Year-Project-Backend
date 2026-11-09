using System;
using System.Collections.Generic;

namespace HouseofTutorAPI.Models;

public partial class CoursePlanSession
{
    public int SessionId { get; set; }

    public int PlanId { get; set; }

    public int ClassNumber { get; set; }

    public int WeekNumber { get; set; }

    public DateOnly ClassDate { get; set; }

    public TimeOnly StartTime { get; set; }

    public TimeOnly EndTime { get; set; }

    public string Status { get; set; } = null!;

    public DateTime? CreatedDate { get; set; }

    public virtual RequestCoursePlan Plan { get; set; } = null!;
}
