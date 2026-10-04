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
        private readonly IWebHostEnvironment _environment;
        public StudentController(HouseofTutorContext _db, IWebHostEnvironment environment)
        {
            db = _db;
            _environment = environment;
        }

        //Save Schedule like Tutor(Multiple)
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
            var oldSchedules = await db.StudentSchedules
                .Where(s => s.StudentId == student.StudentId)
                .ToListAsync();

            if (oldSchedules.Any())
            {
                db.StudentSchedules.RemoveRange(oldSchedules);
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
            await db.StudentSchedules
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
                var schedules = await db.StudentSchedules
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

                var schedules = await db.StudentSchedules
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

        [Authorize]
        [HttpGet("my-courses-for-student-add-courses")] // get courses of student  for frontend (StudentAddCourses)
        public async Task<IActionResult> GetStudentCoursesForStudentAddCourses()
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
                .Select(sc => new CourseDto
                {
                    course_id = sc.Course.CourseId,
                    course_name = sc.Course.CourseTitle
                })
                .ToListAsync();

            return Ok(courses);
        }

        [Authorize]
        [HttpGet("my-courses")] // get courses of student which Class is not Accepted and for frontend (StudentCourses)
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

                // Hide courses whose request has already been Accepted
                .Where(sc => !db.Requests.Any(r =>
                    r.StudentId == student.StudentId &&
                    r.CourseId == sc.CourseId &&
                    r.Status == "Accepted"))

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
        [Authorize]
        [HttpGet("all-courses")]
        public async Task<IActionResult> GetAllCourses()
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

            var selectedCourseIds = await db.StudentCourses
                .Where(sc => sc.StudentId == student.StudentId)
                .Select(sc => sc.CourseId)
                .ToListAsync();

            var courses = await db.Courses
                .Where(c => !selectedCourseIds.Contains(c.CourseId))
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

        [Authorize]
        [HttpGet("search-by-time-location-visiting-tutor")] // Search tutor and sort Also there is condition to search only VISITING tutor's
        public async Task<IActionResult> SearchTutorByTimeAndLocation( double userLat, double userLng, int courseId, string sortBy = "feedback")
        {
            try
            {
                // =====================================================
                // STEP 0: VERIFY STUDENT
                // =====================================================

                var userIdClaim = User.FindFirst(
                    System.Security.Claims.ClaimTypes.NameIdentifier);

                if (userIdClaim == null)
                {
                    return Unauthorized(new
                    {
                        message = "Invalid token."
                    });
                }

                if (!int.TryParse(userIdClaim.Value, out int userId))
                {
                    return Unauthorized(new
                    {
                        message = "Invalid user ID."
                    });
                }

                var student = await db.Students
                    .FirstOrDefaultAsync(s => s.UserId == userId);

                if (student == null)
                {
                    return Unauthorized(new
                    {
                        message = "Student account not found."
                    });
                }

                int studentId = student.StudentId;


                // =====================================================
                // STEP 1: VALIDATE COURSE
                // =====================================================

                var courseExists = await db.Courses
                    .AnyAsync(c => c.CourseId == courseId);

                if (!courseExists)
                {
                    return NotFound(new
                    {
                        message = "Course not found."
                    });
                }


                // =====================================================
                // STEP 2: NORMALIZE SORT
                // =====================================================

                sortBy = (sortBy ?? "feedback")
                    .Trim()
                    .ToLower();

                var validSortOptions = new[]
                {
                    "institute",
                    "feedback",
                    "grade",
                    "fee"
                };

                if (!validSortOptions.Contains(sortBy))
                {
                    sortBy = "feedback";
                }


                // =====================================================
                // STEP 3: VALIDATE STUDENT LOCATION
                // =====================================================

                if (double.IsNaN(userLat) ||
                    double.IsNaN(userLng) ||
                    double.IsInfinity(userLat) ||
                    double.IsInfinity(userLng))
                {
                    return BadRequest(new
                    {
                        message = "Invalid student location."
                    });
                }


                // =====================================================
                // STEP 4: LOAD STUDENT SCHEDULES
                // =====================================================

                var studentSchedules = await db.StudentSchedules
                    .Where(x => x.StudentId == studentId)
                    .ToListAsync();

                if (studentSchedules.Count == 0)
                {
                    return NotFound(new
                    {
                        message = "Student has no schedules defined."
                    });
                }


                // =====================================================
                // STEP 5: LOAD ACCEPTED REQUESTS
                // =====================================================

                var acceptedRequests = await db.Requests
                    .Where(r => r.Status == "Accepted")
                    .Select(r => new
                    {
                        r.TutorId,
                        r.Day,
                        r.Time,
                        r.RequestType,
                        r.ClassDate
                    })
                    .ToListAsync();


                // =====================================================
                // STEP 6:
                // LOAD ONLY:
                //
                // 1. Selected Course
                // 2. Visiting Tutor
                // 3. Approved Tutor
                // 4. Tutor has Latitude
                // 5. Tutor has Longitude
                //
                // Radius is used later for distance checking.
                // =====================================================

                var allTutorSchedules =
                    await
                    (
                        from sch in db.Schedules

                        join t in db.Tutors
                            on sch.TutorId equals t.TutorId

                        join u in db.Users
                            on t.UserId equals u.UserId

                        join tc in db.TutorCourses
                            on new
                            {
                                TutorId = t.TutorId,
                                CourseId = courseId
                            }
                            equals new
                            {
                                TutorId = tc.TutorId,
                                CourseId = tc.CourseId
                            }

                        join tcr in db.TutorCourseRates
                            on new
                            {
                                TutorId = t.TutorId,
                                CourseId = courseId
                            }
                            equals new
                            {
                                TutorId = tcr.TutorId,
                                CourseId = tcr.CourseId
                            }

                        where

                            // =========================================
                            // SELECTED COURSE
                            // =========================================

                            tc.CourseId == courseId

                            // =========================================
                            // ONLY VISITING TUTOR
                            // Student goes to student's/tutor's place
                            // depending on your Visiting flow
                            // =========================================

                            && t.TeachingMode == "Visiting"

                            // =========================================
                            // ONLY APPROVED TUTOR
                            // =========================================

                            && t.Status == "Approved"

                            // =========================================
                            // TUTOR MUST HAVE LOCATION
                            // =========================================

                            && t.Latitude != null
                            && t.Longitude != null

                        select new
                        {
                            TutorId = t.TutorId,

                            Tutor = t,

                            User = u,

                            // =========================================
                            // COURSE INFORMATION
                            // =========================================

                            Institute = tc.Institute,

                            Grade = tc.Grade,

                            // =========================================
                            // HOURLY RATE
                            // =========================================

                            HourlyRate = tcr.HourlyRate,

                            // =========================================
                            // TUTOR SCHEDULE
                            // =========================================

                            SlotDay = sch.Day,

                            SlotTime = sch.Time
                        }
                    )
                    .ToListAsync();


                Console.WriteLine(
                    $"TOTAL APPROVED VISITING TUTOR SLOTS LOADED: " +
                    $"{allTutorSchedules.Count}");


                // =====================================================
                // STEP 7: FIND COMMON SLOTS
                // =====================================================

                var tutorsWithCommonSlots = allTutorSchedules
                    .GroupBy(x => x.TutorId)
                    .Select(g =>
                    {
                        var tutorSlots = g.ToList();

                        var commonSlots =
                            new List<TutorAvailableSlotDto>();


                        foreach (var ts in tutorSlots)
                        {
                            foreach (var ss in studentSchedules)
                            {
                                // =====================================
                                // CHECK DAY
                                // =====================================

                                if (NormalizeDay(ts.SlotDay) !=
                                    NormalizeDay(ss.Day))
                                {
                                    continue;
                                }


                                // =====================================
                                // CHECK TIME
                                // =====================================

                                if (NormalizeTime(ts.SlotTime) !=
                                    NormalizeTime(ss.Time))
                                {
                                    continue;
                                }


                                // =====================================
                                // CHECK ACCEPTED REQUEST
                                // =====================================

                                var request =
                                    acceptedRequests.FirstOrDefault(r =>
                                        r.TutorId == ts.TutorId &&

                                        NormalizeDay(r.Day) ==
                                        NormalizeDay(ts.SlotDay) &&

                                        NormalizeTime(r.Time) ==
                                        NormalizeTime(ts.SlotTime));


                                // =====================================
                                // NO ACCEPTED REQUEST
                                // =====================================

                                if (request == null)
                                {
                                    commonSlots.Add(
                                        new TutorAvailableSlotDto
                                        {
                                            day = ts.SlotDay,

                                            time = ts.SlotTime,

                                            is_available = true,

                                            availability_message =
                                                "Available",

                                            request_type = "",

                                            class_date = null
                                        });

                                    continue;
                                }


                                // =====================================
                                // NORMAL CLASS
                                // =====================================

                                if (string.Equals(
                                    request.RequestType,
                                    "Normal",
                                    StringComparison.OrdinalIgnoreCase))
                                {
                                    continue;
                                }


                                // =====================================
                                // RESCHEDULE / PRESCHEDULE
                                // =====================================

                                string message =
                                    $"Not Available on this {request.Day}. " +
                                    $"Available onward.";


                                commonSlots.Add(
                                    new TutorAvailableSlotDto
                                    {
                                        day = ts.SlotDay,

                                        time = ts.SlotTime,

                                        is_available = false,

                                        availability_message =
                                            message,

                                        request_type =
                                            request.RequestType,

                                        class_date =
                                            request.ClassDate
                                    });
                            }
                        }


                        // =============================================
                        // REMOVE DUPLICATE SLOTS
                        // =============================================

                        commonSlots = commonSlots
                            .GroupBy(x => new
                            {
                                x.day,
                                x.time
                            })
                            .Select(x => x.First())
                            .ToList();


                        var first = tutorSlots.First();


                        return new
                        {
                            TutorId = g.Key,

                            Tutor = first.Tutor,

                            User = first.User,

                            // =========================================
                            // COURSE INFORMATION
                            // =========================================

                            Institute = first.Institute,

                            Grade = first.Grade,

                            // =========================================
                            // HOURLY RATE
                            // =========================================

                            HourlyRate = first.HourlyRate,

                            // =========================================
                            // COMMON SLOTS
                            // =========================================

                            CommonSlots = commonSlots
                        };
                    })
                    .Where(x => x.CommonSlots.Any())
                    .ToList();


                // =====================================================
                // STEP 8: GET FEEDBACK / RATINGS
                // =====================================================

                var tutorRatings = await db.Feedbacks
                    .Where(f =>
                        f.FeedbackBy == "Student" &&
                        f.CourseId == courseId)
                    .GroupBy(f => f.TutorId)
                    .Select(g => new
                    {
                        TutorId = g.Key,

                        AverageRating =
                            g.Average(x => x.Rating),

                        TotalReviews =
                            g.Count()
                    })
                    .ToListAsync();


                var ratingsLookup =
                    tutorRatings.ToDictionary(
                        r => r.TutorId);


                // =====================================================
                // STEP 9: BUILD RESULT
                // =====================================================

                var result = tutorsWithCommonSlots
                    .Select(x =>
                    {
                        // =============================================
                        // CALCULATE DISTANCE
                        // =============================================

                        double distance =
                            CalculateDistance(
                                userLat,
                                userLng,
                                x.Tutor.Latitude ?? 0,
                                x.Tutor.Longitude ?? 0);


                        // =============================================
                        // GET TUTOR RADIUS
                        // =============================================

                        double tutorRadius =
                            (double)(x.Tutor.Radius ?? 0);


                        // =============================================
                        // GET RATING
                        // =============================================

                        ratingsLookup.TryGetValue(
                            x.TutorId,
                            out var ratingInfo);


                        double averageRating =
                            ratingInfo != null
                                ? Math.Round(
                                    (double)ratingInfo.AverageRating,
                                    1)
                                : 0;


                        int totalReviews =
                            ratingInfo?.TotalReviews ?? 0;


                        // =============================================
                        // RETURN TUTOR
                        // =============================================

                        return new TutorSearchResultDto
                        {
                            // =========================================
                            // BASIC TUTOR INFORMATION
                            // =========================================

                            tutor_id =
                                x.TutorId,

                            tutor_name =
                                x.User.FullName,

                            location =
                                x.Tutor.Location,

                            // =========================================
                            // DISTANCE
                            // =========================================

                            distance =
                                Math.Round(distance, 2),

                            // =========================================
                            // TUTOR RADIUS
                            // =========================================

                            tutor_radius =
                                tutorRadius,

                            // =========================================
                            // COURSE INFORMATION
                            // =========================================

                            institute =
                                x.Institute,

                            grade =
                                x.Grade,

                            // =========================================
                            // HOURLY RATE
                            // =========================================

                            hourly_rate =
                                x.HourlyRate,

                            // =========================================
                            // FEEDBACK
                            // =========================================

                            average_rating =
                                averageRating,

                            total_reviews =
                                totalReviews,

                            // =========================================
                            // COMMON SLOTS
                            // =========================================

                            common_slots =
                                x.CommonSlots
                        };
                    })

                    // =================================================
                    // IMPORTANT:
                    // Only tutors inside their radius are returned.
                    // =================================================

                    .Where(t =>
                        t.distance <= t.tutor_radius)

                    .ToList();


                // =====================================================
                // STEP 10: SORT
                // =====================================================

                switch (sortBy)
                {
                    // =================================================
                    // INSTITUTE A-Z
                    // =================================================

                    case "institute":

                        result = result
                            .OrderBy(x =>
                                x.institute ?? "")
                            .ThenByDescending(x =>
                                x.average_rating)
                            .ThenBy(x =>
                                x.distance)
                            .ToList();

                        break;


                    // =================================================
                    // FEEDBACK HIGH TO LOW
                    // =================================================

                    case "feedback":

                        result = result
                            .OrderByDescending(x =>
                                x.average_rating)
                            .ThenByDescending(x =>
                                x.total_reviews)
                            .ThenBy(x =>
                                x.distance)
                            .ToList();

                        break;


                    // =================================================
                    // GRADE
                    // =================================================

                    case "grade":

                        result = result
                            .OrderBy(x =>
                                GetGradeOrder(x.grade))
                            .ThenByDescending(x =>
                                x.average_rating)
                            .ThenBy(x =>
                                x.distance)
                            .ToList();

                        break;


                    // =================================================
                    // FEE LOW TO HIGH
                    // =================================================

                    case "fee":

                        result = result
                            .OrderBy(x =>
                                x.hourly_rate)
                            .ThenByDescending(x =>
                                x.average_rating)
                            .ThenBy(x =>
                                x.distance)
                            .ToList();

                        break;


                    // =================================================
                    // DEFAULT = FEEDBACK
                    // =================================================

                    default:

                        result = result
                            .OrderByDescending(x =>
                                x.average_rating)
                            .ThenByDescending(x =>
                                x.total_reviews)
                            .ThenBy(x =>
                                x.distance)
                            .ToList();

                        break;
                }


                // =====================================================
                // STEP 11: NO RESULT
                // =====================================================

                if (result.Count == 0)
                {
                    return NotFound(new
                    {
                        message =
                            "No approved Visiting tutors available in your area for this course."
                    });
                }


                // =====================================================
                // STEP 12: FINAL RESPONSE
                // =====================================================

                return Ok(new
                {
                    sort_by = sortBy,

                    course_id = courseId,

                    teaching_mode = "Visiting",

                    total_tutors = result.Count,

                    tutors = result
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    "SEARCH VISITING TUTOR ERROR: " +
                    ex.ToString());

                return StatusCode(
                    500,
                    new
                    {
                        message =
                            "An error occurred while searching Visiting tutors.",

                        error =
                            ex.Message
                    });
            }
        }

        [Authorize]
        [HttpGet("search-non-visiting-tutors")]
        public async Task<IActionResult> SearchNonVisitingTutors(int courseId,string sortBy = "feedback")
        {
            try
            {
                // =====================================================
                // STEP 0: VERIFY STUDENT
                // =====================================================

                var userIdClaim = User.FindFirst(
                    System.Security.Claims.ClaimTypes.NameIdentifier);

                if (userIdClaim == null)
                {
                    return Unauthorized(new
                    {
                        message = "Invalid token."
                    });
                }

                if (!int.TryParse(userIdClaim.Value, out int userId))
                {
                    return Unauthorized(new
                    {
                        message = "Invalid user ID."
                    });
                }

                var student = await db.Students
                    .FirstOrDefaultAsync(s => s.UserId == userId);

                if (student == null)
                {
                    return Unauthorized(new
                    {
                        message = "Student account not found."
                    });
                }

                int studentId = student.StudentId;


                // =====================================================
                // STEP 1: VALIDATE COURSE
                // =====================================================

                var courseExists = await db.Courses
                    .AnyAsync(c => c.CourseId == courseId);

                if (!courseExists)
                {
                    return NotFound(new
                    {
                        message = "Course not found."
                    });
                }


                // =====================================================
                // STEP 2: NORMALIZE SORT
                // =====================================================

                sortBy = (sortBy ?? "feedback")
                    .Trim()
                    .ToLower();

                var validSortOptions = new[]
                {
                    "institute",
                    "feedback",
                    "grade",
                    "fee"
                };

                if (!validSortOptions.Contains(sortBy))
                {
                    sortBy = "feedback";
                }


                // =====================================================
                // STEP 3: LOAD STUDENT SCHEDULES
                // =====================================================

                var studentSchedules = await db.StudentSchedules
                    .Where(x => x.StudentId == studentId)
                    .ToListAsync();

                if (studentSchedules.Count == 0)
                {
                    return NotFound(new
                    {
                        message = "Student has no schedules defined."
                    });
                }


                // =====================================================
                // STEP 4: LOAD ACCEPTED REQUESTS
                // =====================================================

                var acceptedRequests = await db.Requests
                    .Where(r =>
                        r.Status == "Accepted")
                    .Select(r => new
                    {
                        r.TutorId,
                        r.Day,
                        r.Time,
                        r.RequestType,
                        r.ClassDate
                    })
                    .ToListAsync();


                // =====================================================
                // STEP 5: LOAD ACTIVE LT ROOMS
                // =====================================================
                //
                // LT_Room:
                //
                // room_name
                // capacity
                // status
                //
                // Only Active rooms are considered.
                // =====================================================

                var ltRooms = await db.LtRooms
                    .Where(r => r.Status == "Active")
                    .Select(r => new
                    {
                        r.LtRoomId,
                        r.RoomName,
                        r.Capacity,
                        r.Status
                    })
                    .ToListAsync();


                // =====================================================
                // STEP 5.1: LOAD LT ROOM ADMIN AVAILABILITY SCHEDULE
                // =====================================================
                //
                // Admin decides:
                //
                // LT 1 -> Monday -> 4:00-5:00 pm -> Available
                // LT 1 -> Tuesday -> 4:00-5:00 pm -> Unavailable
                //
                // =====================================================

                var ltRoomSchedules = await db.LtRoomSchedules
                    .Where(x =>
                        x.Status == "Available")
                    .ToListAsync();


                // =====================================================
                // STEP 5.2: LOAD LT BOOKINGS
                // =====================================================
                //
                // IMPORTANT:
                //
                // We DO NOT store 0/1/2/3 in another column.
                //
                // We calculate the current count from bookings.
                //
                // Booked      = count
                // Cancelled   = ignored
                // Completed   = ignored for current availability
                //
                // =====================================================

                var ltBookings = await db.LtRoomBookings
                    .Where(x =>
                        x.Status == "Booked")
                    .Select(x => new
                    {
                        x.LtBookingId,
                        x.LtRoomId,
                        x.TutorId,
                        x.CourseId,
                        x.StudyGroupId,
                        x.Day,
                        x.StartTime,
                        x.EndTime,
                        x.ClassDate,
                        x.Status
                    })
                    .ToListAsync();


                Console.WriteLine(
                    "========================================");

                Console.WriteLine(
                    $"ACTIVE LT ROOMS: {ltRooms.Count}");

                Console.WriteLine(
                    $"AVAILABLE LT SCHEDULES: {ltRoomSchedules.Count}");

                Console.WriteLine(
                    $"ACTIVE LT BOOKINGS: {ltBookings.Count}");

                Console.WriteLine(
                    "========================================");


                // =====================================================
                // STEP 5.3: LOAD ONLY NON-VISITING + APPROVED TUTORS
                // =====================================================

                var allTutorSchedules =
                    await
                    (
                        from sch in db.Schedules

                        join t in db.Tutors
                            on sch.TutorId equals t.TutorId

                        join u in db.Users
                            on t.UserId equals u.UserId

                        join tc in db.TutorCourses
                            on new
                            {
                                TutorId = t.TutorId,
                                CourseId = courseId
                            }
                            equals new
                            {
                                TutorId = tc.TutorId,
                                CourseId = tc.CourseId
                            }

                        join tcr in db.TutorCourseRates
                            on new
                            {
                                TutorId = t.TutorId,
                                CourseId = courseId
                            }
                            equals new
                            {
                                TutorId = tcr.TutorId,
                                CourseId = tcr.CourseId
                            }

                        where

                            tc.CourseId == courseId

                            && t.TeachingMode == "Non-Visiting"

                            && t.Status == "Approved"

                        select new
                        {
                            TutorId = t.TutorId,

                            Tutor = t,

                            User = u,

                            Institute = tc.Institute,

                            Grade = tc.Grade,

                            HourlyRate = tcr.HourlyRate,

                            SlotDay = sch.Day,

                            SlotTime = sch.Time
                        }
                    )
                    .ToListAsync();


                Console.WriteLine(
                    $"TOTAL APPROVED NON-VISITING TUTOR SLOTS LOADED: " +
                    $"{allTutorSchedules.Count}");


                // =====================================================
                // STEP 6: FIND COMMON SLOTS
                // =====================================================

                var tutorsWithCommonSlots = allTutorSchedules
                    .GroupBy(x => x.TutorId)
                    .Select(g =>
                    {
                        var tutorSlots = g.ToList();

                        var commonSlots =
                            new List<TutorAvailableSlotDto>();


                        foreach (var ts in tutorSlots)
                        {
                            foreach (var ss in studentSchedules)
                            {
                                // =====================================
                                // CHECK DAY
                                // =====================================

                                if (NormalizeDay(ts.SlotDay) !=
                                    NormalizeDay(ss.Day))
                                {
                                    continue;
                                }


                                // =====================================
                                // CHECK TIME
                                // =====================================

                                if (NormalizeTime(ts.SlotTime) !=
                                    NormalizeTime(ss.Time))
                                {
                                    continue;
                                }


                                // =====================================
                                // CHECK ACCEPTED REQUEST
                                // =====================================

                                var request =
                                    acceptedRequests.FirstOrDefault(r =>
                                        r.TutorId == ts.TutorId &&

                                        NormalizeDay(r.Day) ==
                                        NormalizeDay(ts.SlotDay) &&

                                        NormalizeTime(r.Time) ==
                                        NormalizeTime(ts.SlotTime));


                                // =====================================
                                // NORMAL REQUEST EXISTS
                                // =====================================

                                if (request != null &&
                                    string.Equals(
                                        request.RequestType,
                                        "Normal",
                                        StringComparison.OrdinalIgnoreCase))
                                {
                                    continue;
                                }


                                // =====================================
                                // PARSE TUTOR SLOT TIME
                                // =====================================

                                if (!TryParseTimeRange(
                                        ts.SlotTime,
                                        out TimeSpan tutorStartTime,
                                        out TimeSpan tutorEndTime))
                                {
                                    Console.WriteLine(
                                        $"Unable to parse tutor time: {ts.SlotTime}");

                                    continue;
                                }


                                // =================================================
                                // STEP 6.1:
                                // FIND AVAILABLE LT ROOM
                                // =================================================

                                var availableLtRoom = ltRooms
                                    .Select(room =>
                                    {
                                        // =========================================
                                        // FIND ADMIN SCHEDULE FOR THIS ROOM
                                        // =========================================

                                        var roomSchedules =
                                            ltRoomSchedules
                                                .Where(ls =>
                                                    ls.LtRoomId ==
                                                    room.LtRoomId &&

                                                    NormalizeDay(ls.Day) ==
                                                    NormalizeDay(ts.SlotDay) &&

                                                    NormalizeTime(ls.Time) ==
                                                    NormalizeTime(ts.SlotTime))
                                                .ToList();


                                        // =========================================
                                        // ROOM NOT AVAILABLE BY ADMIN
                                        // =========================================

                                        if (!roomSchedules.Any())
                                        {
                                            return null;
                                        }


                                        // =========================================
                                        // CHECK LT ROOM CAPACITY
                                        // =========================================

                                        int bookedCount =
                                            ltBookings.Count(b =>
                                                b.LtRoomId ==
                                                room.LtRoomId &&

                                                NormalizeDay(b.Day) ==
                                                NormalizeDay(ts.SlotDay) &&

                                                IsTimeOverlapping(
                                                    tutorStartTime,
                                                    tutorEndTime,
                                                    b.StartTime,
                                                    b.EndTime));


                                        // =========================================
                                        // CAPACITY
                                        // =========================================

                                        int capacity =
                                            room.Capacity > 0
                                                ? room.Capacity
                                                : 3;


                                        // =========================================
                                        // ROOM IS FULL
                                        // =========================================

                                        if (bookedCount >= capacity)
                                        {
                                            Console.WriteLine(
                                                $"LT FULL -> " +
                                                $"Room: {room.RoomName}, " +
                                                $"Day: {ts.SlotDay}, " +
                                                $"Time: {ts.SlotTime}, " +
                                                $"Booked: {bookedCount}, " +
                                                $"Capacity: {capacity}");

                                            return null;
                                        }


                                        // =========================================
                                        // ROOM AVAILABLE
                                        // =========================================

                                        return new
                                        {
                                            LtRoomId =
                                                room.LtRoomId,

                                            RoomName =
                                                room.RoomName,

                                            Capacity =
                                                capacity,

                                            BookedCount =
                                                bookedCount,

                                            RemainingCapacity =
                                                capacity - bookedCount
                                        };
                                    })
                                    .FirstOrDefault(x => x != null);


                                // =================================================
                                // STEP 6.2:
                                // NO LT ROOM AVAILABLE
                                // =================================================

                                if (availableLtRoom == null)
                                {
                                    Console.WriteLine(
                                        $"NO LT AVAILABLE -> " +
                                        $"Tutor: {ts.TutorId}, " +
                                        $"Day: {ts.SlotDay}, " +
                                        $"Time: {ts.SlotTime}");

                                    continue;
                                }


                                // =================================================
                                // STEP 6.3:
                                // RESCHEDULE / PRESCHEDULE
                                // =================================================

                                if (request != null)
                                {
                                    string message =
                                        $"Available in {availableLtRoom.RoomName}. " +
                                        $"{availableLtRoom.RemainingCapacity} " +
                                        $"LT place(s) remaining.";


                                    commonSlots.Add(
                                        new TutorAvailableSlotDto
                                        {
                                            day =
                                                ts.SlotDay,

                                            time =
                                                ts.SlotTime,

                                            is_available =
                                                false,

                                            availability_message =
                                                message,

                                            request_type =
                                                request.RequestType,

                                            class_date =
                                                request.ClassDate,

                                            lt_room_id =
                                                availableLtRoom.LtRoomId,

                                            lt_room_name =
                                                availableLtRoom.RoomName,

                                            lt_room_capacity =
                                                availableLtRoom.Capacity,

                                            lt_booked_count =
                                                availableLtRoom.BookedCount,

                                            lt_remaining_capacity =
                                                availableLtRoom.RemainingCapacity
                                        });

                                    continue;
                                }


                                // =================================================
                                // STEP 6.4:
                                // SLOT AVAILABLE
                                // =================================================

                                commonSlots.Add(
                                    new TutorAvailableSlotDto
                                    {
                                        day =
                                            ts.SlotDay,

                                        time =
                                            ts.SlotTime,

                                        is_available =
                                            true,

                                        availability_message =
                                            $"Available in " +
                                            $"{availableLtRoom.RoomName}. " +
                                            $"{availableLtRoom.RemainingCapacity} " +
                                            $"LT place(s) remaining.",

                                        request_type =
                                            "",

                                        class_date =
                                            null,

                                        lt_room_id =
                                            availableLtRoom.LtRoomId,

                                        lt_room_name =
                                            availableLtRoom.RoomName,

                                        lt_room_capacity =
                                            availableLtRoom.Capacity,

                                        lt_booked_count =
                                            availableLtRoom.BookedCount,

                                        lt_remaining_capacity =
                                            availableLtRoom.RemainingCapacity
                                    });
                            }
                        }


                        // =============================================
                        // REMOVE DUPLICATE SLOTS
                        // =============================================

                        commonSlots = commonSlots
                            .GroupBy(x => new
                            {
                                x.day,
                                x.time
                            })
                            .Select(x => x.First())
                            .ToList();


                        var first = tutorSlots.First();


                        return new
                        {
                            TutorId =
                                g.Key,

                            Tutor =
                                first.Tutor,

                            User =
                                first.User,

                            Institute =
                                first.Institute,

                            Grade =
                                first.Grade,

                            HourlyRate =
                                first.HourlyRate,

                            CommonSlots =
                                commonSlots
                        };
                    })
                    .Where(x =>
                        x.CommonSlots.Any(s =>
                            s.is_available))
                    .ToList();


                // =====================================================
                // STEP 7: GET FEEDBACK / RATINGS
                // =====================================================

                var tutorRatings = await db.Feedbacks
                    .Where(f =>
                        f.FeedbackBy == "Student" &&
                        f.CourseId == courseId)
                    .GroupBy(f => f.TutorId)
                    .Select(g => new
                    {
                        TutorId =
                            g.Key,

                        AverageRating =
                            g.Average(x => x.Rating),

                        TotalReviews =
                            g.Count()
                    })
                    .ToListAsync();


                var ratingsLookup =
                    tutorRatings.ToDictionary(
                        r => r.TutorId);


                // =====================================================
                // STEP 8: BUILD RESULT
                // =====================================================

                var result = tutorsWithCommonSlots
                    .Select(x =>
                    {
                        ratingsLookup.TryGetValue(
                            x.TutorId,
                            out var ratingInfo);


                        double averageRating =
                            ratingInfo != null
                                ? Math.Round(
                                    (double)ratingInfo.AverageRating,
                                    1)
                                : 0;


                        int totalReviews =
                            ratingInfo?.TotalReviews ?? 0;


                        return new NonVisitingTutorSearchResultDto
                        {
                            tutor_id =
                                x.TutorId,

                            tutor_name =
                                x.User.FullName,

                            location =
                                x.Tutor.Location,

                            qualification =
                                x.Tutor.Qualification,

                            experience =
                                x.Tutor.Experience,

                            teaching_mode =
                                x.Tutor.TeachingMode,

                            course_id =
                                courseId,

                            institute =
                                x.Institute,

                            grade =
                                x.Grade,

                            hourly_rate =
                                x.HourlyRate,

                            average_rating =
                                averageRating,

                            total_reviews =
                                totalReviews,

                            common_slots =
                                x.CommonSlots
                        };
                    })
                    .ToList();


                // =====================================================
                // STEP 9: SORT
                // =====================================================

                switch (sortBy)
                {
                    case "institute":

                        result = result
                            .OrderBy(x =>
                                x.institute ?? "")
                            .ThenByDescending(x =>
                                x.average_rating)
                            .ToList();

                        break;


                    case "feedback":

                        result = result
                            .OrderByDescending(x =>
                                x.average_rating)
                            .ThenByDescending(x =>
                                x.total_reviews)
                            .ToList();

                        break;


                    case "grade":

                        result = result
                            .OrderBy(x =>
                                GetGradeOrder(x.grade))
                            .ThenByDescending(x =>
                                x.average_rating)
                            .ToList();

                        break;


                    case "fee":

                        result = result
                            .OrderBy(x =>
                                x.hourly_rate)
                            .ThenByDescending(x =>
                                x.average_rating)
                            .ToList();

                        break;


                    default:

                        result = result
                            .OrderByDescending(x =>
                                x.average_rating)
                            .ThenByDescending(x =>
                                x.total_reviews)
                            .ToList();

                        break;
                }


                // =====================================================
                // STEP 10: NO RESULT
                // =====================================================

                if (result.Count == 0)
                {
                    return NotFound(new
                    {
                        message =
                            "No approved Non-Visiting tutors are available for this course and selected schedule because no LT room has available capacity."
                    });
                }


                // =====================================================
                // STEP 11: FINAL RESPONSE
                // =====================================================

                return Ok(new
                {
                    sort_by =
                        sortBy,

                    course_id =
                        courseId,

                    teaching_mode =
                        "Non-Visiting",

                    total_tutors =
                        result.Count,

                    tutors =
                        result
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    "SEARCH NON-VISITING TUTOR ERROR: " +
                    ex.ToString());

                return StatusCode(
                    500,
                    new
                    {
                        message =
                            "An error occurred while searching Non-Visiting tutors.",

                        error =
                            ex.Message
                    });
            }
        }
        //[Authorize]
        //[HttpGet("search-non-visiting-tutors")]
        //public async Task<IActionResult> SearchNonVisitingTutors( int courseId, string sortBy = "feedback")
        //{
        //    try
        //    {
        //        // =====================================================
        //        // STEP 0: VERIFY STUDENT
        //        // =====================================================

        //        var userIdClaim = User.FindFirst(
        //            System.Security.Claims.ClaimTypes.NameIdentifier);

        //        if (userIdClaim == null)
        //        {
        //            return Unauthorized(new
        //            {
        //                message = "Invalid token."
        //            });
        //        }

        //        if (!int.TryParse(userIdClaim.Value, out int userId))
        //        {
        //            return Unauthorized(new
        //            {
        //                message = "Invalid user ID."
        //            });
        //        }

        //        var student = await db.Students
        //            .FirstOrDefaultAsync(s => s.UserId == userId);

        //        if (student == null)
        //        {
        //            return Unauthorized(new
        //            {
        //                message = "Student account not found."
        //            });
        //        }

        //        int studentId = student.StudentId;


        //        // =====================================================
        //        // STEP 1: VALIDATE COURSE
        //        // =====================================================

        //        var courseExists = await db.Courses
        //            .AnyAsync(c => c.CourseId == courseId);

        //        if (!courseExists)
        //        {
        //            return NotFound(new
        //            {
        //                message = "Course not found."
        //            });
        //        }


        //        // =====================================================
        //        // STEP 2: NORMALIZE SORT
        //        // =====================================================

        //        sortBy = (sortBy ?? "feedback")
        //            .Trim()
        //            .ToLower();

        //        var validSortOptions = new[]
        //        {
        //            "institute",
        //            "feedback",
        //            "grade",
        //            "fee"
        //        };

        //        if (!validSortOptions.Contains(sortBy))
        //        {
        //            sortBy = "feedback";
        //        }


        //        // =====================================================
        //        // STEP 3: LOAD STUDENT SCHEDULES
        //        // =====================================================

        //        var studentSchedules = await db.StudentSchedules
        //            .Where(x => x.StudentId == studentId)
        //            .ToListAsync();

        //        if (studentSchedules.Count == 0)
        //        {
        //            return NotFound(new
        //            {
        //                message = "Student has no schedules defined."
        //            });
        //        }


        //        // =====================================================
        //        // STEP 4: LOAD ACCEPTED REQUESTS
        //        // =====================================================

        //        var acceptedRequests = await db.Requests
        //            .Where(r => r.Status == "Accepted")
        //            .Select(r => new
        //            {
        //                r.TutorId,
        //                r.Day,
        //                r.Time,
        //                r.RequestType,
        //                r.ClassDate
        //            })
        //            .ToListAsync();


        //        // =====================================================
        //        // STEP 5:
        //        // LOAD ONLY NON-VISITING + APPROVED TUTORS
        //        //
        //        // IMPORTANT:
        //        // NO Latitude
        //        // NO Longitude
        //        // NO Radius
        //        // NO Distance Calculation
        //        //
        //        // Student will visit the tutor's place.
        //        // =====================================================

        //        var allTutorSchedules =
        //            await
        //            (
        //                from sch in db.Schedules

        //                join t in db.Tutors
        //                    on sch.TutorId equals t.TutorId

        //                join u in db.Users
        //                    on t.UserId equals u.UserId

        //                join tc in db.TutorCourses
        //                    on new
        //                    {
        //                        TutorId = t.TutorId,
        //                        CourseId = courseId
        //                    }
        //                    equals new
        //                    {
        //                        TutorId = tc.TutorId,
        //                        CourseId = tc.CourseId
        //                    }

        //                join tcr in db.TutorCourseRates
        //                    on new
        //                    {
        //                        TutorId = t.TutorId,
        //                        CourseId = courseId
        //                    }
        //                    equals new
        //                    {
        //                        TutorId = tcr.TutorId,
        //                        CourseId = tcr.CourseId
        //                    }

        //                where

        //                    // =========================================
        //                    // SELECTED COURSE
        //                    // =========================================

        //                    tc.CourseId == courseId

        //                    // =========================================
        //                    // ONLY NON-VISITING TUTOR
        //                    // =========================================

        //                    && t.TeachingMode == "Non-Visiting"

        //                    // =========================================
        //                    // ONLY APPROVED TUTOR
        //                    // =========================================

        //                    && t.Status == "Approved"

        //                select new
        //                {
        //                    TutorId = t.TutorId,

        //                    Tutor = t,

        //                    User = u,

        //                    // =========================================
        //                    // COURSE INFORMATION
        //                    // =========================================

        //                    Institute = tc.Institute,

        //                    Grade = tc.Grade,

        //                    // =========================================
        //                    // HOURLY RATE
        //                    // =========================================

        //                    HourlyRate = tcr.HourlyRate,

        //                    // =========================================
        //                    // TUTOR SCHEDULE
        //                    // =========================================

        //                    SlotDay = sch.Day,

        //                    SlotTime = sch.Time
        //                }
        //            )
        //            .ToListAsync();


        //        Console.WriteLine(
        //            $"TOTAL APPROVED NON-VISITING TUTOR SLOTS LOADED: " +
        //            $"{allTutorSchedules.Count}");


        //        // =====================================================
        //        // STEP 6: FIND COMMON SLOTS
        //        // =====================================================

        //        var tutorsWithCommonSlots = allTutorSchedules
        //            .GroupBy(x => x.TutorId)
        //            .Select(g =>
        //            {
        //                var tutorSlots = g.ToList();

        //                var commonSlots =
        //                    new List<TutorAvailableSlotDto>();


        //                foreach (var ts in tutorSlots)
        //                {
        //                    foreach (var ss in studentSchedules)
        //                    {
        //                        // =====================================
        //                        // CHECK DAY
        //                        // =====================================

        //                        if (NormalizeDay(ts.SlotDay) !=
        //                            NormalizeDay(ss.Day))
        //                        {
        //                            continue;
        //                        }


        //                        // =====================================
        //                        // CHECK TIME
        //                        // =====================================

        //                        if (NormalizeTime(ts.SlotTime) !=
        //                            NormalizeTime(ss.Time))
        //                        {
        //                            continue;
        //                        }


        //                        // =====================================
        //                        // CHECK ACCEPTED REQUEST
        //                        // =====================================

        //                        var request =
        //                            acceptedRequests.FirstOrDefault(r =>
        //                                r.TutorId == ts.TutorId &&

        //                                NormalizeDay(r.Day) ==
        //                                NormalizeDay(ts.SlotDay) &&

        //                                NormalizeTime(r.Time) ==
        //                                NormalizeTime(ts.SlotTime));


        //                        // =====================================
        //                        // NO ACCEPTED REQUEST
        //                        // =====================================

        //                        if (request == null)
        //                        {
        //                            commonSlots.Add(
        //                                new TutorAvailableSlotDto
        //                                {
        //                                    day = ts.SlotDay,

        //                                    time = ts.SlotTime,

        //                                    is_available = true,

        //                                    availability_message =
        //                                        "Available",

        //                                    request_type = "",

        //                                    class_date = null
        //                                });

        //                            continue;
        //                        }


        //                        // =====================================
        //                        // NORMAL CLASS
        //                        // =====================================

        //                        if (string.Equals(
        //                            request.RequestType,
        //                            "Normal",
        //                            StringComparison.OrdinalIgnoreCase))
        //                        {
        //                            continue;
        //                        }


        //                        // =====================================
        //                        // RESCHEDULE / PRESCHEDULE
        //                        // =====================================

        //                        string message =
        //                            $"Not Available on this {request.Day}. " +
        //                            $"Available onward.";


        //                        commonSlots.Add(
        //                            new TutorAvailableSlotDto
        //                            {
        //                                day = ts.SlotDay,

        //                                time = ts.SlotTime,

        //                                is_available = false,

        //                                availability_message =
        //                                    message,

        //                                request_type =
        //                                    request.RequestType,

        //                                class_date =
        //                                    request.ClassDate
        //                            });
        //                    }
        //                }


        //                // =============================================
        //                // REMOVE DUPLICATE SLOTS
        //                // =============================================

        //                commonSlots = commonSlots
        //                    .GroupBy(x => new
        //                    {
        //                        x.day,
        //                        x.time
        //                    })
        //                    .Select(x => x.First())
        //                    .ToList();


        //                var first = tutorSlots.First();


        //                return new
        //                {
        //                    TutorId = g.Key,

        //                    Tutor = first.Tutor,

        //                    User = first.User,

        //                    // =========================================
        //                    // COURSE INFORMATION
        //                    // =========================================

        //                    Institute = first.Institute,

        //                    Grade = first.Grade,

        //                    // =========================================
        //                    // HOURLY RATE
        //                    // =========================================

        //                    HourlyRate = first.HourlyRate,

        //                    // =========================================
        //                    // COMMON SLOTS
        //                    // =========================================

        //                    CommonSlots = commonSlots
        //                };
        //            })
        //            .Where(x => x.CommonSlots.Any())
        //            .ToList();


        //        // =====================================================
        //        // STEP 7: GET FEEDBACK / RATINGS
        //        // =====================================================

        //        var tutorRatings = await db.Feedbacks
        //            .Where(f =>
        //                f.FeedbackBy == "Student" &&
        //                f.CourseId == courseId)
        //            .GroupBy(f => f.TutorId)
        //            .Select(g => new
        //            {
        //                TutorId = g.Key,

        //                AverageRating =
        //                    g.Average(x => x.Rating),

        //                TotalReviews =
        //                    g.Count()
        //            })
        //            .ToListAsync();


        //        var ratingsLookup =
        //            tutorRatings.ToDictionary(
        //                r => r.TutorId);


        //        // =====================================================
        //        // STEP 8: BUILD RESULT
        //        //
        //        // IMPORTANT:
        //        // There is NO distance calculation here.
        //        // There is NO radius calculation here.
        //        // =====================================================

        //        var result = tutorsWithCommonSlots
        //            .Select(x =>
        //            {
        //                // =============================================
        //                // GET RATING
        //                // =============================================

        //                ratingsLookup.TryGetValue(
        //                    x.TutorId,
        //                    out var ratingInfo);


        //                double averageRating =
        //                    ratingInfo != null
        //                        ? Math.Round(
        //                            (double)ratingInfo.AverageRating,
        //                            1)
        //                        : 0;


        //                int totalReviews =
        //                    ratingInfo?.TotalReviews ?? 0;


        //                // =============================================
        //                // RETURN TUTOR
        //                // =============================================

        //                return new NonVisitingTutorSearchResultDto
        //                {
        //                    // =========================================
        //                    // BASIC TUTOR INFORMATION
        //                    // =========================================

        //                    tutor_id =
        //                        x.TutorId,

        //                    tutor_name =
        //                        x.User.FullName,

        //                    location =
        //                        x.Tutor.Location,

        //                    qualification =
        //                        x.Tutor.Qualification,

        //                    experience =
        //                        x.Tutor.Experience,

        //                    teaching_mode =
        //                        x.Tutor.TeachingMode,

        //                    // =========================================
        //                    // COURSE INFORMATION
        //                    // =========================================

        //                    course_id =
        //                        courseId,

        //                    institute =
        //                        x.Institute,

        //                    grade =
        //                        x.Grade,

        //                    // =========================================
        //                    // HOURLY RATE
        //                    // =========================================

        //                    hourly_rate =
        //                        x.HourlyRate,

        //                    // =========================================
        //                    // FEEDBACK
        //                    // =========================================

        //                    average_rating =
        //                        averageRating,

        //                    total_reviews =
        //                        totalReviews,

        //                    // =========================================
        //                    // COMMON SLOTS
        //                    // =========================================

        //                    common_slots =
        //                        x.CommonSlots
        //                };
        //            })
        //            .ToList();


        //        // =====================================================
        //        // STEP 9: SORT
        //        // =====================================================

        //        switch (sortBy)
        //        {
        //            // =================================================
        //            // INSTITUTE A-Z
        //            // =================================================

        //            case "institute":

        //                result = result
        //                    .OrderBy(x =>
        //                        x.institute ?? "")
        //                    .ThenByDescending(x =>
        //                        x.average_rating)
        //                    .ToList();

        //                break;


        //            // =================================================
        //            // FEEDBACK HIGH TO LOW
        //            // =================================================

        //            case "feedback":

        //                result = result
        //                    .OrderByDescending(x =>
        //                        x.average_rating)
        //                    .ThenByDescending(x =>
        //                        x.total_reviews)
        //                    .ToList();

        //                break;


        //            // =================================================
        //            // GRADE
        //            // =================================================

        //            case "grade":

        //                result = result
        //                    .OrderBy(x =>
        //                        GetGradeOrder(x.grade))
        //                    .ThenByDescending(x =>
        //                        x.average_rating)
        //                    .ToList();

        //                break;


        //            // =================================================
        //            // FEE LOW TO HIGH
        //            // =================================================

        //            case "fee":

        //                result = result
        //                    .OrderBy(x =>
        //                        x.hourly_rate)
        //                    .ThenByDescending(x =>
        //                        x.average_rating)
        //                    .ToList();

        //                break;


        //            // =================================================
        //            // DEFAULT = FEEDBACK
        //            // =================================================

        //            default:

        //                result = result
        //                    .OrderByDescending(x =>
        //                        x.average_rating)
        //                    .ThenByDescending(x =>
        //                        x.total_reviews)
        //                    .ToList();

        //                break;
        //        }


        //        // =====================================================
        //        // STEP 10: NO RESULT
        //        // =====================================================

        //        if (result.Count == 0)
        //        {
        //            return NotFound(new
        //            {
        //                message =
        //                    "No approved Non-Visiting tutors available for this course."
        //            });
        //        }


        //        // =====================================================
        //        // STEP 11: FINAL RESPONSE
        //        // =====================================================

        //        return Ok(new
        //        {
        //            sort_by = sortBy,

        //            course_id = courseId,

        //            teaching_mode = "Non-Visiting",

        //            total_tutors = result.Count,

        //            tutors = result
        //        });
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine(
        //            "SEARCH NON-VISITING TUTOR ERROR: " +
        //            ex.ToString());

        //        return StatusCode(
        //            500,
        //            new
        //            {
        //                message =
        //                    "An error occurred while searching Non-Visiting tutors.",

        //                error =
        //                    ex.Message
        //            });
        //    }
        //}

        [Authorize]
        [HttpPost("create-request")] //Request multiple tutor
        public async Task<IActionResult> CreateRequest( [FromBody] CreateRequestDto dto)
        {
            using var transaction =
                await db.Database.BeginTransactionAsync();

            try
            {
                // =====================================================
                // STEP 1: Validate Tutors
                // =====================================================

                if (dto.tutor_ids == null ||
                    dto.tutor_ids.Count == 0)
                {
                    return BadRequest(new
                    {
                        message = "At least one tutor must be selected."
                    });
                }

                dto.tutor_ids = dto.tutor_ids
                    .Distinct()
                    .ToList();

                // =====================================================
                // STEP 2: Validate Day / Time
                // =====================================================

                if (string.IsNullOrWhiteSpace(dto.day) ||
                    string.IsNullOrWhiteSpace(dto.time))
                {
                    return BadRequest(new
                    {
                        message = "Day or time missing."
                    });
                }

                // =====================================================
                // STEP 3: Validate Learning Mode
                // =====================================================

                if (string.IsNullOrWhiteSpace(dto.learning_mode))
                {
                    return BadRequest(new
                    {
                        message = "Learning mode is required."
                    });
                }

                if (dto.learning_mode == "SpecificTime")
                {
                    if (!dto.learning_duration.HasValue ||
                        dto.learning_duration <= 0)
                    {
                        return BadRequest(new
                        {
                            message =
                                "Learning duration is required for Specific Time."
                        });
                    }

                    if (string.IsNullOrWhiteSpace(
                        dto.learning_duration_unit))
                    {
                        return BadRequest(new
                        {
                            message =
                                "Learning duration unit is required."
                        });
                    }
                }

                // =====================================================
                // STEP 4: Logged-in Student
                // =====================================================

                var userIdClaim =
                    User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrWhiteSpace(userIdClaim))
                {
                    return Unauthorized(new
                    {
                        message = "User not authenticated."
                    });
                }

                if (!int.TryParse(userIdClaim, out int userId))
                {
                    return Unauthorized(new
                    {
                        message = "Invalid user ID."
                    });
                }

                var student = await db.Students
                    .FirstOrDefaultAsync(
                        s => s.UserId == userId);

                if (student == null)
                {
                    return NotFound(new
                    {
                        message = "Student not found."
                    });
                }

                // =====================================================
                // STEP 5: Course
                // =====================================================

                var courseExists = await db.Courses
                    .AnyAsync(c =>
                        c.CourseId == dto.course_id);

                if (!courseExists)
                {
                    return NotFound(new
                    {
                        message = "Course not found."
                    });
                }

                // =====================================================
                // STEP 6: Tutors
                // =====================================================

                var tutors = await db.Tutors
                    .Where(t =>
                        dto.tutor_ids.Contains(t.TutorId))
                    .ToListAsync();

                var missingTutorIds = dto.tutor_ids
                    .Except(tutors.Select(t => t.TutorId))
                    .ToList();

                if (missingTutorIds.Any())
                {
                    return NotFound(new
                    {
                        message = "One or more tutors were not found.",
                        missing_tutor_ids = missingTutorIds
                    });
                }

                // =====================================================
                // STEP 7: Check Existing Searching Group
                // =====================================================

                var existingGroup = await db.RequestGroups
                    .FirstOrDefaultAsync(g =>
                        g.StudentId == student.StudentId &&
                        g.CourseId == dto.course_id &&
                        g.Status == "Searching");

                if (existingGroup != null)
                {
                    return BadRequest(new
                    {
                        message =
                            "You already have an active tutor request.",
                        request_group_id =
                            existingGroup.RequestGroupId
                    });
                }

                // =====================================================
                // STEP 8: Create Request Group
                // =====================================================

                var group = new RequestGroup
                {
                    StudentId = student.StudentId,

                    CourseId = dto.course_id,

                    CreatedDate = DateTime.Now,

                    Status = "Searching",

                    CurrentRequestId = null
                };

                db.RequestGroups.Add(group);

                await db.SaveChangesAsync();

                // =====================================================
                // STEP 9: Create Tutor Requests
                // =====================================================

                string fullTime =
                    $"{dto.day}, {dto.time}";

                DateTime now = DateTime.Now;

                DateTime firstDeadline =
                    now.AddMinutes(2);

                var requests = new List<Request>();

                for (int i = 0; i < dto.tutor_ids.Count; i++)
                {
                    int tutorId = dto.tutor_ids[i];

                    bool isFirstTutor = i == 0;

                    var tutorRequest = new Request
                    {
                        StudentId = student.StudentId,

                        TutorId = tutorId,

                        CourseId = dto.course_id,

                        Time = fullTime,

                        RequestDate = now,

                        Status = isFirstTutor
                            ? "Pending"
                            : "Queued",

                        ClassDate =
                            DateOnly.FromDateTime(now),

                        Day = dto.day,

                        RequestType = "Normal",

                        ParentRequestId = null,

                        LearningMode =
                            dto.learning_mode,

                        LearningDuration =
                            dto.learning_duration,

                        LearningDurationUnit =
                            dto.learning_duration_unit,

                        RequestGroupId =
                            group.RequestGroupId,

                        TutorSequence = i + 1,

                        ResponseDeadline =
                            isFirstTutor
                                ? firstDeadline
                                : null
                    };

                    requests.Add(tutorRequest);
                }

                db.Requests.AddRange(requests);

                await db.SaveChangesAsync();

                // =====================================================
                // STEP 10: Current Request = First Tutor
                // =====================================================

                group.CurrentRequestId =
                    requests[0].RequestId;

                await db.SaveChangesAsync();

                await transaction.CommitAsync();

                // =====================================================
                // STEP 11: Response
                // =====================================================

                return Ok(new
                {
                    message =
                        "Tutor request created successfully. The first tutor has 2 minutes to accept.",

                    request_group_id =
                        group.RequestGroupId,

                    request_id =
                        requests[0].RequestId,

                    current_tutor_id =
                        requests[0].TutorId,

                    current_tutor_sequence =
                        requests[0].TutorSequence,

                    total_tutors =
                        requests.Count,

                    response_deadline =
                        requests[0].ResponseDeadline,

                    status =
                        requests[0].Status
                });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();

                return StatusCode(500, new
                {
                    message =
                        "Error while creating tutor request.",

                    error =
                        ex.Message
                });
            }
        }

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
                // Find Student
                var student = await db.Students
                    .FirstOrDefaultAsync(s => s.UserId == userId);

                if (student == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "Student not found"
                    });
                }
                // Fetch Student Classes
                var classes = await (
                    from r in db.Requests
                    join t in db.Tutors
                        on r.TutorId equals t.TutorId
                    join tu in db.Users
                        on t.UserId equals tu.UserId
                    join c in db.Courses
                        on r.CourseId equals c.CourseId
                    where r.StudentId == student.StudentId
                          && r.Status == "Accepted"
                    orderby r.ClassDate descending
                    select new
                    {
                        request_id = r.RequestId,
                        tutor_id = t.TutorId,
                        tutor_name = tu.FullName,
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
                    message = "Student classes fetched successfully",
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

        //Today Classes
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

                var student = await db.Students
                    .FirstOrDefaultAsync(s => s.UserId == userId);

                if (student == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "Student not found"
                    });
                }

                var today = DateOnly.FromDateTime(DateTime.Today);

                var classes = await (
                    from r in db.Requests
                    join t in db.Tutors
                        on r.TutorId equals t.TutorId
                    join tu in db.Users
                        on t.UserId equals tu.UserId
                    join c in db.Courses
                        on r.CourseId equals c.CourseId
                    where r.StudentId == student.StudentId
                          && r.Status == "Accepted"
                          && r.ClassDate == today
                    orderby r.Time
                    select new
                    {
                        request_id = r.RequestId,
                        tutor_id = t.TutorId,
                        tutor_name = tu.FullName,
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

        //Tutor available slots 
        [HttpGet("available-slots/{requestId}")]
        public async Task<IActionResult> GetAvailableRescheduleSlots(int requestId)
        {
            var request = await db.Requests
                .FirstOrDefaultAsync(x => x.RequestId == requestId);

            if (request == null)
                return NotFound("Request not found");

            // =====================================================
            // GET TUTOR SCHEDULES
            // =====================================================

            var tutorSchedules = await db.Schedules
                .Where(x => x.TutorId == request.TutorId)
                .ToListAsync();

            // =====================================================
            // GET ACCEPTED CLASSES OF TUTOR
            // =====================================================

            var bookedRequests = await db.Requests
                .Where(x =>
                    x.TutorId == request.TutorId &&
                    x.Status == "Accepted")
                .ToListAsync();

            List<AvailableSlotDto> availableSlots = new();

            foreach (var slot in tutorSchedules)
            {
                bool alreadyAdded = availableSlots.Any(x =>
                    x.Day.Trim().ToLower() ==
                    slot.Day.Trim().ToLower()
                    &&
                    x.Time.Replace(" ", "").ToLower() ==
                    slot.Time.Replace(" ", "").ToLower());

                if (alreadyAdded)
                    continue;

                availableSlots.Add(new AvailableSlotDto
                {
                    Day = slot.Day,
                    Time = slot.Time,
                    ClassDate = DateTime.Today
                });
            }

            return Ok(availableSlots);
        }

        //Request for Re-Schedule to tutors
        [HttpPost("create-reschedule")]
        public async Task<IActionResult> CreateReschedule(RescheduleRequestDto dto)
        {
            var oldRequest = await db.Requests
                .FirstOrDefaultAsync(x => x.RequestId == dto.ParentRequestId);

            if (oldRequest == null)
                return NotFound("Original class not found");

            // Check accepted class conflict

            bool slotBusy = await db.Requests.AnyAsync(x =>
                x.Status == "Accepted"
                && x.ClassDate == DateOnly.FromDateTime(dto.NewClassDate)
                && x.Time == dto.Time
                && (
                    x.TutorId == oldRequest.TutorId
                    || x.StudentId == oldRequest.StudentId
                ));

            if (slotBusy)
                return BadRequest("Slot already booked");

            // Prevent duplicate pending request

            bool pendingExists = await db.Requests.AnyAsync(x =>
                x.ParentRequestId == dto.ParentRequestId
                && x.RequestType == "Reschedule"
                && x.Status == "Pending");

            if (pendingExists)
                return BadRequest("Reschedule request already pending");

            var newRequest = new Request
            {
                StudentId = oldRequest.StudentId,
                TutorId = oldRequest.TutorId,
                CourseId = oldRequest.CourseId,

                Day = dto.Day,
                Time = dto.Time,

                ClassDate = DateOnly.FromDateTime(dto.NewClassDate),

                Status = "RequestedByStudent",
                RequestType = "Reschedule",

                ParentRequestId = oldRequest.RequestId,

                RequestDate = DateTime.Now
            };

            db.Requests.Add(newRequest);

            await db.SaveChangesAsync();

            return Ok(new
            {
                message = "Reschedule request sent successfully"
            });
        }

        //Request for Pre-Schedule to tutor 
        [HttpPost("create-preschedule")]
        public async Task<IActionResult> CreatePreschedule(RescheduleRequestDto dto)
        {
            var oldRequest = await db.Requests
                .FirstOrDefaultAsync(x => x.RequestId == dto.ParentRequestId);

            if (oldRequest == null)
                return NotFound("Original class not found");

            bool slotBusy = await db.Requests.AnyAsync(x =>
                x.Status == "Accepted"
                && x.ClassDate == DateOnly.FromDateTime(dto.NewClassDate)
                && x.Time == dto.Time
                && (
                    x.TutorId == oldRequest.TutorId
                    || x.StudentId == oldRequest.StudentId
                ));

            if (slotBusy)
                return BadRequest("Slot already booked");

            bool pendingExists = await db.Requests.AnyAsync(x =>
                x.ParentRequestId == dto.ParentRequestId
                && x.RequestType == "Preschedule"
                && x.Status == "Pending");

            if (pendingExists)
                return BadRequest("Preschedule request already pending");

            var request = new Request
            {
                StudentId = oldRequest.StudentId,
                TutorId = oldRequest.TutorId,
                CourseId = oldRequest.CourseId,

                Day = dto.Day,
                Time = dto.Time,

                ClassDate = DateOnly.FromDateTime(dto.NewClassDate),

                Status = "RequestedByStudent",
                RequestType = "Preschedule",

                ParentRequestId = oldRequest.RequestId,

                RequestDate = DateTime.Now
            };

            db.Requests.Add(request);

            await db.SaveChangesAsync();

            return Ok(new
            {
                message = "Pre-schedule request sent successfully"
            });
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

        //Accept re-Schedule request sended by Tutor
        [HttpPost("accept-reschedule/{requestId}")]
        public async Task<IActionResult> AcceptReschedule(int requestId)
        {
            var newRequest = await db.Requests
                .FirstOrDefaultAsync(x => x.RequestId == requestId);

            if (newRequest == null)
                return NotFound();

            var oldRequest = await db.Requests
                .FirstOrDefaultAsync(x =>
                    x.RequestId == newRequest.ParentRequestId);

            if (oldRequest == null)
                return NotFound("Old request not found");

            // ACCEPT NEW CLASS
            newRequest.Status = "Accepted";

            // OLD CLASS REPLACED
            oldRequest.Status = "Rescheduled";

            await db.SaveChangesAsync();

            return Ok(new
            {
                message = "Class rescheduled successfully"
            });
        }

        [HttpPost("accept-preschedule/{requestId}")]
        public async Task<IActionResult> AcceptPreschedule(int requestId)
        {
            var request = await db.Requests
                .FirstOrDefaultAsync(x => x.RequestId == requestId);

            if (request == null)
                return NotFound();

            request.Status = "Accepted";

            await db.SaveChangesAsync();

            return Ok(new
            {
                message = "Pre-schedule accepted successfully"
            });
        }

        [Authorize]
        [HttpGet("pre-reschedule-requests")]
        public async Task<IActionResult> GetPreRescheduleRequests()
        {
            try
            {
                // =====================================
                // GET USER ID FROM TOKEN
                // =====================================

                var userIdClaim =
                    User.FindFirst(ClaimTypes.NameIdentifier);

                if (userIdClaim == null)
                {
                    return Unauthorized(new
                    {
                        message = "Invalid token"
                    });
                }

                int userId =
                    int.Parse(userIdClaim.Value);

                // =====================================
                // GET STUDENT
                // =====================================

                var student = await db.Students
                    .FirstOrDefaultAsync(x =>
                        x.UserId == userId);

                if (student == null)
                {
                    return NotFound(new
                    {
                        message = "Student not found"
                    });
                }

                // =====================================
                // GET REQUESTS
                // =====================================

                var requests = await (

                    from r in db.Requests

                    join c in db.Courses
                    on r.CourseId equals c.CourseId

                    join t in db.Tutors
                    on r.TutorId equals t.TutorId

                    join u in db.Users
                    on t.UserId equals u.UserId

                    where
                        r.StudentId == student.StudentId
                        &&
                        r.Status == "RequestedByTutor"
                        &&
                        (
                            r.RequestType == "Reschedule"
                            ||
                            r.RequestType == "Preschedule"
                        )

                    orderby r.RequestDate descending

                    select new
                    {
                        request_id = r.RequestId,

                        tutor_name = u.FullName,

                        course_name = c.CourseTitle,

                        day = r.Day,

                        time = r.Time,

                        class_date = r.ClassDate,

                        request_type = r.RequestType,

                        status = r.Status
                    }

                ).ToListAsync();

                return Ok(requests);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = ex.Message,

                    inner =
                        ex.InnerException != null
                        ? ex.InnerException.Message
                        : ""
                });
            }
        }

        [HttpPost("reject-request/{requestId}")]
        public async Task<IActionResult> RejectRequest(int requestId)
        {
            var request = await db.Requests
                .FirstOrDefaultAsync(x => x.RequestId == requestId);

            if (request == null)
                return NotFound("Request not found");

            request.Status = "Rejected";

            await db.SaveChangesAsync();

            return Ok(new
            {
                message = "Request rejected successfully"
            });
        }

        [Authorize]
        [HttpGet("student-class-history")]
        public async Task<IActionResult> GetStudentClassHistory()
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

                // Find Student using UserId
                var student = await db.Students
                    .FirstOrDefaultAsync(s => s.UserId == userId);

                if (student == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "Student not found"
                    });
                }

                int studentId = student.StudentId;

                // Fetch Completed Classes
                var history = await (
                    from r in db.Requests

                    join t in db.Tutors
                        on r.TutorId equals t.TutorId

                    join tu in db.Users
                        on t.UserId equals tu.UserId

                    join c in db.Courses
                        on r.CourseId equals c.CourseId

                    where r.StudentId == studentId
                          && r.Status == "Complete"

                    orderby r.ClassDate descending

                    select new
                    {
                        request_id = r.RequestId,

                        tutor_id = t.TutorId,
                        tutor_name = tu.FullName,

                        student_id = r.StudentId,

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
                    message = "Student class history fetched successfully",
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

        [Authorize]
        [HttpGet("my-tutors")]
        public async Task<IActionResult> GetMyTutors()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);

            if (userIdClaim == null)
                return Unauthorized(new { message = "Invalid token" });

            int userId = int.Parse(userIdClaim.Value);

            var student = await db.Students
                .FirstOrDefaultAsync(s => s.UserId == userId);

            if (student == null)
                return NotFound(new { message = "Student not found" });

            var tutors = await db.Requests
                .Where(r =>
                    r.StudentId == student.StudentId &&
                    (r.Status == "Accepted" || r.Status == "Complete"))
                .Select(r => new StudentTutorCourseDto
                {
                    TutorId = r.Tutor.TutorId,

                    TutorName = r.Tutor.User.FullName,

                    CourseId = r.CourseId.Value,

                    CourseName = r.Course.CourseTitle,

                    IsCompleted = db.TutorCourses
                        .Where(tc =>
                            tc.TutorId == r.TutorId &&
                            tc.CourseId == r.CourseId)
                        .Select(tc => tc.IsCompleted)
                        .FirstOrDefault(),

                    CompletedDate = db.TutorCourses
                        .Where(tc =>
                            tc.TutorId == r.TutorId &&
                            tc.CourseId == r.CourseId)
                        .Select(tc => tc.CompletedDate)
                        .FirstOrDefault(),

                    FeedbackGiven =
                        db.Feedbacks.Any(f =>
                            f.StudentId == student.StudentId &&
                            f.TutorId == r.TutorId &&
                            f.CourseId == r.CourseId &&
                            f.FeedbackBy == "Student"),

                    Grade = db.StudentCourses
                        .Where(sc =>
                            sc.StudentId == student.StudentId &&
                            sc.CourseId == r.CourseId)
                        .Select(sc => sc.Grade)
                        .FirstOrDefault()
                })
                .Distinct()
                .ToListAsync();

            return Ok(tutors);
        }

        [Authorize]
        [HttpPost("save-grade")]
        public async Task<IActionResult> SaveGrade([FromBody] StudentGradeRequest request)
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

                int userId = int.Parse(userIdClaim.Value);

                var student = await db.Students
                    .FirstOrDefaultAsync(x => x.UserId == userId);

                if (student == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "Student not found."
                    });
                }

                if (string.IsNullOrWhiteSpace(request.Grade))
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Grade is required."
                    });
                }

                var studentCourse = await db.StudentCourses
                    .FirstOrDefaultAsync(x =>
                        x.StudentId == student.StudentId &&
                        x.CourseId == request.CourseId);

                if (studentCourse == null)
                {
                    studentCourse = new StudentCourse
                    {
                        StudentId = student.StudentId,
                        CourseId = request.CourseId,
                        Grade = request.Grade.Trim().ToUpper()
                    };

                    db.StudentCourses.Add(studentCourse);
                }
                else
                {
                    studentCourse.Grade = request.Grade.Trim().ToUpper();
                }

                await db.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    message = "Grade saved successfully.",
                    grade = studentCourse.Grade
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
        [HttpPost("give-feedback")]
        public async Task<IActionResult> GiveFeedback([FromBody] StudentFeedbackRequest request)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);

            if (userIdClaim == null)
                return Unauthorized(new { message = "Invalid token" });

            int userId = int.Parse(userIdClaim.Value);

            var student = await db.Students
                .FirstOrDefaultAsync(s => s.UserId == userId);

            if (student == null)
                return NotFound();

            bool exists = await db.Feedbacks.AnyAsync(f =>
                f.StudentId == student.StudentId &&
                f.TutorId == request.TutorId &&
                f.CourseId == request.CourseId &&
                f.FeedbackBy == "Student");

            if (exists)
            {
                return BadRequest(new
                {
                    message = "Feedback already submitted"
                });
            }

            Feedback feedback = new Feedback
            {
                StudentId = student.StudentId,
                TutorId = request.TutorId,
                CourseId = request.CourseId,
                Rating = request.Rating,
                Comment = request.Comment,
                FeedbackBy = "Student",
                FeedbackDate = DateTime.UtcNow
            };

            db.Feedbacks.Add(feedback);

            await db.SaveChangesAsync();

            return Ok(new
            {
                message = "Feedback submitted successfully"
            });
        }

        //tp get how many notifications
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

            var student = await db.Students
                .FirstOrDefaultAsync(s => s.UserId == userId);

            if (student == null)
                return NotFound(new
                {
                    message = "Student not found"
                });

            var count = await db.Requests.CountAsync(x =>
                x.StudentId == student.StudentId &&
                x.Status == "RequestedByTutor" &&
                (
                    x.RequestType == "Reschedule" ||
                    x.RequestType == "Preschedule"
                )
            );

            return Ok(new
            {
                count
            });
        }

        // =========================================================
        // STUDENT UPLOAD COURSE CONTENT / ASYNC LESSON PLAN
        // =========================================================

        [Authorize]
        [HttpPost("upload-course-content")]
        [RequestSizeLimit(100 * 1024 * 1024)]
        public async Task<IActionResult> UploadCourseContent([FromForm] UploadCourseContentDto model)
        {
            try
            {
                Console.WriteLine("=================================");
                Console.WriteLine("COURSE CONTENT UPLOAD RECEIVED");
                Console.WriteLine("Content-Type: " + Request.ContentType);
                Console.WriteLine("Content-Length: " + Request.ContentLength);
                Console.WriteLine("Course ID: " + model.course_id);
                Console.WriteLine("Title: " + model.title);
                Console.WriteLine("File: " + model.file?.FileName);
                Console.WriteLine("File Size: " + model.file?.Length);
                Console.WriteLine("=================================");

                var userIdClaim =
                    User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrWhiteSpace(userIdClaim))
                {
                    return Unauthorized(new
                    {
                        message = "User is not logged in."
                    });
                }

                if (!int.TryParse(userIdClaim, out int userId))
                {
                    return Unauthorized(new
                    {
                        message = "Invalid user token."
                    });
                }

                var student = await db.Students
                    .FirstOrDefaultAsync(s => s.UserId == userId);

                if (student == null)
                {
                    return NotFound(new
                    {
                        message = "Student not found."
                    });
                }

                if (model.course_id <= 0)
                {
                    return BadRequest(new
                    {
                        message = "Invalid course ID."
                    });
                }

                var course = await db.Courses
                    .FirstOrDefaultAsync(
                        c => c.CourseId == model.course_id);

                if (course == null)
                {
                    return NotFound(new
                    {
                        message = "Course not found."
                    });
                }

                var studentCourse =
                    await db.StudentCourses.FirstOrDefaultAsync(sc =>
                        sc.StudentId == student.StudentId &&
                        sc.CourseId == model.course_id);

                if (studentCourse == null)
                {
                    return BadRequest(new
                    {
                        message =
                            "Student is not enrolled in this course."
                    });
                }

                if (string.IsNullOrWhiteSpace(model.title))
                {
                    return BadRequest(new
                    {
                        message = "Title is required."
                    });
                }

                if (model.file == null || model.file.Length == 0)
                {
                    return BadRequest(new
                    {
                        message = "Please select a file."
                    });
                }

                const long maxFileSize =
                    100 * 1024 * 1024;

                if (model.file.Length > maxFileSize)
                {
                    return BadRequest(new
                    {
                        message =
                            "File size cannot be greater than 100 MB."
                    });
                }

                Console.WriteLine("Saving file...");

                string filePath =
                    await SaveCourseContent(
                        model.file,
                        student.StudentId,
                        model.course_id);

                Console.WriteLine(
                    "Saved File Path: " + filePath);

                if (filePath == "Invalid")
                {
                    return BadRequest(new
                    {
                        message =
                            "Only PDF, DOC, DOCX, JPG, JPEG and PNG files are allowed."
                    });
                }

                if (string.IsNullOrEmpty(filePath))
                {
                    return StatusCode(500, new
                    {
                        message =
                            "Unable to save course content file."
                    });
                }

                var content = new StudentCourseContent
                {
                    StudentId = student.StudentId,
                    CourseId = model.course_id,
                    Title = model.title.Trim(),

                    Description =
                        string.IsNullOrWhiteSpace(model.description)
                            ? null
                            : model.description.Trim(),

                    FileName =
                        Path.GetFileName(model.file.FileName),

                    FilePath = filePath,

                    UploadedDate = DateTime.Now
                };

                db.StudentCourseContents.Add(content);

                await db.SaveChangesAsync();

                Console.WriteLine(
                    "COURSE CONTENT SAVED TO DATABASE");

                return Ok(new
                {
                    status = "Success",

                    message =
                        "Course content uploaded successfully.",

                    data = new
                    {
                        content_id = content.ContentId,
                        student_id = content.StudentId,
                        course_id = content.CourseId,
                        course_title = course.CourseTitle,
                        title = content.Title,
                        description = content.Description,
                        file_name = content.FileName,
                        file_path = content.FilePath,
                        uploaded_date = content.UploadedDate
                    }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    "=================================");

                Console.WriteLine(
                    "COURSE CONTENT UPLOAD ERROR");

                Console.WriteLine(
                    ex.ToString());

                Console.WriteLine(
                    "=================================");

                return StatusCode(500, new
                {
                    message =
                        ex.InnerException?.Message ??
                        ex.Message
                });
            }
        }

        // =========================================================
        // SAVE COURSE CONTENT FILE
        // =========================================================
        private async Task<string> SaveCourseContent(IFormFile file, int studentId, int courseId)
        {
            if (file == null ||
                file.Length == 0)
            {
                return string.Empty;
            }

            // =====================================================
            // ALLOWED FILE TYPES
            // =====================================================

            var allowedExtensions =
                new HashSet<string>(
                    StringComparer.OrdinalIgnoreCase)
                {
                ".pdf",
                ".doc",
                ".docx",
                ".jpg",
                ".jpeg",
                ".png"
                };

            // =====================================================
            // EXTENSION
            // =====================================================

            string extension =
                Path.GetExtension(
                    file.FileName
                );

            if (string.IsNullOrWhiteSpace(extension) ||
                !allowedExtensions.Contains(extension))
            {
                return "Invalid";
            }

            // =====================================================
            // WWWROOT
            // =====================================================

            string webRootPath =
                _environment.WebRootPath;

            if (string.IsNullOrWhiteSpace(webRootPath))
            {
                webRootPath =
                    Path.Combine(
                        Directory.GetCurrentDirectory(),
                        "wwwroot"
                    );
            }

            // =====================================================
            // CREATE FOLDER
            // =====================================================

            string folder =
                Path.Combine(
                    webRootPath,
                    "CourseContent"
                );

            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }

            // =====================================================
            // UNIQUE FILE NAME
            // =====================================================

            string uniqueFileName =
                $"Student_{studentId}" +
                $"_Course_{courseId}" +
                extension.ToLowerInvariant();

            // =====================================================
            // FULL FILE PATH
            // =====================================================

            string fullFilePath =
                Path.Combine(
                    folder,
                    uniqueFileName
                );

            // =====================================================
            // SAVE FILE
            // =====================================================

            await using (
                var stream =
                    new FileStream(
                        fullFilePath,
                        FileMode.Create,
                        FileAccess.Write,
                        FileShare.None)
            )
            {
                await file.CopyToAsync(stream);
            }

            // =====================================================
            // DATABASE PATH
            // =====================================================

            return
                "/CourseContent/" +
                uniqueFileName;
        }

        // =========================================================
        // STUDENT GET OWN COURSE CONTENT
        // =========================================================
        [Authorize]
        [HttpGet("my-course-content/{courseId}")]
        public async Task<IActionResult> GetMyCourseContent(int courseId)
        {
            try
            {
                var userIdClaim =
                    User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrEmpty(userIdClaim))
                {
                    return Unauthorized(new
                    {
                        message = "User is not logged in."
                    });
                }

                int userId = int.Parse(userIdClaim);


                var student = await db.Students
                    .FirstOrDefaultAsync(s => s.UserId == userId);

                if (student == null)
                {
                    return NotFound(new
                    {
                        message = "Student not found."
                    });
                }


                var content = await db.StudentCourseContents
                    .Where(c =>
                        c.StudentId == student.StudentId &&
                        c.CourseId == courseId)
                    .OrderByDescending(c => c.UploadedDate)
                    .Select(c => new
                    {
                        content_id = c.ContentId,
                        student_id = c.StudentId,
                        course_id = c.CourseId,
                        course_title = c.Course.CourseTitle,
                        title = c.Title,
                        description = c.Description,
                        file_name = c.FileName,
                        file_path = c.FilePath,
                        uploaded_date = c.UploadedDate
                    })
                    .ToListAsync();


                return Ok(new
                {
                    status = "Success",
                    count = content.Count,
                    data = content
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Error: " +
                        (ex.InnerException?.Message ?? ex.Message)
                });
            }
        }

        // =========================================================
        // STUDENT GET OWN FEE
        // Only students with FeeResponsibility = ByMe
        // =========================================================

        [Authorize]
        [HttpGet("student-fee")]
        public IActionResult GetStudentFee()
        {
            try
            {
                // =====================================================
                // 1. GET LOGGED-IN USER ID FROM JWT
                // =====================================================

                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrEmpty(userIdClaim))
                {
                    return Unauthorized("User ID not found.");
                }

                int userId = int.Parse(userIdClaim);


                // =====================================================
                // 2. GET LOGGED-IN STUDENT
                // =====================================================

                var student = db.Students
                    .Include(s => s.User)
                    .FirstOrDefault(s =>
                        s.UserId == userId &&
                        s.FeeResponsibility == "ByMe");

                if (student == null)
                {
                    return Unauthorized(
                        "Student not found or Fee Responsibility is not ByMe."
                    );
                }


                // =====================================================
                // 3. GET ALL FEES OF THIS STUDENT
                // =====================================================

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


                // =====================================================
                // 4. COURSE-WISE FEE
                // =====================================================

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


                // =====================================================
                // 5. STUDENT TOTALS
                // =====================================================

                decimal totalFee = courseFees.Sum(x => x.TotalFee);

                decimal totalPaid = courseFees.Sum(x => x.Paid);

                decimal totalRemaining = courseFees.Sum(x => x.Remaining);


                // =====================================================
                // 6. RETURN STUDENT FEE
                // =====================================================

                return Ok(new
                {
                    StudentId = student.StudentId,

                    StudentName = student.User != null
                        ? student.User.FullName
                        : "Unknown",

                    FeeResponsibility = student.FeeResponsibility,

                    TotalFee = totalFee,

                    TotalPaid = totalPaid,

                    TotalRemaining = totalRemaining,

                    Courses = courseFees
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "An error occurred while getting student fee.",
                    error = ex.Message
                });
            }
        }


        // =========================================================
        // STUDENT SEND PAYMENT
        // Only students with FeeResponsibility = ByMe
        // =========================================================

        [Authorize]
        [HttpPost("student-send-payment")]
        public IActionResult SendStudentPayment(PaymentRequestDto model)
        {
            try
            {
                // =====================================================
                // 1. GET LOGGED-IN USER ID FROM JWT
                // =====================================================

                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrEmpty(userIdClaim))
                {
                    return Unauthorized("User ID not found.");
                }

                int userId = int.Parse(userIdClaim);


                // =====================================================
                // 2. GET LOGGED-IN STUDENT
                // Only FeeResponsibility = ByMe
                // =====================================================

                var student = db.Students
                    .FirstOrDefault(s =>
                        s.UserId == userId &&
                        s.FeeResponsibility == "ByMe");

                if (student == null)
                {
                    return Unauthorized(
                        "Student not found or Fee Responsibility is not ByMe."
                    );
                }


                // =====================================================
                // 3. GET STUDENT'S FEE
                // IMPORTANT:
                // Fee must belong to logged-in student
                // =====================================================

                var fee = db.StudentCourseFees
                    .FirstOrDefault(x =>
                        x.FeeId == model.FeeId &&
                        x.StudentId == student.StudentId);

                if (fee == null)
                {
                    return NotFound("Fee not found for this student.");
                }


                // =====================================================
                // 4. GET ALREADY RECEIVED PAYMENT
                // =====================================================

                var received = db.Payments
                    .Where(x =>
                        x.FeeId == model.FeeId &&
                        x.TutorStatus == "Received")
                    .Sum(x => (decimal?)x.Amount) ?? 0;


                // =====================================================
                // 5. CALCULATE REMAINING
                // =====================================================

                var remaining = fee.TotalFee - received;


                // =====================================================
                // 6. CHECK PAYMENT AMOUNT
                // =====================================================

                if (model.Amount <= 0)
                {
                    return BadRequest("Invalid Amount.");
                }

                if (model.Amount > remaining)
                {
                    return BadRequest(
                        "Amount exceeds remaining balance."
                    );
                }


                // =====================================================
                // 7. CREATE PAYMENT
                // =====================================================

                var payment = new Payment
                {
                    FeeId = model.FeeId,

                    Amount = model.Amount,

                    PaymentType = model.Amount == remaining
                        ? "Full"
                        : "Partial",

                    PaymentDate = DateTime.Now,

                    // Student sends payment
                    ParentStatus = "Sent",

                    // Tutor has not received it yet
                    TutorStatus = "Pending"
                };


                // =====================================================
                // 8. SAVE PAYMENT
                // =====================================================

                db.Payments.Add(payment);

                db.SaveChanges();


                // =====================================================
                // 9. SUCCESS RESPONSE
                // =====================================================

                return Ok(new
                {
                    Message = "Payment Sent Successfully.",

                    FeeId = model.FeeId,

                    Amount = model.Amount,

                    PaymentType = payment.PaymentType
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "An error occurred while sending payment.",
                    error = ex.Message
                });
            }
        }

        // ============================================================
        // SEARCH ALL STUDENTS / SEARCH STUDENTS BY NAME
        // ============================================================
        [Authorize]
        [HttpGet("search-students")]
        public async Task<IActionResult> SearchStudents([FromQuery(Name = "name")] string? name = "")
        {
            try
            {
                // ============================================================
                // STEP 1: VERIFY LOGGED-IN USER
                // ============================================================

                var userIdClaim =
                    User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrWhiteSpace(userIdClaim))
                {
                    return Unauthorized(new
                    {
                        message = "User is not authenticated."
                    });
                }

                if (!int.TryParse(userIdClaim, out int userId))
                {
                    return Unauthorized(new
                    {
                        message = "Invalid user ID."
                    });
                }

                // ============================================================
                // STEP 2: GET LOGGED-IN STUDENT
                // ============================================================

                var student = await db.Students
                    .FirstOrDefaultAsync(s => s.UserId == userId);

                if (student == null)
                {
                    return NotFound(new
                    {
                        message = "Student profile not found."
                    });
                }

                // ============================================================
                // STEP 3: CLEAN SEARCH NAME
                // ============================================================

                name = (name ?? string.Empty).Trim();

                // ============================================================
                // STEP 4: GET OTHER STUDENTS
                // ============================================================

                var studentsQuery =
                    from s in db.Students
                    join u in db.Users
                        on s.UserId equals u.UserId
                    where
                        s.StudentId != student.StudentId &&
                        u.Role == "Student"
                    select new
                    {
                        studentId = s.StudentId,
                        userId = u.UserId,
                        fullName = u.FullName,
                        email = u.Email
                    };

                // ============================================================
                // STEP 5: APPLY SEARCH ONLY WHEN NAME IS PROVIDED
                // ============================================================

                if (!string.IsNullOrWhiteSpace(name))
                {
                    studentsQuery = studentsQuery.Where(x =>
                        x.fullName != null &&
                        x.fullName.Contains(name));
                }

                // ============================================================
                // STEP 6: GET STUDENTS
                // ============================================================

                var students = await studentsQuery
                    .OrderBy(x => x.fullName)
                    .ToListAsync();

                // ============================================================
                // STEP 7: GET FRIENDSHIP RECORDS
                // ============================================================

                var friendshipRecords = await db.StudentFriends
                    .Where(f =>
                        f.StudentId == student.StudentId ||
                        f.FriendStudentId == student.StudentId)
                    .ToListAsync();

                // ============================================================
                // STEP 8: ADD FRIENDSHIP STATUS
                // ============================================================

                var result = students.Select(s =>
                {
                    // --------------------------------------------------------
                    // Find relationship between current student and target
                    // --------------------------------------------------------

                    var friendship = friendshipRecords
                        .Where(f =>
                            (
                                f.StudentId == student.StudentId &&
                                f.FriendStudentId == s.studentId
                            )
                            ||
                            (
                                f.StudentId == s.studentId &&
                                f.FriendStudentId == student.StudentId
                            )
                        )
                        .OrderByDescending(f => f.FriendshipId)
                        .FirstOrDefault();

                    // --------------------------------------------------------
                    // Default values
                    // --------------------------------------------------------

                    string relationshipStatus = "None";

                    bool friendRequestSent = false;

                    bool friendRequestReceived = false;

                    // --------------------------------------------------------
                    // Check friendship
                    // --------------------------------------------------------

                    if (friendship != null)
                    {
                        // ----------------------------------------------------
                        // ACCEPTED
                        // ----------------------------------------------------

                        if (friendship.Status == "Accepted")
                        {
                            relationshipStatus = "Accepted";
                        }

                        // ----------------------------------------------------
                        // PENDING
                        // ----------------------------------------------------

                        else if (friendship.Status == "Pending")
                        {
                            // Current student sent request
                            if (friendship.StudentId == student.StudentId)
                            {
                                relationshipStatus = "PendingSent";
                                friendRequestSent = true;
                            }

                            // Other student sent request
                            else
                            {
                                relationshipStatus = "PendingReceived";
                                friendRequestReceived = true;
                            }
                        }

                        // ----------------------------------------------------
                        // REJECTED
                        // ----------------------------------------------------

                        else if (friendship.Status == "Rejected")
                        {
                            relationshipStatus = "Rejected";
                        }

                        // ----------------------------------------------------
                        // BLOCKED
                        // ----------------------------------------------------

                        else if (friendship.Status == "Blocked")
                        {
                            relationshipStatus = "Blocked";
                        }
                    }

                    // --------------------------------------------------------
                    // Return student
                    // --------------------------------------------------------

                    return new
                    {
                        studentId = s.studentId,
                        userId = s.userId,
                        fullName = s.fullName,
                        email = s.email,

                        relationshipStatus = relationshipStatus,

                        friendRequestSent = friendRequestSent,

                        friendRequestReceived = friendRequestReceived
                    };
                }).ToList();

                // ============================================================
                // STEP 9: RETURN RESPONSE
                // ============================================================

                return Ok(new
                {
                    search = name,
                    totalStudents = result.Count,
                    students = result
                });
            }
            catch (Exception ex)
            {
                // ============================================================
                // ERROR
                // ============================================================

                return StatusCode(500, new
                {
                    message = "An error occurred while searching students.",
                    error = ex.Message
                });
            }
        }

        // ============================================================
        // ADD FRIEND
        // ============================================================
        [Authorize]
        [HttpPost("add-friend")]
        public async Task<IActionResult> AddFriend([FromBody] AddFriendRequest? model)
        {
            try
            {
                // ============================================================
                // STEP 1: VERIFY LOGGED-IN USER
                // ============================================================

                var userIdClaim =
                    User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrWhiteSpace(userIdClaim))
                {
                    return Unauthorized(new
                    {
                        message = "User is not authenticated."
                    });
                }

                if (!int.TryParse(userIdClaim, out int userId))
                {
                    return Unauthorized(new
                    {
                        message = "Invalid user ID."
                    });
                }

                // ============================================================
                // STEP 2: GET CURRENT STUDENT
                // ============================================================

                var student = await db.Students
                    .FirstOrDefaultAsync(s => s.UserId == userId);

                if (student == null)
                {
                    return NotFound(new
                    {
                        message = "Student profile not found."
                    });
                }

                // ============================================================
                // STEP 3: VALIDATE REQUEST
                // ============================================================

                if (model == null || model.FriendStudentId <= 0)
                {
                    return BadRequest(new
                    {
                        message = "Please provide a valid friend student ID."
                    });
                }

                // ============================================================
                // STEP 4: PREVENT ADDING YOURSELF
                // ============================================================

                if (student.StudentId == model.FriendStudentId)
                {
                    return BadRequest(new
                    {
                        message = "You cannot add yourself as a friend."
                    });
                }

                // ============================================================
                // STEP 5: FIND TARGET STUDENT
                // ============================================================

                var friendStudent = await (
                    from s in db.Students
                    join u in db.Users
                        on s.UserId equals u.UserId

                    where
                        s.StudentId == model.FriendStudentId &&
                        u.Role == "Student"

                    select new
                    {
                        studentId = s.StudentId,
                        userId = u.UserId,
                        fullName = u.FullName
                    }
                ).FirstOrDefaultAsync();

                if (friendStudent == null)
                {
                    return NotFound(new
                    {
                        message = "Student not found."
                    });
                }

                // ============================================================
                // STEP 6: CHECK EXISTING FRIENDSHIP
                // ============================================================

                var existingFriendship = await db.StudentFriends
                    .Where(f =>
                        (
                            f.StudentId == student.StudentId &&
                            f.FriendStudentId == model.FriendStudentId
                        )
                        ||
                        (
                            f.StudentId == model.FriendStudentId &&
                            f.FriendStudentId == student.StudentId
                        )
                    )
                    .OrderByDescending(f => f.FriendshipId)
                    .FirstOrDefaultAsync();

                // ============================================================
                // STEP 7: HANDLE EXISTING FRIENDSHIP
                // ============================================================

                if (existingFriendship != null)
                {
                    // --------------------------------------------------------
                    // ALREADY FRIENDS
                    // --------------------------------------------------------

                    if (existingFriendship.Status == "Accepted")
                    {
                        return BadRequest(new
                        {
                            message =
                                "You are already friends with this student.",

                            status = "Accepted"
                        });
                    }

                    // --------------------------------------------------------
                    // PENDING
                    // --------------------------------------------------------

                    if (existingFriendship.Status == "Pending")
                    {
                        // Current student already sent request
                        if (existingFriendship.StudentId ==
                            student.StudentId)
                        {
                            return BadRequest(new
                            {
                                message =
                                    "Friend request already sent.",

                                status = "PendingSent",

                                friendshipId =
                                    existingFriendship.FriendshipId
                            });
                        }

                        // Other student sent request
                        return BadRequest(new
                        {
                            message =
                                "This student has already sent you a friend request. Please accept or reject it.",

                            status = "PendingReceived",

                            friendshipId =
                                existingFriendship.FriendshipId
                        });
                    }

                    // --------------------------------------------------------
                    // BLOCKED
                    // --------------------------------------------------------

                    if (existingFriendship.Status == "Blocked")
                    {
                        return BadRequest(new
                        {
                            message =
                                "This friendship is blocked.",

                            status = "Blocked"
                        });
                    }

                    // --------------------------------------------------------
                    // REJECTED
                    // --------------------------------------------------------
                    //
                    // Allow sending a new request.
                    // --------------------------------------------------------

                    if (existingFriendship.Status == "Rejected")
                    {
                        existingFriendship.StudentId =
                            student.StudentId;

                        existingFriendship.FriendStudentId =
                            model.FriendStudentId;

                        existingFriendship.Status =
                            "Pending";

                        existingFriendship.RequestedDate =
                            DateTime.Now;

                        existingFriendship.AcceptedDate =
                            null;

                        await db.SaveChangesAsync();

                        return Ok(new
                        {
                            message =
                                "Friend request sent successfully.",

                            friendshipId =
                                existingFriendship.FriendshipId,

                            status = "Pending",

                            friendStudentId =
                                friendStudent.studentId,

                            friendName =
                                friendStudent.fullName
                        });
                    }
                }

                // ============================================================
                // STEP 8: CREATE NEW FRIEND REQUEST
                // ============================================================

                var friendship = new StudentFriend
                {
                    StudentId =
                        student.StudentId,

                    FriendStudentId =
                        model.FriendStudentId,

                    Status =
                        "Pending",

                    RequestedDate =
                        DateTime.Now
                };

                db.StudentFriends.Add(friendship);

                await db.SaveChangesAsync();

                // ============================================================
                // STEP 9: RETURN SUCCESS
                // ============================================================

                return Ok(new
                {
                    message =
                        "Friend request sent successfully.",

                    friendshipId =
                        friendship.FriendshipId,

                    status =
                        friendship.Status,

                    friendStudentId =
                        friendStudent.studentId,

                    friendName =
                        friendStudent.fullName
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message =
                        "An error occurred while adding friend.",

                    error =
                        ex.Message
                });
            }
        }

        [Authorize]
        [HttpGet("friend-requests")]
        public async Task<IActionResult> GetFriendRequests()
        {
            try
            {
                // ============================================================
                // STEP 1: VERIFY LOGGED-IN USER
                // ============================================================

                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrEmpty(userIdClaim))
                {
                    return Unauthorized(new
                    {
                        message = "User is not authenticated."
                    });
                }

                if (!int.TryParse(userIdClaim, out int userId))
                {
                    return Unauthorized(new
                    {
                        message = "Invalid user ID."
                    });
                }

                // ============================================================
                // STEP 2: GET LOGGED-IN STUDENT
                // ============================================================

                var student = await db.Students
                    .FirstOrDefaultAsync(s => s.UserId == userId);

                if (student == null)
                {
                    return NotFound(new
                    {
                        message = "Student profile not found."
                    });
                }

                // ============================================================
                // STEP 3: GET PENDING FRIEND REQUESTS
                // ============================================================
                //
                // friendStudentId = logged-in student
                //
                // studentId = student who sent the request
                //
                // Example:
                //
                // Ahmed -> Maryam
                //
                // student_id       = Ahmed
                // friend_student_id = Maryam
                //
                // If Maryam is logged in, this request will be returned.
                // ============================================================

                var requests = await (
                    from f in db.StudentFriends

                    join sender in db.Students
                        on f.StudentId equals sender.StudentId

                    join senderUser in db.Users
                        on sender.UserId equals senderUser.UserId

                    where f.FriendStudentId == student.StudentId
                          && f.Status == "Pending"

                    orderby f.RequestedDate descending

                    select new
                    {
                        friendshipId = f.FriendshipId,

                        senderStudentId = sender.StudentId,

                        senderUserId = senderUser.UserId,

                        senderName = senderUser.FullName,

                        senderEmail = senderUser.Email,

                        status = f.Status,

                        requestedDate = f.RequestedDate
                    }
                ).ToListAsync();

                // ============================================================
                // STEP 4: RETURN RESPONSE
                // ============================================================

                return Ok(new
                {
                    totalRequests = requests.Count,
                    requests = requests
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "An error occurred while getting friend requests.",
                    error = ex.Message
                });
            }
        }

        [Authorize]
        [HttpPost("accept-friend-request")]
        public async Task<IActionResult> AcceptFriendRequest([FromBody] FriendRequestActionRequest model)
        {
            try
            {
                // ============================================================
                // STEP 1: VERIFY LOGGED-IN USER
                // ============================================================

                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrEmpty(userIdClaim))
                {
                    return Unauthorized(new
                    {
                        message = "User is not authenticated."
                    });
                }

                if (!int.TryParse(userIdClaim, out int userId))
                {
                    return Unauthorized(new
                    {
                        message = "Invalid user ID."
                    });
                }

                // ============================================================
                // STEP 2: GET LOGGED-IN STUDENT
                // ============================================================

                var student = await db.Students
                    .FirstOrDefaultAsync(s => s.UserId == userId);

                if (student == null)
                {
                    return NotFound(new
                    {
                        message = "Student profile not found."
                    });
                }

                // ============================================================
                // STEP 3: VALIDATE REQUEST
                // ============================================================

                if (model == null || model.FriendshipId <= 0)
                {
                    return BadRequest(new
                    {
                        message = "Please provide a valid friendship ID."
                    });
                }

                // ============================================================
                // STEP 4: FIND FRIEND REQUEST
                // ============================================================
                //
                // IMPORTANT:
                //
                // friend_student_id MUST be the logged-in student.
                //
                // This prevents Student A from accepting Student B's
                // friend request that was actually sent to Student C.
                // ============================================================

                var friendship = await db.StudentFriends
                    .FirstOrDefaultAsync(f =>
                        f.FriendshipId == model.FriendshipId &&
                        f.FriendStudentId == student.StudentId &&
                        f.Status == "Pending"
                    );

                if (friendship == null)
                {
                    return NotFound(new
                    {
                        message = "Pending friend request not found."
                    });
                }

                // ============================================================
                // STEP 5: ACCEPT REQUEST
                // ============================================================

                friendship.Status = "Accepted";
                friendship.AcceptedDate = DateTime.Now;

                await db.SaveChangesAsync();

                // ============================================================
                // STEP 6: GET FRIEND NAME
                // ============================================================

                var senderStudent = await (
                    from s in db.Students
                    join u in db.Users
                        on s.UserId equals u.UserId
                    where s.StudentId == friendship.StudentId
                    select new
                    {
                        studentId = s.StudentId,
                        fullName = u.FullName
                    }
                ).FirstOrDefaultAsync();

                // ============================================================
                // STEP 7: RETURN SUCCESS
                // ============================================================

                return Ok(new
                {
                    message = "Friend request accepted successfully.",
                    friendshipId = friendship.FriendshipId,
                    status = friendship.Status,
                    friendStudentId = friendship.StudentId,
                    friendName = senderStudent?.fullName
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "An error occurred while accepting friend request.",
                    error = ex.Message
                });
            }
        }

        [Authorize]
        [HttpPost("reject-friend-request")]
        public async Task<IActionResult> RejectFriendRequest([FromBody] FriendRequestActionRequest model)
        {
            try
            {
                // ============================================================
                // STEP 1: VERIFY LOGGED-IN USER
                // ============================================================

                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrEmpty(userIdClaim))
                {
                    return Unauthorized(new
                    {
                        message = "User is not authenticated."
                    });
                }

                if (!int.TryParse(userIdClaim, out int userId))
                {
                    return Unauthorized(new
                    {
                        message = "Invalid user ID."
                    });
                }

                // ============================================================
                // STEP 2: GET LOGGED-IN STUDENT
                // ============================================================

                var student = await db.Students
                    .FirstOrDefaultAsync(s => s.UserId == userId);

                if (student == null)
                {
                    return NotFound(new
                    {
                        message = "Student profile not found."
                    });
                }

                // ============================================================
                // STEP 3: VALIDATE REQUEST
                // ============================================================

                if (model == null || model.FriendshipId <= 0)
                {
                    return BadRequest(new
                    {
                        message = "Please provide a valid friendship ID."
                    });
                }

                // ============================================================
                // STEP 4: FIND PENDING REQUEST
                // ============================================================

                var friendship = await db.StudentFriends
                    .FirstOrDefaultAsync(f =>
                        f.FriendshipId == model.FriendshipId &&
                        f.FriendStudentId == student.StudentId &&
                        f.Status == "Pending"
                    );

                if (friendship == null)
                {
                    return NotFound(new
                    {
                        message = "Pending friend request not found."
                    });
                }

                // ============================================================
                // STEP 5: REJECT REQUEST
                // ============================================================

                friendship.Status = "Rejected";

                await db.SaveChangesAsync();

                // ============================================================
                // STEP 6: RETURN SUCCESS
                // ============================================================

                return Ok(new
                {
                    message = "Friend request rejected successfully.",
                    friendshipId = friendship.FriendshipId,
                    status = friendship.Status
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "An error occurred while rejecting friend request.",
                    error = ex.Message
                });
            }
        }

        [Authorize]
        [HttpGet("all-friends")]
        public async Task<IActionResult> GetAllFriends()
        {
            try
            {
                // ============================================================
                // STEP 1: GET LOGGED-IN USER ID
                // ============================================================

                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrEmpty(userIdClaim))
                {
                    return Unauthorized(new
                    {
                        success = false,
                        message = "User ID not found in token."
                    });
                }

                if (!int.TryParse(userIdClaim, out int userId))
                {
                    return Unauthorized(new
                    {
                        success = false,
                        message = "Invalid User ID."
                    });
                }


                // ============================================================
                // STEP 2: FIND LOGGED-IN STUDENT
                // ============================================================

                var student = await db.Students
                    .FirstOrDefaultAsync(s => s.UserId == userId);

                if (student == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "Student profile not found."
                    });
                }


                // ============================================================
                // STEP 3: GET ACCEPTED FRIENDS
                // ============================================================

                var friends = await (
                    from friendship in db.StudentFriends

                    let friendStudentId =
                        friendship.StudentId == student.StudentId
                            ? friendship.FriendStudentId
                            : friendship.StudentId

                    join friendStudent in db.Students
                        on friendStudentId equals friendStudent.StudentId

                    join friendUser in db.Users
                        on friendStudent.UserId equals friendUser.UserId

                    where
                        friendship.Status == "Accepted"
                        &&
                        (
                            friendship.StudentId == student.StudentId
                            ||
                            friendship.FriendStudentId == student.StudentId
                        )

                    orderby friendUser.FullName

                    select new
                    {
                        friendshipId = friendship.FriendshipId,

                        friendStudentId = friendStudent.StudentId,

                        friendUserId = friendUser.UserId,

                        friendName = friendUser.FullName,

                        friendEmail = friendUser.Email,

                        friendPhone = friendUser.Phone,

                        friendshipStatus = friendship.Status,

                        requestedDate = friendship.RequestedDate,

                        acceptedDate = friendship.AcceptedDate
                    }
                ).ToListAsync();


                // ============================================================
                // STEP 4: RETURN RESPONSE
                // ============================================================

                return Ok(new
                {
                    success = true,

                    message = "Friends retrieved successfully.",

                    totalFriends = friends.Count,

                    friends = friends
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,

                    message = "An error occurred while getting friends.",

                    error = ex.Message
                });
            }
        }
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

        private int GetGradeOrder(string? grade)
        {
            string value =
                (grade ?? "")
                .Trim()
                .ToUpper();

            return value switch
            {
                "A+" => 1,
                "A" => 2,
                "A-" => 3,

                "B+" => 4,
                "B" => 5,
                "B-" => 6,

                "C+" => 7,
                "C" => 8,
                "C-" => 9,

                "D+" => 10,
                "D" => 11,
                "D-" => 12,

                "E+" => 13,
                "E" => 14,
                "E-" => 15,

                "F" => 16,

                _ => 99
            };
        }

        private bool TryParseTimeRange(string timeRange,out TimeSpan startTime,out TimeSpan endTime)
        {
            startTime = TimeSpan.Zero;
            endTime = TimeSpan.Zero;

            if (string.IsNullOrWhiteSpace(timeRange))
            {
                return false;
            }

            string value =
                timeRange
                    .Trim()
                    .ToLower()
                    .Replace(" ", "");


            // =====================================================
            // EXAMPLE:
            // 4:00-5:00pm
            // =====================================================

            var parts =
                value.Split(
                    '-',
                    StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length != 2)
            {
                return false;
            }


            string startPart = parts[0];
            string endPart = parts[1];


            // =====================================================
            // IF AM/PM ONLY EXISTS ON END TIME
            //
            // 4:00-5:00pm
            //
            // APPLY SAME AM/PM TO START
            // =====================================================

            bool endHasAm =
                endPart.Contains("am");

            bool endHasPm =
                endPart.Contains("pm");

            bool startHasAm =
                startPart.Contains("am");

            bool startHasPm =
                startPart.Contains("pm");


            if (!startHasAm &&
                !startHasPm &&
                (endHasAm || endHasPm))
            {
                startPart +=
                    endHasAm ? "am" : "pm";
            }


            // =====================================================
            // ADD SPACE BEFORE AM/PM
            // =====================================================

            startPart =
                startPart
                    .Replace("am", " am")
                    .Replace("pm", " pm")
                    .Trim();

            endPart =
                endPart
                    .Replace("am", " am")
                    .Replace("pm", " pm")
                    .Trim();


            // =====================================================
            // PARSE START
            // =====================================================

            if (!DateTime.TryParse(
                    startPart,
                    out DateTime startDateTime))
            {
                return false;
            }


            // =====================================================
            // PARSE END
            // =====================================================

            if (!DateTime.TryParse(
                    endPart,
                    out DateTime endDateTime))
            {
                return false;
            }


            startTime =
                startDateTime.TimeOfDay;

            endTime =
                endDateTime.TimeOfDay;


            // =====================================================
            // VALID RANGE
            // =====================================================

            if (startTime >= endTime)
            {
                return false;
            }


            return true;
        }

        private bool IsTimeOverlapping(TimeSpan requestedStart,TimeSpan requestedEnd,TimeOnly bookedStart,TimeOnly bookedEnd)
        {
            TimeSpan bookedStartTime = bookedStart.ToTimeSpan();
            TimeSpan bookedEndTime = bookedEnd.ToTimeSpan();

            return
                requestedStart < bookedEndTime &&
                requestedEnd > bookedStartTime;
        }
    }

    //Dto stand for (Data Transfer Object)
    public class TutorAvailableSlotDto
    {
        public string day { get; set; }
        public string time { get; set; }
        // New
        public bool is_available { get; set; }
        public string availability_message { get; set; }
        public string request_type { get; set; }
        public DateOnly? class_date { get; set; }
        // =====================================================
        // LT ROOM INFORMATION
        // =====================================================

        public int? lt_room_id { get; set; }

        public string lt_room_name { get; set; }

        public int? lt_room_capacity { get; set; }

        public int? lt_booked_count { get; set; }

        public int? lt_remaining_capacity { get; set; }

    }

    public class TutorSearchResultDto
    {
        public int tutor_id { get; set; }

        public string? tutor_name { get; set; }

        public string? location { get; set; }

        public double distance { get; set; }

        public double tutor_radius { get; set; }

        // ==========================================
        // COURSE INFORMATION
        // ==========================================

        public string? institute { get; set; }

        public string? grade { get; set; }

        public decimal hourly_rate { get; set; }

        // ==========================================
        // FEEDBACK
        // ==========================================

        public double average_rating { get; set; }

        public int total_reviews { get; set; }

        // ==========================================
        // SLOTS
        // ==========================================

        public List<TutorAvailableSlotDto> common_slots { get; set; }
            = new List<TutorAvailableSlotDto>();
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

    //Request Multiple tutor 
    public class CreateRequestDto
    {
        public List<int> tutor_ids { get; set; } = new List<int>();

        public int course_id { get; set; }

        public string day { get; set; }

        public string time { get; set; }

        public string learning_mode { get; set; }

        public int? learning_duration { get; set; }

        public string? learning_duration_unit { get; set; }
    }
    public class StudentSaveScheduleDto
    {
        public string? availabilityType { get; set; }

        public DateOnly? startDate { get; set; }

        public DateOnly? endDate { get; set; }

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

    //Get tutor with Course
    public class StudentTutorCourseDto
    {
        public int TutorId { get; set; }

        public string TutorName { get; set; }

        public int CourseId { get; set; }

        public string CourseName { get; set; }

        public bool IsCompleted { get; set; }

        public DateTime? CompletedDate { get; set; }

        public bool FeedbackGiven { get; set; }

        public string Grade { get; set; }
    }
    public class StudentGradeRequest
    {
        public int CourseId { get; set; }

        public string Grade { get; set; } = "";
    }

    //Give Feedback
    public class StudentFeedbackRequest
    {
        public int TutorId { get; set; }
        public int CourseId { get; set; }
        public int Rating { get; set; }
        public string Comment { get; set; }
    }
    // Single slot
    public class studentCommonSlotDto
    {
        public string Day { get; set; }
        public string Time { get; set; }
    }

    // Result wrapper
    public class StudentCommonSlotDto
    {
        public bool Found { get; set; }
        public List<studentCommonSlotDto> Slots { get; set; } = new();
    }

    //To Upload Course Content
    public class UploadCourseContentDto
    {
        public int course_id { get; set; }

        public string title { get; set; }

        public string description { get; set; }

        public IFormFile file { get; set; }
    }

    //Non-Visiting Tutor Search
    public class NonVisitingTutorSearchResultDto
    {
        public int tutor_id { get; set; }

        public string? tutor_name { get; set; }

        public string? location { get; set; }

        public string? qualification { get; set; }

        public int? experience { get; set; }

        public string? teaching_mode { get; set; }

        public int course_id { get; set; }

        public string? institute { get; set; }

        public string? grade { get; set; }

        public decimal hourly_rate { get; set; }

        public double average_rating { get; set; }

        public int total_reviews { get; set; }

        public List<TutorAvailableSlotDto> common_slots { get; set; }
            = new List<TutorAvailableSlotDto>();
    }

    public class AddFriendRequest
    {
        public int FriendStudentId { get; set; }
    }

    public class FriendRequestActionRequest
    {
        public int FriendshipId { get; set; }
    }
}
