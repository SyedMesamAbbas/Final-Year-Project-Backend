using System;
using System.Collections.Generic;

namespace HouseofTutorAPI.Models;

public partial class Payment
{
    public int PaymentId { get; set; }

    public int? FeeId { get; set; }

    public decimal? Amount { get; set; }

    public string? PaymentType { get; set; }

    public DateTime? PaymentDate { get; set; }

    public string? ParentStatus { get; set; }

    public string? TutorStatus { get; set; }

    public string? Remarks { get; set; }

    public virtual StudentCourseFee? Fee { get; set; }
}
