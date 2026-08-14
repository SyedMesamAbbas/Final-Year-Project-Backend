using HouseofTutorAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace HouseofTutorAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ParentController : ControllerBase
    {
        private readonly HouseofTutorContext db;

        public ParentController(HouseofTutorContext _db)
        {
            db = _db;
        }
        private async Task<Student?> VerifyChild(int studentId)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userIdClaim))
                return null;

            int userId = int.Parse(userIdClaim);

            var parent = await db.Users
                .FirstOrDefaultAsync(x => x.UserId == userId);

            if (parent == null)
                return null;

            return await db.Students.FirstOrDefaultAsync(x =>
                x.StudentId == studentId &&
                x.FatherCnic == parent.Cnic);
        }

        // GET ALL CHILDREN OF LOGGED-IN PARENT
        [HttpGet("my-children")]
        public async Task<IActionResult> GetMyChildren()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrEmpty(userIdClaim))
                    return Unauthorized(new
                    {
                        message = "User not logged in."
                    });

                int userId = int.Parse(userIdClaim);

                var parent = await db.Users
                    .FirstOrDefaultAsync(x => x.UserId == userId);

                if (parent == null)
                {
                    return NotFound(new
                    {
                        message = "Parent not found."
                    });
                }

                if (string.IsNullOrWhiteSpace(parent.Cnic))
                {
                    return BadRequest(new
                    {
                        message = "Parent CNIC not found."
                    });
                }

                var children = await (
                    from s in db.Students
                    join u in db.Users
                        on s.UserId equals u.UserId
                    where s.FatherCnic == parent.Cnic
                    select new
                    {
                        s.StudentId,
                        u.UserId,
                        u.FullName,
                        u.Email,
                        u.Phone,
                        u.Cnic,
                        s.Location,
                        s.Latitude,
                        s.Longitude
                    }).ToListAsync();

                return Ok(children);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = ex.Message
                });
            }
        }

        // GET CHILD PROFILE
        [HttpGet("child-profile/{studentId}")]
        public async Task<IActionResult> ChildProfile(int studentId)
        {
            try
            {
                var verify = await VerifyChild(studentId);

                if (verify == null)
                {
                    return Unauthorized(new
                    {
                        message = "This student does not belong to you."
                    });
                }

                var child = await (
                    from s in db.Students
                    join u in db.Users
                        on s.UserId equals u.UserId
                    where s.StudentId == studentId
                    select new
                    {
                        s.StudentId,
                        u.UserId,
                        u.FullName,
                        u.Email,
                        u.Phone,
                        u.Cnic,
                        s.Location,
                        s.Latitude,
                        s.Longitude
                    }).FirstOrDefaultAsync();

                if (child == null)
                {
                    return NotFound(new
                    {
                        message = "Student not found."
                    });
                }

                return Ok(child);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = ex.Message
                });
            }
        }

        // GET CHILD COURSES
        [HttpGet("child-courses/{studentId}")]
        public async Task<IActionResult> ChildCourses(int studentId)
        {
            try
            {
                var verify = await VerifyChild(studentId);

                if (verify == null)
                {
                    return Unauthorized(new
                    {
                        message = "This student does not belong to you."
                    });
                }

                var courses = await (
                    from sc in db.StudentCourses
                    join c in db.Courses
                        on sc.CourseId equals c.CourseId
                    where sc.StudentId == studentId
                    select new
                    {
                        c.CourseId,
                        c.CourseTitle
                    }).ToListAsync();

                return Ok(courses);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = ex.Message
                });
            }
        }


        [HttpGet("child-classes/{studentId}/{courseId}")]
        public async Task<IActionResult> ChildClasses(int studentId, int courseId)
        {
            try
            {
                var verify = await VerifyChild(studentId);

                if (verify == null)
                {
                    return Unauthorized(new
                    {
                        message = "This student does not belong to you."
                    });
                }

                var classes = await (
                    from r in db.Requests
                    join t in db.Tutors
                        on r.TutorId equals t.TutorId
                    join u in db.Users
                        on t.UserId equals u.UserId
                    join c in db.Courses
                        on r.CourseId equals c.CourseId

                    where r.StudentId == studentId
                       && r.CourseId == courseId      // <-- Filter by course

                    orderby r.ClassDate descending,
                            r.Day,
                            r.Time

                    select new ParentChildClassesDto
                    {
                        RequestId = r.RequestId,
                        TutorId = t.TutorId,
                        TutorName = u.FullName,
                        TutorEmail = u.Email,
                        TutorPhone = u.Phone,

                        CourseId = c.CourseId,
                        CourseName = c.CourseTitle,

                        RequestDate = r.RequestDate,
                        Status = r.Status,
                        Day = r.Day,
                        Time = r.Time,
                        ClassDate = r.ClassDate,
                        RequestType = r.RequestType,
                        ParentRequestId = r.ParentRequestId
                    }

                ).ToListAsync();

                return Ok(classes);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = ex.Message
                });
            }
        }

        // GET CHILD SCHEDULE
        [HttpGet("child-schedule/{studentId}")]
        public async Task<IActionResult> ChildSchedule(int studentId)
        {
            try
            {
                var verify = await VerifyChild(studentId);

                if (verify == null)
                {
                    return Unauthorized(new
                    {
                        message = "This student does not belong to you."
                    });
                }

                var schedule = await db.StudentSchedules
                    .Where(x => x.StudentId == studentId)
                    .OrderBy(x => x.Day)
                    .ThenBy(x => x.Time)
                    .Select(x => new
                    {
                        x.ScheduleId,
                        x.Day,
                        x.Time,
                        x.StartDate,
                        x.EndDate,
                        x.Type
                    })
                    .ToListAsync();

                return Ok(schedule);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = ex.Message
                });
            }
        }

        [HttpGet("child-tutors/{studentId}")]
        public async Task<IActionResult> ChildTutors(int studentId)
        {
            try
            {
                var verify = await VerifyChild(studentId);

                if (verify == null)
                {
                    return Unauthorized(new
                    {
                        message = "This student does not belong to you."
                    });
                }

                var tutors = await (
                    from r in db.Requests

                    join t in db.Tutors
                        on r.TutorId equals t.TutorId

                    join u in db.Users
                        on t.UserId equals u.UserId

                    join c in db.Courses
                        on r.CourseId equals c.CourseId

                    where r.StudentId == studentId
                    // && r.Status == "Accepted"

                    select new
                    {
                        TutorId = t.TutorId,

                        FullName = u.FullName,
                        Email = u.Email,
                        Phone = u.Phone,

                        Qualification = t.Qualification,
                        Experience = t.Experience,
                        Radius = t.Radius,

                        CourseTitle = c.CourseTitle
                    })
                    .ToListAsync();

                var result = tutors
                    .GroupBy(x => x.TutorId)
                    .Select(g => new
                    {
                        TutorId = g.Key,

                        FullName = g.First().FullName,
                        Email = g.First().Email,
                        Phone = g.First().Phone,

                        Qualification = g.First().Qualification,
                        Experience = g.First().Experience,
                        Radius = g.First().Radius,

                        CoursesTeaching = g
                            .Select(x => x.CourseTitle)
                            .Distinct()
                            .ToList()
                    })
                    .ToList();

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = ex.Message
                });
            }
        }

        // GET CHILD CLASSES
        [HttpGet("child-classes/{studentId}")]
        public async Task<IActionResult> ChildClasses(int studentId)
        {
            try
            {
                var verify = await VerifyChild(studentId);

                if (verify == null)
                {
                    return Unauthorized(new
                    {
                        message = "This student does not belong to you."
                    });
                }

                var classes = await (
                    from r in db.Requests
                    join t in db.Tutors
                        on r.TutorId equals t.TutorId
                    join u in db.Users
                        on t.UserId equals u.UserId
                    join c in db.Courses
                        on r.CourseId equals c.CourseId
                    where r.StudentId == studentId
                    orderby r.ClassDate descending, r.Day ,r.Time
                    select new ParentChildClassesDto
                    {
                        RequestId = r.RequestId,
                        TutorId = t.TutorId,
                        TutorName = u.FullName,
                        TutorEmail = u.Email,
                        TutorPhone = u.Phone,
                        CourseId = c.CourseId,
                        CourseName = c.CourseTitle,
                        RequestDate = r.RequestDate,
                        Status = r.Status,
                        Day = r.Day,
                        Time = r.Time,
                        ClassDate = r.ClassDate,
                        RequestType = r.RequestType,
                        ParentRequestId = r.ParentRequestId
                    }

                ).ToListAsync();

                return Ok(classes);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = ex.Message
                });
            }
        }

        //[Authorize] Only get total fee
        //[HttpGet("child-fee/{studentId}")]
        //public IActionResult GetChildFee(int studentId)
        //{
        //    int userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

        //    var parent = db.Users.FirstOrDefault(x => x.UserId == userId);

        //    if (parent == null)
        //    {
        //        return Unauthorized();
        //    }

        //    var student = db.Students
        //        .Include(s => s.User)
        //        .FirstOrDefault(s =>
        //            s.StudentId == studentId &&
        //            s.FatherCnic == parent.Cnic);

        //    if (student == null)
        //    {
        //        return Unauthorized("This child does not belong to the logged-in parent.");
        //    }
        //    if (student == null)
        //        return Unauthorized();

        //    var fees = (from fee in db.StudentCourseFees
        //                join course in db.Courses
        //                on fee.CourseId equals course.CourseId

        //                join tutor in db.Tutors
        //                on fee.TutorId equals tutor.TutorId

        //                join user in db.Users
        //                on tutor.UserId equals user.UserId

        //                where fee.StudentId == studentId

        //                select new
        //                {
        //                    fee.FeeId,
        //                    fee.CourseId,
        //                    Course = course.CourseTitle,
        //                    Tutor = user.FullName,
        //                    TotalFee = fee.TotalFee,

        //                    Paid = db.Payments
        //                        .Where(x => x.FeeId == fee.FeeId &&
        //                                    x.TutorStatus == "Received")
        //                        .Sum(x => (decimal?)x.Amount) ?? 0,

        //                    Remaining =
        //                        fee.TotalFee -
        //                        (db.Payments
        //                        .Where(x => x.FeeId == fee.FeeId &&
        //                                    x.TutorStatus == "Received")
        //                        .Sum(x => (decimal?)x.Amount) ?? 0)
        //                }).ToList();

        //    return Ok(fees);
        //}

        [Authorize]
        [HttpGet("children-fee")]
        public IActionResult GetChildrenFee()
        {
            try
            {
                int userId = int.Parse(
                    User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                );

                // Get logged-in parent
                var parent = db.Users
                    .FirstOrDefault(x =>
                        x.UserId == userId &&
                        x.Role == "Parent");

                if (parent == null)
                {
                    return Unauthorized("Parent not found.");
                }

                // Get all children of this parent
                var children = db.Students
                    .Include(s => s.User)
                    .Where(s => s.FatherCnic == parent.Cnic)
                    .ToList();

                if (!children.Any())
                {
                    return Ok(new
                    {
                        TotalFee = 0,
                        TotalPaid = 0,
                        TotalRemaining = 0,
                        Children = new List<object>()
                    });
                }

                var result = children.Select(student =>
                {
                    // Get all fees of this child
                    var fees = (
                        from fee in db.StudentCourseFees

                        join course in db.Courses
                            on fee.CourseId equals course.CourseId

                        join tutor in db.Tutors
                            on fee.TutorId equals tutor.TutorId

                        join tutorUser in db.Users
                            on tutor.UserId equals tutorUser.UserId

                        where fee.StudentId == student.StudentId

                        select new
                        {
                            fee.FeeId,
                            fee.CourseId,
                            Course = course.CourseTitle,
                            Tutor = tutorUser.FullName,
                            TotalFee = fee.TotalFee,

                            Paid = db.Payments
                                .Where(p =>
                                    p.FeeId == fee.FeeId &&
                                    p.TutorStatus == "Received")
                                .Sum(p => (decimal?)p.Amount) ?? 0
                        }
                    ).ToList();

                    // Course-wise remaining
                    var courseFees = fees.Select(f => new
                    {
                        f.FeeId,
                        f.CourseId,
                        f.Course,
                        f.Tutor,
                        f.TotalFee,
                        f.Paid,
                        Remaining = f.TotalFee - f.Paid
                    }).ToList();

                    // Child total
                    decimal childTotalFee = courseFees.Sum(x => x.TotalFee);
                    decimal childPaid = courseFees.Sum(x => x.Paid);
                    decimal childRemaining = courseFees.Sum(x => x.Remaining);

                    return new
                    {
                        StudentId = student.StudentId,

                        StudentName = student.User != null
                            ? student.User.FullName
                            : "Unknown",

                        TotalFee = childTotalFee,
                        TotalPaid = childPaid,
                        TotalRemaining = childRemaining,

                        Courses = courseFees
                    };
                }).ToList();

                // Parent overall totals
                decimal totalFee = result.Sum(x => x.TotalFee);
                decimal totalPaid = result.Sum(x => x.TotalPaid);
                decimal totalRemaining = result.Sum(x => x.TotalRemaining);

                return Ok(new
                {
                    TotalFee = totalFee,
                    TotalPaid = totalPaid,
                    TotalRemaining = totalRemaining,

                    Children = result
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "An error occurred while getting children fees.",
                    error = ex.Message
                });
            }
        }

        [Authorize]
        [HttpPost("send-payment")]
        public IActionResult SendPayment(PaymentRequestDto model)
        {
            var fee = db.StudentCourseFees
                .FirstOrDefault(x => x.FeeId == model.FeeId);

            if (fee == null)
                return NotFound();

            var received = db.Payments
                .Where(x => x.FeeId == model.FeeId &&
                            x.TutorStatus == "Received")
                .Sum(x => (decimal?)x.Amount) ?? 0;

            var remaining = fee.TotalFee - received;

            if (model.Amount <= 0)
                return BadRequest("Invalid Amount.");

            if (model.Amount > remaining)
                return BadRequest("Amount exceeds remaining balance.");

            var payment = new Payment
            {
                FeeId = model.FeeId,
                Amount = model.Amount,
                PaymentType = model.Amount == remaining ? "Full" : "Partial",
                PaymentDate = DateTime.Now,
                ParentStatus = "Sent",
                TutorStatus = "Pending"
            };

            db.Payments.Add(payment);
            db.SaveChanges();

            return Ok(new
            {
                Message = "Payment Sent Successfully."
            });
        }
    }
    public class ParentChildClassesDto
    {
        public int RequestId { get; set; }
        public int TutorId { get; set; }
        public string TutorName { get; set; }
        public string TutorEmail { get; set; }
        public string TutorPhone { get; set; }
        public int CourseId { get; set; }
        public string CourseName { get; set; }
        public DateTime? RequestDate { get; set; }
        public string Status { get; set; }
        public string Day { get; set; }
        public string Time { get; set; }
        public DateOnly? ClassDate { get; set; }
        public string RequestType { get; set; }
        public int? ParentRequestId { get; set; }
    }

    public class PaymentRequestDto
    {
        public int FeeId { get; set; }
        public decimal Amount { get; set; }
    }
}

