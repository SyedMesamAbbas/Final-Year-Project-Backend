using System;
using System.Collections.Generic;

namespace HouseofTutorAPI.Models;

public partial class StudyGroup
{
    public int StudyGroupId { get; set; }

    public string GroupName { get; set; } = null!;

    public int CreatedByStudentId { get; set; }

    public int CourseId { get; set; }

    public int? TutorId { get; set; }

    public int MaxStudents { get; set; }

    public string Status { get; set; } = null!;

    public DateTime? CreatedDate { get; set; }

    public virtual Course Course { get; set; } = null!;

    public virtual Student CreatedByStudent { get; set; } = null!;

    public virtual ICollection<LtRoomBooking> LtRoomBookings { get; set; } = new List<LtRoomBooking>();

    public virtual ICollection<Request> Requests { get; set; } = new List<Request>();

    public virtual ICollection<StudyGroupMember> StudyGroupMembers { get; set; } = new List<StudyGroupMember>();

    public virtual Tutor? Tutor { get; set; }
}
