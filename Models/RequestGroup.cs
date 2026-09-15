using System;
using System.Collections.Generic;

namespace HouseofTutorAPI.Models;

public partial class RequestGroup
{
    public int RequestGroupId { get; set; }

    public int StudentId { get; set; }

    public int CourseId { get; set; }

    public DateTime? CreatedDate { get; set; }

    public string Status { get; set; } = null!;

    public int? CurrentRequestId { get; set; }

    public virtual Course Course { get; set; } = null!;

    public virtual ICollection<Request> Requests { get; set; } = new List<Request>();

    public virtual Student Student { get; set; } = null!;
}
