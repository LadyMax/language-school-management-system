using LanguageSchoolManagement.Data;
using LanguageSchoolManagement.Domain.Entities;
using LanguageSchoolManagement.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LanguageSchoolManagement.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CourseRequestsController : ControllerBase
{
    private readonly AppDbContext _db;

    public CourseRequestsController(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>Submit a Join or Leave request. After Leave is approved, user can submit Join again for the same course.</summary>
    [HttpPost]
    public async Task<ActionResult<CourseRequestDto>> Submit([FromBody] SubmitCourseRequestRequest body, CancellationToken ct)
    {
        var (userId, courseId, type) = (body.UserId, body.CourseId, body.Type);

        var course = await _db.Courses.FindAsync(new object[] { courseId }, ct);
        if (course == null || !course.IsActive)
            return NotFound("Course not found or inactive.");

        var user = await _db.Users.FindAsync(new object[] { userId }, ct);
        if (user == null || !user.IsActive)
            return NotFound("User not found or inactive.");

        var isEnrolled = await _db.Enrollments.AnyAsync(e => e.UserId == userId && e.CourseId == courseId && e.IsActive, ct);

        if (type == RequestType.Join)
        {
            if (isEnrolled)
                return BadRequest("Already enrolled in this course.");
        }
        else // Leave
        {
            if (!isEnrolled)
                return BadRequest("Not enrolled in this course.");
        }

        var existingPending = await _db.CourseRequests
            .AnyAsync(r => r.UserId == userId && r.CourseId == courseId && r.Status == RequestStatus.Pending, ct);
        if (existingPending)
            return BadRequest("You already have a pending request for this course.");

        var request = new CourseRequest
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            CourseId = courseId,
            Type = type,
            Status = RequestStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };
        _db.CourseRequests.Add(request);
        await _db.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(GetById), new { id = request.Id }, new CourseRequestDto(request));
    }

    /// <summary>Get request by id.</summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CourseRequestDto>> GetById(Guid id, CancellationToken ct)
    {
        var request = await _db.CourseRequests.FindAsync(new object[] { id }, ct);
        if (request == null)
            return NotFound();
        return Ok(new CourseRequestDto(request));
    }

    /// <summary>Approve or reject a request. Approving Join creates Enrollment; approving Leave removes it (user can request Join again later).</summary>
    [HttpPost("{id:guid}/review")]
    public async Task<ActionResult<CourseRequestDto>> Review(Guid id, [FromBody] ReviewRequest body, CancellationToken ct)
    {
        var request = await _db.CourseRequests
            .Include(r => r.User)
            .Include(r => r.Course)
            .FirstOrDefaultAsync(r => r.Id == id, ct);
        if (request == null)
            return NotFound();

        if (request.Status != RequestStatus.Pending)
            return BadRequest("Request is already reviewed.");

        request.Status = body.Approve ? RequestStatus.Approved : RequestStatus.Rejected;
        request.ReviewedAt = DateTime.UtcNow;
        request.ReviewedBy = body.ReviewedBy;

        if (body.Approve)
        {
            if (request.Type == RequestType.Join)
            {
                _db.Enrollments.Add(new Enrollment
                {
                    Id = Guid.NewGuid(),
                    UserId = request.UserId,
                    CourseId = request.CourseId,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                });
            }
            else // Leave: keep history, set to inactive only
            {
                var enrollment = await _db.Enrollments
                    .FirstOrDefaultAsync(e => e.UserId == request.UserId && e.CourseId == request.CourseId && e.IsActive, ct);
                if (enrollment != null)
                {
                    enrollment.IsActive = false;
                    enrollment.EndedAt = DateTime.UtcNow;
                }
            }
        }

        await _db.SaveChangesAsync(ct);
        return Ok(new CourseRequestDto(request));
    }
}

public class CourseRequestDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid CourseId { get; set; }
    public RequestType Type { get; set; }
    public RequestStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public Guid? ReviewedBy { get; set; }

    public CourseRequestDto() { }

    public CourseRequestDto(CourseRequest r)
    {
        Id = r.Id;
        UserId = r.UserId;
        CourseId = r.CourseId;
        Type = r.Type;
        Status = r.Status;
        CreatedAt = r.CreatedAt;
        ReviewedAt = r.ReviewedAt;
        ReviewedBy = r.ReviewedBy;
    }
}

public class SubmitCourseRequestRequest
{
    public Guid UserId { get; set; }
    public Guid CourseId { get; set; }
    public RequestType Type { get; set; }
}

public class ReviewRequest
{
    public bool Approve { get; set; }
    public Guid? ReviewedBy { get; set; }
}
