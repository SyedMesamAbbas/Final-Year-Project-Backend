using System;
using System.Collections.Generic;

namespace HouseofTutorAPI.Models;

public partial class TutorCourseProgrammingLanguage
{
    public int TutorId { get; set; }

    public int CourseId { get; set; }

    public int LanguageId { get; set; }

    public virtual TutorCourse TutorCourse { get; set; } = null!;

    public virtual ProgrammingLanguage Language { get; set; } = null!;
}
