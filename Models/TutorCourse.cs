using System;
using System.Collections.Generic;

namespace HouseofTutorAPI.Models;

public partial class TutorCourse
{
    public int TutorId { get; set; }

    public int CourseId { get; set; }

    public string? Grade { get; set; }

    public virtual Course Course { get; set; } = null!;

    public virtual Tutor Tutor { get; set; } = null!;
}
