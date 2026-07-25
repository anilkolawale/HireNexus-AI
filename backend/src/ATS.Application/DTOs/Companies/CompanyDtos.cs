namespace ATS.Application.DTOs.Companies;

public record CompanyDto(Guid Id, string Name, string? Website, string? LogoUrl, string? Industry, string? Description);
public record CreateCompanyDto(string Name, string? Website, string? Industry, string? Description);

public record DepartmentDto(Guid Id, string Name, Guid CompanyId);
public record CreateDepartmentDto(string Name, Guid CompanyId);

public record DesignationDto(Guid Id, string Title, Guid DepartmentId);
public record CreateDesignationDto(string Title, Guid DepartmentId);

public record OfficeLocationDto(Guid Id, string Name, string Address, string City, string Country, Guid CompanyId);
public record CreateOfficeLocationDto(string Name, string Address, string City, string Country, Guid CompanyId);
