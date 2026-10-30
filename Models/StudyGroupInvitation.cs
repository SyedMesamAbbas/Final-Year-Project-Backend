using System;
using System.Collections.Generic;

namespace HouseofTutorAPI.Models;

public partial class StudyGroupInvitation
{
    public int InvitationId { get; set; }

    public int StudyGroupId { get; set; }

    public int InvitedStudentId { get; set; }

    public int InvitedByStudentId { get; set; }

    public DateTime? InvitationDate { get; set; }

    public string Status { get; set; } = null!;

    public virtual Student InvitedByStudent { get; set; } = null!;

    public virtual Student InvitedStudent { get; set; } = null!;

    public virtual StudyGroup StudyGroup { get; set; } = null!;
}
