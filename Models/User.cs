using System;
using System.Collections.Generic;

namespace HouseofTutorAPI.Models;

public partial class User
{
    public int UserId { get; set; }

    public string? FullName { get; set; }

    public string? Email { get; set; }

    public string? Phone { get; set; }

    public string? Cnic { get; set; }

    public string? Password { get; set; }

    public string? Role { get; set; }

    public virtual ICollection<Student> Students { get; set; } = new List<Student>();

    public virtual ICollection<Tutor> Tutors { get; set; } = new List<Tutor>();
}
