using System;
using System.Collections.Generic;

namespace HouseofTutorAPI.Models;

public partial class StudentCourseContent
{
    public int ContentId { get; set; }

    public int StudentId { get; set; }

    public int CourseId { get; set; }

    public string Title { get; set; } = null!;

    public string? FileName { get; set; }

    public string? FilePath { get; set; }

    public string? Description { get; set; }

    public DateTime? UploadedDate { get; set; }

    public virtual Course Course { get; set; } = null!;

    public virtual Student Student { get; set; } = null!;
}
