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
                return Unauthorized(new { message = "Invalid token" });
            }

            int userId = int.Parse(userIdClaim.Value);

            var tutor = await db.Tutors
                .FirstOrDefaultAsync(t => t.UserId == userId);

            if (tutor == null)
            {
                return NotFound(new { message = "Tutor not found" });
            }

            // =========================================
            // FETCH SCHEDULES
            // =========================================
            var schedules = await db.Schedules
                .Where(s => s.TutorId == tutor.TutorId)
                .ToListAsync();

            // =========================================
            // FETCH ACCEPTED REQUESTS FOR THIS TUTOR
            // =========================================
            var requests = await db.Requests
                .Where(r =>
                    r.TutorId == tutor.TutorId &&
                    r.Status == "Accepted")
                .ToListAsync();

            // =========================================
            // HELPER: strip leading "Day, " prefix
            // from request Time if present.
            //
            // DB stores Time as e.g. "Fri, 10:00-11:00 am"
            // but Schedule stores "10:00-11:00 am"
            // So we normalise by dropping everything up to
            // and including the first ", " in the time string.
            // =========================================
            static string NormalizeTime(string raw)
            {
                if (string.IsNullOrWhiteSpace(raw))
                    return "";

                var trimmed = raw.Trim();

                // If the string contains ", " assume the part
                // before it is a day prefix — drop it.
                var commaIdx = trimmed.IndexOf(", ");
                if (commaIdx >= 0)
                    trimmed = trimmed.Substring(commaIdx + 2).Trim();

                return trimmed.ToLower();
            }

            // =========================================
            // BUILD RESULT WITH slot_status
            // =========================================
            var result = schedules.Select(schedule =>
            {
                var scheduleDay = (schedule.Day ?? "").Trim().ToLower();
                var scheduleTime = (schedule.Time ?? "").Trim().ToLower();

                // Match by day + normalised time
                var classRequest = requests.FirstOrDefault(r =>
                    (r.Day ?? "").Trim().ToLower() == scheduleDay
                    &&
                    NormalizeTime(r.Time) == scheduleTime
                );

                // =====================================
                // DETERMINE SLOT STATUS
                // green  = no accepted class on this slot
                // red    = Normal class booked
                // yellow = Pre/Reschedule booked
                // =====================================
                string slotStatus = "green";

                if (classRequest != null)
                {
                    var rType = (classRequest.RequestType ?? "").Trim();

                    if (
                        rType.Equals("Preschedule",
                            StringComparison.OrdinalIgnoreCase) ||
                        rType.Equals("Reschedule",
                            StringComparison.OrdinalIgnoreCase)
                    )
                    {
                        slotStatus = "yellow";
                    }
                    else
                    {
                        // "Normal" or any other accepted type → red
                        slotStatus = "red";
                    }
                }

                return new
                {
                    schedule_id = schedule.ScheduleId,
                    day = schedule.Day,
                    time = schedule.Time,
                    start_date = schedule.StartDate,
                    end_date = schedule.EndDate,
                    type = schedule.Type,
                    slot_status = slotStatus,
                    request_type = classRequest?.RequestType,
                    request_id = classRequest?.RequestId
                };
            });

            return Ok(result);
        }

        //Tutor get Student Classes Request which are pending only
        [Authorize]
        [HttpGet("my-requests")]
        public async Task<IActionResult> GetMyRequests()
        {
            try
            {
                // Get Logged In User
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);

                if (userIdClaim == null)
                {
                    return Unauthorized(new
                    {
                        success = false,
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
                        success = false,
                        message = "Tutor not found"
                    });
                }

                // Fetch Pending Requests
                var requests = await db.Requests
                    .Include(r => r.Student)
                        .ThenInclude(s => s.User)
                    .Include(r => r.Course)
                    .Where(r =>
                        r.TutorId == tutor.TutorId &&
                        r.Status == "Pending")
                    .OrderByDescending(r => r.RequestDate)
                    .Select(r => new
                    {
                        request_id = r.RequestId,
                        student_id = r.Student.StudentId,
                        student_name = r.Student.User.FullName,
                        course_id = r.CourseId,
                        course_name = r.Course.CourseTitle,
                        request_date = r.RequestDate,
                        class_date = r.ClassDate,
                        day = r.Day,
                        time = r.Time,
                        request_type = r.RequestType,
                        status = r.Status,
                        parent_request_id = r.ParentRequestId
                    })
                    .ToListAsync();

                return Ok(new
                {
                    success = true,
                    message = "Pending requests fetched successfully.",
                    data = requests
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

        //Tutor accept student classes request for nomal
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

        //Tutor reject student classes request for nomal
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

        

        //Get All Courses from Courses table on plus(+) icon
        [Authorize]
        [HttpGet("all-courses")]
        public async Task<IActionResult> GetAllCourses()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);

            if (userIdClaim == null)
                return Unauthorized(new { message = "Invalid token" });

            int userId = int.Parse(userIdClaim.Value);

            var tutor = await db.Tutors
                .FirstOrDefaultAsync(t => t.UserId == userId);

            if (tutor == null)
                return NotFound(new { message = "Tutor not found" });

            var addedCourseIds = await db.TutorCourses
                .Where(tc => tc.TutorId == tutor.TutorId)
                .Select(tc => tc.CourseId)
                .ToListAsync();

            var courses = await db.Courses
                .Where(c => !addedCourseIds.Contains(c.CourseId))
                .Select(c => new CourseDto
                {
                    course_id = c.CourseId,
                    course_name = c.CourseTitle
                })
                .ToListAsync();

            return Ok(courses);
        }

        //Add Cousres in Tutor_Course from courses table
        //[Authorize]
        //[HttpPost("add-courses")]
        //public async Task<IActionResult> AddTutorCourses([FromBody] AddTutorCoursesDto dto)
        //{
        //    if (dto == null || dto.Courses == null || dto.Courses.Count == 0)
        //        return BadRequest(new { message = "No courses selected" });

        //    var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);

        //    if (userIdClaim == null)
        //        return Unauthorized(new { message = "Invalid token" });

        //    int userId = int.Parse(userIdClaim.Value);

        //    var tutor = await db.Tutors
        //        .FirstOrDefaultAsync(t => t.UserId == userId);

        //    if (tutor == null)
        //        return NotFound(new { message = "Tutor not found" });

        //    var newEntries = new List<TutorCourse>();

        //    foreach (var item in dto.Courses)
        //    {
        //        if (string.IsNullOrWhiteSpace(item.Grade))
        //            continue;

        //        string grade = item.Grade.Trim().ToUpper();

        //        if (!new[] { "A", "B", "C", "D", "F" }.Contains(grade))
        //            continue;

        //        bool exists = await db.TutorCourses.AnyAsync(tc =>
        //            tc.TutorId == tutor.TutorId &&
        //            tc.CourseId == item.CourseId
        //        );

        //        if (!exists)
        //        {
        //            newEntries.Add(new TutorCourse
        //            {
        //                TutorId = tutor.TutorId,
        //                CourseId = item.CourseId,
        //                Grade = grade
        //            });
        //        }
        //    }

        //    if (newEntries.Count > 0)
        //    {
        //        await db.TutorCourses.AddRangeAsync(newEntries);
        //        await db.SaveChangesAsync();
        //    }

        //    return Ok(new
        //    {
        //        message = "Courses added successfully",
        //        added = newEntries.Count
        //    });
        //}


        [Authorize]
        [HttpPost("add-courses")]
        public async Task<IActionResult> AddTutorCourses([FromBody] AddTutorCoursesDto dto)
        {
            if (dto == null || dto.Courses == null || dto.Courses.Count == 0)
                return BadRequest(new { message = "No courses selected." });

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);

            if (userIdClaim == null)
                return Unauthorized(new { message = "Invalid token." });

            int userId = int.Parse(userIdClaim.Value);

            var tutor = await db.Tutors
                .FirstOrDefaultAsync(t => t.UserId == userId);

            if (tutor == null)
                return NotFound(new { message = "Tutor not found." });

            int added = 0;

            foreach (var item in dto.Courses)
            {
                if (string.IsNullOrWhiteSpace(item.Grade))
                    continue;

                string grade = item.Grade.Trim().ToUpper();

                if (!new[] { "A", "B", "C", "D", "F" }.Contains(grade))
                    continue;

                if (item.HourlyRate <= 0)
                    continue;

                bool exists = await db.TutorCourses.AnyAsync(tc =>
                    tc.TutorId == tutor.TutorId &&
                    tc.CourseId == item.CourseId);

                if (exists)
                    continue;

                // Save Tutor Course
                db.TutorCourses.Add(new TutorCourse
                {
                    TutorId = tutor.TutorId,
                    CourseId = item.CourseId,
                    Grade = grade
                });

                // Save Hourly Rate
                db.TutorCourseRates.Add(new TutorCourseRate
                {
                    TutorId = tutor.TutorId,
                    CourseId = item.CourseId,
                    HourlyRate = item.HourlyRate
                });

                added++;
            }

            await db.SaveChangesAsync();

            return Ok(new
            {
                message = "Courses added successfully.",
                added
            });
        }
        //Get Tutor_courses that they teach
        //[Authorize]
        //[HttpGet("my-courses")]
        //public async Task<IActionResult> GetTutorCourses()
        //{
        //    var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);

        //    if (userIdClaim == null)
        //        return Unauthorized();

        //    int userId = int.Parse(userIdClaim.Value);

        //    var tutor = await db.Tutors
        //        .FirstOrDefaultAsync(t => t.UserId == userId);

        //    if (tutor == null)
        //        return NotFound();

        //    var courses = await db.TutorCourses
        //        .Where(tc => tc.TutorId == tutor.TutorId)
        //        .Select(tc => new
        //        {
        //            course_id = tc.Course.CourseId,
        //            course_name = tc.Course.CourseTitle,
        //            grade = tc.Grade
        //        })
        //        .ToListAsync();
        //    var data = await (
        //        from tc in db.TutorCourses
        //        join c in db.Courses
        //            on tc.CourseId equals c.CourseId
        //        join r in db.TutorCourseRates
        //            on new { tc.TutorId, tc.CourseId }
        //            equals new { r.TutorId, r.CourseId }
        //        where tc.TutorId == tutor.TutorId
        //        select new
        //        {
        //            course_id = c.CourseId,
        //            course_name = c.CourseTitle,
        //            grade = tc.Grade,
        //            hourly_rate = r.HourlyRate
        //        }
        //    ).ToListAsync();

        //    return Ok(data);

        //    return Ok(courses);
        //}
        [Authorize]
        [HttpGet("my-courses")]
        public async Task<IActionResult> GetTutorCourses()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);

            if (userIdClaim == null)
                return Unauthorized(new { message = "Invalid token." });

            int userId = int.Parse(userIdClaim.Value);

            var tutor = await db.Tutors
                .FirstOrDefaultAsync(t => t.UserId == userId);

            if (tutor == null)
                return NotFound(new { message = "Tutor not found." });

            var courses = await (
                from tc in db.TutorCourses
                join c in db.Courses
                    on tc.CourseId equals c.CourseId
                join r in db.TutorCourseRates
                    on new { tc.TutorId, tc.CourseId }
                    equals new { r.TutorId, r.CourseId }
                    into rates
                from r in rates.DefaultIfEmpty() // Left Join
                where tc.TutorId == tutor.TutorId
                select new
                {
                    course_id = c.CourseId,
                    course_name = c.CourseTitle,
                    grade = tc.Grade,
                    hourly_rate = r != null ? r.HourlyRate : 0
                }
            ).ToListAsync();

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

        // STUDENT PROFILE API
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

        // TUTOR TODAY CLASSES API METHOD
        [Authorize]
        [HttpGet("today-classes")]
        public async Task<IActionResult> GetTodayClasses()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);

                if (userIdClaim == null)
                {
                    return Unauthorized(new
                    {
                        success = false,
                        message = "Invalid token"
                    });
                }

                int userId = Convert.ToInt32(userIdClaim.Value);

                var tutor = await db.Tutors
                    .FirstOrDefaultAsync(t => t.UserId == userId);

                if (tutor == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "Tutor not found"
                    });
                }

                var today = DateOnly.FromDateTime(DateTime.Today);

                var classes = await (
                    from r in db.Requests

                    join s in db.Students
                        on r.StudentId equals s.StudentId

                    join su in db.Users
                        on s.UserId equals su.UserId

                    join c in db.Courses
                        on r.CourseId equals c.CourseId

                    where r.TutorId == tutor.TutorId
                          && r.Status == "Accepted"
                          && r.ClassDate == today

                    orderby r.Time

                    select new
                    {
                        request_id = r.RequestId,
                        student_id = s.StudentId,
                        student_name = su.FullName,
                        course_id = c.CourseId,
                        course_name = c.CourseTitle,
                        class_date = r.ClassDate,
                        day = r.Day,
                        time = r.Time,
                        request_type = r.RequestType,
                        status = r.Status,
                        request_date = r.RequestDate
                    }
                ).ToListAsync();

                return Ok(new
                {
                    success = true,
                    message = "Today's classes fetched successfully",
                    data = classes
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

        // TUTOR ALL CLASSES API METHOD
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
                        success = false,
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
                        success = false,
                        message = "Tutor not found"
                    });
                }

                // Fetch All Classes
                var classes = await (
                    from r in db.Requests

                    join s in db.Students
                        on r.StudentId equals s.StudentId

                    join su in db.Users
                        on s.UserId equals su.UserId

                    join c in db.Courses
                        on r.CourseId equals c.CourseId

                    where r.TutorId == tutor.TutorId
                          && r.Status == "Accepted"

                    orderby r.ClassDate descending, r.Time

                    select new
                    {
                        request_id = r.RequestId,

                        student_id = s.StudentId,

                        student_name = su.FullName,

                        course_id = c.CourseId,

                        course_name = c.CourseTitle,

                        class_date = r.ClassDate,

                        day = r.Day,

                        time = r.Time,

                        request_type = r.RequestType,

                        status = r.Status,

                        request_date = r.RequestDate
                    }
                ).ToListAsync();

                return Ok(new
                {
                    success = true,
                    message = "All classes fetched successfully",
                    data = classes
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

        // AUTO RESCHEDULE
        [Authorize]
        [HttpPost("reschedule")]
        public async Task<IActionResult> AutoReschedule(AutoScheduleDto dto)
        {
            try
            {
                // Get Logged In User
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);

                if (userIdClaim == null)
                {
                    return Unauthorized(new
                    {
                        success = false,
                        message = "Invalid token"
                    });
                }

                int userId = Convert.ToInt32(userIdClaim.Value);

                // Find Tutor
                var tutor = await db.Tutors
                    .FirstOrDefaultAsync(x => x.UserId == userId);

                if (tutor == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "Tutor not found"
                    });
                }

                // Find Original Request
                var request = await db.Requests
                    .FirstOrDefaultAsync(x =>
                        x.RequestId == dto.RequestId &&
                        x.TutorId == tutor.TutorId);

                if (request == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "Class not found"
                    });
                }

                // Cannot reschedule completed class
                if (request.Status == "Completed")
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Completed class cannot be rescheduled."
                    });
                }

                //// Cannot reschedule cancelled class
                //if (request.Status == "Cancelled")
                //{
                //    return BadRequest(new
                //    {
                //        success = false,
                //        message = "Cancelled class cannot be rescheduled."
                //    });
                //}

                //---------------------------------------------------
                // FIND COMMON SLOT
                //---------------------------------------------------

                var slot = await FindCommonSlot(
                    request.TutorId.Value,
                    request.StudentId.Value);

                //---------------------------------------------------
                // COMMON SLOT FOUND
                //---------------------------------------------------

                if (slot.Found)
                {
                    await AutoMoveClass(
                        request,
                        slot,
                        "Reschedule");

                    return Ok(new
                    {
                        success = true,

                        autoScheduled = true,

                        manualRequired = false,

                        message = "Class automatically rescheduled.",

                        data = new
                        {
                            request.RequestId,

                            slot.Day,

                            slot.Time,

                            slot.ClassDate,

                            RequestType = "Reschedule",

                            Status = "Accepted"
                        }
                    });
                }

                //---------------------------------------------------
                // NO COMMON SLOT
                //---------------------------------------------------

                return Ok(new
                {
                    success = true,

                    autoScheduled = false,

                    manualRequired = true,

                    message = "No common free slot found. Please choose a day, time and date.",

                    data = new
                    {
                        request.RequestId,

                        request.StudentId,

                        request.CourseId
                    }
                });

            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

        // AUTO PRESCHEDULE
        [Authorize]
        [HttpPost("preschedule")]
        public async Task<IActionResult> AutoPreschedule(AutoScheduleDto dto)
        {
            try
            {
                // Get Logged In User
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);

                if (userIdClaim == null)
                {
                    return Unauthorized(new
                    {
                        success = false,
                        message = "Invalid token"
                    });
                }

                int userId = Convert.ToInt32(userIdClaim.Value);

                // Find Tutor
                var tutor = await db.Tutors
                    .FirstOrDefaultAsync(x => x.UserId == userId);

                if (tutor == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "Tutor not found"
                    });
                }

                // Find Original Request
                var request = await db.Requests
                    .FirstOrDefaultAsync(x =>
                        x.RequestId == dto.RequestId &&
                        x.TutorId == tutor.TutorId);

                if (request == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "Class not found"
                    });
                }

                // Cannot preschedule completed class
                if (request.Status == "Completed")
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Completed class cannot be prescheduled."
                    });
                }

                // Cannot preschedule cancelled class
                if (request.Status == "Cancelled")
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Cancelled class cannot be prescheduled."
                    });
                }

                //---------------------------------------------------
                // FIND COMMON SLOT
                //---------------------------------------------------

                var slot = await FindCommonSlot(
                    request.TutorId.Value,
                    request.StudentId.Value);

                //---------------------------------------------------
                // COMMON SLOT FOUND
                //---------------------------------------------------

                if (slot.Found)
                {
                    await AutoMoveClass(
                        request,
                        slot,
                        "Preschedule");

                    return Ok(new
                    {
                        success = true,

                        autoScheduled = true,

                        manualRequired = false,

                        message = "Class automatically prescheduled.",

                        data = new
                        {
                            request.RequestId,

                            slot.Day,

                            slot.Time,

                            slot.ClassDate,

                            RequestType = "Preschedule",

                            Status = "Accepted"
                        }
                    });
                }

                //---------------------------------------------------
                // NO COMMON SLOT
                //---------------------------------------------------

                return Ok(new
                {
                    success = true,

                    autoScheduled = false,

                    manualRequired = true,

                    message = "No common free slot found. Please choose a day, time and date.",

                    data = new
                    {
                        request.RequestId,

                        request.StudentId,

                        request.CourseId
                    }
                });

            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

        // MANUAL SCHEDULE
        [Authorize]
        [HttpPost("manual-schedule")]
        public async Task<IActionResult> ManualSchedule(ManualScheduleDto dto)
        {
            try
            {
                //--------------------------------------------------
                // LOGIN USER
                //--------------------------------------------------

                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);

                if (userIdClaim == null)
                {
                    return Unauthorized(new
                    {
                        success = false,
                        message = "Invalid Token"
                    });
                }

                int userId = Convert.ToInt32(userIdClaim.Value);

                //--------------------------------------------------
                // FIND TUTOR
                //--------------------------------------------------

                var tutor = await db.Tutors
                    .FirstOrDefaultAsync(x => x.UserId == userId);

                if (tutor == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "Tutor not found"
                    });
                }

                //--------------------------------------------------
                // FIND ORIGINAL CLASS
                //--------------------------------------------------

                var oldRequest = await db.Requests
                    .FirstOrDefaultAsync(x =>
                        x.RequestId == dto.RequestId &&
                        x.TutorId == tutor.TutorId);

                if (oldRequest == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "Original class not found"
                    });
                }

                //--------------------------------------------------
                // NULL CHECK
                //--------------------------------------------------

                if (!oldRequest.TutorId.HasValue ||
                    !oldRequest.StudentId.HasValue ||
                    !oldRequest.CourseId.HasValue)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Invalid class information."
                    });
                }

                //--------------------------------------------------
                // CHECK SLOT BUSY
                //--------------------------------------------------

                bool busy = await IsSlotBusy(
                    oldRequest.TutorId.Value,
                    oldRequest.StudentId.Value,
                    DateOnly.FromDateTime(dto.ClassDate),
                    dto.Time);

                if (busy)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Selected slot is already booked."
                    });
                }

                //--------------------------------------------------
                // CREATE REQUEST
                //--------------------------------------------------

                Request request = new Request
                {
                    TutorId = oldRequest.TutorId,

                    StudentId = oldRequest.StudentId,

                    CourseId = oldRequest.CourseId,

                    ParentRequestId = oldRequest.RequestId,

                    Day = dto.Day,

                    Time = dto.Time,

                    ClassDate = DateOnly.FromDateTime(dto.ClassDate),

                    RequestType = dto.RequestType,

                    Status = "RequestedByTutor",

                    RequestDate = DateTime.Now
                };

                db.Requests.Add(request);

                await db.SaveChangesAsync();

                //--------------------------------------------------
                // RESPONSE
                //--------------------------------------------------

                return Ok(new
                {
                    success = true,

                    message = "Schedule request sent to student successfully.",

                    data = new
                    {
                        request.RequestId,

                        request.Day,

                        request.Time,

                        request.ClassDate,

                        request.RequestType,

                        request.Status
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

        //Complete the class
        [HttpPut("complete-class/{requestId}")]
        public async Task<IActionResult> CompleteClass(int requestId)
        {
            try
            {
                // Find request by ID
                var request = await db.Requests
                    .FirstOrDefaultAsync(x => x.RequestId == requestId);

                if (request == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "Request not found"
                    });
                }

                // Update status
                request.Status = "Complete";

                // Save changes
                await db.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    message = "Class marked as complete successfully",
                    data = request
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

        // CANCEL the CLASS
        [Authorize]
        [HttpPut("cancel-class/{requestId}")]
        public async Task<IActionResult> CancelClass(int requestId)
        {
            try
            {
                var request = await db.Requests
                    .FirstOrDefaultAsync(x =>
                        x.RequestId == requestId);

                if (request == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "Request not found"
                    });
                }

                request.Status = "Cancelled";

                await db.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    message = "Class cancelled successfully"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }


        // GET cancelled-classes
        //[Authorize]
        //[HttpGet("cancelled-classes")]
        //public async Task<IActionResult> GetCancelledClasses()
        //{
        //    try
        //    {
        //        // Get Logged In User
        //        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);

        //        if (userIdClaim == null)
        //        {
        //            return Unauthorized(new
        //            {
        //                success = false,
        //                message = "Invalid token."
        //            });
        //        }

        //        int userId = int.Parse(userIdClaim.Value);

        //        // Get Tutor
        //        var tutor = await db.Tutors
        //            .FirstOrDefaultAsync(t => t.UserId == userId);

        //        if (tutor == null)
        //        {
        //            return NotFound(new
        //            {
        //                success = false,
        //                message = "Tutor not found."
        //            });
        //        }

        //        // Get Cancelled Classes
        //        var cancelledClasses = await (
        //            from r in db.Requests
        //            join s in db.Students on r.StudentId equals s.StudentId
        //            join u in db.Users on s.UserId equals u.UserId
        //            join c in db.Courses on r.CourseId equals c.CourseId
        //            where r.TutorId == tutor.TutorId
        //                  && r.Status == "Cancelled"
        //            orderby r.ClassDate descending
        //            select new
        //            {
        //                RequestId = r.RequestId,
        //                StudentId = s.StudentId,
        //                StudentName = u.FullName,
        //                CourseId = c.CourseId,
        //                CourseName = c.CourseTitle,
        //                Day = r.Day,
        //                Time = r.Time,
        //                ClassDate = r.ClassDate,
        //                RequestType = r.RequestType,
        //                Status = r.Status,
        //                RequestDate = r.RequestDate
        //            }
        //        ).ToListAsync();

        //        return Ok(new
        //        {
        //            success = true,
        //            total = cancelledClasses.Count,
        //            data = cancelledClasses
        //        });
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(500, new
        //        {
        //            success = false,
        //            message = ex.Message
        //        });
        //    }
        //}
        [Authorize]
        [HttpGet("cancel-classes")]
        public async Task<IActionResult> GetAllCancelClasses()
        {
            try
            {
                // Get Logged In User ID
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);

                if (userIdClaim == null)
                {
                    return Unauthorized(new
                    {
                        success = false,
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
                        success = false,
                        message = "Tutor not found"
                    });
                }

                // Fetch All Classes
                var classes = await (
                    from r in db.Requests

                    join s in db.Students
                        on r.StudentId equals s.StudentId

                    join su in db.Users
                        on s.UserId equals su.UserId

                    join c in db.Courses
                        on r.CourseId equals c.CourseId

                    where r.TutorId == tutor.TutorId
                          && r.Status == "Cancelled"

                    orderby r.ClassDate descending, r.Time

                    select new
                    {
                        request_id = r.RequestId,

                        student_id = s.StudentId,

                        student_name = su.FullName,

                        course_id = c.CourseId,

                        course_name = c.CourseTitle,

                        class_date = r.ClassDate,

                        day = r.Day,

                        time = r.Time,

                        request_type = r.RequestType,

                        status = r.Status,

                        request_date = r.RequestDate
                    }
                ).ToListAsync();

                return Ok(new
                {
                    success = true,
                    message = "All classes fetched successfully",
                    data = classes
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }
        [Authorize]
        [HttpGet("tutor-class-history")]
        public async Task<IActionResult> GetTutorClassHistory()
        {
            try
            {
                // Get Logged-in User ID from Token
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);

                if (userIdClaim == null)
                {
                    return Unauthorized(new
                    {
                        success = false,
                        message = "Invalid token"
                    });
                }

                int userId = Convert.ToInt32(userIdClaim.Value);

                // Find Tutor using UserId
                var tutor = await db.Tutors
                    .FirstOrDefaultAsync(t => t.UserId == userId);

                if (tutor == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "Tutor not found"
                    });
                }

                int tutorId = tutor.TutorId;

                // Fetch History
                var history = await (
                    from r in db.Requests

                    join s in db.Students
                        on r.StudentId equals s.StudentId

                    join u in db.Users
                        on s.UserId equals u.UserId

                    join c in db.Courses
                        on r.CourseId equals c.CourseId

                    where r.TutorId == tutorId
                          && r.Status == "Complete"

                    orderby r.ClassDate descending

                    select new
                    {
                        request_id = r.RequestId,

                        student_id = s.StudentId,
                        student_name = u.FullName,

                        tutor_id = r.TutorId,

                        course_id = c.CourseId,
                        course_name = c.CourseTitle,

                        status = r.Status,
                        request_type = r.RequestType,

                        class_date = r.ClassDate,
                        day = r.Day,
                        time = r.Time,

                        request_date = r.RequestDate
                    }

                ).ToListAsync();

                return Ok(new
                {
                    success = true,
                    message = "Tutor class history fetched successfully",
                    data = history
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

        [HttpGet("my-dashboard")]
        [Authorize]
        public async Task<IActionResult> GetTutorDashboard()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userIdClaim))
                return Unauthorized();

            int userId = int.Parse(userIdClaim);

            var tutor = await db.Tutors
                .FirstOrDefaultAsync(t => t.UserId == userId);

            if (tutor == null)
                return NotFound(new { message = "Tutor not found" });

            var requests = await db.Requests
                .Include(r => r.Student)
                .ThenInclude(s => s.User)
                .Where(r =>
                    r.TutorId == tutor.TutorId &&
                    (r.Status == "Accepted" || r.Status == "Complete"))
                .ToListAsync();

            var result = new TutorDashboardDto
            {
                TotalClasses = requests.Count,

                Students = requests
                    .Where(r => r.Student != null)
                    .GroupBy(r => new
                    {
                        StudentId = r.Student!.StudentId,
                        StudentName = r.Student.User != null
                            ? r.Student.User.FullName
                            : "Unknown Student"
                    })
                    .Select(g => new TutorStudentClassDto
                    {
                        StudentId = g.Key.StudentId,
                        StudentName = g.Key.StudentName ?? "Unknown Student",
                        TotalClasses = g.Count(),

                        Classes = g.Select(c => new ClassDetailDto
                        {
                            RequestId = c.RequestId,
                            ClassDate = c.ClassDate,
                            Time = c.Time,
                            RequestType = c.RequestType,
                            Status = c.Status
                        }).ToList()
                    })
                    .OrderBy(x => x.StudentName)
                    .ToList()
            };

            return Ok(result);
        }

        //[Authorize]
        //[HttpPost("mark-course-done")]
        //public async Task<IActionResult> MarkCourseDone([FromBody] MarkCourseDoneRequest request)
        //{
        //    try
        //    {
        //        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);

        //        if (userIdClaim == null)
        //            return Unauthorized(new { message = "Invalid token" });

        //        int userId = int.Parse(userIdClaim.Value);

        //        var tutor = await db.Tutors
        //            .FirstOrDefaultAsync(t => t.UserId == userId);

        //        if (tutor == null)
        //            return NotFound(new { message = "Tutor not found" });

        //        var tutorCourse = await db.TutorCourses
        //            .FirstOrDefaultAsync(tc =>
        //                tc.TutorId == tutor.TutorId &&
        //                tc.CourseId == request.CourseId);

        //        if (tutorCourse == null)
        //        {
        //            return NotFound(new
        //            {
        //                message = "Course not found for this tutor"
        //            });
        //        }

        //        tutorCourse.IsCompleted = true;
        //        tutorCourse.CompletedDate = DateTime.UtcNow;

        //        if (!string.IsNullOrWhiteSpace(request.Grade))
        //        {
        //            tutorCourse.Grade = request.Grade;
        //        }

        //        await db.SaveChangesAsync();

        //        return Ok(new
        //        {
        //            message = "Course marked as completed successfully"
        //        });
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(500, new
        //        {
        //            message = ex.Message
        //        });
        //    }
        //}
        
        //mark as done and insert total fee of Stuednt per Course 
        [Authorize]
        [HttpPost("mark-course-done")]
        public async Task<IActionResult> MarkCourseDone([FromBody] MarkCourseDoneRequest request)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);

                if (userIdClaim == null)
                    return Unauthorized(new { message = "Invalid token." });

                int userId = int.Parse(userIdClaim.Value);

                var tutor = await db.Tutors
                    .FirstOrDefaultAsync(t => t.UserId == userId);

                if (tutor == null)
                    return NotFound(new { message = "Tutor not found." });

                // Verify student exists
                var student = await db.Students
                    .FirstOrDefaultAsync(s => s.StudentId == request.StudentId);

                if (student == null)
                    return BadRequest(new
                    {
                        message = "Student not found."
                    });

                // Verify tutor teaches this course
                var tutorCourse = await db.TutorCourses
                    .FirstOrDefaultAsync(tc =>
                        tc.TutorId == tutor.TutorId &&
                        tc.CourseId == request.CourseId);

                if (tutorCourse == null)
                    return BadRequest(new
                    {
                        message = "Course not assigned to tutor."
                    });

                // Mark course completed
                tutorCourse.IsCompleted = true;
                tutorCourse.CompletedDate = DateTime.Now;

                if (!string.IsNullOrWhiteSpace(request.Grade))
                    tutorCourse.Grade = request.Grade.Trim().ToUpper();

                // Hourly rate
                var rate = await db.TutorCourseRates
                    .FirstOrDefaultAsync(r =>
                        r.TutorId == tutor.TutorId &&
                        r.CourseId == request.CourseId);

                if (rate == null)
                {
                    return BadRequest(new
                    {
                        message = "Hourly rate not found."
                    });
                }

                // Completed classes of this student
                // Mark all Accepted classes as Complete
                var classes = await db.Requests
                    .Where(r =>
                        r.StudentId == request.StudentId &&
                        r.TutorId == tutor.TutorId &&
                        r.CourseId == request.CourseId)
                    .ToListAsync();

                if (!classes.Any())
                {
                    return BadRequest(new
                    {
                        message = "No classes found."
                    });
                }

                foreach (var cls in classes)
                {
                    if (cls.Status == "Accepted")
                    {
                        cls.Status = "Complete";
                    }
                }

                double totalHours = 0;

                foreach (var cls in classes)
                {
                    if (string.IsNullOrWhiteSpace(cls.Time))
                        continue;

                    var parts = cls.Time.Split('-');

                    if (parts.Length != 2)
                        continue;

                    if (DateTime.TryParse(parts[0].Trim(), out DateTime start) &&
                        DateTime.TryParse(parts[1].Trim(), out DateTime end))
                    {
                        totalHours += (end - start).TotalHours;
                    }
                }

                decimal totalFee = (decimal)totalHours * rate.HourlyRate;

                var fee = await db.StudentCourseFees
                    .FirstOrDefaultAsync(f =>
                        f.StudentId == request.StudentId &&
                        f.TutorId == tutor.TutorId &&
                        f.CourseId == request.CourseId);

                if (fee == null)
                {
                    db.StudentCourseFees.Add(new StudentCourseFee
                    {
                        StudentId = request.StudentId,
                        TutorId = tutor.TutorId,
                        CourseId = request.CourseId,
                        TotalFee = totalFee,
                        CreatedDate = DateTime.Now
                    });
                }
                else
                {
                    fee.TotalFee = totalFee;
                    fee.CreatedDate = DateTime.Now;
                }

                await db.SaveChangesAsync();

                return Ok(new
                {
                    message = "Course marked as completed successfully.",
                    totalHours,
                    hourlyRate = rate.HourlyRate,
                    totalFee
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = ex.InnerException?.Message ?? ex.Message,
                    stack = ex.StackTrace
                });
            }
        }

        //[Authorize]
        //[HttpGet("my-courses-for-mark-as-done")]
        //public async Task<IActionResult> GetTutorCoursesForMarkDone()
        //{
        //    try
        //    {
        //        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);

        //        if (userIdClaim == null)
        //            return Unauthorized(new { message = "Invalid token" });

        //        int userId = int.Parse(userIdClaim.Value);

        //        var tutor = await db.Tutors
        //            .FirstOrDefaultAsync(t => t.UserId == userId);

        //        if (tutor == null)
        //            return NotFound(new { message = "Tutor not found" });

        //        var data = await db.Requests
        //            .Where(r => r.TutorId == tutor.TutorId)
        //            .Select(r => new
        //            {
        //                request_id = r.RequestId,
        //                student_id = r.StudentId,
        //                student_name = r.Student.User.FullName,
        //                course_id = r.CourseId,
        //                course_name = r.Course.CourseTitle,
        //                status = r.Status,
        //                is_completed = db.TutorCourses
        //                    .Where(tc => tc.TutorId == tutor.TutorId &&
        //                                 tc.CourseId == r.CourseId)
        //                    .Select(tc => tc.IsCompleted)
        //                    .FirstOrDefault(),
        //                completed_date = db.TutorCourses
        //                    .Where(tc => tc.TutorId == tutor.TutorId &&
        //                                 tc.CourseId == r.CourseId)
        //                    .Select(tc => tc.CompletedDate)
        //                    .FirstOrDefault(),
        //                grade = db.TutorCourses
        //                    .Where(tc => tc.TutorId == tutor.TutorId &&
        //                                 tc.CourseId == r.CourseId)
        //                    .Select(tc => tc.Grade)
        //                    .FirstOrDefault()
        //            })
        //            .Distinct()
        //            .ToListAsync();

        //        return Ok(data);
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(500, new
        //        {
        //            message = ex.InnerException?.Message ?? ex.Message
        //        });
        //    }
        //}

        [Authorize]
        [HttpGet("my-courses-for-mark-as-done")]
        public async Task<IActionResult> GetTutorCoursesForMarkDone()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);

                if (userIdClaim == null)
                    return Unauthorized(new { message = "Invalid token" });

                int userId = int.Parse(userIdClaim.Value);

                var tutor = await db.Tutors
                    .FirstOrDefaultAsync(t => t.UserId == userId);

                if (tutor == null)
                    return NotFound(new { message = "Tutor not found" });

                var data = await db.Requests
                    .Where(r => r.TutorId == tutor.TutorId)
                    .GroupBy(r => new
                    {
                        r.StudentId,
                        r.CourseId,
                        r.TutorId
                    })
                    .Select(g => new
                    {
                        request_id = g.First().RequestId,

                        student_id = g.Key.StudentId,
                        student_name = g.First().Student.User.FullName,

                        course_id = g.Key.CourseId,
                        course_name = g.First().Course.CourseTitle,

                        status = g.First().Status,

                        is_completed = db.TutorCourses
                            .Where(tc => tc.TutorId == tutor.TutorId &&
                                         tc.CourseId == g.Key.CourseId)
                            .Select(tc => tc.IsCompleted)
                            .FirstOrDefault(),

                        completed_date = db.TutorCourses
                            .Where(tc => tc.TutorId == tutor.TutorId &&
                                         tc.CourseId == g.Key.CourseId)
                            .Select(tc => tc.CompletedDate)
                            .FirstOrDefault(),

                        grade = db.TutorCourses
                            .Where(tc => tc.TutorId == tutor.TutorId &&
                                         tc.CourseId == g.Key.CourseId)
                            .Select(tc => tc.Grade)
                            .FirstOrDefault()
                    })
                    .OrderBy(x => x.student_name)
                    .ThenBy(x => x.course_name)
                    .ToListAsync();

                return Ok(data);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = ex.InnerException?.Message ?? ex.Message
                });
            }
        }

        [Authorize]
        [HttpGet("my-students")]
        public async Task<IActionResult> GetMyStudents()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);

            if (userIdClaim == null)
                return Unauthorized(new { message = "Invalid token" });

            int userId = int.Parse(userIdClaim.Value);

            var tutor = await db.Tutors
                .FirstOrDefaultAsync(t => t.UserId == userId);

            if (tutor == null)
                return NotFound(new { message = "Tutor not found" });

            var students = await db.Requests
                .Where(r =>
                    r.TutorId == tutor.TutorId &&
                    r.Status == "Accepted")
                .Select(r => new TutorStudentCourseDto
                {
                    StudentId = r.Student.StudentId,
                    StudentName = r.Student.User.FullName,

                    CourseId = r.CourseId.Value,
                    CourseName = r.Course.CourseTitle,

                    IsCompleted =
                        db.TutorCourses
                        .Where(tc =>
                            tc.TutorId == tutor.TutorId &&
                            tc.CourseId == r.CourseId)
                        .Select(tc => tc.IsCompleted)
                        .FirstOrDefault(),

                    CompletedDate =
                        db.TutorCourses
                        .Where(tc =>
                            tc.TutorId == tutor.TutorId &&
                            tc.CourseId == r.CourseId)
                        .Select(tc => tc.CompletedDate)
                        .FirstOrDefault(),

                    FeedbackGiven =
                        db.Feedbacks.Any(f =>
                            f.StudentId == r.StudentId &&
                            f.TutorId == tutor.TutorId &&
                            f.CourseId == r.CourseId)
                })
                .Distinct()
                .ToListAsync();

            return Ok(students);
        }

        [Authorize]
        [HttpPost("complete-course")]
        public async Task<IActionResult> CompleteCourse([FromBody] CompleteCourseRequest request)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);

            if (userIdClaim == null)
                return Unauthorized(new { message = "Invalid token" });

            int userId = int.Parse(userIdClaim.Value);

            var tutor = await db.Tutors
                .FirstOrDefaultAsync(t => t.UserId == userId);

            if (tutor == null)
                return NotFound();

            var tutorCourse = await db.TutorCourses
                .FirstOrDefaultAsync(tc =>
                    tc.TutorId == tutor.TutorId &&
                    tc.CourseId == request.CourseId);

            if (tutorCourse == null)
                return NotFound();

            tutorCourse.IsCompleted = true;
            tutorCourse.CompletedDate = DateTime.UtcNow;

            await db.SaveChangesAsync();

            return Ok(new
            {
                message = "Course completed successfully"
            });
        }

        [Authorize]
        [HttpPost("give-feedback")]
        public async Task<IActionResult> GiveFeedback([FromBody] TutorFeedbackRequest request)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);

            if (userIdClaim == null)
                return Unauthorized(new { message = "Invalid token" });

            int userId = int.Parse(userIdClaim.Value);

            var tutor = await db.Tutors
                .FirstOrDefaultAsync(t => t.UserId == userId);

            if (tutor == null)
                return NotFound();

            bool exists = await db.Feedbacks.AnyAsync(f =>
                f.StudentId == request.StudentId &&
                f.CourseId == request.CourseId &&
                f.TutorId == tutor.TutorId);

            if (exists)
            {
                return BadRequest(new
                {
                    message = "Feedback already submitted"
                });
            }

            Feedback feedback = new Feedback
            {
                StudentId = request.StudentId,
                TutorId = tutor.TutorId,
                CourseId = request.CourseId,
                Rating = request.Rating,
                Comment = request.Comment,
                FeedbackBy="Tutor",
                FeedbackDate = DateTime.UtcNow
            };

            db.Feedbacks.Add(feedback);

            await db.SaveChangesAsync();

            return Ok(new
            {
                message = "Feedback submitted successfully"
            });
        }

        //To get pre and re schedule notification's
        [HttpGet("notifications")]
        [Authorize]
        public async Task<IActionResult> GetNotifications()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);

            if (userIdClaim == null)
                return Unauthorized(new
                {
                    message = "Invalid token"
                });

            int userId = int.Parse(userIdClaim.Value);

            var tutor = await db.Tutors
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.UserId == userId);

            if (tutor == null)
                return Unauthorized(new
                {
                    message = "Tutor not found"
                });

            var notifications = await (
                from r in db.Requests

                join s in db.Students
                    on r.StudentId equals s.StudentId

                join u in db.Users
                    on s.UserId equals u.UserId

                join c in db.Courses
                    on r.CourseId equals c.CourseId

                where r.TutorId == tutor.TutorId
                      && r.Status == "RequestedByStudent"
                      && (
                            r.RequestType == "Reschedule" ||
                            r.RequestType == "Preschedule"
                         )

                orderby r.RequestDate descending

                select new
                {
                    r.RequestId,
                    r.ParentRequestId,
                    r.RequestType,
                    r.Day,
                    r.Time,
                    r.ClassDate,
                    r.RequestDate,

                    StudentName = u.FullName,

                    c.CourseTitle
                }
            ).ToListAsync();

            return Ok(notifications);
        }

        //Accept Re & Pre Schedule Request
        [HttpPut("accept-re-and-pre-schedule-request/{requestId}")]
        [Authorize]
        public async Task<IActionResult> AcceptReandPreScheduleRequest(int requestId)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);

            if (userIdClaim == null)
                return Unauthorized(new
                {
                    message = "Invalid token"
                });

            int userId = int.Parse(userIdClaim.Value);

            var tutor = await db.Tutors
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.UserId == userId);

            if (tutor == null)
                return Unauthorized(new
                {
                    message = "Tutor not found"
                });

            var request = await db.Requests
                .FirstOrDefaultAsync(x =>
                    x.RequestId == requestId
                    && x.TutorId == tutor.TutorId);

            if (request == null)
                return NotFound(new
                {
                    message = "Request not found"
                });

            var parent = await db.Requests
                .FirstOrDefaultAsync(x =>
                    x.RequestId == request.ParentRequestId);

            if (parent != null)
            {
                parent.Status = "Cancelled";
            }

            request.Status = "Accepted";

            await db.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                message = "Request accepted successfully"
            });
        }

        //Reject Re & Pre Schedule Request
        [HttpPut("reject-re-and-pre-schedule-request/{requestId}")]
        [Authorize]
        public async Task<IActionResult> RejectReandPreScheduleRequest(int requestId)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);

            if (userIdClaim == null)
                return Unauthorized(new
                {
                    message = "Invalid token"
                });

            int userId = int.Parse(userIdClaim.Value);

            var tutor = await db.Tutors
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.UserId == userId);

            if (tutor == null)
                return Unauthorized(new
                {
                    message = "Tutor not found"
                });

            var request = await db.Requests
                .FirstOrDefaultAsync(x =>
                    x.RequestId == requestId
                    && x.TutorId == tutor.TutorId);

            if (request == null)
                return NotFound(new
                {
                    message = "Request not found"
                });

            request.Status = "Rejected";

            await db.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                message = "Request rejected"
            });
        }

        //to get how many notification
        [HttpGet("notification-badge-count")]
        [Authorize]
        public async Task<IActionResult> GetNotificationBadgeCount()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);

            if (userIdClaim == null)
                return Unauthorized(new
                {
                    message = "Invalid token"
                });

            int userId = int.Parse(userIdClaim.Value);

            var tutor = await db.Tutors
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.UserId == userId);

            if (tutor == null)
                return Unauthorized(new
                {
                    message = "Tutor not found"
                });

            var count = await db.Requests.CountAsync(x =>
                x.TutorId == tutor.TutorId
                && x.Status == "RequestedByStudent"
                && (
                    x.RequestType == "Reschedule"
                    || x.RequestType == "Preschedule"
                )
            );

            return Ok(new
            {
                count
            });
        }


        [Authorize]
        [HttpGet("payment-list")]
        public IActionResult PaymentList()
        {
            int userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

            var tutor = db.Tutors
                .FirstOrDefault(x => x.UserId == userId);

            if (tutor == null)
                return NotFound();

            var data = (from payment in db.Payments
                        join fee in db.StudentCourseFees
                        on payment.FeeId equals fee.FeeId

                        join student in db.Students
                        on fee.StudentId equals student.StudentId

                        join user in db.Users
                        on student.UserId equals user.UserId

                        join course in db.Courses
                        on fee.CourseId equals course.CourseId

                        where fee.TutorId == tutor.TutorId

                        select new
                        {
                            payment.PaymentId,
                            Student = user.FullName,
                            Course = course.CourseTitle,
                            payment.Amount,
                            payment.PaymentType,
                            payment.ParentStatus,
                            payment.TutorStatus,
                            payment.PaymentDate
                        }).ToList();

            return Ok(data);
        }

        //Update Payment status Recieved or Not Recieved 
        [Authorize]
        [HttpPut("payment-status")]
        public IActionResult UpdatePaymentStatus(PaymentStatusModel model)
        {
            var payment = db.Payments
                .FirstOrDefault(x => x.PaymentId == model.PaymentId);

            if (payment == null)
                return NotFound();

            if (model.Status != "Received" &&
                model.Status != "NotReceived")
            {
                return BadRequest("Invalid Status.");
            }

            payment.TutorStatus = model.Status;

            db.SaveChanges();

            return Ok(new
            {
                Message = "Payment Status Updated."
            });
        }

        //HELPER METHODS 
        private string NormalizeDay(string day)
        {
            if (string.IsNullOrWhiteSpace(day))
                return "";

            day = day.Trim().ToLower();

            return day switch
            {
                "monday" => "mon",
                "mon" => "mon",

                "tuesday" => "tue",
                "tue" => "tue",
                "tues" => "tue",

                "wednesday" => "wed",
                "wed" => "wed",

                "thursday" => "thu",
                "thu" => "thu",
                "thur" => "thu",
                "thurs" => "thu",

                "friday" => "fri",
                "fri" => "fri",

                // Fix incorrect database values
                "fir" => "fri",

                "saturday" => "sat",
                "sat" => "sat",

                "sunday" => "sun",
                "sun" => "sun",

                _ => day.Length >= 3 ? day.Substring(0, 3) : day
            };
        }
        private string NormalizeTime(string time)
        {
            return time.Replace(" ", "")
                       .ToLower();
        }

        //Check Busy Slot
        private async Task<bool> IsSlotBusy( int tutorId, int studentId, DateOnly classDate, string time)
        {
            return await db.Requests.AnyAsync(x =>
                x.Status == "Accepted"
                && x.ClassDate == classDate
                && x.Time == time
                && (
                    x.TutorId == tutorId ||
                    x.StudentId == studentId
                ));
        }

        private DateOnly GetNextDate(string day)
        {
            day = NormalizeDay(day);

            DayOfWeek target = day switch
            {
                "sun" => DayOfWeek.Sunday,
                "mon" => DayOfWeek.Monday,
                "tue" => DayOfWeek.Tuesday,
                "wed" => DayOfWeek.Wednesday,
                "thu" => DayOfWeek.Thursday,
                "fri" => DayOfWeek.Friday,
                "sat" => DayOfWeek.Saturday,
                _ => throw new Exception($"Invalid day value: {day}")
            };

            DateTime today = DateTime.Today;

            while (today.DayOfWeek != target)
            {
                today = today.AddDays(1);
            }

            return DateOnly.FromDateTime(today);
        }

        //Find First Common Free Slot
        private async Task<CommonSlotDto> FindCommonSlot(int tutorId, int studentId)
        {
            var tutorSchedules = await db.Schedules
                .Where(x => x.TutorId == tutorId)
                .ToListAsync();

            var studentSchedules = await db.StudentSchedules
                .Where(x => x.StudentId == studentId)
                .ToListAsync();

            foreach (var tutorSlot in tutorSchedules)
            {
                foreach (var studentSlot in studentSchedules)
                {
                    if (NormalizeDay(tutorSlot.Day) != NormalizeDay(studentSlot.Day))
                        continue;

                    if (NormalizeTime(tutorSlot.Time) != NormalizeTime(studentSlot.Time))
                        continue;

                    DateOnly nextDate = GetNextDate(studentSlot.Day);

                    bool busy = await IsSlotBusy(
                        tutorId,
                        studentId,
                        nextDate,
                        tutorSlot.Time);

                    if (busy)
                        continue;

                    return new CommonSlotDto
                    {
                        Found = true,

                        Day = tutorSlot.Day,

                        Time = tutorSlot.Time,

                        ClassDate = nextDate
                    };
                }
            }

            return new CommonSlotDto
            {
                Found = false
            };
        }

        //Auto Update Existing Request 
        private async Task AutoMoveClass(Request request, CommonSlotDto slot, string requestType)
        {
            request.Day = slot.Day;

            request.Time = slot.Time;

            request.ClassDate = slot.ClassDate;

            request.RequestType = requestType;

            request.Status = "Accepted";

            request.RequestDate = DateTime.Now;

            db.Requests.Update(request);

            await db.SaveChangesAsync();
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

        public DateOnly? StartDate { get; set; }

        public DateOnly? EndDate { get; set; }

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
    public class TutorCourseGradeDto
    {
        public int CourseId { get; set; }
        public string Grade { get; set; }   // A, B, C, D, F
        public decimal HourlyRate { get; set; }
    }

    public class AddTutorCoursesDto
    {
        public List<TutorCourseGradeDto> Courses { get; set; }
    }

    //to get all course
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
    public class AvailableSlotDto
    {
        public string Day { get; set; }
        public string Time { get; set; }
        public DateTime ClassDate { get; set; }
    }

    public class RescheduleRequestDto
    {
        public int ParentRequestId { get; set; }

        public DateTime NewClassDate { get; set; }

        public string Day { get; set; }

        public string Time { get; set; }
    }

    //For Tutor Dashboard
    public class TutorDashboardDto
    {
        public int TotalClasses { get; set; }
        public List<TutorStudentClassDto> Students { get; set; } = new();
    }

    public class TutorStudentClassDto
    {
        public int StudentId { get; set; }
        public string StudentName { get; set; } = "";
        public int TotalClasses { get; set; }
        public List<ClassDetailDto> Classes { get; set; } = new();
    }

    public class ClassDetailDto
    {
        public int RequestId { get; set; }
        public DateOnly? ClassDate { get; set; }
        public string? Time { get; set; }
        public string? RequestType { get; set; }
        public string? Status { get; set; }
    }

    // Course Mark as Done DTO
    public class MarkCourseDoneRequest
    {
        public int StudentId { get; set; }
        public int CourseId { get; set; }
        public string? Grade { get; set; }
    }

    //Get Student studying the course from the logined tutor
    public class TutorStudentCourseDto
    {
        public int StudentId { get; set; }

        public string StudentName { get; set; }

        public int CourseId { get; set; }

        public string CourseName { get; set; }

        public bool IsCompleted { get; set; }

        public DateTime? CompletedDate { get; set; }

        public bool FeedbackGiven { get; set; }
    }

    //Complete Course
    public class CompleteCourseRequest
    {
        public int CourseId { get; set; }
    }

    //Give Feedback
    public class TutorFeedbackRequest
    {
        public int StudentId { get; set; }

        public int CourseId { get; set; }

        public int Rating { get; set; }

        public string Comment { get; set; }
    }

    //Re and Pre Schedule DTO's
    public class AutoScheduleDto
    {
        public int RequestId { get; set; }

        // Reschedule OR Preschedule
        public string RequestType { get; set; } = "";
    }

    public class ManualScheduleDto
    {
        public int RequestId { get; set; }

        public string RequestType { get; set; } = "";

        public string Day { get; set; } = "";

        public string Time { get; set; } = "";

        public DateTime ClassDate { get; set; }
    }

    public class CommonSlotDto
    {
        public bool Found { get; set; }

        public string Day { get; set; } = "";

        public string Time { get; set; } = "";

        public DateOnly ClassDate { get; set; }
    }

    public class PaymentStatusModel
    {
        public int PaymentId { get; set; }
        public string Status { get; set; } // Received / NotReceived
    }
}
