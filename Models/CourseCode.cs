using System;
using System.Collections.Generic;

namespace HouseofTutorAPI.Models;

public partial class CourseCode
{
    public int CourseCodeId { get; set; }

    public int CourseId { get; set; }

    public string Institute { get; set; } = null!;

    public string CourseCode1 { get; set; } = null!;

    public string? Program { get; set; }

    public string? Description { get; set; }

    public virtual Course Course { get; set; } = null!;
}
