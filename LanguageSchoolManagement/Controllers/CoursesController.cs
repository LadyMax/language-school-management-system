using LanguageSchoolManagement.Data;
using LanguageSchoolManagement.Domain.Entities;
using LanguageSchoolManagement.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace LanguageSchoolManagement.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CoursesController : ControllerBase
{
    private readonly AppDbContext _db;

    public CoursesController(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>Get all active courses.</summary>
    [HttpGet]
    public ActionResult<IEnumerable<CourseDto>> GetAll()
    {
        var list = _db.Courses
            .Where(c => c.IsActive)
            .Select(c => new CourseDto(c))
            .ToList();
        return Ok(list);
    }

    /// <summary>Get course by id.</summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CourseDto>> GetById(Guid id, CancellationToken ct)
    {
        var course = await _db.Courses.FindAsync(new object[] { id }, ct);
        if (course == null)
            return NotFound();
        return Ok(new CourseDto(course));
    }

    /// <summary>Create a new course (admin).</summary>
    [HttpPost]
    public async Task<ActionResult<CourseDto>> Create([FromBody] CreateCourseRequest request, CancellationToken ct)
    {
        var course = new Course
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Description = request.Description ?? "",
            LanguageCode = request.LanguageCode ?? "",
            LanguageName = request.LanguageName ?? "",
            Level = request.Level,
            IsActive = true
        };
        _db.Courses.Add(course);
        await _db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(GetById), new { id = course.Id }, new CourseDto(course));
    }
}

public class CourseDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string LanguageCode { get; set; } = "";
    public string LanguageName { get; set; } = "";
    public CourseLevel Level { get; set; }
    public bool IsActive { get; set; }

    public CourseDto() { }

    public CourseDto(Course c)
    {
        Id = c.Id;
        Name = c.Name;
        Description = c.Description;
        LanguageCode = c.LanguageCode;
        LanguageName = c.LanguageName;
        Level = c.Level;
        IsActive = c.IsActive;
    }
}

public class CreateCourseRequest
{
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public string? LanguageCode { get; set; }
    public string? LanguageName { get; set; }
    public CourseLevel Level { get; set; }
}
