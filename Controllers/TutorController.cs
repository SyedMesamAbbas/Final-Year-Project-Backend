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
    public class TutorController : ControllerBase
    {
        private readonly HouseofTutorContext db;

        public TutorController(HouseofTutorContext _db)
        {
            db = _db;
        }

        //Tutor Save Schedule
        //[Authorize]
        //[HttpPost("save-schedule")]
        //public async Task<IActionResult> SaveSchedule([FromBody] SaveScheduleDto dto)
        //{
        //    if (dto == null || dto.slots == null || dto.slots.Count == 0)
        //        return BadRequest(new { message = "No schedule data provided" });

        //    var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);

        //    if (userIdClaim == null)
        //        return Unauthorized(new { message = "Invalid token" });

        //    int userId = int.Parse(userIdClaim.Value);

        //    var tutor = await db.Tutors.FirstOrDefaultAsync(t => t.UserId == userId);

        //    if (tutor == null)
        //        return NotFound(new { message = "Tutor not found" });

        //    var newSchedules = new List<Schedule>();

        //    foreach (var slot in dto.slots)
        //    {
        //        string day = slot.day.Trim();
        //        string time = slot.time.Trim();

        //        bool exists = await db.Schedules.AnyAsync(s =>
        //            s.TutorId == tutor.TutorId &&
        //            s.Day.ToLower() == day.ToLower() &&
        //            s.Time.ToLower() == time.ToLower()
        //        );

        //        if (!exists)
        //        {
        //            newSchedules.Add(new Schedule
        //            {
        //                TutorId = tutor.TutorId,
        //                Day = day,
        //                Time = time
        //            });
        //        }
        //    }

        //    if (newSchedules.Count > 0)
        //    {
        //        await db.Schedules.AddRangeAsync(newSchedules);
        //        await db.SaveChangesAsync();
        //    }

        //    return Ok(new
        //    {
        //        message = "Schedule saved successfully",
        //        added = newSchedules.Count
        //    });
        //}

        //[Authorize]
        //[HttpPost("save-schedule")]
        //public async Task<IActionResult> SaveSchedule([FromBody] SaveScheduleDto dto)
        //{
        //    if (dto == null)
        //        return BadRequest(new { message = "No data provided" });

        //    var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);

        //    if (userIdClaim == null)
        //        return Unauthorized(new { message = "Invalid token" });

        //    int userId = int.Parse(userIdClaim.Value);

        //    var tutor = await db.Tutors
        //        .FirstOrDefaultAsync(t => t.UserId == userId);

        //    if (tutor == null)
        //        return NotFound(new { message = "Tutor not found" });

        //    var newSchedules = new List<Schedule>();


        //    // =========================================
        //    // TEACH FOR SPECIFIC TIME
        //    // =========================================
        //    if (dto.TeachType == "specific")
        //    {
        //        if (dto.StartDate == null || dto.EndDate == null)
        //        {
        //            return BadRequest(new
        //            {
        //                message = "Start date and End date required"
        //            });
        //        }

        //        var schedule = new Schedule
        //        {
        //            TutorId = tutor.TutorId,
        //            Day = "Specific",
        //            Time = "Full Day",
        //            StartDate = dto.StartDate,
        //            EndDate = dto.EndDate
        //        };

        //        newSchedules.Add(schedule);
        //    }


        //    // =========================================
        //    // FULL TIME SCHEDULE
        //    // =========================================
        //    else
        //    {
        //        if (dto.Slots == null || dto.Slots.Count == 0)
        //        {
        //            return BadRequest(new
        //            {
        //                message = "Please select schedule slots"
        //            });
        //        }

        //        foreach (var slot in dto.Slots)
        //        {
        //            string day = slot.Day.Trim();
        //            string time = slot.Time.Trim();

        //            bool exists = await db.Schedules.AnyAsync(s =>
        //                s.TutorId == tutor.TutorId &&
        //                s.Day.ToLower() == day.ToLower() &&
        //                s.Time.ToLower() == time.ToLower()
        //            );

        //            if (!exists)
        //            {
        //                newSchedules.Add(new Schedule
        //                {
        //                    TutorId = tutor.TutorId,
        //                    Day = day,
        //                    Time = time,
        //                    StartDate = null,
        //                    EndDate = null
        //                });
        //            }
        //        }
        //    }

        //    // SAVE
        //    if (newSchedules.Count > 0)
        //    {
        //        await db.Schedules.AddRangeAsync(newSchedules);
        //        await db.SaveChangesAsync();
        //    }

        //    return Ok(new
        //    {
        //        message = "Schedule saved successfully",
        //        added = newSchedules.Count
        //    });
        //}

        //[Authorize]
        //[HttpPost("save-schedule")]
        //public async Task<IActionResult> SaveSchedule([FromBody] SaveScheduleDto dto)
        //{
        //    if (dto == null)
        //        return BadRequest(new { message = "No data provided" });

        //    var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);

        //    if (userIdClaim == null)
        //        return Unauthorized(new { message = "Invalid token" });

        //    int userId = int.Parse(userIdClaim.Value);

        //    var tutor = await db.Tutors
        //        .FirstOrDefaultAsync(t => t.UserId == userId);

        //    if (tutor == null)
        //        return NotFound(new { message = "Tutor not found" });

        //    var newSchedules = new List<Schedule>();


        //    // ====================================================
        //    // TEACH FOR SPECIFIC TIME  => SHORT TIME
        //    // Save actual Day + Time selected by tutor
        //    // ====================================================
        //    if (dto.TeachType?.ToLower() == "specific")
        //    {
        //        if (dto.StartDate == null || dto.EndDate == null)
        //        {
        //            return BadRequest(new
        //            {
        //                message = "Start date and End date required"
        //            });
        //        }

        //        if (dto.Slots == null || dto.Slots.Count == 0)
        //        {
        //            return BadRequest(new
        //            {
        //                message = "Please select schedule slots"
        //            });
        //        }

        //        foreach (var slot in dto.Slots)
        //        {
        //            string day = slot.Day.Trim();
        //            string time = slot.Time.Trim();

        //            bool exists = await db.Schedules.AnyAsync(s =>
        //                s.TutorId == tutor.TutorId &&
        //                s.Day.ToLower() == day.ToLower() &&
        //                s.Time.ToLower() == time.ToLower() &&
        //                s.Type.ToLower() == "short time"
        //            );

        //            if (!exists)
        //            {
        //                newSchedules.Add(new Schedule
        //                {
        //                    TutorId = tutor.TutorId,
        //                    Day = day,
        //                    Time = time,
        //                    StartDate = dto.StartDate,
        //                    EndDate = dto.EndDate,
        //                    Type = "Short Time"
        //                });
        //            }
        //        }
        //    }


        //    // ====================================================
        //    // FULL TIME / PART TIME => LONG TIME
        //    // ====================================================
        //    else
        //    {
        //        if (dto.Slots == null || dto.Slots.Count == 0)
        //        {
        //            return BadRequest(new
        //            {
        //                message = "Please select schedule slots"
        //            });
        //        }

        //        foreach (var slot in dto.Slots)
        //        {
        //            string day = slot.Day.Trim();
        //            string time = slot.Time.Trim();

        //            bool exists = await db.Schedules.AnyAsync(s =>
        //                s.TutorId == tutor.TutorId &&
        //                s.Day.ToLower() == day.ToLower() &&
        //                s.Time.ToLower() == time.ToLower() &&
        //                s.Type.ToLower() == "long time"
        //            );

        //            if (!exists)
        //            {
        //                newSchedules.Add(new Schedule
        //                {
        //                    TutorId = tutor.TutorId,
        //                    Day = day,
        //                    Time = time,
        //                    StartDate = null,
        //                    EndDate = null,
        //                    Type = "Long Time"
        //                });
        //            }
        //        }
        //    }
        //    // SAVE
        //    if (newSchedules.Count > 0)
        //    {
        //        await db.Schedules.AddRangeAsync(newSchedules);
        //        await db.SaveChangesAsync();
        //    }

        //    return Ok(new
        //    {
        //        message = "Schedule saved successfully",
        //        added = newSchedules.Count
        //    });
        //}
        [Authorize]
        [HttpPost("save-schedule")]
        public async Task<IActionResult> SaveSchedule([FromBody] SaveScheduleDto dto)
        {
            if (dto == null)
            {
                return BadRequest(new
                {
                    message = "No data provided"
                });
            }

            var userIdClaim =
                User.FindFirst(ClaimTypes.NameIdentifier);

            if (userIdClaim == null)
            {
                return Unauthorized(new
                {
                    message = "Invalid token"
                });
            }

            int userId = int.Parse(userIdClaim.Value);

            var tutor = await db.Tutors
                .FirstOrDefaultAsync(t =>
                    t.UserId == userId);

            if (tutor == null)
            {
                return NotFound(new
                {
                    message = "Tutor not found"
                });
            }

            // ====================================================
            // REMOVE OLD SCHEDULES
            // ====================================================
            var oldSchedules = await db.Schedules
                .Where(s => s.TutorId == tutor.TutorId)
                .ToListAsync();

            if (oldSchedules.Any())
            {
                db.Schedules.RemoveRange(oldSchedules);
                await db.SaveChangesAsync();
            }

            // ====================================================
            // VALIDATION
            // ====================================================
            if (dto.Slots == null || dto.Slots.Count == 0)
            {
                return BadRequest(new
                {
                    message = "Please select schedule slots"
                });
            }

            var newSchedules = new List<Schedule>();

            // ====================================================
            // SHORT TIME (SPECIFIC)
            // ====================================================
            if (dto.TeachType?.ToLower() == "specific")
            {
                if (dto.StartDate == null || dto.EndDate == null)
                {
                    return BadRequest(new
                    {
                        message = "Start date and End date required"
                    });
                }

                foreach (var slot in dto.Slots)
                {
                    newSchedules.Add(new Schedule
                    {
                        TutorId = tutor.TutorId,

                        Day = slot.Day.Trim(),
                        Time = slot.Time.Trim(),

                        StartDate = dto.StartDate,
                        EndDate = dto.EndDate,

                        Type = "Short Time"
                    });
                }
            }

            // ====================================================
            // LONG TIME
            // ====================================================
            else
            {
                foreach (var slot in dto.Slots)
                {
                    newSchedules.Add(new Schedule
                    {
                        TutorId = tutor.TutorId,

                        Day = slot.Day.Trim(),
                        Time = slot.Time.Trim(),

                        StartDate = null,
                        EndDate = null,

                        Type = "Long Time"
                    });
                }
            }

            // ====================================================
            // SAVE NEW SCHEDULE
            // ====================================================
            await db.Schedules.AddRangeAsync(newSchedules);

            await db.SaveChangesAsync();

            return Ok(new
            {
                message = "Schedule updated successfully",
                total = newSchedules.Count
            });
        }

        [Authorize]
        [HttpGet("get-schedule")]
        public async Task<IActionResult> GetSchedule()
        {
            var userIdClaim =
                User.FindFirst(ClaimTypes.NameIdentifier);

            if (userIdClaim == null)
            {
                return Unauthorized(new
                {
                    message = "Invalid token"
                });
            }

            int userId = int.Parse(userIdClaim.Value);

            var tutor = await db.Tutors
                .FirstOrDefaultAsync(t =>
                    t.UserId == userId);

            if (tutor == null)
            {
                return NotFound(new
                {
                    message = "Tutor not found"
                });
            }

            var schedules = await db.Schedules
                .Where(s => s.TutorId == tutor.TutorId)
                .Select(s => new
                {
                    schedule_id = s.ScheduleId,
                    day = s.Day,
                    time = s.Time,
                    start_date = s.StartDate,
                    end_date = s.EndDate,
                    type = s.Type
                })
                .OrderBy(s => s.day)
                .ToListAsync();

            return Ok(schedules);
        }

        //Tutor get Student Classes Request which are pending only
        //[Authorize]
        //[HttpGet("my-requests")]
        //public async Task<IActionResult> GetTutorRequests()
        //{
        //    var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);

        //    if (userIdClaim == null)
        //        return Unauthorized(new { message = "Invalid token" });

        //    int userId = int.Parse(userIdClaim.Value);

        //    var tutor = await db.Tutors
        //        .FirstOrDefaultAsync(t => t.UserId == userId);

        //    if (tutor == null)
        //        return NotFound(new { message = "Tutor not found" });

        //    var requests = await db.Requests
        //        .Where(r => r.TutorId == tutor.TutorId && r.Status == "Pending")
        //        .Include(r => r.Student)
        //            .ThenInclude(s => s.User)
        //        .Include(r => r.Course)
        //        .OrderByDescending(r => r.RequestDate)
        //        .Select(r => new
        //        {
        //            request_id = r.RequestId,
        //            student_name = r.Student.User.FullName,
        //            course_name = r.Course.CourseTitle,
        //            request_date = r.RequestDate,
        //            status = r.Status
        //        })
        //        .ToListAsync();

        //    return Ok(requests);
        //}
        // ===============================
        // MY REQUESTS API
        // ===============================

        [Authorize]
        [HttpGet("my-requests")]
        public async Task<IActionResult> GetMyRequests()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);

                if (userIdClaim == null)
                {
                    return Unauthorized(new
                    {
                        message = "Invalid token"
                    });
                }

                int userId = Convert.ToInt32(userIdClaim.Value);

                // Find Tutor
                var tutor = await db.Tutors
                    .FirstOrDefaultAsync(t => t.UserId == userId);

                if (tutor == null)
                {
                    return NotFound(new
                    {
                        message = "Tutor not found"
                    });
                }

                // Fetch Requests
                var requests = await db.Requests
                    .Include(r => r.Student)
                        .ThenInclude(s => s.User)
                    .Include(r => r.Course)
                    .Where(r =>
                        r.TutorId == tutor.TutorId &&
                        r.Status == "Pending"
                    )
                    .Select(r => new
                    {
                        request_id = r.RequestId,

                        // IMPORTANT
                        // This StudentId is used in StudentProfile API
                        student_id = r.Student.StudentId,

                        student_name = r.Student.User.FullName,

                        course_name = r.Course.CourseTitle,

                        request_date = r.RequestDate
                    })
                    .ToListAsync();

                return Ok(requests);
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    message = ex.Message
                });
            }
        }

        //Tutor accept student classes request
        [Authorize]
        [HttpPut("accept-request/{id}")]
        public async Task<IActionResult> AcceptRequest(int id)
        {
            var request = await db.Requests.FindAsync(id);

            if (request == null)
                return NotFound(new { message = "Request not found" });

            request.Status = "Accepted";

            await db.SaveChangesAsync();

            return Ok(new { message = "Request accepted" });
        }

        //Tutor reject student classes request
        [Authorize]
        [HttpPut("reject-request/{id}")]
        public async Task<IActionResult> RejectRequest(int id)
        {
            var request = await db.Requests.FindAsync(id);

            if (request == null)
                return NotFound(new { message = "Request not found" });

            request.Status = "Rejected";

            await db.SaveChangesAsync();

            return Ok(new { message = "Request rejected" });
        }

        //Tutor get today classes
        [Authorize]
        [HttpGet("today-classes")]
        public async Task<IActionResult> GetTodayClasses()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);

            if (userIdClaim == null)
                return Unauthorized(new { message = "Invalid token" });

            int userId = int.Parse(userIdClaim.Value);

            var tutor = await db.Tutors
                .FirstOrDefaultAsync(t => t.UserId == userId);

            if (tutor == null)
                return NotFound(new { message = "Tutor not found" });

            DateTime today = DateTime.Today;

            var classes = await db.Requests
                .Where(r =>
                    r.TutorId == tutor.TutorId &&
                    r.Status == "Accepted" &&
                    r.RequestDate.HasValue &&
                    r.RequestDate.Value.Date == today
                )
                .Include(r => r.Student)
                    .ThenInclude(s => s.User)
                .Include(r => r.Course)
                .OrderByDescending(r => r.RequestDate)
                .Select(r => new TodayClassDto
                {
                    request_id = r.RequestId,
                    student_name = r.Student.User.FullName,
                    course_name = r.Course.CourseTitle,
                    time = r.Time ?? "Not Set",

                    date = r.RequestDate.Value.ToString("yyyy-MM-dd")
                })
                .ToListAsync();

            return Ok(classes);
        }

        //Get All Courses from Courses table on plus(+) icon
        [HttpGet("all-courses")]
        public async Task<IActionResult> GetAllCourses()
        {
            var courses = await db.Courses
                .Select(c => new CourseDto
                {
                     course_id = c.CourseId,
                    course_name = c.CourseTitle
                })
                .ToListAsync();

            return Ok(courses);
        }

        //Add Cousres in Tutor_Course from courses table
        [Authorize]
        [HttpPost("add-courses")]
        public async Task<IActionResult> AddTutorCourses([FromBody] AddTutorCoursesDto dto)
        {
            if (dto == null || dto.courseIds == null || dto.courseIds.Count == 0)
                return BadRequest(new { message = "No courses selected" });
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);

            if (userIdClaim == null)
                return Unauthorized(new { message = "Invalid token" });

            int userId = int.Parse(userIdClaim.Value);

            var tutor = await db.Tutors
                .FirstOrDefaultAsync(t => t.UserId == userId);

            if (tutor == null)
                return NotFound(new { message = "Tutor not found" });

            var newEntries = new List<TutorCourse>();

            foreach (var courseId in dto.courseIds)
            {
                bool exists = await db.TutorCourses.AnyAsync(tc =>
                    tc.TutorId == tutor.TutorId &&
                    tc.CourseId == courseId
                );

                if (!exists)
                {
                    newEntries.Add(new TutorCourse
                    {
                        TutorId = tutor.TutorId,
                        CourseId = courseId
                    });
                }
            }

            if (newEntries.Count > 0)
            {
                await db.TutorCourses.AddRangeAsync(newEntries);
                await db.SaveChangesAsync();
            }

            return Ok(new
            {
                message = "Courses added successfully",
                added = newEntries.Count
            });
        }

        //Get Tutor_courses that they teach
        [Authorize]
        [HttpGet("my-courses")]
        public async Task<IActionResult> GetTutorCourses()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);

            if (userIdClaim == null)
                return Unauthorized(new { message = "Invalid token" });

            int userId = int.Parse(userIdClaim.Value);

            var tutor = await db.Tutors
                .FirstOrDefaultAsync(t => t.UserId == userId);

            if (tutor == null)
                return NotFound(new { message = "Tutor not found" });

            var courses = await db.TutorCourses
                .Where(tc => tc.TutorId == tutor.TutorId)
                .Include(tc => tc.Course)
                .Select(tc => new CourseDto
                {
                    course_id = tc.Course.CourseId,
                    course_name = tc.Course.CourseTitle
                })
                .ToListAsync();

            return Ok(courses);
        }

        [Authorize]
        [HttpGet("my-profile")]
        public async Task<IActionResult> GetTutorProfile()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);

                if (userIdClaim == null)
                    return Unauthorized(new { message = "Invalid token" });

                int userId = int.Parse(userIdClaim.Value);

                var tutor = await db.Tutors
                    .Where(t => t.UserId == userId)
                    .Select(t => new
                    {
                        full_name = t.User.FullName,
                        email = t.User.Email,
                        cnic = t.User.Cnic,
                        phone = t.User.Phone,
                        experience = t.Experience,
                        qualification = t.Qualification,
                        radius = t.Radius
                    })
                    .FirstOrDefaultAsync();

                if (tutor == null)
                    return NotFound(new { message = "Tutor not found" });

                return Ok(tutor);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Internal server error",
                    error = ex.Message
                });
            }
        }

        // ===============================
        // STUDENT PROFILE API
        // ===============================

        [Authorize]
        [HttpGet("student-profile/{studentId}")]
        public async Task<IActionResult> GetStudentProfile(int studentId)
        {
            try
            {
                // Debug Check
                Console.WriteLine($"Student ID Received: {studentId}");

                var studentData = await db.Students
                    .Include(s => s.User)
                    .Where(s => s.StudentId == studentId)
                    .Select(s => new
                    {
                        student_id = s.StudentId,

                        full_name = s.User.FullName,

                        email = s.User.Email,

                        cnic = s.User.Cnic,

                        phone = s.User.Phone,

                        //gender = s.User.Gender,

                        //address = s.User.Address,

                        //profile_image = s.User.ProfileImage,

                        location = s.Location,

                        latitude = s.Latitude,

                        longitude = s.Longitude,

                        //created_at = s.User.CreatedAt
                    })
                    .FirstOrDefaultAsync();

                if (studentData == null)
                {
                    return NotFound(new
                    {
                        message = "Student not found"
                    });
                }

                return Ok(studentData);
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    message = ex.Message
                });
            }
        }
        // =========================================
        // TUTOR ALL CLASSES API
        // =========================================

        [Authorize]
        [HttpGet("all-classes")]
        public async Task<IActionResult> GetAllClasses()
        {
            try
            {
                // Get Logged In User ID
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);

                if (userIdClaim == null)
                {
                    return Unauthorized(new
                    {
                        message = "Invalid token"
                    });
                }

                int userId = Convert.ToInt32(userIdClaim.Value);

                // Find Tutor
                var tutor = await db.Tutors
                    .FirstOrDefaultAsync(t => t.UserId == userId);

                if (tutor == null)
                {
                    return NotFound(new
                    {
                        message = "Tutor not found"
                    });
                }

                // Fetch All Classes
                var classes = await db.Requests
                    .Include(r => r.Student)
                        .ThenInclude(s => s.User)
                    .Include(r => r.Course)
                    .Where(r =>
                        r.TutorId == tutor.TutorId &&
                        r.Status == "Accepted"
                    )
                    .OrderByDescending(r => r.RequestDate)
                    .Select(r => new
                    {
                        request_id = r.RequestId,

                        student_id = r.Student.StudentId,

                        student_name = r.Student.User.FullName,

                        course_name = r.Course.CourseTitle,

                        date = r.RequestDate.HasValue? r.RequestDate.Value.ToString("yyyy-MM-dd"): "N/A",

                        time = r.Time != null
                            ? r.Time
                            : "N/A",

                        status = r.Status
                    })
                    .ToListAsync();

                return Ok(classes);
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    message = ex.Message
                });
            }
        }

    }

    //Dto stand for (Data Transfer Object)
    //Handle multiple save Schedule
    //public class SaveScheduleDto
    //{
    //    public List<SlotDto> slots { get; set; }
    //}

    //public class SlotDto
    //{
    //    public string day { get; set; }
    //    public string time { get; set; }
    //}
    public class SaveScheduleDto
    {
        public string? TeachType { get; set; } 

        public DateTime? StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        public List<SlotDto> Slots { get; set; } = new();
    }

    public class SlotDto
    {
        public string Day { get; set; } = string.Empty;

        public string Time { get; set; } = string.Empty;
    }

    //See student Request
    public class TutorRequestDto
    {
        public int request_id { get; set; }
        public string student_name { get; set; }
        public string course_name { get; set; }
        public string request_date { get; set; }
        public string status { get; set; }
    }

    //Tutor today classes
    public class TodayClassDto
    {
        public int request_id { get; set; }
        public string student_name { get; set; }
        public string course_name { get; set; }
        public string time { get; set; }
        public string date { get; set; }
    }

    //Tutor Add courses
    public class AddTutorCoursesDto
    {
        public List<int> courseIds { get; set; }
    }
    public class CourseDto
    {
        public int course_id { get; set; }
        public string course_name { get; set; }
    }

    //Get All Classes of Tutor
    public class TutorClassDto
    {
        public int request_id { get; set; }
        public string student_name { get; set; }
        public string course_name { get; set; }
        public string day { get; set; }
        public string time { get; set; }
        public string date { get; set; }
    }
}
