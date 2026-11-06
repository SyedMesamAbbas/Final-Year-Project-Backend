using System;
using System.Collections.Generic;

namespace HouseofTutorAPI.Models;

public partial class TutorPreviousTeacher
{
    public int PreviousTeacherId { get; set; }

    public int TutorId { get; set; }

    public string TeacherName { get; set; } = null!;

    public string Institute { get; set; } = null!;

    public int? CourseId { get; set; }

    public int? StudyYear { get; set; }

    public string? Description { get; set; }

    public DateTime? CreatedDate { get; set; }

    public virtual Course? Course { get; set; }

    public virtual Tutor Tutor { get; set; } = null!;
}
