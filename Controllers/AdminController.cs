using HouseofTutorAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

namespace HouseofTutorAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AdminController : ControllerBase
    {
        private readonly HouseofTutorContext db;

        public AdminController(HouseofTutorContext _db)
        {
            db = _db;
        }

        [HttpGet("dashboard")]
        public async Task<IActionResult> GetAdminDashboard()
        {
            try
            {
                var totalTutors =
                    await db.Tutors.CountAsync();

                var totalStudents =
                    await db.Students.CountAsync();

                var totalClasses =
                    await db.Requests
                        .Where(r => r.Status == "Accepted")
                        .CountAsync();

                var totalSubjects =
                    await db.Courses.CountAsync();

                return Ok(new
                {
                    totalTutors = totalTutors,
                    totalStudents = totalStudents,
                    totalClasses = totalClasses,
                    totalSubjects = totalSubjects
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Something went wrong",
                    error = ex.Message
                });
            }
        }

        //[HttpGet("all-tutors")]
        //public async Task<IActionResult> GetAllTutors()
        //{
        //    try
        //    {
        //        var tutors = await db.Tutors
        //            .Include(t => t.User)
        //            .Include(t => t.TutorCourses)
        //                .ThenInclude(tc => tc.Course)
        //            .Select(t => new
        //            {
        //                tutor_id = t.TutorId,
        //                full_name = t.User.FullName,
        //                subjects = t.TutorCourses
        //                    .Select(tc => tc.Course.CourseTitle)
        //                    .ToList(),
        //                avg_rating = db.Feedbacks
        //                    .Where(f => f.TutorId == t.TutorId)
        //                    .Select(f => (double?)f.Rating)
        //                    .Average() ?? 0,
        //                total_reviews = db.Feedbacks
        //                    .Count(f => f.TutorId == t.TutorId),
        //                status = t.Status
        //            })
        //            .ToListAsync();

        //        return Ok(tutors);
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(500, new
        //        {
        //            message = "Error loading tutors",
        //            error = ex.Message
        //        });
        //    }
        //}
        
        [HttpGet("all-tutors")]// Get All Pending Tutor
        public async Task<IActionResult> GetAllTutors()
        {
            try
            {
                var tutors = await db.Tutors
                    .Where(t => t.Status == "Pending")
                    .Include(t => t.User)
                    .Select(t => new
                    {
                        tutorId = t.TutorId,
                        userId = t.UserId,

                        // From Users table
                        fullName = t.User.FullName,
                        email = t.User.Email,
                        phone = t.User.Phone,
                        cnic = t.User.Cnic,

                        // From Tutor table
                        qualification = t.Qualification,
                        experience = t.Experience,
                        location = t.Location,
                        radius = t.Radius,
                        latitude = t.Latitude,
                        longitude = t.Longitude,

                        status = t.Status
                    })
                    .ToListAsync();

                return Ok(tutors);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Error loading approved tutors",
                    error = ex.InnerException?.Message ?? ex.Message
                });
            }
        }

        //[HttpGet("all-tutors")]
        //public async Task<IActionResult> GetAllTutors()
        //{
        //    try
        //    {
        //        var tutors = await db.Tutors
        //            .Include(t => t.User)
        //            .Include(t => t.TutorCourses)
        //                .ThenInclude(tc => tc.Course)
        //            .Select(t => new
        //            {
        //                id = t.TutorId,

        //                fullName = t.User.FullName,

        //                //profileImage = t.User.ProfileImage,

        //                subjects = t.TutorCourses
        //                    .Select(tc => tc.Course.CourseTitle)
        //                    .ToList(),

        //                rating = db.Feedbacks
        //                    .Where(f => f.TutorId == t.TutorId)
        //                    .Select(f => (double?)f.Rating)
        //                    .Average() ?? 0,

        //                totalReviews = db.Feedbacks
        //                    .Count(f => f.TutorId == t.TutorId),

        //                status = t.Status ?? "Pending"
        //            })
        //            .ToListAsync();

        //        return Ok(tutors);
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(500, new
        //        {
        //            message = "Error loading tutors",
        //            error = ex.Message
        //        });
        //    }
        //}

        //To View and See detials of tutor
        [HttpGet("tutor/{id}")] //View Tutor Details 
        public async Task<IActionResult> GetTutor(int id)
        {
            var tutor = await db.Tutors
                .Include(x => x.User)
                .Include(x => x.TutorCourses)
                    .ThenInclude(x => x.Course)
                .FirstOrDefaultAsync(x => x.TutorId == id);

            if (tutor == null)
                return NotFound(new { message = "Tutor not found." });

            var result = new
            {
                id = tutor.TutorId,

                fullName = tutor.User.FullName,
                email = tutor.User.Email,
                phone = tutor.User.Phone,
                cnic = tutor.User.Cnic,

                qualification = tutor.Qualification,
                experience = tutor.Experience,
                location = tutor.Location,
                radius = tutor.Radius,

                status = tutor.Status,

                subjects = tutor.TutorCourses
                    .Select(x => x.Course.CourseTitle)
                    .ToList(),

                rating = db.Feedbacks
                    .Where(x => x.TutorId == tutor.TutorId)
                    .Average(x => (double?)x.Rating) ?? 0,

                totalReviews = db.Feedbacks
                    .Count(x => x.TutorId == tutor.TutorId)
            };

            return Ok(result);
        }

        //Approve Tutor
        [HttpPut("approve-tutor/{id}")]
        public async Task<IActionResult> ApproveTutor(int id)
        {
            var tutor = await db.Tutors.FindAsync(id);

            if (tutor == null)
                return NotFound(new { message = "Tutor not found." });

            tutor.Status = "Approved";

            await db.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                message = "Tutor approved successfully."
            });
        }

        //Reject Tutor
        [HttpPut("reject-tutor/{id}")]
        public async Task<IActionResult> RejectTutor(int id)
        {
            var tutor = await db.Tutors.FindAsync(id);

            if (tutor == null)
                return NotFound(new { message = "Tutor not found." });

            tutor.Status = "Rejected";

            await db.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                message = "Tutor rejected successfully."
            });
        }

        //Block Tutor
        [HttpPut("block-tutor/{id}")]
        public async Task<IActionResult> BlockTutor(int id)
        {
            var tutor = await db.Tutors.FindAsync(id);

            if (tutor == null)
                return NotFound(new { message = "Tutor not found." });

            tutor.Status = "Blocked";

            await db.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                message = "Tutor blocked successfully."
            });
        }

        //get only approved Tutor's
        [HttpGet("approved-tutors")]
        public async Task<IActionResult> GetApprovedTutors()
        {
            try
            {
                var tutors = await db.Tutors
                    .Where(t => t.Status == "Approved")
                    .Include(t => t.User)
                    .Include(t => t.TutorCourses)
                        .ThenInclude(tc => tc.Course)
                    .Select(t => new
                    {
                        id = t.TutorId,

                        fullName = t.User.FullName,

                        // profileImage = t.User.ProfileImage,

                        subjects = t.TutorCourses
                            .Select(tc => tc.Course.CourseTitle)
                            .ToList(),

                        rating = db.Feedbacks
                            .Where(f => f.TutorId == t.TutorId)
                            .Select(f => (double?)f.Rating)
                            .Average() ?? 0,

                        totalReviews = db.Feedbacks
                            .Count(f => f.TutorId == t.TutorId),

                        status = t.Status
                    })
                    .ToListAsync();

                return Ok(tutors);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Error loading approved tutors",
                    error = ex.InnerException?.Message ?? ex.Message
                });
            }
        }

        //Get Block Tutor
        [HttpGet("blocked-users")]
        public async Task<IActionResult> GetBlockedUsers()
        {
            var tutors = await db.Tutors
                .Include(x => x.User)
                .Include(x => x.TutorCourses)
                    .ThenInclude(x => x.Course)
                .Where(x => x.Status == "Blocked")
                .Select(x => new
                {
                    id = x.TutorId,
                    fullName = x.User.FullName,
                    role = "Tutor",
                    subjects = string.Join(", ",
                        x.TutorCourses.Select(c => c.Course.CourseTitle))
                })
                .ToListAsync();

            return Ok(tutors);
        }

        //Restore blocked Tutor
        [HttpPut("restore-user/{id}")]
        public async Task<IActionResult> RestoreUser(int id)
        {
            var tutor = await db.Tutors.FindAsync(id);

            if (tutor == null)
            {
                return NotFound(new
                {
                    message = "Tutor not found."
                });
            }

            tutor.Status = "Approved";

            await db.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                message = "Tutor restored successfully."
            });
        }

        [HttpGet("all-students")]
        public async Task<IActionResult> GetAllStudents()
        {
            try
            {
                var students = await db.Students
                    .Include(s => s.User)
                    .Select(s => new
                    {
                        id = s.StudentId,

                        userId = s.UserId,

                        fullName = s.User.FullName,

                        email = s.User.Email,

                        phone = s.User.Phone,

                        cnic = s.User.Cnic,

                        role = s.User.Role,

                        location = s.Location,

                        latitude = s.Latitude,

                        longitude = s.Longitude
                    })
                    .ToListAsync();

                return Ok(students);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Error loading students",
                    error = ex.Message
                });
            }
        }

        [HttpPut("block-student/{studentId}")]
        public async Task<IActionResult> BlockStudent(int studentId)
        {
            try
            {
                var student = await db.Students
                    .FirstOrDefaultAsync(s => s.StudentId == studentId);

                if (student == null)
                {
                    return NotFound(new
                    {
                        message = "Student not found"
                    });
                }

                student.Status = "Blocked";

                await db.SaveChangesAsync();

                return Ok(new
                {
                    message = "Student blocked successfully",
                    studentId = student.StudentId,
                    status = student.Status
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Error blocking student",
                    error = ex.InnerException?.Message ?? ex.Message
                });
            }
        }

        [HttpPut("reject-student/{studentId}")]
        public async Task<IActionResult> RejectStudent(int studentId)
        {
            try
            {
                var student = await db.Students
                    .FirstOrDefaultAsync(s => s.StudentId == studentId);

                if (student == null)
                {
                    return NotFound(new
                    {
                        message = "Student not found"
                    });
                }

                student.Status = "Rejected";

                await db.SaveChangesAsync();

                return Ok(new
                {
                    message = "Student rejected successfully",
                    studentId = student.StudentId,
                    status = student.Status
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Error rejecting student",
                    error = ex.InnerException?.Message ?? ex.Message
                });
            }
        }

        [HttpGet("blocked-students")]
        public async Task<IActionResult> GetBlockedStudents()
        {
            try
            {
                var blockedStudents = await db.Students
                    .Where(s => s.Status == "Blocked")
                    .Include(s => s.User)
                    .Select(s => new
                    {
                        studentId = s.StudentId,
                        userId = s.UserId,

                        fullName = s.User.FullName,
                        email = s.User.Email,
                        phone = s.User.Phone,
                        cnic = s.User.Cnic,

                        location = s.Location,
                        fatherCnic = s.FatherCnic,

                        status = s.Status
                    })
                    .ToListAsync();

                return Ok(blockedStudents);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Error getting blocked students",
                    error = ex.InnerException?.Message ?? ex.Message
                });
            }
        }

        [HttpPut("restore-student/{studentId}")]
        public async Task<IActionResult> RestoreStudent(int studentId)
        {
            try
            {
                var student = await db.Students
                    .FirstOrDefaultAsync(s => s.StudentId == studentId);

                if (student == null)
                {
                    return NotFound(new
                    {
                        message = "Student not found"
                    });
                }

                // Make sure only blocked students are restored
                if (student.Status != "Blocked")
                {
                    return BadRequest(new
                    {
                        message = "Student is not currently blocked"
                    });
                }

                student.Status = "Active";

                await db.SaveChangesAsync();

                return Ok(new
                {
                    message = "Student restored successfully",
                    studentId = student.StudentId,
                    status = student.Status
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Error restoring student",
                    error = ex.InnerException?.Message ?? ex.Message
                });
            }
        }


        //[HttpGet("all-classes")]
        //public async Task<IActionResult> GetAllClasses()
        //{
        //    try
        //    {
        //        var classes = await db.Requests
        //            .Include(r => r.Student)
        //                .ThenInclude(s => s.User)
        //            .Include(r => r.Tutor)
        //                .ThenInclude(t => t.User)
        //            .Include(r => r.Course)
        //            .Select(r => new
        //            {
        //                request_id = r.RequestId,

        //                student_name = r.Student.User.FullName,
        //                tutor_name = r.Tutor.User.FullName,
        //                course_name = r.Course.CourseTitle,
        //                date = r.RequestDate.HasValue
        //                    ? r.RequestDate.Value.ToString("yyyy-MM-dd")
        //                    : "",
        //                time = r.RequestDate.HasValue
        //                    ? r.RequestDate.Value.ToString("hh:mm tt")
        //                    : "",
        //                status = r.Status
        //            })
        //            .OrderByDescending(r => r.request_id)
        //            .ToListAsync();

        //        return Ok(classes);
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(500, new
        //        {
        //            message = "Error loading classes",
        //            error = ex.Message
        //        });
        //    }
        //}


        [HttpGet("all-classes")]
        public async Task<IActionResult> GetAllClasses()
        {
            try
            {
                var classes = await db.Requests
                    .Include(r => r.Student)
                        .ThenInclude(s => s.User)
                    .Include(r => r.Tutor)
                        .ThenInclude(t => t.User)
                    .Include(r => r.Course)
                    .Select(r => new
                    {
                        id = r.RequestId,

                        studentName = r.Student.User.FullName,

                        tutorName = r.Tutor.User.FullName,

                        subjectName = r.Course.CourseTitle,

                        classTime = r.RequestDate,

                        status = r.Status
                    })
                    .OrderByDescending(x => x.id)
                    .ToListAsync();

                return Ok(classes);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Error loading classes",
                    error = ex.InnerException?.Message ?? ex.Message
                });
            }
        }

        //[HttpGet("all-feedback")]
        //public async Task<IActionResult> GetAllFeedback()
        //{
        //    try
        //    {
        //        var feedbacks = await db.Feedbacks
        //            .Include(f => f.Student)
        //                .ThenInclude(s => s.User)
        //            .Include(f => f.Tutor)
        //                .ThenInclude(t => t.User)
        //            .Select(f => new
        //            {
        //                feedback_id = f.FeedbackId,

        //                student_name = f.Student.User.FullName,
        //                tutor_name = f.Tutor.User.FullName,

        //                rating = f.Rating ?? 0,
        //                comment = f.Comment ?? "",
        //                date = f.FeedbackDate.HasValue
        //                    ? f.FeedbackDate.Value.ToString("yyyy-MM-dd")
        //                    : "",

        //                time = f.FeedbackDate.HasValue
        //                    ? f.FeedbackDate.Value.ToString("hh:mm tt")
        //                    : ""
        //            })
        //            .OrderByDescending(f => f.feedback_id)
        //            .ToListAsync();

        //        return Ok(feedbacks);
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(500, new
        //        {
        //            message = "Error loading feedback",
        //            error = ex.Message
        //        });
        //    }
        //}

        [HttpGet("all-feedback")]
        public async Task<IActionResult> GetAllFeedback()
        {
            try
            {
                var feedbacks = await db.Feedbacks
                    .Include(f => f.Student)
                        .ThenInclude(s => s.User)
                    .Include(f => f.Tutor)
                        .ThenInclude(t => t.User)
                    .Include(f => f.Course)
                    .OrderByDescending(f => f.FeedbackId)
                    .Select(f => new
                    {
                        id = f.FeedbackId,

                        studentName = f.Student != null && f.Student.User != null
                            ? f.Student.User.FullName
                            : "",

                        tutorName = f.Tutor != null && f.Tutor.User != null
                            ? f.Tutor.User.FullName
                            : "",

                        courseName = f.Course != null
                            ? f.Course.CourseTitle
                            : "",

                        feedbackText = f.Comment ?? "",

                        rating = f.Rating ?? 0,

                        date = f.FeedbackDate.HasValue
                            ? f.FeedbackDate.Value.ToString("yyyy-MM-dd")
                            : "",

                        time = f.FeedbackDate.HasValue
                            ? f.FeedbackDate.Value.ToString("hh:mm tt")
                            : "",

                        feedbackBy = f.FeedbackBy ?? ""
                    })
                    .ToListAsync();

                return Ok(feedbacks);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Failed to load feedback.",
                    error = ex.InnerException?.Message ?? ex.Message
                });
            }
        }

        //[HttpDelete("delete-feedback/{id}")]
        //public async Task<IActionResult> DeleteFeedback(int id)
        //{
        //    var feedback = await db.Feedbacks.FindAsync(id);

        //    if (feedback == null)
        //        return NotFound(new { message = "Feedback not found" });

        //    db.Feedbacks.Remove(feedback);
        //    await db.SaveChangesAsync();

        //    return Ok(new { message = "Feedback deleted successfully" });
        //}

        [HttpDelete("delete-feedback/{id}")]
        public async Task<IActionResult> DeleteFeedback(int id)
        {
            try
            {
                var feedback = await db.Feedbacks
                    .FirstOrDefaultAsync(f => f.FeedbackId == id);

                if (feedback == null)
                {
                    return NotFound(new
                    {
                        message = "Feedback not found."
                    });
                }

                db.Feedbacks.Remove(feedback);

                await db.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    message = "Feedback deleted successfully."
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Unable to delete feedback.",
                    error = ex.InnerException?.Message ?? ex.Message
                });
            }
        }

        [HttpGet("all-subjects")]
        public async Task<IActionResult> GetAllSubjects()
        {
            try
            {
                var subjects = await db.Courses
                    .Select(c => new
                    {
                        id = c.CourseId,
                        courseTitle = c.CourseTitle
                    })
                    .ToListAsync();

                return Ok(subjects);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Error loading subjects",
                    error = ex.Message
                });
            }
        }

        //[HttpPost("add-subject")]
        //public async Task<IActionResult> AddSubject([FromBody] AddSubjectDTO dto)
        //{
        //    try
        //    {
        //        if (dto == null ||
        //            string.IsNullOrWhiteSpace(dto.CourseTitle))
        //        {
        //            return BadRequest(new
        //            {
        //                message = "Course title is required"
        //            });
        //        }

        //        var course = new Course
        //        {
        //            CourseTitle = dto.CourseTitle.Trim()
        //        };

        //        db.Courses.Add(course);

        //        await db.SaveChangesAsync();

        //        return Ok(new
        //        {
        //            message = "Subject added successfully"
        //        });
        //    }
        //    catch (Exception ex)
        //    {
        //        return StatusCode(500, new
        //        {
        //            message = "Error adding subject",
        //            error = ex.Message
        //        });
        //    }
        //}
        
        [HttpPost("add-subject")]
        public async Task<IActionResult> AddSubject([FromBody] AddSubjectDTO dto)
        {
            try
            {
                // Validate request
                if (dto == null || string.IsNullOrWhiteSpace(dto.CourseTitle))
                {
                    return BadRequest(new
                    {
                        message = "Course title is required"
                    });
                }

                // Validate Min Rate
                if (!dto.MinRate.HasValue)
                {
                    return BadRequest(new
                    {
                        message = "Minimum rate is required"
                    });
                }

                // Validate Max Rate
                if (!dto.MaxRate.HasValue)
                {
                    return BadRequest(new
                    {
                        message = "Maximum rate is required"
                    });
                }

                // Validate rates
                if (dto.MinRate.Value < 0 || dto.MaxRate.Value < 0)
                {
                    return BadRequest(new
                    {
                        message = "Rates cannot be negative"
                    });
                }

                if (dto.MinRate.Value > dto.MaxRate.Value)
                {
                    return BadRequest(new
                    {
                        message = "Minimum rate cannot be greater than maximum rate"
                    });
                }

                // Check if course already exists
                var existingCourse = await db.Courses
                    .FirstOrDefaultAsync(c =>
                        c.CourseTitle.ToLower() == dto.CourseTitle.Trim().ToLower());

                if (existingCourse != null)
                {
                    return BadRequest(new
                    {
                        message = "This course already exists"
                    });
                }

                // Create Course
                var course = new Course
                {
                    CourseTitle = dto.CourseTitle.Trim()
                };

                db.Courses.Add(course);

                // Save course first so CourseId is generated
                await db.SaveChangesAsync();

                // Create Tutor Course Rate record
                var tutorCourseRate = new TutorCourseRate
                {
                    CourseId = course.CourseId,

                    AdminSetMinHourlyRate = dto.MinRate.Value,
                    AdminSetMaxHourlyRate = dto.MaxRate.Value
                };

                db.TutorCourseRates.Add(tutorCourseRate);

                // Save Tutor_Course_Rate
                await db.SaveChangesAsync();

                return Ok(new
                {
                    message = "Subject added successfully",
                    courseId = course.CourseId,
                    courseTitle = course.CourseTitle,
                    minRate = tutorCourseRate.AdminSetMinHourlyRate,
                    maxRate = tutorCourseRate.AdminSetMaxHourlyRate
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Error adding subject",
                    error = ex.InnerException?.Message ?? ex.Message
                });
            }
        }

        [HttpDelete("delete-subject/{id}")]
        public async Task<IActionResult> DeleteSubject(int id)
        {
            try
            {
                var subject = await db.Courses
                    .FirstOrDefaultAsync(c =>
                        c.CourseId == id);

                if (subject == null)
                {
                    return NotFound(new
                    {
                        message = "Subject not found"
                    });
                }

                db.Courses.Remove(subject);

                await db.SaveChangesAsync();

                return Ok(new
                {
                    message = "Subject deleted successfully"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Error deleting subject",
                    error = ex.Message
                });
            }
        }

        [Authorize]
        [HttpGet("all-tutors-courses")]
        public async Task<IActionResult> GetAllTutorsCourses()
        {
            try
            {
                var tutors = await db.Tutors
                    .Include(t => t.User)
                    .ToListAsync();

                var result = new List<object>();

                foreach (var tutor in tutors)
                {
                    var courses = await
                    (
                        from tc in db.TutorCourses
                        join c in db.Courses
                            on tc.CourseId equals c.CourseId
                        join r in db.TutorCourseRates
                            on new { tc.TutorId, tc.CourseId }
                            equals new { r.TutorId, r.CourseId }
                            into rateJoin
                        from rate in rateJoin.DefaultIfEmpty()

                        where tc.TutorId == tutor.TutorId

                        select new
                        {
                            tutor_id = tc.TutorId,
                            course_id = c.CourseId,
                            course_name = c.CourseTitle,
                            grade = tc.Grade,
                            hourly_rate = rate != null ? rate.HourlyRate : 0,
                            admin_set_min_hourly_rate = rate != null
                             ? rate.AdminSetMinHourlyRate
                             : null,
                            admin_set_max_hourly_rate = rate != null
                             ? rate.AdminSetMaxHourlyRate
                             : null,
                            is_completed = tc.IsCompleted,
                            completed_date = tc.CompletedDate
                        }
                    ).ToListAsync();

                    result.Add(new
                    {
                        tutor_id = tutor.TutorId,
                        tutor_name = tutor.User.FullName,
                        qualification = tutor.Qualification,
                        experience = tutor.Experience,
                        status = tutor.Status,
                        location = tutor.Location,

                        total_courses = courses.Count,

                        courses = courses
                    });
                }

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

        [Authorize]
        [HttpPost("set-rate-by-qualification")]
        public async Task<IActionResult> SetRateByQualification(QualificationRateDTO dto)
        {
            var tutorRates = await
            (
                from t in db.Tutors
                join r in db.TutorCourseRates
                    on t.TutorId equals r.TutorId
                where t.Qualification == dto.Qualification
                select r
            ).ToListAsync();

            if (!tutorRates.Any())
            {
                return NotFound(new
                {
                    message = "No tutors found with this qualification."
                });
            }

            foreach (var rate in tutorRates)
            {
                rate.AdminSetMinHourlyRate = dto.MinHourlyRate;
                rate.AdminSetMaxHourlyRate = dto.MaxHourlyRate;
            }

            await db.SaveChangesAsync();

            return Ok(new
            {
                message = "Rates updated successfully.",
                updated = tutorRates.Count
            });
        }

        [Authorize]
        [HttpPost("set-rate-by-experience")]
        public async Task<IActionResult> SetRateByExperience(ExperienceRateDTO dto)
        {
            var tutorRates = await
            (
                from t in db.Tutors
                join r in db.TutorCourseRates
                    on t.TutorId equals r.TutorId
                where t.Experience >= dto.MinExperience
                   && t.Experience <= dto.MaxExperience
                select r
            ).ToListAsync();

            if (!tutorRates.Any())
            {
                return NotFound(new
                {
                    message = "No tutors found."
                });
            }

            foreach (var rate in tutorRates)
            {
                rate.AdminSetMinHourlyRate = dto.MinHourlyRate;
                rate.AdminSetMaxHourlyRate = dto.MaxHourlyRate;
            }

            await db.SaveChangesAsync();

            return Ok(new
            {
                message = "Rates updated successfully.",
                updated = tutorRates.Count
            });
        }

        [Authorize]
        [HttpPost("set-rate-by-students")]
        public async Task<IActionResult> SetRateByStudents(StudentCountRateDTO dto)
        {
            var tutorStudentCounts =
                await db.Requests
                .Where(x => x.Status == "Accepted")
                .GroupBy(x => x.TutorId)
                .Select(x => new
                {
                    TutorId = x.Key,
                    Students = x.Select(a => a.StudentId).Distinct().Count()
                })
                .ToListAsync();

            int updated = 0;

            foreach (var tutor in tutorStudentCounts)
            {
                if (tutor.Students >= dto.MinStudents &&
                    tutor.Students <= dto.MaxStudents)
                {
                    var rates = await db.TutorCourseRates
                        .Where(x => x.TutorId == tutor.TutorId)
                        .ToListAsync();

                    foreach (var rate in rates)
                    {
                        rate.AdminSetMinHourlyRate = dto.MinHourlyRate;
                        rate.AdminSetMaxHourlyRate = dto.MaxHourlyRate;
                    }

                    updated += rates.Count;
                }
            }

            await db.SaveChangesAsync();

            return Ok(new
            {
                message = "Rates updated successfully.",
                updated
            });
        }

        [Authorize]
        [HttpPost("set-rate-by-rating")]
        public async Task<IActionResult> SetRateByRating(RatingRateDTO dto)
        {
            var tutorRatings =
                await db.Feedbacks
                .GroupBy(x => x.TutorId)
                .Select(x => new
                {
                    TutorId = x.Key,
                    Rating = x.Average(a => a.Rating)
                })
                .ToListAsync();

            int updated = 0;

            foreach (var tutor in tutorRatings)
            {
                if (tutor.Rating >= dto.MinRating &&
                    tutor.Rating <= dto.MaxRating)
                {
                    var rates = await db.TutorCourseRates
                        .Where(x => x.TutorId == tutor.TutorId)
                        .ToListAsync();

                    foreach (var rate in rates)
                    {
                        rate.AdminSetMinHourlyRate = dto.MinHourlyRate;
                        rate.AdminSetMaxHourlyRate = dto.MaxHourlyRate;
                    }

                    updated += rates.Count;
                }
            }

            await db.SaveChangesAsync();

            return Ok(new
            {
                message = "Rates updated successfully.",
                updated
            });
        }

        [Authorize]
        [HttpPost("set-rate-by-tutor-course")]
        public async Task<IActionResult> SetRateByTutorCourse(TutorCourseRateDTO dto)
        {
            try
            {
                var rate = await db.TutorCourseRates
                    .FirstOrDefaultAsync(r => r.TutorId == dto.TutorId && r.CourseId == dto.CourseId);

                if (rate == null)
                {
                    // create it if it doesn't exist yet
                    rate = new TutorCourseRate
                    {
                        TutorId = dto.TutorId,
                        CourseId = dto.CourseId,
                        HourlyRate = 0
                    };
                    db.TutorCourseRates.Add(rate);
                }

                rate.AdminSetMinHourlyRate = dto.MinHourlyRate;
                rate.AdminSetMaxHourlyRate = dto.MaxHourlyRate;

                await db.SaveChangesAsync();

                return Ok(new { message = "Rate updated successfully." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }


        [HttpGet("notification-count")]
        public async Task<IActionResult> GetNotificationCount()
        {
            try
            {
                // Example: count pending tutor approvals
                int count = await db.Tutors
                    .CountAsync(t => t.Status == "Pending");

                return Ok(new
                {
                    count = count
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Error getting notification count",
                    error = ex.InnerException?.Message ?? ex.Message
                });
            }
        }
    }
    public class AddSubjectDTO
    {
        public string CourseTitle { get; set; }
        public decimal? MinRate { get; set; }
        public decimal? MaxRate { get; set; }
    }

    public class UpdateStudentStatusDTO
    {
        public string Status { get; set; }
    }

    //Set Rate According to Qualification
    public class QualificationRateDTO
    {
        public string Qualification { get; set; }

        public decimal MinHourlyRate { get; set; }

        public decimal MaxHourlyRate { get; set; }
    }

    //Set Rate According to Teaching Experience
    public class ExperienceRateDTO
    {
        public int MinExperience { get; set; }

        public int MaxExperience { get; set; }

        public decimal MinHourlyRate { get; set; }

        public decimal MaxHourlyRate { get; set; }
    }

    //Set Rate According to Number of Students Taught
    public class StudentCountRateDTO
    {
        public int MinStudents { get; set; }

        public int MaxStudents { get; set; }

        public decimal MinHourlyRate { get; set; }

        public decimal MaxHourlyRate { get; set; }
    }

    //Set Rate According to Average Rating
    public class RatingRateDTO
    {
        public double MinRating { get; set; }

        public double MaxRating { get; set; }

        public decimal MinHourlyRate { get; set; }

        public decimal MaxHourlyRate { get; set; }
    }

    public class TutorCourseRateDTO
    {
        public int TutorId { get; set; }
        public int CourseId { get; set; }
        public decimal MinHourlyRate { get; set; }
        public decimal MaxHourlyRate { get; set; }
    }
}
