using System;
using System.Collections.Generic;

namespace HouseofTutorAPI.Models;

public partial class LtRoomSchedule
{
    public int LtScheduleId { get; set; }

    public int LtRoomId { get; set; }

    public string Day { get; set; } = null!;

    public string Time { get; set; } = null!;

    public DateOnly? StartDate { get; set; }

    public DateOnly? EndDate { get; set; }

    public string Status { get; set; } = null!;

    public DateTime? CreatedDate { get; set; }

    public virtual LtRoom LtRoom { get; set; } = null!;
}
