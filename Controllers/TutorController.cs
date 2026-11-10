using Azure.Core;
using HouseofTutorAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Threading.Channels;
using static System.Runtime.InteropServices.JavaScript.JSType;
using Request = HouseofTutorAPI.Models.Request;

namespace HouseofTutorAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TutorController : ControllerBase
    {
        private readonly HouseofTutorContext db;

        private readonly IWebHostEnvironment _environment;
        public TutorController(HouseofTutorContext _db, IWebHostEnvironment environment)
        {
            db = _db;
            _environment = environment;
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
                // STEP 1: Get logged-in user
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);

                if (userIdClaim == null ||
                    !int.TryParse(userIdClaim.Value, out int userId))
                {
                    return Unauthorized(new
                    {
                        success = false,
                        message = "Invalid token."
                    });
                }

                // STEP 2: Find tutor
                var tutor = await db.Tutors
                    .FirstOrDefaultAsync(t => t.UserId == userId);

                if (tutor == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "Tutor not found."
                    });
                }

                // STEP 3: Fetch pending requests and their course plans
                // LEFT JOIN is used because Normal requests do not have
                // a record in Request_Course_Plan.

                var requests = await (
                    from r in db.Requests

                    join p in db.RequestCoursePlans
                        on r.RequestGroupId equals (int?)p.RequestGroupId
                        into planGroup

                    from plan in planGroup.DefaultIfEmpty()

                    where r.TutorId == tutor.TutorId
                          && r.Status == "Pending"

                    orderby r.RequestDate descending

                    select new
                    {
                        // ---------------------------------
                        // REQUEST INFORMATION
                        // ---------------------------------

                        request_id = r.RequestId,

                        request_group_id = r.RequestGroupId,

                        request_date = r.RequestDate,

                        class_date = r.ClassDate,

                        day = r.Day,

                        time = r.Time,

                        request_type = r.RequestType,

                        status = r.Status,

                        parent_request_id = r.ParentRequestId,

                        tutor_sequence = r.TutorSequence,

                        response_deadline = r.ResponseDeadline,

                        // ---------------------------------
                        // STUDENT INFORMATION
                        // ---------------------------------

                        student_id = r.Student.StudentId,

                        student_name = r.Student.User.FullName,

                        student_email = r.Student.User.Email,

                        student_phone = r.Student.User.Phone,

                        // ---------------------------------
                        // COURSE INFORMATION
                        // ---------------------------------

                        course_id = r.CourseId,

                        course_name = r.Course.CourseTitle,

                        standard_duration_weeks =
                            r.Course.StandardDurationWeeks,

                        standard_classes_per_week =
                            r.Course.StandardClassesPerWeek,

                        // ---------------------------------
                        // LEARNING INFORMATION
                        // ---------------------------------

                        learning_mode = r.LearningMode,

                        learning_duration = r.LearningDuration,

                        learning_duration_unit = r.LearningDurationUnit,

                        // ---------------------------------
                        // COURSE PLAN INFORMATION
                        // ---------------------------------

                        has_course_plan = plan != null,

                        plan_type = plan == null
                            ? "Normal"
                            : plan.PlanType,

                        original_duration_weeks = plan == null
                            ? (int?)null
                            : plan.OriginalDurationWeeks,

                        original_classes_per_week = plan == null
                            ? (int?)null
                            : plan.OriginalClassesPerWeek,

                        requested_duration_weeks = plan == null
                            ? (int?)null
                            : plan.RequestedDurationWeeks,

                        requested_classes_per_week = plan == null
                            ? (int?)null
                            : plan.RequestedClassesPerWeek,

                        total_classes = plan == null
                            ? (int?)null
                            : plan.TotalClasses,

                        plan_start_date = plan == null
                            ? (DateOnly?)null
                            : plan.StartDate,

                        target_end_date = plan == null
                            ? (DateOnly?)null
                            : plan.TargetEndDate,

                        plan_status = plan == null
                            ? null
                            : plan.Status,

                        tutor_remarks = plan == null
                            ? null
                            : plan.TutorRemarks,

                        approved_date = plan == null
                            ? (DateTime?)null
                            : plan.ApprovedDate,

                        approved_by_tutor_id = plan == null
                            ? (int?)null
                            : plan.ApprovedByTutorId,

                        class_duration_minutes = plan == null
                            ? (int?)null
                            : plan.ClassDurationMinutes,

                        plan_created_date = plan == null
                            ? (DateTime?)null
                            : plan.CreatedDate
                    }
                ).ToListAsync();

                // STEP 4: Return response
                return Ok(new
                {
                    success = true,

                    message = "Pending requests fetched successfully.",

                    tutor_id = tutor.TutorId,

                    total_requests = requests.Count,

                    data = requests
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,

                    message = "Error while fetching tutor requests.",

                    error = ex.InnerException?.Message ?? ex.Message
                });
            }
        }


        //[Authorize]
        //[HttpPut("accept-request/{id}")]
        //public async Task<IActionResult> AcceptRequest(int id)
        //{
        //    var (request, error) = await GetOwnPendingRequest(id);

        //    if (error != null)
        //        return error;

        //    request!.Status = "Accepted";
        //    request.ResponseDeadline = null;

        //    // Close the request group and cancel the remaining tutors in the queue
        //    if (request.RequestGroupId.HasValue)
        //    {
        //        var group = await db.RequestGroups
        //            .FirstOrDefaultAsync(g => g.RequestGroupId == request.RequestGroupId);

        //        if (group != null)
        //        {
        //            group.Status = "Accepted";
        //            group.CurrentRequestId = request.RequestId;

        //            var others = await db.Requests
        //                .Where(r =>
        //                    r.RequestGroupId == group.RequestGroupId &&
        //                    r.RequestId != request.RequestId &&
        //                    (r.Status == "Queued" || r.Status == "Pending"))
        //                .ToListAsync();

        //            foreach (var other in others)
        //            {
        //                other.Status = "Cancelled";
        //                other.ResponseDeadline = null;
        //            }
        //        }
        //    }

        //    await db.SaveChangesAsync();

        //    return Ok(new { message = "Request accepted" });
        //}

        [Authorize]
        [HttpPut("accept-request/{id}")]
        public async Task<IActionResult> AcceptRequest(int id)
        {
            using var transaction =
                await db.Database.BeginTransactionAsync();

            try
            {
                var (request, error) = await GetOwnPendingRequest(id);

                if (error != null)
                    return error;

                request = request!;

                RequestCoursePlan? plan = null;

                RequestGroup? group = null;

                if (request.RequestGroupId.HasValue)
                {
                    group = await db.RequestGroups
                        .FirstOrDefaultAsync(g =>
                            g.RequestGroupId ==
                            request.RequestGroupId.Value);

                    if (group != null)
                    {
                        plan = await db.RequestCoursePlans
                            .FirstOrDefaultAsync(p =>
                                p.RequestGroupId == group.RequestGroupId);
                    }
                }

                // =====================================================
                // NORMAL AND ACCELERATED COURSE PLANS
                // =====================================================

                if (plan != null)
                {
                    if (group == null ||
                        !request.StudentId.HasValue ||
                        !request.TutorId.HasValue ||
                        !request.CourseId.HasValue)
                    {
                        return BadRequest(new
                        {
                            message = "Request information is incomplete."
                        });
                    }

                    if (!plan.StartDate.HasValue ||
                        plan.RequestedDurationWeeks <= 0 ||
                        plan.RequestedClassesPerWeek <= 0)
                    {
                        return BadRequest(new
                        {
                            message = "Course plan information is incomplete."
                        });
                    }

                    // Calculate required classes
                    int requiredClasses =
                        plan.RequestedDurationWeeks *
                        plan.RequestedClassesPerWeek;

                    if (requiredClasses != plan.TotalClasses)
                    {
                        return BadRequest(new
                        {
                            message = "Course plan total classes do not match."
                        });
                    }

                    // =================================================
                    // STEP 1: GET TUTOR AND STUDENT SCHEDULES
                    // =================================================

                    var tutorSchedules = await db.Schedules
                        .Where(s => s.TutorId == request.TutorId)
                        .ToListAsync();

                    var studentSchedules = await db.StudentSchedules
                        .Where(s => s.StudentId == request.StudentId)
                        .ToListAsync();

                    // =================================================
                    // STEP 2: FIND COMMON WEEKLY SLOTS
                    // =================================================

                    var commonSlots = (
                        from tutorSlot in tutorSchedules

                        from studentSlot in studentSchedules

                        where
                            !string.IsNullOrWhiteSpace(tutorSlot.Day) &&

                            !string.IsNullOrWhiteSpace(studentSlot.Day) &&

                            !string.IsNullOrWhiteSpace(tutorSlot.Time) &&

                            !string.IsNullOrWhiteSpace(studentSlot.Time) &&

                            NormalizePlanDay(tutorSlot.Day) ==
                            NormalizePlanDay(studentSlot.Day) &&

                            NormalizePlanTime(tutorSlot.Time) ==
                            NormalizePlanTime(studentSlot.Time)

                        select new
                        {
                            Day = tutorSlot.Day!,

                            Time = tutorSlot.Time!,

                            TutorStartDate = tutorSlot.StartDate,

                            TutorEndDate = tutorSlot.EndDate,

                            StudentStartDate = studentSlot.StartDate,

                            StudentEndDate = studentSlot.EndDate
                        }
                    )
                    .GroupBy(x =>
                        NormalizePlanDay(x.Day) + "|" +
                        NormalizePlanTime(x.Time))
                    .Select(g => g.First())
                    .ToList();

                    // =================================================
                    // STEP 3: CHECK REQUIRED WEEKLY SLOTS
                    // =================================================

                    if (commonSlots.Count <
                        plan.RequestedClassesPerWeek)
                    {
                        return BadRequest(new
                        {
                            message = "Tutor and student do not have enough common weekly slots.",

                            required_slots_per_week =
                                plan.RequestedClassesPerWeek,

                            available_common_slots =
                                commonSlots.Count
                        });
                    }

                    // =================================================
                    // STEP 4: PRIORITIZE STUDENT'S SELECTED SLOT
                    // =================================================

                    var selectedSlots = commonSlots
                        .OrderByDescending(s =>
                            NormalizePlanDay(s.Day) ==
                            NormalizePlanDay(request.Day) &&

                            NormalizePlanTime(s.Time) ==
                            NormalizePlanTime(request.Time))
                        .Take(plan.RequestedClassesPerWeek)
                        .ToList();

                    bool requestedSlotExists = selectedSlots.Any(s =>
                        NormalizePlanDay(s.Day) ==
                        NormalizePlanDay(request.Day) &&

                        NormalizePlanTime(s.Time) ==
                        NormalizePlanTime(request.Time));

                    if (!requestedSlotExists)
                    {
                        return BadRequest(new
                        {
                            message = "The student's selected day and time are not available in the common tutor/student schedule."
                        });
                    }

                    // =================================================
                    // STEP 5: GENERATE ALL COURSE CLASSES
                    // =================================================

                    var sessions =
                        new List<(DateOnly Date, string Day, string Time)>();

                    DateOnly startDate = plan.StartDate.Value;

                    for (int week = 0;
                         week < plan.RequestedDurationWeeks;
                         week++)
                    {
                        foreach (var slot in selectedSlots)
                        {
                            DateOnly classDate = GetPlanDateForDay(
                                startDate.AddDays(week * 7),
                                slot.Day
                            );

                            // Check tutor schedule date range
                            bool tutorScheduleActive =
                                (!slot.TutorStartDate.HasValue ||
                                 classDate >= slot.TutorStartDate.Value) &&

                                (!slot.TutorEndDate.HasValue ||
                                 classDate <= slot.TutorEndDate.Value);

                            // Check student schedule date range
                            bool studentScheduleActive =
                                (!slot.StudentStartDate.HasValue ||
                                 classDate >= slot.StudentStartDate.Value) &&

                                (!slot.StudentEndDate.HasValue ||
                                 classDate <= slot.StudentEndDate.Value);

                            if (!tutorScheduleActive ||
                                !studentScheduleActive)
                            {
                                return BadRequest(new
                                {
                                    message = "Selected schedule is not active for the complete course plan.",

                                    day = slot.Day,

                                    time = slot.Time,

                                    date = classDate
                                });
                            }

                            // Check existing accepted classes
                            bool isBusy = await db.Requests.AnyAsync(r =>
                                r.Status == "Accepted" &&

                                r.ClassDate == classDate &&

                                r.Time == slot.Time &&

                                (
                                    r.TutorId == request.TutorId ||
                                    r.StudentId == request.StudentId
                                ));

                            if (isBusy)
                            {
                                return BadRequest(new
                                {
                                    message = "A schedule conflict was found. Please update the tutor or student schedule.",

                                    day = slot.Day,

                                    time = slot.Time,

                                    date = classDate
                                });
                            }

                            sessions.Add((
                                classDate,
                                slot.Day,
                                slot.Time
                            ));
                        }
                    }

                    // =================================================
                    // STEP 6: VALIDATE TOTAL GENERATED CLASSES
                    // =================================================

                    if (sessions.Count != plan.TotalClasses)
                    {
                        return BadRequest(new
                        {
                            message = "Could not generate all required classes.",

                            expected_classes = plan.TotalClasses,

                            generated_classes = sessions.Count
                        });
                    }

                    // =================================================
                    // STEP 7: SORT CLASSES BY DATE
                    // =================================================

                    sessions = sessions
                        .OrderBy(s => s.Date)
                        .ThenBy(s => NormalizePlanTime(s.Time))
                        .ToList();

                    // =================================================
                    // STEP 8: UPDATE FIRST REQUEST
                    // =================================================

                    var firstSession = sessions[0];

                    request.Status = "Accepted";

                    request.ResponseDeadline = null;

                    request.ClassDate = firstSession.Date;

                    request.Day = firstSession.Day;

                    request.Time = firstSession.Time;

                    request.RequestType = "Normal";

                    request.RequestDate = DateTime.Now;

                    // =================================================
                    // STEP 9: CREATE REMAINING CLASS REQUESTS
                    // =================================================

                    var additionalRequests = new List<Request>();

                    for (int i = 1; i < sessions.Count; i++)
                    {
                        var session = sessions[i];

                        additionalRequests.Add(new Request
                        {
                            StudentId = request.StudentId,

                            TutorId = request.TutorId,

                            CourseId = request.CourseId,

                            RequestDate = DateTime.Now,

                            Status = "Accepted",

                            Time = session.Time,

                            ClassDate = session.Date,

                            Day = session.Day,

                            RequestType = "Normal",

                            ParentRequestId = null,

                            LearningMode = request.LearningMode,

                            LearningDuration = request.LearningDuration,

                            LearningDurationUnit =
                                request.LearningDurationUnit,

                            RequestGroupId = request.RequestGroupId,

                            TutorSequence = request.TutorSequence,

                            ResponseDeadline = null
                        });
                    }

                    if (additionalRequests.Count > 0)
                    {
                        db.Requests.AddRange(additionalRequests);
                    }

                    // =================================================
                    // STEP 10: APPROVE COURSE PLAN
                    // =================================================

                    plan.TargetEndDate =
                        sessions.Max(s => s.Date);

                    plan.Status = "Approved";

                    plan.ApprovedDate = DateTime.Now;

                    plan.ApprovedByTutorId = request.TutorId.Value;

                    // Close request group
                    group.Status = "Accepted";

                    group.CurrentRequestId = request.RequestId;

                    // Cancel other tutors
                    var otherRequests = await db.Requests
                        .Where(r =>
                            r.RequestGroupId == group.RequestGroupId &&

                            r.RequestId != request.RequestId &&

                            (
                                r.Status == "Queued" ||
                                r.Status == "Pending"
                            ))
                        .ToListAsync();

                    foreach (var other in otherRequests)
                    {
                        other.Status = "Cancelled";

                        other.ResponseDeadline = null;
                    }

                    await db.SaveChangesAsync();

                    await transaction.CommitAsync();

                    // =================================================
                    // STEP 11: RESPONSE
                    // =================================================

                    return Ok(new
                    {
                        success = true,

                        message = "Course plan approved and all classes scheduled successfully.",

                        request_group_id = group.RequestGroupId,

                        plan_id = plan.PlanId,

                        plan_type = plan.PlanType,

                        plan_status = plan.Status,

                        approved_date = plan.ApprovedDate,

                        approved_by_tutor_id =
                            plan.ApprovedByTutorId,

                        original_duration_weeks =
                            plan.OriginalDurationWeeks,

                        original_classes_per_week =
                            plan.OriginalClassesPerWeek,

                        duration_weeks =
                            plan.RequestedDurationWeeks,

                        classes_per_week =
                            plan.RequestedClassesPerWeek,

                        total_classes = plan.TotalClasses,

                        start_date = plan.StartDate,

                        target_end_date = plan.TargetEndDate,

                        generated_classes = sessions.Select(s => new
                        {
                            date = s.Date,

                            day = s.Day,

                            time = s.Time
                        }).ToList()
                    });
                }

                // =====================================================
                // LEGACY REQUEST WITHOUT COURSE PLAN
                // =====================================================

                request.Status = "Accepted";

                request.ResponseDeadline = null;

                if (request.RequestGroupId.HasValue)
                {
                    group ??= await db.RequestGroups
                        .FirstOrDefaultAsync(g =>
                            g.RequestGroupId ==
                            request.RequestGroupId.Value);

                    if (group != null)
                    {
                        group.Status = "Accepted";

                        group.CurrentRequestId = request.RequestId;

                        var others = await db.Requests
                            .Where(r =>
                                r.RequestGroupId == group.RequestGroupId &&

                                r.RequestId != request.RequestId &&

                                (
                                    r.Status == "Queued" ||
                                    r.Status == "Pending"
                                ))
                            .ToListAsync();

                        foreach (var other in others)
                        {
                            other.Status = "Cancelled";

                            other.ResponseDeadline = null;
                        }
                    }
                }

                await db.SaveChangesAsync();

                await transaction.CommitAsync();

                return Ok(new
                {
                    success = true,

                    message = "Request accepted successfully."
                });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();

                return StatusCode(500, new
                {
                    success = false,

                    message = "Error while accepting request.",

                    error = ex.InnerException?.Message ?? ex.Message
                });
            }
        }


        //Tutor reject student classes request for nomal
        [Authorize]
        [HttpPut("reject-request/{id}")]
        public async Task<IActionResult> RejectRequest(int id)
        {
            var (request, error) = await GetOwnPendingRequest(id);

            if (error != null)
                return error;

            request!.Status = "Rejected";
            request.ResponseDeadline = null;

            // Pass the request on to the next queued tutor
            if (request.RequestGroupId.HasValue)
            {
                var group = await db.RequestGroups
                    .FirstOrDefaultAsync(g => g.RequestGroupId == request.RequestGroupId);

                if (group != null &&
                    group.Status == "Searching" &&
                    group.CurrentRequestId == request.RequestId)
                {
                    var next = await db.Requests
                        .Where(r =>
                            r.RequestGroupId == group.RequestGroupId &&
                            r.Status == "Queued")
                        .OrderBy(r => r.TutorSequence)
                        .FirstOrDefaultAsync();

                    if (next != null)
                    {
                        var now = DateTime.Now;

                        next.Status = "Pending";
                        next.RequestDate = now;
                        next.ResponseDeadline = now.AddMinutes(2);

                        group.CurrentRequestId = next.RequestId;
                    }
                    else
                    {
                        group.Status = "Rejected";
                        group.CurrentRequestId = null;
                    }
                }
            }

            await db.SaveChangesAsync();

            return Ok(new { message = "Request rejected" });
        }
        
        // Loads a request that belongs to the logged-in tutor and is still Pending
        private async Task<(Request? request, IActionResult? error)> GetOwnPendingRequest(int id)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!int.TryParse(userIdClaim, out int userId))
                return (null, Unauthorized(new { message = "Invalid token" }));

            var tutor = await db.Tutors.FirstOrDefaultAsync(t => t.UserId == userId);

            if (tutor == null)
                return (null, NotFound(new { message = "Tutor not found" }));

            var request = await db.Requests.FindAsync(id);

            if (request == null)
                return (null, NotFound(new { message = "Request not found" }));

            if (request.TutorId != tutor.TutorId)
                return (null, StatusCode(403, new { message = "This request does not belong to you" }));

            if (request.Status != "Pending")
                return (null, BadRequest(new { message = $"This request is already {request.Status?.ToLower()}" }));

            return (request, null);
        }
//        Fix 1 (queue handling) is in AcceptRequest and RejectRequest:
//        - AcceptRequest is the if (request.RequestGroupId.HasValue) block(lines 363–386).
//        - RejectRequest is the if (request.RequestGroupId.HasValue) block(lines 407–439).

//        Fix 2 (ownership and Pending checks) is in GetOwnPendingRequest:
//        - It sits at lines 447–471 and is called at the top of both actions.
//        - It returns 404, 403 or 400 before any data changes.
        ////Tutor reject student classes request for nomal
        //[Authorize]
        //[HttpPut("reject-request/{id}")]
        //public async Task<IActionResult> RejectRequest(int id)
        //{
        //    var request = await db.Requests.FindAsync(id);

        //    if (request == null)
        //        return NotFound(new { message = "Request not found" });

        //    request.Status = "Rejected";

        //    await db.SaveChangesAsync();

        //    return Ok(new { message = "Request rejected" });
        //}

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

        //[Authorize] // Add Course and Hourly rate and Hourly rate must between Min and Max Rate, Also Institute
        //[HttpPost("add-courses")]
        //public async Task<IActionResult> AddTutorCourses([FromBody] AddTutorCoursesDto dto)
        //{
        //    try
        //    {
        //        // =====================================================
        //        // 1. Validate request
        //        // =====================================================
        //        if (dto == null ||
        //            dto.Courses == null ||
        //            dto.Courses.Count == 0)
        //        {
        //            return BadRequest(new
        //            {
        //                message = "No courses selected."
        //            });
        //        }

        //        // =====================================================
        //        // 2. Get User ID from Token
        //        // =====================================================
        //        var userIdClaim =
        //            User.FindFirst(ClaimTypes.NameIdentifier);

        //        if (userIdClaim == null)
        //        {
        //            return Unauthorized(new
        //            {
        //                message = "Invalid token."
        //            });
        //        }

        //        if (!int.TryParse(
        //                userIdClaim.Value,
        //                out int userId))
        //        {
        //            return Unauthorized(new
        //            {
        //                message = "Invalid user ID."
        //            });
        //        }

        //        // =====================================================
        //        // 3. Find Tutor
        //        // =====================================================
        //        var tutor = await db.Tutors
        //            .FirstOrDefaultAsync(t =>
        //                t.UserId == userId);

        //        if (tutor == null)
        //        {
        //            return NotFound(new
        //            {
        //                message = "Tutor not found."
        //            });
        //        }

        //        int added = 0;

        //        // =====================================================
        //        // 4. Valid Grades
        //        // =====================================================
        //        var validGrades = new[]
        //        {
        //            "A",
        //            "B",
        //            "C",
        //            "D",
        //            "F"
        //        };

        //        // =====================================================
        //        // 5. Process Courses
        //        // =====================================================
        //        foreach (var item in dto.Courses)
        //        {
        //            // -------------------------------------------------
        //            // Validate Course ID
        //            // -------------------------------------------------
        //            if (item.CourseId <= 0)
        //            {
        //                continue;
        //            }

        //            // -------------------------------------------------
        //            // Validate Grade
        //            // -------------------------------------------------
        //            if (string.IsNullOrWhiteSpace(item.Grade))
        //            {
        //                continue;
        //            }

        //            string grade =
        //                item.Grade.Trim().ToUpper();

        //            if (!validGrades.Contains(grade))
        //            {
        //                return BadRequest(new
        //                {
        //                    message =
        //                        $"Invalid grade '{item.Grade}' for course ID {item.CourseId}."
        //                });
        //            }

        //            // -------------------------------------------------
        //            // Validate Institute
        //            // -------------------------------------------------
        //            if (string.IsNullOrWhiteSpace(item.Institute))
        //            {
        //                return BadRequest(new
        //                {
        //                    message =
        //                        $"Institute is required for course ID {item.CourseId}."
        //                });
        //            }

        //            string institute =
        //                item.Institute.Trim();

        //            if (institute.Length > 150)
        //            {
        //                return BadRequest(new
        //                {
        //                    message =
        //                        $"Institute name cannot be more than 150 characters for course ID {item.CourseId}."
        //                });
        //            }

        //            // -------------------------------------------------
        //            // Validate Hourly Rate
        //            // -------------------------------------------------
        //            if (item.HourlyRate <= 0)
        //            {
        //                return BadRequest(new
        //                {
        //                    message =
        //                        $"Hourly rate must be greater than 0 for course ID {item.CourseId}."
        //                });
        //            }

        //            // =================================================
        //            // 6. Get Course
        //            // =================================================
        //            var course = await db.Courses
        //                .FirstOrDefaultAsync(c =>
        //                    c.CourseId == item.CourseId);

        //            if (course == null)
        //            {
        //                return BadRequest(new
        //                {
        //                    message =
        //                        $"Course with ID {item.CourseId} not found."
        //                });
        //            }

        //            // =================================================
        //            // 7. Get Admin Min / Max Rate
        //            // =================================================
        //            if (!course.AdminSetMinHourlyRate.HasValue ||
        //                !course.AdminSetMaxHourlyRate.HasValue)
        //            {
        //                return BadRequest(new
        //                {
        //                    message =
        //                        $"Hourly rate range is not configured for {course.CourseTitle}."
        //                });
        //            }

        //            decimal minRate =
        //                course.AdminSetMinHourlyRate.Value;

        //            decimal maxRate =
        //                course.AdminSetMaxHourlyRate.Value;

        //            // =================================================
        //            // 8. Validate Min <= Max
        //            // =================================================
        //            if (minRate > maxRate)
        //            {
        //                return BadRequest(new
        //                {
        //                    message =
        //                        $"Invalid hourly rate configuration for {course.CourseTitle}."
        //                });
        //            }

        //            // =================================================
        //            // 9. Validate Tutor Hourly Rate
        //            // =================================================
        //            if (item.HourlyRate < minRate ||
        //                item.HourlyRate > maxRate)
        //            {
        //                return BadRequest(new
        //                {
        //                    message =
        //                        $"You can set hourly rate between Rs. {minRate:0.##} and Rs. {maxRate:0.##} for {course.CourseTitle}.",

        //                    courseId = course.CourseId,
        //                    courseTitle = course.CourseTitle,
        //                    minRate = minRate,
        //                    maxRate = maxRate,
        //                    enteredRate = item.HourlyRate
        //                });
        //            }

        //            // =================================================
        //            // 10. Check Existing Tutor Course
        //            // =================================================
        //            bool exists = await db.TutorCourses
        //                .AnyAsync(tc =>
        //                    tc.TutorId == tutor.TutorId &&
        //                    tc.CourseId == item.CourseId);

        //            if (exists)
        //            {
        //                continue;
        //            }

        //            // =================================================
        //            // 11. Save Tutor Course
        //            //     Grade + Institute
        //            // =================================================
        //            var tutorCourse = new TutorCourse
        //            {
        //                TutorId = tutor.TutorId,
        //                CourseId = item.CourseId,
        //                Grade = grade,
        //                Institute = institute
        //            };

        //            db.TutorCourses.Add(tutorCourse);

        //            // =================================================
        //            // 12. Save Tutor Hourly Rate
        //            // =================================================
        //            var tutorCourseRate = new TutorCourseRate
        //            {
        //                TutorId = tutor.TutorId,
        //                CourseId = item.CourseId,
        //                HourlyRate = item.HourlyRate,

        //                // Save current Admin limits as well
        //                AdminSetMinHourlyRate = minRate,
        //                AdminSetMaxHourlyRate = maxRate
        //            };

        //            db.TutorCourseRates.Add(tutorCourseRate);

        //            added++;
        //        }

        //        // =====================================================
        //        // 13. No Courses Added
        //        // =====================================================
        //        if (added == 0)
        //        {
        //            return BadRequest(new
        //            {
        //                message =
        //                    "No courses were added. The selected courses may already exist or the provided data is invalid."
        //            });
        //        }

        //        // =====================================================
        //        // 14. Save Changes
        //        // =====================================================
        //        await db.SaveChangesAsync();

        //        // =====================================================
        //        // 15. Success
        //        // =====================================================
        //        return Ok(new
        //        {
        //            message = "Courses added successfully.",
        //            added
        //        });
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(500, new
        //        {
        //            message = "Error adding courses.",
        //            error =
        //                ex.InnerException?.Message ??
        //                ex.Message
        //        });
        //    }
        //}

        [Authorize]
        [HttpPost("add-courses")]
        public async Task<IActionResult> AddTutorCourses([FromBody] AddTutorCoursesDto dto)
        {
            await using var transaction =
                await db.Database.BeginTransactionAsync();

            try
            {
                // =====================================================
                // 1. Validate Request
                // =====================================================

                if (dto == null ||
                    dto.Courses == null ||
                    dto.Courses.Count == 0)
                {
                    return BadRequest(new
                    {
                        message = "No courses selected."
                    });
                }

                // =====================================================
                // 2. Get User ID From JWT Token
                // =====================================================

                var userIdClaim =
                    User.FindFirst(ClaimTypes.NameIdentifier);

                if (userIdClaim == null ||
                    !int.TryParse(userIdClaim.Value, out int userId))
                {
                    return Unauthorized(new
                    {
                        message = "Invalid token or user ID."
                    });
                }

                // =====================================================
                // 3. Find Tutor
                // =====================================================

                var tutor = await db.Tutors
                    .FirstOrDefaultAsync(t => t.UserId == userId);

                if (tutor == null)
                {
                    return NotFound(new
                    {
                        message = "Tutor not found."
                    });
                }

                // =====================================================
                // 4. Valid Grades
                // =====================================================

                var validGrades = new[]
                {
            "A",
            "B",
            "C",
            "D",
            "F"
        };

                int added = 0;
                int skipped = 0;

                // Prevent duplicate courses in the same request
                var processedCourseIds = new HashSet<int>();

                // =====================================================
                // 5. Process Selected Courses
                // =====================================================

                foreach (var item in dto.Courses)
                {
                    // -------------------------------------------------
                    // Validate Course ID
                    // -------------------------------------------------

                    if (item.CourseId <= 0)
                    {
                        return BadRequest(new
                        {
                            message = "Invalid course ID."
                        });
                    }

                    if (!processedCourseIds.Add(item.CourseId))
                    {
                        return BadRequest(new
                        {
                            message =
                                $"Course ID {item.CourseId} is duplicated in your request."
                        });
                    }

                    // -------------------------------------------------
                    // Validate Grade
                    // -------------------------------------------------

                    if (string.IsNullOrWhiteSpace(item.Grade))
                    {
                        return BadRequest(new
                        {
                            message =
                                $"Grade is required for course ID {item.CourseId}."
                        });
                    }

                    string grade = item.Grade.Trim().ToUpperInvariant();

                    if (!validGrades.Contains(grade))
                    {
                        return BadRequest(new
                        {
                            message =
                                $"Invalid grade '{item.Grade}' for course ID {item.CourseId}."
                        });
                    }

                    // -------------------------------------------------
                    // Validate Institute
                    // -------------------------------------------------

                    if (string.IsNullOrWhiteSpace(item.Institute))
                    {
                        return BadRequest(new
                        {
                            message =
                                $"Institute is required for course ID {item.CourseId}."
                        });
                    }

                    string institute = item.Institute.Trim();

                    if (institute.Length > 150)
                    {
                        return BadRequest(new
                        {
                            message =
                                "Institute name cannot exceed 150 characters."
                        });
                    }

                    // -------------------------------------------------
                    // Validate Previous Teacher
                    // -------------------------------------------------

                    if (string.IsNullOrWhiteSpace(item.PreviousTeacher))
                    {
                        return BadRequest(new
                        {
                            message =
                                $"Previous teacher is required for course ID {item.CourseId}."
                        });
                    }

                    string previousTeacher = item.PreviousTeacher.Trim();

                    if (previousTeacher.Length > 150)
                    {
                        return BadRequest(new
                        {
                            message =
                                "Previous teacher name cannot exceed 150 characters."
                        });
                    }

                    // -------------------------------------------------
                    // Validate Study Year (Optional)
                    // -------------------------------------------------

                    if (item.StudyYear.HasValue &&
                        (item.StudyYear.Value < 1900 ||
                         item.StudyYear.Value > DateTime.Now.Year))
                    {
                        return BadRequest(new
                        {
                            message =
                                $"Invalid study year for course ID {item.CourseId}."
                        });
                    }

                    // -------------------------------------------------
                    // Validate Hourly Rate
                    // -------------------------------------------------

                    if (item.HourlyRate <= 0)
                    {
                        return BadRequest(new
                        {
                            message =
                                $"Hourly rate must be greater than zero for course ID {item.CourseId}."
                        });
                    }

                    // =================================================
                    // 6. Find Course
                    // =================================================

                    var course = await db.Courses
                        .FirstOrDefaultAsync(c =>
                            c.CourseId == item.CourseId);

                    if (course == null)
                    {
                        return BadRequest(new
                        {
                            message =
                                $"Course with ID {item.CourseId} not found."
                        });
                    }

                    // =================================================
                    // 7. Get Admin Minimum and Maximum Rate
                    // =================================================

                    if (!course.AdminSetMinHourlyRate.HasValue ||
                        !course.AdminSetMaxHourlyRate.HasValue)
                    {
                        return BadRequest(new
                        {
                            message =
                                $"Hourly rate range is not configured for {course.CourseTitle}."
                        });
                    }

                    decimal minRate =
                        course.AdminSetMinHourlyRate.Value;

                    decimal maxRate =
                        course.AdminSetMaxHourlyRate.Value;

                    // =================================================
                    // 8. Validate Admin Rate Configuration
                    // =================================================

                    if (minRate <= 0 || maxRate <= 0 ||
                        minRate > maxRate)
                    {
                        return BadRequest(new
                        {
                            message =
                                $"Invalid hourly rate configuration for {course.CourseTitle}."
                        });
                    }

                    // =================================================
                    // 9. Validate Tutor Hourly Rate
                    // =================================================

                    if (item.HourlyRate < minRate ||
                        item.HourlyRate > maxRate)
                    {
                        return BadRequest(new
                        {
                            message =
                                $"You can set hourly rate between Rs. {minRate:0.##} and Rs. {maxRate:0.##} for {course.CourseTitle}.",

                            courseId = course.CourseId,
                            courseTitle = course.CourseTitle,
                            minRate,
                            maxRate,
                            enteredRate = item.HourlyRate
                        });
                    }

                    // =================================================
                    // 10. Validate Programming Subject
                    // =================================================

                    int? languageId = null;

                    bool? labCompleted = null;

                    if (item.IsProgrammingSubject)
                    {
                        // Programming language is required
                        if (string.IsNullOrWhiteSpace(
                            item.ProgrammingLanguage))
                        {
                            return BadRequest(new
                            {
                                message =
                                    $"Programming language is required for {course.CourseTitle}."
                            });
                        }

                        string languageName =
                            item.ProgrammingLanguage.Trim();

                        if (languageName.Length > 100)
                        {
                            return BadRequest(new
                            {
                                message =
                                    "Programming language cannot exceed 100 characters."
                            });
                        }

                        // Lab checkbox must have a value
                        if (!item.LabCompleted.HasValue)
                        {
                            return BadRequest(new
                            {
                                message =
                                    $"Please specify whether you completed the lab for {course.CourseTitle}."
                            });
                        }

                        // Find existing programming language
                        var programmingLanguage =
                            await db.ProgrammingLanguages
                                .FirstOrDefaultAsync(l =>
                                    l.LanguageName == languageName &&
                                    l.Status == true);

                        if (programmingLanguage == null)
                        {
                            return BadRequest(new
                            {
                                message =
                                    $"Programming language '{languageName}' does not exist or is inactive. Please select a valid language."
                            });
                        }

                        languageId = programmingLanguage.LanguageId;

                        labCompleted = item.LabCompleted.Value;
                    }

                    // =================================================
                    // 11. Check Existing Tutor Course
                    // =================================================

                    bool exists = await db.TutorCourses
                        .AnyAsync(tc =>
                            tc.TutorId == tutor.TutorId &&
                            tc.CourseId == item.CourseId);

                    if (exists)
                    {
                        skipped++;
                        continue;
                    }

                    // =================================================
                    // 12. Save Tutor Course
                    // =================================================

                    var tutorCourse = new TutorCourse
                    {
                        TutorId = tutor.TutorId,

                        CourseId = item.CourseId,

                        Grade = grade,

                        Institute = institute,

                        // If not a programming subject, save NULL
                        LabCompleted = labCompleted,

                        // Default verification status
                        GradeVerificationStatus = "Pending"
                    };

                    db.TutorCourses.Add(tutorCourse);

                    // =================================================
                    // 13. Save Tutor Hourly Rate
                    // =================================================

                    var tutorCourseRate = new TutorCourseRate
                    {
                        TutorId = tutor.TutorId,

                        CourseId = item.CourseId,

                        HourlyRate = item.HourlyRate,

                        AdminSetMinHourlyRate = minRate,

                        AdminSetMaxHourlyRate = maxRate
                    };

                    db.TutorCourseRates.Add(tutorCourseRate);

                    // =================================================
                    // 14. Save Previous Teacher
                    // =================================================

                    var previousTeachers = new TutorPreviousTeacher
                    {
                        TutorId = tutor.TutorId,

                        TeacherName = previousTeacher,

                        Institute = institute,

                        CourseId = item.CourseId,

                        StudyYear = item.StudyYear,

                        Description = null
                    };

                    db.TutorPreviousTeachers.Add(previousTeachers);

                    // =================================================
                    // 15. Save Programming Language
                    // =================================================

                    if (languageId.HasValue)
                    {
                        var tutorCourseLanguage =
                            new TutorCourseProgrammingLanguage
                            {
                                TutorId = tutor.TutorId,

                                CourseId = item.CourseId,

                                LanguageId = languageId.Value
                            };

                        db.TutorCourseProgrammingLanguages.Add(
                            tutorCourseLanguage);
                    }

                    added++;
                }

                // =====================================================
                // 16. Check If Any Course Was Added
                // =====================================================

                if (added == 0)
                {
                    return BadRequest(new
                    {
                        message =
                            "No new courses were added. The selected courses may already exist.",

                        added,
                        skipped
                    });
                }

                // =====================================================
                // 17. Save All Changes
                // =====================================================

                await db.SaveChangesAsync();

                // =====================================================
                // 18. Commit Transaction
                // =====================================================

                await transaction.CommitAsync();

                // =====================================================
                // 19. Success Response
                // =====================================================

                return Ok(new
                {
                    message = "Courses added successfully.",

                    added,

                    skipped
                });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();

                return StatusCode(500, new
                {
                    message = "Error adding tutor courses.",

                    error =
                        ex.InnerException?.Message ?? ex.Message
                });
            }
        }


        [Authorize]
        [HttpGet("my-courses")]
        public async Task<IActionResult> GetTutorCourses()
        {
            try
            {
                // 1. Get logged-in user ID from JWT token
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);

                if (userIdClaim == null ||
                    !int.TryParse(userIdClaim.Value, out int userId))
                {
                    return Unauthorized(new
                    {
                        message = "Invalid token."
                    });
                }

                // 2. Find tutor
                var tutor = await db.Tutors
                    .FirstOrDefaultAsync(t => t.UserId == userId);

                if (tutor == null)
                {
                    return NotFound(new
                    {
                        message = "Tutor not found."
                    });
                }

                // 3. Get all enrolled courses with related information
                var courses = await (
                    from tc in db.TutorCourses

                    join c in db.Courses
                        on tc.CourseId equals c.CourseId

                    // Left join: Tutor Course Rate
                    join rate in db.TutorCourseRates
                        on new { tc.TutorId, tc.CourseId }
                        equals new { rate.TutorId, rate.CourseId }
                        into rates

                    from rate in rates.DefaultIfEmpty()

                    where tc.TutorId == tutor.TutorId

                    select new
                    {
                        // Course Information
                        course_id = c.CourseId,
                        course_name = c.CourseTitle,

                        // Tutor Course Information
                        grade = tc.Grade,
                        institute = tc.Institute,

                        // Hourly Rate
                        hourly_rate = rate != null
                            ? rate.HourlyRate
                            : 0,

                        // Previous Teacher Information
                        previous_teacher = db.TutorPreviousTeachers
                            .Where(p =>
                                p.TutorId == tc.TutorId &&
                                p.CourseId == tc.CourseId)
                            .Select(p => p.TeacherName)
                            .FirstOrDefault(),

                        previous_teacher_institute = db.TutorPreviousTeachers
                            .Where(p =>
                                p.TutorId == tc.TutorId &&
                                p.CourseId == tc.CourseId)
                            .Select(p => p.Institute)
                            .FirstOrDefault(),

                        study_year = db.TutorPreviousTeachers
                            .Where(p =>
                                p.TutorId == tc.TutorId &&
                                p.CourseId == tc.CourseId)
                            .Select(p => p.StudyYear)
                            .FirstOrDefault(),

                        previous_teacher_description = db.TutorPreviousTeachers
                            .Where(p =>
                                p.TutorId == tc.TutorId &&
                                p.CourseId == tc.CourseId)
                            .Select(p => p.Description)
                            .FirstOrDefault(),

                        // Programming Subject Information
                        is_programming_subject = db.TutorCourseProgrammingLanguages
                            .Any(p =>
                                p.TutorId == tc.TutorId &&
                                p.CourseId == tc.CourseId),

                        programming_language = (
                            from tcp in db.TutorCourseProgrammingLanguages

                            join pl in db.ProgrammingLanguages
                                on tcp.LanguageId equals pl.LanguageId

                            where tcp.TutorId == tc.TutorId
                                  && tcp.CourseId == tc.CourseId

                            select pl.LanguageName
                        ).FirstOrDefault(),

                        // Lab Information
                        lab_completed = tc.LabCompleted,

                        // Grade Verification Information
                        grade_verification_status = tc.GradeVerificationStatus,
                        verified_by = tc.VerifiedBy,
                        verified_date = tc.VerifiedDate,
                        verification_remarks = tc.VerificationRemarks
                    }
                ).ToListAsync();

                // 4. Return response
                return Ok(new
                {
                    message = "Tutor courses retrieved successfully.",
                    total_courses = courses.Count,
                    courses = courses
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "An error occurred while retrieving tutor courses.",
                    error = ex.Message
                });
            }
        }
        //[Authorize]
        //[HttpGet("my-courses")]
        //public async Task<IActionResult> GetTutorCourses()
        //{
        //    var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);

        //    if (userIdClaim == null)
        //        return Unauthorized(new { message = "Invalid token." });

        //    int userId = int.Parse(userIdClaim.Value);

        //    var tutor = await db.Tutors
        //        .FirstOrDefaultAsync(t => t.UserId == userId);

        //    if (tutor == null)
        //        return NotFound(new { message = "Tutor not found." });

        //    var courses = await (
        //        from tc in db.TutorCourses
        //        join c in db.Courses
        //            on tc.CourseId equals c.CourseId
        //        join r in db.TutorCourseRates
        //            on new { tc.TutorId, tc.CourseId }
        //            equals new { r.TutorId, r.CourseId }
        //            into rates
        //        from r in rates.DefaultIfEmpty() // Left Join
        //        where tc.TutorId == tutor.TutorId
        //        select new
        //        {
        //            course_id = c.CourseId,
        //            course_name = c.CourseTitle,
        //            grade = tc.Grade,
        //            hourly_rate = r != null ? r.HourlyRate : 0,
        //            institute = tc.Institute
        //        }
        //    ).ToListAsync();

        //    return Ok(courses);
        //}
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
        [Authorize]
        [HttpPut("complete-class/{requestId}")]
        public async Task<IActionResult> CompleteClass(int requestId)
        {
            try
            {
                // STEP 1: Get logged-in tutor
                var userIdClaim =
                    User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (!int.TryParse(userIdClaim, out int userId))
                {
                    return Unauthorized(new
                    {
                        success = false,
                        message = "Invalid user token."
                    });
                }

                var tutor = await db.Tutors
                    .FirstOrDefaultAsync(t => t.UserId == userId);

                if (tutor == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "Tutor not found."
                    });
                }

                // STEP 2: Find class
                var request = await db.Requests
                    .FirstOrDefaultAsync(r =>
                        r.RequestId == requestId);

                if (request == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "Request not found."
                    });
                }

                // STEP 3: Verify class belongs to logged-in tutor
                if (request.TutorId != tutor.TutorId)
                {
                    return Forbid();
                }

                if (request.Status != "Accepted" &&
                    request.Status != "Complete")
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Only accepted classes can be marked completed."
                    });
                }

                // STEP 4: Mark this individual class completed
                request.Status = "Complete";

                RequestCoursePlan? plan = null;

                int completedClasses = 0;

                int totalClasses = 0;

                bool courseCompleted = false;

                // STEP 5: Find course plan
                if (request.RequestGroupId.HasValue)
                {
                    plan = await db.RequestCoursePlans
                        .FirstOrDefaultAsync(p =>
                            p.RequestGroupId ==
                            request.RequestGroupId.Value);
                }

                // STEP 6: Check all classes of this course
                if (plan != null)
                {
                    totalClasses = plan.TotalClasses;

                    var courseRequests = await db.Requests
                        .Where(r =>
                            r.RequestGroupId == request.RequestGroupId &&

                            r.StudentId == request.StudentId &&

                            r.TutorId == tutor.TutorId &&

                            r.CourseId == request.CourseId &&

                            r.Status != "Cancelled" &&

                            r.Status != "Rejected")
                        .ToListAsync();

                    completedClasses = courseRequests
                        .Count(r => r.Status == "Complete");

                    // STEP 7: Complete the entire course
                    if (courseRequests.Count >= plan.TotalClasses &&
                        completedClasses >= plan.TotalClasses)
                    {
                        plan.Status = "Completed";

                        plan.TargetEndDate =
                            request.ClassDate;

                        courseCompleted = true;
                    }
                }

                // STEP 8: Save changes
                await db.SaveChangesAsync();

                // STEP 9: Return response
                return Ok(new
                {
                    success = true,

                    message = courseCompleted
                        ? "Class completed. All required course classes are now completed."
                        : "Class marked as completed successfully.",

                    request_id = request.RequestId,

                    class_status = request.Status,

                    plan_id = plan?.PlanId,

                    plan_status = plan?.Status,

                    completed_classes = completedClasses,

                    total_classes = totalClasses,

                    remaining_classes = Math.Max(
                        0,
                        totalClasses - completedClasses
                    ),

                    course_completed = courseCompleted,

                    tutor_available = courseCompleted,

                    student_available = courseCompleted
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,

                    message = "Error completing class.",

                    error = ex.InnerException?.Message ?? ex.Message
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


        [Authorize]
        [HttpPost("mark-course-done")]
        public async Task<IActionResult> MarkCourseDone([FromBody] MarkCourseDoneRequest request)
        {
            try
            {
                // STEP 1: Logged-in tutor
                var userIdClaim =
                    User.FindFirst(ClaimTypes.NameIdentifier);

                if (userIdClaim == null)
                {
                    return Unauthorized(new
                    {
                        message = "Invalid token."
                    });
                }

                int userId = int.Parse(userIdClaim.Value);

                var tutor = await db.Tutors
                    .FirstOrDefaultAsync(t => t.UserId == userId);

                if (tutor == null)
                {
                    return NotFound(new
                    {
                        message = "Tutor not found."
                    });
                }

                // STEP 2: Verify student
                var student = await db.Students
                    .FirstOrDefaultAsync(s =>
                        s.StudentId == request.StudentId);

                if (student == null)
                {
                    return BadRequest(new
                    {
                        message = "Student not found."
                    });
                }

                // STEP 3: Verify tutor course
                var tutorCourse = await db.TutorCourses
                    .FirstOrDefaultAsync(tc =>
                        tc.TutorId == tutor.TutorId &&
                        tc.CourseId == request.CourseId);

                if (tutorCourse == null)
                {
                    return BadRequest(new
                    {
                        message = "Course not assigned to tutor."
                    });
                }

                // STEP 4: Get the approved course plan
                var plan = await db.RequestCoursePlans
                    .Include(p => p.RequestGroup)
                    .Where(p =>
                        p.RequestGroup.StudentId == request.StudentId &&

                        p.RequestGroup.CourseId == request.CourseId &&

                        p.ApprovedByTutorId == tutor.TutorId)
                    .OrderByDescending(p => p.CreatedDate)
                    .FirstOrDefaultAsync();

                if (plan != null)
                {
                    // Course must already be completed through
                    // individual class completion.
                    if (plan.Status != "Completed")
                    {
                        return BadRequest(new
                        {
                            message = "You cannot mark this course done yet. All required classes must first be completed.",

                            required_classes = plan.TotalClasses,

                            plan_status = plan.Status
                        });
                    }
                }

                // STEP 5: Get actual classes
                var classesQuery = db.Requests.Where(r =>
                    r.StudentId == request.StudentId &&

                    r.TutorId == tutor.TutorId &&

                    r.CourseId == request.CourseId);

                if (plan != null)
                {
                    classesQuery = classesQuery.Where(r =>
                        r.RequestGroupId == plan.RequestGroupId);
                }

                var classes = await classesQuery.ToListAsync();

                if (!classes.Any())
                {
                    return BadRequest(new
                    {
                        message = "No classes found."
                    });
                }

                // Never automatically complete remaining classes.
                int completedCount = classes.Count(c =>
                    c.Status == "Complete");

                int requiredCount = plan?.TotalClasses ?? classes.Count;

                if (completedCount < requiredCount)
                {
                    return BadRequest(new
                    {
                        message = "All required classes must be completed before finalizing the course.",

                        completed_classes = completedCount,

                        required_classes = requiredCount,

                        remaining_classes = requiredCount - completedCount
                    });
                }

                if (classes.Any(c => c.Status != "Complete"))
                {
                    return BadRequest(new
                    {
                        message = "Some scheduled classes are not completed."
                    });
                }

                // STEP 6: Get hourly rate
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

                // STEP 7: Calculate total hours
                double totalHours = 0;

                foreach (var cls in classes)
                {
                    if (string.IsNullOrWhiteSpace(cls.Time))
                        continue;

                    var parts = cls.Time.Split('-');

                    if (parts.Length != 2)
                        continue;

                    if (DateTime.TryParse(
                        parts[0].Trim(),
                        out DateTime start) &&

                        DateTime.TryParse(
                            parts[1].Trim(),
                            out DateTime end))
                    {
                        totalHours +=
                            (end - start).TotalHours;
                    }
                }

                decimal totalFee =
                    (decimal)totalHours * rate.HourlyRate;

                // STEP 8: Save tutor course completion
                tutorCourse.IsCompleted = true;

                tutorCourse.CompletedDate = DateTime.Now;

                if (!string.IsNullOrWhiteSpace(request.Grade))
                {
                    tutorCourse.Grade =
                        request.Grade.Trim().ToUpper();
                }

                // STEP 9: Create or update student course fee
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
                    success = true,

                    message = "Course finalized successfully.",

                    student_id = request.StudentId,

                    tutor_id = tutor.TutorId,

                    course_id = request.CourseId,

                    completed_classes = completedCount,

                    total_classes = requiredCount,

                    totalHours = totalHours,

                    hourlyRate = rate.HourlyRate,

                    totalFee = totalFee,

                    course_status = "Completed"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Error finalizing course.",

                    error = ex.InnerException?.Message ?? ex.Message
                });
            }
        }

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

        // =========================================================
        // TUTOR PAYMENT LIST
        // =========================================================
        [Authorize]
        [HttpGet("payment-list")]
        public IActionResult PaymentList()
        {
            try
            {
                // =====================================================
                // 1. GET TUTOR USER ID FROM JWT
                // =====================================================

                int userId = int.Parse(
                    User.FindFirst(ClaimTypes.NameIdentifier).Value
                );


                // =====================================================
                // 2. FIND TUTOR
                // =====================================================

                var tutor = db.Tutors
                    .FirstOrDefault(x => x.UserId == userId);

                if (tutor == null)
                {
                    return NotFound(new
                    {
                        message = "Tutor not found"
                    });
                }


                // =====================================================
                // 3. GET PAYMENT LIST
                //
                // IMPORTANT:
                // Received payments are NOT shown in the list.
                // =====================================================

                var payments = (
                    from payment in db.Payments

                    join fee in db.StudentCourseFees
                        on payment.FeeId equals fee.FeeId

                    join student in db.Students
                        on fee.StudentId equals student.StudentId

                    join user in db.Users
                        on student.UserId equals user.UserId

                    join course in db.Courses
                        on fee.CourseId equals course.CourseId

                    where fee.TutorId == tutor.TutorId
                          && payment.TutorStatus != "Received"

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
                    }
                ).ToList();


                // =====================================================
                // 4. CALCULATE TOTAL COLLECTED
                //
                // Only payments whose TutorStatus is "Received"
                // are counted in TotalCollected.
                // =====================================================

                decimal totalCollected = (
                    from payment in db.Payments

                    join fee in db.StudentCourseFees
                        on payment.FeeId equals fee.FeeId

                    where fee.TutorId == tutor.TutorId
                          && payment.TutorStatus == "Received"

                    select (decimal?)payment.Amount
                ).Sum() ?? 0;


                // =====================================================
                // 5. RETURN BOTH
                // =====================================================

                return Ok(new
                {
                    payments = payments,
                    totalCollected = totalCollected
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Error while loading payment list",
                    error = ex.Message
                });
            }
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

        [Authorize]
        [HttpGet("tutor-course-content/{studentId}/{courseId}")]
        public async Task<IActionResult> GetTutorCourseContent( int studentId, int courseId)
        {
            try
            {
                // =====================================================
                // STEP 1: GET LOGGED-IN USER ID
                // =====================================================

                var userIdClaim =
                    User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrEmpty(userIdClaim))
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
                        message = "Invalid user ID."
                    });
                }


                // =====================================================
                // STEP 2: FIND TUTOR
                // =====================================================

                var tutor = await db.Tutors
                    .FirstOrDefaultAsync(t => t.UserId == userId);

                if (tutor == null)
                {
                    return NotFound(new
                    {
                        message = "Tutor not found."
                    });
                }


                // =====================================================
                // STEP 3: VERIFY TUTOR + STUDENT + COURSE
                // =====================================================

                bool hasRelationship =
                    await db.Requests.AnyAsync(r =>
                        r.StudentId == studentId &&
                        r.TutorId == tutor.TutorId &&
                        r.CourseId == courseId
                    );

                if (!hasRelationship)
                {
                    return Forbid();
                }


                // =====================================================
                // STEP 4: GET COURSE CONTENT
                // =====================================================

                var content =
                    await db.StudentCourseContents
                    .Where(c =>
                        c.StudentId == studentId &&
                        c.CourseId == courseId)
                    .OrderByDescending(c => c.UploadedDate)
                    .Select(c => new
                    {
                        content_id = c.ContentId,

                        student_id = c.StudentId,

                        course_id = c.CourseId,

                        course_title =
                            c.Course.CourseTitle,

                        title = c.Title,

                        description = c.Description,

                        file_name = c.FileName,

                        file_path = c.FilePath,

                        uploaded_date = c.UploadedDate
                    })
                    .ToListAsync();


                // =====================================================
                // STEP 5: CREATE CORRECT FILE URL
                //
                // IMPORTANT:
                //
                // API:
                // http://192.168.137.1:5000/api
                //
                // Static file:
                // http://192.168.137.1:5000/CourseContent/file.jpg
                //
                // NOT:
                // http://192.168.137.1:5000/api/CourseContent/file.jpg
                // =====================================================

                var result = content.Select(c =>
                {
                    string fileUrl = null;

                    if (!string.IsNullOrWhiteSpace(c.file_path))
                    {
                        string cleanPath = c.file_path
                            .Replace("\\", "/")
                            .TrimStart('/');

                        // -------------------------------------------------
                        // If database contains:
                        //
                        // CourseContent/Student_2_Course_1.jpg
                        //
                        // OR:
                        //
                        // /CourseContent/Student_2_Course_1.jpg
                        // -------------------------------------------------

                        fileUrl =
                            $"{Request.Scheme}://{Request.Host}/{cleanPath}";
                    }
                    else if (!string.IsNullOrWhiteSpace(c.file_name))
                    {
                        // -------------------------------------------------
                        // Fallback if FilePath is empty
                        // -------------------------------------------------

                        string cleanFileName =
                            Uri.EscapeDataString(c.file_name);

                        fileUrl =
                            $"{Request.Scheme}://{Request.Host}/CourseContent/{cleanFileName}";
                    }

                    return new
                    {
                        content_id = c.content_id,

                        student_id = c.student_id,

                        course_id = c.course_id,

                        course_title = c.course_title,

                        title = c.title,

                        description = c.description,

                        file_name = c.file_name,

                        file_path = c.file_path,

                        // NEW:
                        // Correct URL for opening file
                        file_url = fileUrl,

                        uploaded_date = c.uploaded_date
                    };
                }).ToList();


                // =====================================================
                // STEP 6: RETURN RESPONSE
                // =====================================================

                return Ok(new
                {
                    status = "Success",

                    message =
                        result.Count > 0
                            ? "Course content found."
                            : "No course content uploaded yet.",

                    count = result.Count,

                    data = result
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message =
                        "Error: " +
                        (ex.InnerException?.Message ?? ex.Message)
                });
            }
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

        private string NormalizePlanDay(string? day)
        {
            if (string.IsNullOrWhiteSpace(day))
                return "";

            string value = day.Trim().ToLower();

            return value switch
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

                "saturday" => "sat",
                "sat" => "sat",

                "sunday" => "sun",
                "sun" => "sun",

                _ => value.Length >= 3 ? value.Substring(0, 3) : value
            };
        }

        private string NormalizePlanTime(string? time)
        {
            if (string.IsNullOrWhiteSpace(time))
                return "";

            return time
                .Replace(" ", "")
                .ToLower();
        }

        private DateOnly GetPlanDateForDay(DateOnly fromDate, string day)
        {
            string normalizedDay = NormalizePlanDay(day);

            DayOfWeek targetDay = normalizedDay switch
            {
                "mon" => DayOfWeek.Monday,
                "tue" => DayOfWeek.Tuesday,
                "wed" => DayOfWeek.Wednesday,
                "thu" => DayOfWeek.Thursday,
                "fri" => DayOfWeek.Friday,
                "sat" => DayOfWeek.Saturday,
                "sun" => DayOfWeek.Sunday,

                _ => throw new Exception("Invalid schedule day: " + day)
            };

            int daysToAdd =
                ((int)targetDay - (int)fromDate.DayOfWeek + 7) % 7;

            return fromDate.AddDays(daysToAdd);
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
        public string Institute { get; set; }
    }

    //to get all course
    public class CourseDto 
    {
        public int course_id { get; set; } 
        public string course_name { get; set; } 
    }

    public class AddTutorCoursesDto
    {
        public List<AddTutorCourseItemDto> Courses { get; set; }
            = new List<AddTutorCourseItemDto>();
    }

    public class AddTutorCourseItemDto
    {
        public int CourseId { get; set; }

        public string Grade { get; set; }

        public string Institute { get; set; }

        public decimal HourlyRate { get; set; }

        // Previous teacher information
        public string PreviousTeacher { get; set; }

        // Programming subject checkbox
        public bool IsProgrammingSubject { get; set; }

        // Required only when IsProgrammingSubject = true
        public string ProgrammingLanguage { get; set; }

        // Required only when IsProgrammingSubject = true
        public bool? LabCompleted { get; set; }

        // Optional
        public int? StudyYear { get; set; }
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
