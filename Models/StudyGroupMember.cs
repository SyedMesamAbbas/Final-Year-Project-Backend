using System;
using System.Collections.Generic;

namespace HouseofTutorAPI.Models;

public partial class StudyGroupMember
{
    public int GroupMemberId { get; set; }

    public int StudyGroupId { get; set; }

    public int StudentId { get; set; }

    public DateTime? JoinedDate { get; set; }

    public string Status { get; set; } = null!;

    public virtual Student Student { get; set; } = null!;

    public virtual StudyGroup StudyGroup { get; set; } = null!;
}
