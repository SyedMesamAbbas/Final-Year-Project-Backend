using System;
using System.Collections.Generic;

namespace HouseofTutorAPI.Models;

public partial class TutorCourse
{
    public int TutorId { get; set; }

    public int CourseId { get; set; }

    public string? Grade { get; set; }

    public bool IsCompleted { get; set; }

    public DateTime? CompletedDate { get; set; }

    public string? Institute { get; set; }

    public bool? LabCompleted { get; set; }

    public string GradeVerificationStatus { get; set; } = null!;

    public int? VerifiedBy { get; set; }

    public DateTime? VerifiedDate { get; set; }

    public string? VerificationRemarks { get; set; }

    public virtual Course Course { get; set; } = null!;

    public virtual Tutor Tutor { get; set; } = null!;

    public virtual ICollection<TutorCourseEvidence> TutorCourseEvidences { get; set; } = new List<TutorCourseEvidence>();

    public virtual User? VerifiedByNavigation { get; set; }

    public virtual ICollection<ProgrammingLanguage> Languages { get; set; } = new List<ProgrammingLanguage>();

    public virtual ICollection<TutorCourseProgrammingLanguage> TutorCourseProgrammingLanguages { get; set; } = new List<TutorCourseProgrammingLanguage>();
}
