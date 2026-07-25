using ATS.Domain.Common;

namespace ATS.Domain.Entities;

public class Company : AuditableEntity
{
    public string Name { get; set; } = default!;
    public string? Website { get; set; }
    public string? LogoUrl { get; set; }
    public string? Industry { get; set; }
    public string? Description { get; set; }

    public ICollection<Department> Departments { get; set; } = new List<Department>();
    public ICollection<OfficeLocation> OfficeLocations { get; set; } = new List<OfficeLocation>();
    public ICollection<Job> Jobs { get; set; } = new List<Job>();
    public ICollection<User> Users { get; set; } = new List<User>();
}

public class Department : AuditableEntity
{
    public string Name { get; set; } = default!;
    public Guid CompanyId { get; set; }
    public Company Company { get; set; } = default!;
    public ICollection<Designation> Designations { get; set; } = new List<Designation>();
    public ICollection<Job> Jobs { get; set; } = new List<Job>();
}

public class Designation : AuditableEntity
{
    public string Title { get; set; } = default!;
    public Guid DepartmentId { get; set; }
    public Department Department { get; set; } = default!;
}

public class OfficeLocation : AuditableEntity
{
    public string Name { get; set; } = default!;
    public string Address { get; set; } = default!;
    public string City { get; set; } = default!;
    public string Country { get; set; } = default!;
    public Guid CompanyId { get; set; }
    public Company Company { get; set; } = default!;
}
