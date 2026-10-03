using System;
using System.Collections.Generic;

namespace HouseofTutorAPI.Models;

public partial class LtRoom
{
    public int LtRoomId { get; set; }

    public string RoomName { get; set; } = null!;

    public int Capacity { get; set; }

    public string Status { get; set; } = null!;

    public DateTime? CreatedDate { get; set; }

    public virtual ICollection<LtRoomBooking> LtRoomBookings { get; set; } = new List<LtRoomBooking>();

    public virtual ICollection<LtRoomSchedule> LtRoomSchedules { get; set; } = new List<LtRoomSchedule>();
}
