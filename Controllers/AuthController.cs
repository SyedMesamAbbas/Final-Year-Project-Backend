using HouseofTutorAPI.Models;
using HouseofTutorAPI.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Principal;
using System.Text;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace HouseofTutorAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly HouseofTutorContext db;
        private readonly GeoService _geoService;
        private readonly IConfiguration _configuration;

        public AuthController(HouseofTutorContext _db, IConfiguration configuration, GeoService geoService)
        {
             db = _db;
            _configuration = configuration;
            _geoService = geoService;
        }

        [HttpPost("register")]//parent password is last 5 digit
        public async Task<IActionResult> Register([FromBody] RegisterDTO dto)
        {
            using var transaction = await db.Database.BeginTransactionAsync();

            try
            {
                // -----------------------------------------
                // 1. Validate Request
                // -----------------------------------------
                if (dto == null)
                {
                    return BadRequest(new
                    {
                        message = "Invalid data."
                    });
                }

                // -----------------------------------------
                // 2. Normalize Email
                // -----------------------------------------
                dto.email = dto.email?.Trim().ToLower();

                // -----------------------------------------
                // 3. Normalize Father CNIC
                // -----------------------------------------
                string fatherCnic = dto.FatherCNIC?
                    .Replace("-", "")
                    .Trim();

                // -----------------------------------------
                // 4. Validate Father CNIC
                // -----------------------------------------
                if (dto.role == "Student")
                {
                    if (string.IsNullOrWhiteSpace(fatherCnic))
                    {
                        return BadRequest(new
                        {
                            message = "Father CNIC is required."
                        });
                    }

                    if (fatherCnic.Length < 5)
                    {
                        return BadRequest(new
                        {
                            message = "Father CNIC must contain at least 5 digits."
                        });
                    }
                }

                // -----------------------------------------
                // 5. Check Email Already Exists
                // -----------------------------------------
                if (await db.Users.AnyAsync(u => u.Email == dto.email))
                {
                    return BadRequest(new
                    {
                        message = "Email already exists."
                    });
                }

                // -----------------------------------------
                // 6. Only Student and Tutor Can Register
                // -----------------------------------------
                if (dto.role != "Student" &&
                    dto.role != "Tutor")
                {
                    return BadRequest(new
                    {
                        message = "Invalid role."
                    });
                }

                // -----------------------------------------
                // 7. Create Student/Tutor User
                // -----------------------------------------
                var user = new User
                {
                    FullName = dto.fullName,
                    Email = dto.email,
                    Phone = dto.phone,
                    Cnic = dto.cnic,
                    Password = dto.password,
                    Role = dto.role == "Student"
                        ? "Student"
                        : "Tutor"
                };

                db.Users.Add(user);

                await db.SaveChangesAsync();

                // =========================================
                // STUDENT
                // =========================================
                if (dto.role == "Student")
                {
                    // -----------------------------------------
                    // Create Student
                    // -----------------------------------------
                    db.Students.Add(new Student
                    {
                        UserId = user.UserId,
                        FatherCnic = fatherCnic,
                        Location = null
                    });

                    // -----------------------------------------
                    // Check Parent Account
                    // -----------------------------------------
                    bool parentExists = await db.Users.AnyAsync(x =>
                        x.Role == "Parent" &&
                        x.Cnic == fatherCnic);

                    // -----------------------------------------
                    // Create Parent Account
                    // -----------------------------------------
                    if (!parentExists)
                    {
                        // Parent password = LAST 5 digits
                        // of Father CNIC
                        string parentPassword =
                            fatherCnic.Substring(
                                fatherCnic.Length - 5,
                                5
                            );

                        var parentUser = new User
                        {
                            FullName = "Parent",

                            // Father CNIC is being used
                            // as Parent login email
                            Email = fatherCnic,

                            Phone = null,

                            Cnic = fatherCnic,

                            // LAST 5 digits of CNIC
                            Password = parentPassword,

                            Role = "Parent"
                        };

                        db.Users.Add(parentUser);
                    }
                }

                // =========================================
                // TUTOR
                // =========================================
                else
                {
                    db.Tutors.Add(new Tutor
                    {
                        UserId = user.UserId,
                        Qualification = dto.qualification,
                        Experience = dto.experience ?? 0,
                        Radius = dto.radius ?? 0,
                        Location = null,
                        Status = "Pending"
                    });
                }

                // -----------------------------------------
                // 8. Save Everything
                // -----------------------------------------
                await db.SaveChangesAsync();

                // -----------------------------------------
                // 9. Commit Transaction
                // -----------------------------------------
                await transaction.CommitAsync();

                // -----------------------------------------
                // 10. Success Response
                // -----------------------------------------
                return Ok(new
                {
                    message = "User registered successfully.",
                    userId = user.UserId
                });
            }
            catch (Exception ex)
            {
                // -----------------------------------------
                // Rollback
                // -----------------------------------------
                await transaction.RollbackAsync();

                return BadRequest(new
                {
                    message = ex.Message,
                    inner = ex.InnerException?.Message
                });
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDTO dto)
        {
            if (dto == null ||
                string.IsNullOrWhiteSpace(dto.Email) ||
                string.IsNullOrWhiteSpace(dto.Password))
            {
                return BadRequest(new { message = "Email/CNIC and password required" });
            }

            string login = dto.Email.Trim();
            string password = dto.Password.Trim();

            string normalizedEmail = login.ToLower();
            string normalizedCnic = login.Replace("-", "").Trim();

            var user = await db.Users.FirstOrDefaultAsync(u =>
                u.Email.ToLower() == normalizedEmail ||
                u.Cnic.Replace("-", "") == normalizedCnic);

            if (user == null)
            {
                return Unauthorized(new
                {
                    message = "Invalid email/CNIC"
                });
            }

            if ((user.Password ?? "").Trim() != password)
            {
                return Unauthorized(new
                {
                    message = "Invalid password"
                });
            }

            object? roleData = null;
            double? latitude = null;
            double? longitude = null;

            if (user.Role == "Tutor")
            {
                var tutor = await db.Tutors
                    .FirstOrDefaultAsync(x => x.UserId == user.UserId);

                if (tutor != null)
                {
                    roleData = new
                    {
                        qualification = tutor.Qualification,
                        experience = tutor.Experience,
                        location = tutor.Location,
                        radius = tutor.Radius,
                        status = "Pending" // tutor.Status
                    };

                    latitude = tutor.Latitude;
                    longitude = tutor.Longitude;
                }
            }
            else if (user.Role == "Student")
            {
                var student = await db.Students
                    .FirstOrDefaultAsync(x => x.UserId == user.UserId);

                if (student != null)
                {
                    roleData = new
                    {
                        location = student.Location
                    };

                    latitude = student.Latitude;
                    longitude = student.Longitude;
                }
            }
            else if (user.Role == "Parent")
            {
                roleData = new
                {
                    access = "Parent Access"
                };
            }
            else if (user.Role == "Admin")
            {
                roleData = new
                {
                    access = "Full Admin Access"
                };
            }

            var token = GenerateJwtToken(
                user.UserId.ToString(),
                user.Role,
                user.FullName,
                user.Email,
                latitude,
                longitude
            );

            return Ok(new
            {
                token,
                userId = user.UserId,
                role = user.Role,
                fullName = user.FullName,
                email = user.Email,
                phone = user.Phone,
                roleData,
                latitude,
                longitude,
                message = "Login successful"
            });
        }
        

        [HttpPost("update-location")]
        public async Task<IActionResult> UpdateLocation([FromBody] UpdateLocationDTO dto)
        {
            try
            {
                if (dto == null)
                    return BadRequest(new { message = "Invalid data" });

                if (dto.Latitude == 0 || dto.Longitude == 0)
                    return BadRequest(new { message = "Invalid coordinates" });

                var user = await db.Users.FindAsync(dto.UserId);
                if (user == null)
                    return NotFound(new { message = "User not found" });

                var address = await _geoService.GetAddressAsync(dto.Latitude, dto.Longitude);

                Console.WriteLine($"Lat: {dto.Latitude}, Lng: {dto.Longitude}");
                Console.WriteLine($"Address from OSM: {address}");

                if (string.IsNullOrWhiteSpace(address))
                {
                    address = "Unknown location";
                }

                var role = user.Role?.Trim().ToLower();

                if (role == "student")
                {
                    var student = await db.Students
                        .FirstOrDefaultAsync(s => s.UserId == user.UserId);

                    if (student == null)
                        return NotFound(new { message = "Student not found" });

                    student.Location = address;
                    student.Latitude = dto.Latitude;
                    student.Longitude = dto.Longitude;
                }
                else if (role == "tutor")
                {
                    var tutor = await db.Tutors
                        .FirstOrDefaultAsync(t => t.UserId == user.UserId);

                    if (tutor == null)
                        return NotFound(new { message = "Tutor not found" });

                    tutor.Location = address;
                    tutor.Latitude = dto.Latitude;
                    tutor.Longitude = dto.Longitude;
                }
                else
                {
                    return BadRequest(new { message = "Invalid user role" });
                }

                await db.SaveChangesAsync();

                return Ok(new
                {
                    message = "Location updated successfully",
                    address = address,
                    latitude = dto.Latitude,
                    longitude = dto.Longitude
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine(" ERROR: " + ex.Message);

                return StatusCode(500, new
                {
                    message = "Internal server error",
                    error = ex.Message
                });
            }
        }

        private string GenerateJwtToken(string userId,string role,string fullName,string email,double? latitude,double? longitude)
        {
            var keyString = _configuration["Jwt:Key"];

            if (string.IsNullOrEmpty(keyString))
                throw new Exception("JWT Key is NULL");

            if (keyString.Length < 32)
                throw new Exception("JWT Key too short (must be 32+ chars)");

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyString));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Role, role),
                new Claim(ClaimTypes.Name, fullName),
                new Claim(ClaimTypes.Email, email),

                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };
            if (latitude.HasValue)
                claims.Add(new Claim("latitude", latitude.Value.ToString()));

            if (longitude.HasValue)
                claims.Add(new Claim("longitude", longitude.Value.ToString()));

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddDays(7),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
    public class LoginDTO
    {
        public string? Email { get; set; }
        public string? Password { get; set; }
    }

    public class RegisterDTO
    {
        public string fullName { get; set; }
        public string email { get; set; }
        public string phone { get; set; }
        public string cnic { get; set; }
        public string password { get; set; }
        public string role { get; set; }
        public string? qualification { get; set; }
        public int? experience { get; set; }
        public int? radius { get; set; }
        public string? location { get; set; }
        public string? FatherCNIC { get; set; }
    }

    public class UpdateLocationDTO
    {
        public int UserId { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }
}
