using System;
using System.Collections.Generic;

namespace HouseofTutorAPI.Models;

public partial class StudentFriend
{
    public int FriendshipId { get; set; }

    public int StudentId { get; set; }

    public int FriendStudentId { get; set; }

    public string Status { get; set; } = null!;

    public DateTime? RequestedDate { get; set; }

    public DateTime? AcceptedDate { get; set; }

    public virtual Student FriendStudent { get; set; } = null!;

    public virtual Student Student { get; set; } = null!;
}
