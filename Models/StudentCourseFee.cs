using System;
using System.Collections.Generic;

namespace HouseofTutorAPI.Models;

public partial class StudentCourseFee
{
    public int FeeId { get; set; }

    public int StudentId { get; set; }

    public int TutorId { get; set; }

    public int CourseId { get; set; }

    public decimal TotalFee { get; set; }

    public DateTime? CreatedDate { get; set; }

    public string FeeResponsibility { get; set; } = null!;

    public virtual Course Course { get; set; } = null!;

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();

    public virtual Student Student { get; set; } = null!;

    public virtual Tutor Tutor { get; set; } = null!;
}
