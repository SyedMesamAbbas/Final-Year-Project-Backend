using System;
using System.Collections.Generic;

namespace HouseofTutorAPI.Models;

public partial class StudentCourse
{
    public int StudentId { get; set; }

    public int CourseId { get; set; }

    public string? Grade { get; set; }

    public virtual Course Course { get; set; } = null!;
}
