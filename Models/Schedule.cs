using System;
using System.Collections.Generic;

namespace HouseofTutorAPI.Models;

public partial class Schedule
{
    public int ScheduleId { get; set; }

    public int? TutorId { get; set; }

    public string? Day { get; set; }

    public DateTime? StartDate { get; set; }
    
    public DateTime? EndDate { get; set; }

    public string? Type { get; set; }

    public string? Time { get; set; }
    public virtual Tutor? Tutor { get; set; }
}
