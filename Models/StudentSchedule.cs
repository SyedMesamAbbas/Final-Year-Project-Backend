using System;
using System.Collections.Generic;

namespace HouseofTutorAPI.Models;

public partial class StudentSchedule
{
    public int ScheduleId { get; set; }

    public int? StudentId { get; set; }

    public string? Day { get; set; }

    public string? Time { get; set; }

    public DateOnly? StartDate { get; set; }

    public DateOnly? EndDate { get; set; }

    public string? Type { get; set; }

    public virtual Student? Student { get; set; }
}
