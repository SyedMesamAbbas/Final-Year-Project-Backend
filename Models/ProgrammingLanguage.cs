using System;
using System.Collections.Generic;

namespace HouseofTutorAPI.Models;

public partial class ProgrammingLanguage
{
    public int LanguageId { get; set; }

    public string LanguageName { get; set; } = null!;

    public bool Status { get; set; }

    public virtual ICollection<StudentCourse> StudentCourses { get; set; } = new List<StudentCourse>();

    public virtual ICollection<TutorCourse> TutorCourses { get; set; } = new List<TutorCourse>();
    public virtual ICollection<TutorCourseProgrammingLanguage> TutorCourseProgrammingLanguages { get; set; } = new List<TutorCourseProgrammingLanguage>();
}
