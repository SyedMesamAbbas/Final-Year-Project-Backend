using System;
using System.Collections.Generic;

namespace HouseofTutorAPI.Models;

public partial class TutorCourseRate
{
    public int RateId { get; set; }

    public int TutorId { get; set; }

    public int CourseId { get; set; }

    public decimal HourlyRate { get; set; }

    public decimal? AdminSetMinHourlyRate { get; set; }

    public decimal? AdminSetMaxHourlyRate { get; set; }
    public virtual Course Course { get; set; } = null!;

    public virtual Tutor Tutor { get; set; } = null!;
}
