using System.Net;
using System.Net.Http.Json;
using ATS.IntegrationTests.Fixtures;
using FluentAssertions;
using Xunit;

namespace ATS.IntegrationTests.Controllers;

public class AuthControllerTests : IClassFixture<AtsWebApplicationFactory>
{
    private readonly HttpClient _client;

    public AuthControllerTests(AtsWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Register_NewCandidate_ReturnsAccessToken()
    {
        // DbSeeder runs on startup (including against the InMemory provider here), so the
        // Candidate role already exists and registration should succeed end-to-end.
        var registerPayload = new
        {
            firstName = "Test",
            lastName = "Candidate",
            email = $"test-{Guid.NewGuid():N}@ats.local",
            password = "Test@12345",
            role = 5 // Candidate
        };

        var response = await _client.PostAsJsonAsync("/api/auth/register", registerPayload);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetJobs_Anonymous_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/jobs");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
