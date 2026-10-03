using System;
using System.Collections.Generic;

namespace HouseofTutorAPI.Models;

public partial class LtRoomBooking
{
    public int LtBookingId { get; set; }

    public int LtRoomId { get; set; }

    public int TutorId { get; set; }

    public int CourseId { get; set; }

    public int? StudyGroupId { get; set; }

    public string Day { get; set; } = null!;

    public TimeOnly StartTime { get; set; }

    public TimeOnly EndTime { get; set; }

    public DateOnly? ClassDate { get; set; }

    public string Status { get; set; } = null!;

    public DateTime? CreatedDate { get; set; }

    public virtual Course Course { get; set; } = null!;

    public virtual LtRoom LtRoom { get; set; } = null!;

    public virtual StudyGroup? StudyGroup { get; set; }

    public virtual Tutor Tutor { get; set; } = null!;
}
