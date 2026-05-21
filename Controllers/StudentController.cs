using HouseofTutorAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace HouseofTutorAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class StudentController : ControllerBase
    {
        private readonly HouseofTutorContext db;
        public StudentController(HouseofTutorContext _db)
        {
            db = _db;
        }

        //Save Schedule like Tutor(Multiple)
        //[Authorize]
        //[HttpPost("save-student-schedule")]
        //public async Task<IActionResult> SaveStudentSchedule([FromBody] StudentSaveScheduleDto dto)
        //{
        //    if (dto == null || dto.slots == null || dto.slots.Count == 0)
        //        return BadRequest(new { message = "No schedule data provided" });

        //    var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);

        //    if (userIdClaim == null)
        //        return Unauthorized(new { message = "Invalid token" });

        //    int userId = int.Parse(userIdClaim.Value);

        //    var student = await db.Students.FirstOrDefaultAsync(s => s.UserId == userId);

        //    if (student == null)
        //        return NotFound(new { message = "Student not found" });

        //    var newSchedules = new List<StudentSchedule>();

        //    foreach (var slot in dto.slots)
        //    {
        //        string day = slot.day.Trim();
        //        string time = slot.time.Trim();

        //        bool exists = await db.Student_Schedules.AnyAsync(s =>
        //            s.StudentId == student.StudentId &&
        //            s.Day.ToLower() == day.ToLower() &&
        //            s.Time.ToLower() == time.ToLower()
        //        );

        //        if (!exists)
        //        {
        //            newSchedules.Add(new StudentSchedule
        //            {
        //                StudentId = student.StudentId,
        //                Day = day,
        //                Time = time
        //            });
        //        }
        //    }

        //    if (newSchedules.Count > 0)
        //    {
        //        await db.Student_Schedules.AddRangeAsync(newSchedules);
        //        await db.SaveChangesAsync();
        //    }

        //    return Ok(new
        //    {
        //        message = "Student schedule saved successfully",
        //        added = newSchedules.Count
        //    });
        //}


        [Authorize]
        [HttpPost("save-student-schedule")]
        public async Task<IActionResult> SaveStudentSchedule([FromBody] StudentSaveScheduleDto dto)
        {
            // ====================================================
            // VALIDATE DTO
            // ====================================================
            if (dto == null)
            {
                return BadRequest(new
                {
                    message = "No data provided"
                });
            }

            // ====================================================
            // GET USER
            // ====================================================
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

            var student = await db.Students
                .FirstOrDefaultAsync(s =>
                    s.UserId == userId);

            if (student == null)
            {
                return NotFound(new
                {
                    message = "Student not found"
                });
            }

            // ====================================================
            // REMOVE OLD SCHEDULES
            // ====================================================
            var oldSchedules = await db.Student_Schedules
                .Where(s => s.StudentId == student.StudentId)
                .ToListAsync();

            if (oldSchedules.Any())
            {
                db.Student_Schedules.RemoveRange(oldSchedules);
                await db.SaveChangesAsync();
            }

            // ====================================================
            // VALIDATION
            // ====================================================
            if (dto.slots == null || dto.slots.Count == 0)
            {
                return BadRequest(new
                {
                    message = "Please select schedule slots"
                });
            }

            var newSchedules = new List<StudentSchedule>();

            // ====================================================
            // SPECIFIC TIME
            // ====================================================
            if (dto.availabilityType?.ToLower() == "specific")
            {
                if (dto.startDate == null ||
                    dto.endDate == null)
                {
                    return BadRequest(new
                    {
                        message =
                            "Start date and End date required"
                    });
                }

                foreach (var slot in dto.slots)
                {
                    newSchedules.Add(new StudentSchedule
                    {
                        StudentId = student.StudentId,

                        Day = slot.day.Trim(),
                        Time = slot.time.Trim(),

                        StartDate = dto.startDate,
                        EndDate = dto.endDate,

                        Type = "Specific Time"
                    });
                }
            }

            // ====================================================
            // FULL TIME
            // ====================================================
            else
            {
                foreach (var slot in dto.slots)
                {
                    newSchedules.Add(new StudentSchedule
                    {
                        StudentId = student.StudentId,

                        Day = slot.day.Trim(),
                        Time = slot.time.Trim(),

                        StartDate = null,
                        EndDate = null,

                        Type = "Full Time"
                    });
                }
            }

            // ====================================================
            // SAVE NEW SCHEDULE
            // ====================================================
            await db.Student_Schedules
                .AddRangeAsync(newSchedules);

            await db.SaveChangesAsync();

            return Ok(new
            {
                message =
                    "Student schedule updated successfully",

                total = newSchedules.Count
            });
        }

        //Shown in StudentHome Slots
        [Authorize]
        [HttpGet("get-student-schedules")]
        public async Task<IActionResult> GetStudentSchedules()
        {
            try
            {
                // =========================================
                // GET USER
                // =========================================
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

                // =========================================
                // FIND STUDENT
                // =========================================
                var student = await db.Students
                    .FirstOrDefaultAsync(s =>
                        s.UserId == userId);

                if (student == null)
                {
                    return NotFound(new
                    {
                        message = "Student not found"
                    });
                }

                // =========================================
                // GET SCHEDULES
                // =========================================
                var schedules = await db.Student_Schedules
                    .Where(s =>
                        s.StudentId == student.StudentId)
                    .Select(s => new
                    {
                        id = s.ScheduleId,

                        day = s.Day,

                        time = s.Time,

                        startDate = s.StartDate,

                        endDate = s.EndDate,

                        type = s.Type
                    })
                    .ToListAsync();

                // =========================================
                // RETURN
                // =========================================
                return Ok(schedules);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = ex.Message
                });
            }
        }

        //Get The Schedule from Database to show in DropDown
        [Authorize]
        [HttpGet("get-student-schedule")]
        public async Task<IActionResult> GetStudentSchedule()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);

                if (userIdClaim == null)
                    return Unauthorized(new { message = "Invalid token" });

                int userId = int.Parse(userIdClaim.Value);

                var student = await db.Students
                    .FirstOrDefaultAsync(s => s.UserId == userId);

                if (student == null)
                    return NotFound(new { message = "Student not found" });

                var schedules = await db.Student_Schedules
                    .Where(s => s.StudentId == student.StudentId)
                    .Select(s => new
                    {
                        day = s.Day,
                        time = s.Time
                    })
                    .OrderBy(s => s.day)
                    .ThenBy(s => s.time)
                    .ToListAsync();

                return Ok(new
                {
                    success = true,
                    message = "Student schedule fetched successfully",
                    data = schedules
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error fetching schedule",
                    error = ex.Message
                });
            }
        }

        // GET MY COURSES 
        [Authorize]
        [HttpGet("my-courses")]
        public async Task<IActionResult> GetStudentCourses()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)
                              ?? User.FindFirst("sub");

            if (userIdClaim == null)
                return Unauthorized(new { message = "Invalid token" });

            int userId = int.Parse(userIdClaim.Value);

            var student = await db.Students
                .FirstOrDefaultAsync(s => s.UserId == userId);

            if (student == null)
                return NotFound(new { message = "Student not found" });

            var courses = await db.StudentCourses
                .Where(sc => sc.StudentId == student.StudentId)
                .Include(sc => sc.Course)
                .Select(sc => new CourseDto
                {
                    course_id = sc.Course.CourseId,
                    course_name = sc.Course.CourseTitle
                })
                .ToListAsync();

            return Ok(courses);
        }

        // GET ALL COURSES
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

        // ADD COURSES
        [Authorize]
        [HttpPost("add-courses")]
        public async Task<IActionResult> AddStudentCourses([FromBody] AddStudentCoursesDto dto)
        {
            if (dto == null || dto.courseIds == null || dto.courseIds.Count == 0)
                return BadRequest(new { message = "No courses selected" });

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)
                              ?? User.FindFirst("sub");

            if (userIdClaim == null)
                return Unauthorized(new { message = "Invalid token" });

            int userId = int.Parse(userIdClaim.Value);

            var student = await db.Students
                .FirstOrDefaultAsync(s => s.UserId == userId);

            if (student == null)
                return NotFound(new { message = "Student not found" });

            var newEntries = new List<StudentCourse>();

            foreach (var courseId in dto.courseIds)
            {
                bool exists = await db.StudentCourses.AnyAsync(sc =>
                    sc.StudentId == student.StudentId &&
                    sc.CourseId == courseId
                );

                if (!exists)
                {
                    newEntries.Add(new StudentCourse
                    {
                        StudentId = student.StudentId,
                        CourseId = courseId
                    });
                }
            }

            if (newEntries.Count > 0)
            {
                await db.StudentCourses.AddRangeAsync(newEntries);
                await db.SaveChangesAsync();
            }

            return Ok(new
            {
                message = "Courses added successfully",
                added = newEntries.Count
            });
        }

        //Search Tutor based on schedule and tutor's Radius
        //[HttpGet("search-by-time-location")]
        //public async Task<IActionResult> SearchTutorByTimeAndLocation(string day, string time, double userLat, double userLng)
        //{
        //    if (string.IsNullOrEmpty(day) || string.IsNullOrEmpty(time))
        //        return BadRequest(new { message = "Day or time missing" });

        //    string inputDay = day.Trim().ToLower();
        //    string inputTime = time.Trim().ToLower().Replace(" ", "");

        //    var tutors = await (
        //        from sch in db.Schedules
        //        join t in db.Tutors on sch.TutorId equals t.TutorId
        //        join u in db.Users on t.UserId equals u.UserId
        //        where sch.Day.ToLower() == inputDay
        //        && sch.Time.ToLower().Replace(" ", "").Contains(inputTime)
        //        && t.Latitude != null
        //        && t.Longitude != null
        //        select new { Tutor = t, User = u }
        //    ).ToListAsync();

        //    var result = tutors
        //        .Select(x =>
        //        {
        //            double distance = CalculateDistance(
        //                userLat,
        //                userLng,
        //                x.Tutor.Latitude ?? 0,
        //                x.Tutor.Longitude ?? 0
        //            );

        //            double tutorRadius = (double)(x.Tutor.Radius ?? 0);

        //            return new TutorSearchResultDto
        //            {
        //                tutor_id = x.Tutor.TutorId,
        //                tutor_name = x.User.FullName,
        //                location = x.Tutor.Location,
        //                distance = distance,
        //                tutor_radius = tutorRadius
        //            };
        //        })
        //        .Where(t =>
        //            t.distance <= t.tutor_radius
        //        )

        //        .OrderBy(t => t.distance)
        //        .ToList();

        //    if (result.Count == 0)
        //        return NotFound(new { message = "No tutors available in your area" });

        //    return Ok(result);
        //}

        [HttpGet("search-by-time-location")]
        public async Task<IActionResult> SearchTutorByTimeAndLocation(
        string day,
        string time,
        double userLat,
        double userLng)
        {
            if (string.IsNullOrEmpty(day) || string.IsNullOrEmpty(time))
            {
                return BadRequest(new
                {
                    message = "Day or time missing"
                });
            }

            string inputDay = day.Trim().ToLower();
            string inputTime = time.Trim().ToLower().Replace(" ", "");

            // ==========================================================
            // FORMAT SEARCH SLOT
            // Example:
            // day = Mon
            // time = 10:00-11:00 am
            //
            // Result:
            // mon,10:00-11:00am
            // ==========================================================
            string searchSlot =
                $"{inputDay},{inputTime}"
                .Replace(" ", "")
                .ToLower();

            // ==========================================================
            // GET BUSY TUTORS
            // Accepted requests only
            // ==========================================================
            var busyTutorIds = await db.Requests
                .Where(r =>
                    r.Status.ToLower() == "accepted"
                    &&
                    r.Time.ToLower().Replace(" ", "") == searchSlot
                )
                .Select(r => r.TutorId)
                .Distinct()
                .ToListAsync();

            // ==========================================================
            // GET AVAILABLE TUTORS
            // ==========================================================
            var tutors = await (
                from sch in db.Schedules
                join t in db.Tutors
                    on sch.TutorId equals t.TutorId

                join u in db.Users
                    on t.UserId equals u.UserId

                where sch.Day.ToLower() == inputDay
                && sch.Time.ToLower().Replace(" ", "") == inputTime

                // REMOVE BUSY TUTORS
                && !busyTutorIds.Contains(t.TutorId)

                && t.Latitude != null
                && t.Longitude != null

                select new
                {
                    Tutor = t,
                    User = u
                }

            ).Distinct().ToListAsync();

            // ==========================================================
            // DISTANCE FILTER
            // ==========================================================
            var result = tutors
                .Select(x =>
                {
                    double distance = CalculateDistance(
                        userLat,
                        userLng,
                        x.Tutor.Latitude ?? 0,
                        x.Tutor.Longitude ?? 0
                    );

                    double tutorRadius =
                        (double)(x.Tutor.Radius ?? 0);

                    return new TutorSearchResultDto
                    {
                        tutor_id = x.Tutor.TutorId,
                        tutor_name = x.User.FullName,
                        location = x.Tutor.Location,
                        distance = distance,
                        tutor_radius = tutorRadius
                    };
                })

                .Where(t =>
                    t.distance <= t.tutor_radius
                )

                .OrderBy(t => t.distance)
                .ToList();

            // ==========================================================
            // NO TUTORS
            // ==========================================================
            if (result.Count == 0)
            {
                return NotFound(new
                {
                    message = "No tutors available in your area"
                });
            }

            return Ok(result);
        }


        //Get All Classes
        [Authorize]
        [HttpGet("my-classes")]
        public async Task<IActionResult> GetStudentClasses()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);

            if (userIdClaim == null)
                return Unauthorized(new { message = "Invalid token" });

            int userId = int.Parse(userIdClaim.Value);

            var student = await db.Students
                .FirstOrDefaultAsync(s => s.UserId == userId);

            if (student == null)
                return NotFound(new { message = "Student not found" });

            var classes = await db.Requests
                .Where(r => r.StudentId == student.StudentId)
                .Include(r => r.Tutor)
                    .ThenInclude(t => t.User)
                .Include(r => r.Course)
                .Select(r => new StudentClassDto
                {
                    request_id = r.RequestId,
                    tutor_name = r.Tutor.User.FullName,
                    course_name = r.Course.CourseTitle,
                    request_date = r.RequestDate,
                    time = r.Time
                })
                .OrderByDescending(r => r.request_date)
                .ToListAsync();

            return Ok(classes);
        }

        //Request to Tutor
        [HttpPost("create-request")]
        public async Task<IActionResult> CreateRequest([FromBody] CreateRequestDto dto)
        {
            Console.WriteLine($"DAY: {dto.day}, TIME: {dto.time}");

            if (string.IsNullOrEmpty(dto.day) || string.IsNullOrEmpty(dto.time))
                return BadRequest(new { message = "Day or time missing" });

            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

            var student = await db.Students
                .FirstOrDefaultAsync(s => s.UserId == userId);

            string fullTime = $"{dto.day}, {dto.time}";

            var request = new Request
            {
                StudentId = student.StudentId,
                TutorId = dto.tutor_id,
                CourseId = dto.course_id,
                Time = fullTime,
                RequestDate = DateTime.UtcNow,
                Status = "Pending"
            };

            db.Requests.Add(request);
            await db.SaveChangesAsync();

            return Ok(new { message = "Saved", fullTime });
        }

        [Authorize]
        [HttpGet("my-profile")]
        public async Task<IActionResult> GetStudentProfile()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);

                if (userIdClaim == null)
                    return Unauthorized(new { message = "Invalid token" });

                int userId = int.Parse(userIdClaim.Value);

                var studentData = await db.Students
                    .Include(s => s.User)
                    .Where(s => s.UserId == userId)
                    .Select(s => new
                    {
                        student_id = s.StudentId,
                        full_name = s.User.FullName,
                        email = s.User.Email,
                        cnic = s.User.Cnic,
                        phone = s.User.Phone,
                        location = s.Location,
                        latitude = s.Latitude,
                        longitude = s.Longitude
                    })
                    .FirstOrDefaultAsync();

                if (studentData == null)
                    return NotFound(new { message = "Student not found" });

                return Ok(studentData);
            }
            catch (Exception ex)
            {
                Console.WriteLine("ERROR: " + ex.Message);

                return StatusCode(500, new
                {
                    message = "Internal server error",
                    error = ex.Message
                });
            }
        }

        //Help Method to Calulate Radius 
        private double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
        {
            // Earth radius in kilometers
            const double R = 6371;

            // Convert degrees to radians
            double dLat = DegreesToRadians(lat2 - lat1);
            double dLon = DegreesToRadians(lon2 - lon1);

            double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                       Math.Cos(DegreesToRadians(lat1)) *
                       Math.Cos(DegreesToRadians(lat2)) *
                       Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

            double distance = R * c;

            return distance;
        }
        private double DegreesToRadians(double degrees)
        {
            return degrees * (Math.PI / 180);
        }

    }


    //Dto stand for (Data Transfer Object)
    public class SearchTutorDto
    {
        public string day { get; set; }
        public string time { get; set; }
        public int student_id { get; set; }
    }

    public class TutorSearchResultDto
    {
        public int tutor_id { get; set; }
        public string tutor_name { get; set; }
        public string location { get; set; }
        public double distance { get; set; }
        public double tutor_radius { get; set; }
    }

    //to get all classes of student
    public class StudentClassDto
    {
        public int request_id { get; set; }
        public string tutor_name { get; set; }
        public string course_name { get; set; }
        public DateTime? request_date { get; set; }
        public string time { get; set; }
    }

    //Student request tutor for class
    public class CreateRequestDto
    {
        public int tutor_id { get; set; }
        public int course_id { get; set; }
        public string day { get; set; }
        public string time { get; set; }
    }
    public class StudentSaveScheduleDto
    {
        public string? availabilityType { get; set; }

        public DateTime? startDate { get; set; }

        public DateTime? endDate { get; set; }

        public List<StudentSlotDto>? slots { get; set; }
    }

    public class StudentSlotDto
    {
        public string? day { get; set; }

        public string? time { get; set; }
    }

    public class AddStudentCoursesDto
    {
        public List<int> courseIds { get; set; }
    }
}