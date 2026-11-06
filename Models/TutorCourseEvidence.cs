using System;
using System.Collections.Generic;

namespace HouseofTutorAPI.Models;

public partial class TutorCourseEvidence
{
    public int EvidenceId { get; set; }

    public int TutorId { get; set; }

    public int CourseId { get; set; }

    public string EvidenceType { get; set; } = null!;

    public string FileName { get; set; } = null!;

    public string FilePath { get; set; } = null!;

    public DateTime? UploadedDate { get; set; }

    public string VerificationStatus { get; set; } = null!;

    public int? VerifiedBy { get; set; }

    public DateTime? VerifiedDate { get; set; }

    public string? Remarks { get; set; }

    public virtual TutorCourse TutorCourse { get; set; } = null!;

    public virtual User? VerifiedByNavigation { get; set; }
}
