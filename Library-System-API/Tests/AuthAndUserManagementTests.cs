using LibrarySystem.Business.DTOs.Auth;
using LibrarySystem.Business.DTOs.Users;
using LibrarySystem.Business.Interfaces;
using LibrarySystem.Business.Services;
using LibrarySystem.DataAccess.Context;
using LibrarySystem.DataAccess.UnitOfWork;
using LibrarySystem.Shared.Authentication;
using LibrarySystem.Shared.Configuration;
using LibrarySystem.Shared.Enums;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Moq;

namespace LibrarySystem.Tests;

/// <summary>
/// End-to-end unit tests for the auth bootstrap / refresh-token rotation /
/// admin user-management flows, running against an in-memory SQLite database
/// with the real UnitOfWork so transactions behave like production.
/// </summary>
[TestClass]
public sealed class AuthAndUserManagementTests
{
    private readonly SqliteConnection _connection;
    private readonly LibraryDBContext _context;
    private readonly UnitOfWork _unitOfWork;
    private readonly AuthService _authService;
    private readonly UserService _userService;

    public AuthAndUserManagementTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<LibraryDBContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new LibraryDBContext(options);
        _context.Database.EnsureCreated();

        _unitOfWork = new UnitOfWork(_context);

        var jwtSettings = Options.Create(new JwtSettings
        {
            Secret = "unit-test-secret-key-with-at-least-32-chars!!",
            Issuer = "TestIssuer",
            Audience = "TestAudience",
            ExpiryMinutes = 15,
            RefreshTokenExpirationDays = 7
        });

        _authService = new AuthService(
            _unitOfWork,
            new JwtService(jwtSettings),
            jwtSettings,
            Mock.Of<IEmailSender>(),
            Options.Create(new AppSettings()));

        _userService = new UserService(_unitOfWork);
    }

    [TestCleanup]
    public void Cleanup()
    {
        _unitOfWork.DisposeAsync().AsTask().GetAwaiter().GetResult();
        _context.Dispose();
        _connection.Dispose();
    }

    // ---- One-time admin bootstrap ----

    [TestMethod]
    public async Task CreateAdmin_WhenNoAdminExists_CreatesAdminAndReturnsTokenPair()
    {
        var result = await _authService.CreateAdminAsync(new CreateAdminRequestDto
        {
            Name = "System Admin",
            Email = "admin@library.com",
            Password = "StrongPassword123!"
        });

        result.IsSuccess.Should().BeTrue();
        result.Value.Role.Should().Be("Admin");
        result.Value.Token.Should().NotBeEmpty();
        result.Value.RefreshToken.Should().NotBeEmpty();

        var admin = await _context.Users.SingleAsync(u => u.Email == "admin@library.com");
        admin.Role.Should().Be(UserRole.Admin);

        // Password must be stored as a PBKDF2 hash, never plaintext.
        admin.PasswordHash.Should().NotBe("StrongPassword123!");
        admin.PasswordHash.Should().Contain(".");

        // The refresh token must be stored hashed only.
        var stored = await _context.RefreshTokens.SingleAsync();
        stored.TokenHash.Should().NotBe(result.Value.RefreshToken);
    }

    [TestMethod]
    public async Task CreateAdmin_WhenAdminAlreadyExists_ReturnsConflict()
    {
        await _authService.CreateAdminAsync(new CreateAdminRequestDto
        {
            Name = "First", Email = "admin@library.com", Password = "StrongPassword123!"
        });

        var result = await _authService.CreateAdminAsync(new CreateAdminRequestDto
        {
            Name = "Second", Email = "admin2@library.com", Password = "StrongPassword123!"
        });

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("CONFLICT");
        result.Error.Message.Should().Contain("already exists");
        result.Error.Message.Should().Contain("no longer available");

        _context.Users.Count(u => u.Role == UserRole.Admin).Should().Be(1);
    }

    // ---- Refresh-token flow, rotation, revocation ----

    [TestMethod]
    public async Task RefreshToken_Rotates_OldTokenIsNoLongerUsable()
    {
        var login = (await _authService.CreateAdminAsync(new CreateAdminRequestDto
        {
            Name = "Admin", Email = "admin@library.com", Password = "StrongPassword123!"
        })).Value;

        var refreshed = await _authService.RefreshTokenAsync(new RefreshTokenRequestDto
        {
            RefreshToken = login.RefreshToken
        });

        refreshed.IsSuccess.Should().BeTrue();
        refreshed.Value.Token.Should().NotBe(login.Token);
        refreshed.Value.RefreshToken.Should().NotBe(login.RefreshToken);
        refreshed.Value.Role.Should().Be("Admin");

        // Replay of the rotated (old) refresh token must be rejected.
        var replay = await _authService.RefreshTokenAsync(new RefreshTokenRequestDto
        {
            RefreshToken = login.RefreshToken
        });

        replay.IsSuccess.Should().BeFalse();
        replay.Error!.Code.Should().Be("UNAUTHORIZED");
    }

    [TestMethod]
    public async Task RefreshToken_UnknownToken_ReturnsUnauthorizedWithoutDetails()
    {
        var result = await _authService.RefreshTokenAsync(new RefreshTokenRequestDto
        {
            RefreshToken = "totally-unknown-token"
        });

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("UNAUTHORIZED");
        result.Error.Message.Should().Be("Invalid refresh token.");
    }

    [TestMethod]
    public async Task RefreshToken_ExpiredToken_ReturnsUnauthorized()
    {
        var login = (await _authService.CreateAdminAsync(new CreateAdminRequestDto
        {
            Name = "Admin", Email = "admin@library.com", Password = "StrongPassword123!"
        })).Value;

        // Simulate expiry directly in the store (hash lookup is opaque).
        var stored = await _context.RefreshTokens.SingleAsync();
        stored.ExpiresAtUtc = DateTime.UtcNow.AddMinutes(-1);
        await _context.SaveChangesAsync();

        var result = await _authService.RefreshTokenAsync(new RefreshTokenRequestDto
        {
            RefreshToken = login.RefreshToken
        });

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("UNAUTHORIZED");
    }

    [TestMethod]
    public async Task RevokeRefreshToken_PreventsFurtherUse_AndIsIdempotent()
    {
        var login = (await _authService.CreateAdminAsync(new CreateAdminRequestDto
        {
            Name = "Admin", Email = "admin@library.com", Password = "StrongPassword123!"
        })).Value;

        (await _authService.RevokeRefreshTokenAsync(login.RefreshToken, login.UserId)).IsSuccess
            .Should().BeTrue();
        _context.RefreshTokens.Single().RevokedAtUtc.Should().NotBeNull();

        var afterRevocation = await _authService.RefreshTokenAsync(new RefreshTokenRequestDto
        {
            RefreshToken = login.RefreshToken
        });
        afterRevocation.IsSuccess.Should().BeFalse();

        // Revoking again (logout twice) must still succeed.
        (await _authService.RevokeRefreshTokenAsync(login.RefreshToken, login.UserId)).IsSuccess
            .Should().BeTrue();
    }

    [TestMethod]
    public async Task RefreshToken_DeactivatedUser_ReturnsUnauthorized()
    {
        var login = (await _authService.CreateAdminAsync(new CreateAdminRequestDto
        {
            Name = "Admin", Email = "admin@library.com", Password = "StrongPassword123!"
        })).Value;

        var admin = await _context.Users.SingleAsync();
        admin.IsActive = false;
        await _context.SaveChangesAsync();

        var result = await _authService.RefreshTokenAsync(new RefreshTokenRequestDto
        {
            RefreshToken = login.RefreshToken
        });

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("UNAUTHORIZED");
    }

    [TestMethod]
    public async Task Login_IssuesTokenPair_WithAdminRole()
    {
        await _authService.CreateAdminAsync(new CreateAdminRequestDto
        {
            Name = "Admin", Email = "admin@library.com", Password = "StrongPassword123!"
        });

        var login = await _authService.LoginAsync(new LoginRequestDto
        {
            Email = "admin@library.com",
            Password = "StrongPassword123!"
        });

        login.IsSuccess.Should().BeTrue();
        login.Value.Role.Should().Be("Admin");
        login.Value.Token.Should().NotBeEmpty();
        login.Value.RefreshToken.Should().NotBeEmpty();
        login.Value.RefreshTokenExpiresAtUtc.Should().BeAfter(DateTime.UtcNow);
    }

    [TestMethod]
    public async Task Register_AlwaysCreatesUserRole_IgnoringClientRoleHints()
    {
        // RegisterRequestDto carries no role field at all — the backend is
        // authoritative. Verify the created account is a plain User.
        var result = await _authService.RegisterAsync(new RegisterRequestDto
        {
            FullName = "Test",
            Email = "test@example.com",
            Password = "Password123!"
        });

        result.IsSuccess.Should().BeTrue();
        result.Value.Role.Should().Be("User");
        (await _context.Users.SingleAsync(u => u.Email == "test@example.com"))
            .Role.Should().Be(UserRole.User);
    }

    // ---- Admin user management ----

    [TestMethod]
    public async Task CreateUser_ByAdmin_CreatesAdditionalAdmin()
    {
        var result = await _userService.CreateUserAsync(new CreateUserRequestDto
        {
            Name = "Second Admin",
            Email = "admin2@library.com",
            Password = "AnotherStrong123!",
            Role = UserRole.Admin
        });

        result.IsSuccess.Should().BeTrue();
        result.Value.Role.Should().Be("Admin");
        _context.Users.Count(u => u.Role == UserRole.Admin).Should().Be(1);
    }

    [TestMethod]
    public async Task UpdateUserRole_DemotingLastAdmin_IsRefused()
    {
        var admin = (await _userService.CreateUserAsync(new CreateUserRequestDto
        {
            Name = "Only Admin", Email = "admin@library.com",
            Password = "StrongPassword123!", Role = UserRole.Admin
        })).Value;

        var result = await _userService.UpdateUserRoleAsync(admin.Id,
            new UpdateUserRoleRequestDto { Role = UserRole.User });

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("BUSINESS_RULE_VIOLATION");
        result.Error.Message.Should().Contain("at least one administrator");
        _context.Users.Single().Role.Should().Be(UserRole.Admin);
    }

    [TestMethod]
    public async Task UpdateUserRole_DemotingAdminWithAnotherAdminPresent_Succeeds()
    {
        await _userService.CreateUserAsync(new CreateUserRequestDto
        {
            Name = "Admin One", Email = "admin1@library.com",
            Password = "StrongPassword123!", Role = UserRole.Admin
        });
        var adminTwo = (await _userService.CreateUserAsync(new CreateUserRequestDto
        {
            Name = "Admin Two", Email = "admin2@library.com",
            Password = "StrongPassword123!", Role = UserRole.Admin
        })).Value;

        var result = await _userService.UpdateUserRoleAsync(adminTwo.Id,
            new UpdateUserRoleRequestDto { Role = UserRole.User });

        result.IsSuccess.Should().BeTrue();
        result.Value.Role.Should().Be("User");
    }

    [TestMethod]
    public async Task UpdateUserRole_PromotesUserToAdmin_AndRevokesSessions()
    {
        var login = (await _authService.CreateAdminAsync(new CreateAdminRequestDto
        {
            Name = "Admin", Email = "admin@library.com", Password = "StrongPassword123!"
        })).Value;

        var user = (await _userService.CreateUserAsync(new CreateUserRequestDto
        {
            Name = "Plain User", Email = "user@library.com",
            Password = "UserPassword123!", Role = UserRole.User
        })).Value;

        var result = await _userService.UpdateUserRoleAsync(user.Id,
            new UpdateUserRoleRequestDto { Role = UserRole.Admin });

        result.IsSuccess.Should().BeTrue();

        // The admin's own active session stays valid (their token was not touched).
        _context.RefreshTokens.Count(t => t.UserId == login.UserId && t.RevokedAtUtc == null)
            .Should().Be(1);
        // The promoted user had no active tokens; nothing to revoke.
        _context.RefreshTokens.Count(t => t.UserId == user.Id).Should().Be(0);
    }

    [TestMethod]
    public async Task UpdateStatus_CannotDeactivateSelf()
    {
        var admin = (await _userService.CreateUserAsync(new CreateUserRequestDto
        {
            Name = "Admin", Email = "admin@library.com",
            Password = "StrongPassword123!", Role = UserRole.Admin
        })).Value;

        var result = await _userService.UpdateUserStatusAsync(
            admin.Id, new UpdateUserStatusRequestDto { IsActive = false }, admin.Id);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("BUSINESS_RULE_VIOLATION");
    }

    [TestMethod]
    public async Task UpdateStatus_CannotDeactivateLastActiveAdmin()
    {
        var admin = (await _userService.CreateUserAsync(new CreateUserRequestDto
        {
            Name = "Admin", Email = "admin@library.com",
            Password = "StrongPassword123!", Role = UserRole.Admin
        })).Value;

        var result = await _userService.UpdateUserStatusAsync(
            admin.Id, new UpdateUserStatusRequestDto { IsActive = false }, Guid.NewGuid());

        result.IsSuccess.Should().BeFalse();
        result.Error!.Message.Should().Contain("at least one active administrator");
    }

    [TestMethod]
    public async Task UpdateStatus_DeactivatingUser_RevokesSessions()
    {
        var login = (await _authService.CreateAdminAsync(new CreateAdminRequestDto
        {
            Name = "Admin", Email = "admin@library.com", Password = "StrongPassword123!"
        })).Value;

        await _userService.CreateUserAsync(new CreateUserRequestDto
        {
            Name = "Admin Two", Email = "admin2@library.com",
            Password = "StrongPassword123!", Role = UserRole.Admin
        });

        var user = (await _userService.CreateUserAsync(new CreateUserRequestDto
        {
            Name = "Plain User", Email = "user@library.com",
            Password = "UserPassword123!", Role = UserRole.User
        })).Value;

        var userLogin = await _authService.LoginAsync(new LoginRequestDto
        {
            Email = "user@library.com", Password = "UserPassword123!"
        });

        userLogin.IsSuccess.Should().BeTrue();

        var result = await _userService.UpdateUserStatusAsync(
            user.Id, new UpdateUserStatusRequestDto { IsActive = false }, login.UserId);

        result.IsSuccess.Should().BeTrue();

        // The deactivated user's refresh token must have been revoked.
        _context.RefreshTokens.Count(t => t.UserId == user.Id && t.RevokedAtUtc == null)
            .Should().Be(0);
    }

    [TestMethod]
    public async Task GetUsers_ReturnsPagedSafeProjection()
    {
        await _userService.CreateUserAsync(new CreateUserRequestDto
        {
            Name = "Alpha", Email = "alpha@example.com",
            Password = "Password123!", Role = UserRole.User
        });
        await _userService.CreateUserAsync(new CreateUserRequestDto
        {
            Name = "Beta", Email = "beta@example.com",
            Password = "Password123!", Role = UserRole.Admin
        });

        var result = await _userService.GetUsersAsync(new UsersQueryDto { Page = 1, PageSize = 1 });

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalItems.Should().Be(2);
        result.Value.Items.Should().HaveCount(1);
        result.Value.Items[0].Name.Should().Be("Alpha"); // ordered by name
    }
}
