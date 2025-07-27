using GoogleAuthTotpPrototype.Data;
using GoogleAuthTotpPrototype.Models;
using GoogleAuthTotpPrototype.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace GoogleAuthTotpPrototype.Tests;

public class UserServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
    private readonly Mock<ITotpService> _totpServiceMock;
    private readonly IConfiguration _configuration;
    private readonly Mock<ILogger<UserService>> _loggerMock;
    private readonly UserService _userService;
    private readonly ITestOutputHelper _output;

    public UserServiceTests(ITestOutputHelper output)
    {
        _output = output;

        // Create in-memory database
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);

        // Create mock UserManager
        var userStoreMock = new Mock<IUserStore<ApplicationUser>>();
        _userManagerMock = new Mock<UserManager<ApplicationUser>>(
            userStoreMock.Object, null, null, null, null, null, null, null, null);

        // Create mock TOTP service
        _totpServiceMock = new Mock<ITotpService>();

        // Create test configuration
        var configurationBuilder = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Totp:MaxFailureAttempts"] = "3",
                ["Totp:LockoutDurationMinutes"] = "5"
            });
        _configuration = configurationBuilder.Build();

        // Create mock logger
        _loggerMock = new Mock<ILogger<UserService>>();

        // Create UserService instance
        _userService = new UserService(
            _userManagerMock.Object,
            _context,
            _totpServiceMock.Object,
            _configuration,
            _loggerMock.Object);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    [Fact]
    public async Task SetupTotpAsync_WithValidInputs_ShouldReturnTrue()
    {
        // Arrange
        var userId = "test-user-id";
        var secret = "JBSWY3DPEHPK3PXP";
        var user = new ApplicationUser
        {
            Id = userId,
            Email = "test@example.com",
            UserName = "testuser"
        };

        _userManagerMock.Setup(x => x.FindByIdAsync(userId))
            .ReturnsAsync(user);
        _userManagerMock.Setup(x => x.UpdateAsync(It.IsAny<ApplicationUser>()))
            .ReturnsAsync(IdentityResult.Success);

        _output.WriteLine($"✅ SetupTotpAsync_WithValidInputs_ShouldReturnTrue:");
        _output.WriteLine($"   User ID: {userId}");
        _output.WriteLine($"   Secret: {secret}");

        // Act
        var result = await _userService.SetupTotpAsync(userId, secret);

        // Assert
        Assert.True(result);
        Assert.Equal(secret, user.TotpSecret);
        Assert.True(user.IsTotpEnabled);
        Assert.Equal(0, user.TotpFailureCount);
        Assert.Null(user.LastTotpFailure);
        Assert.Null(user.TotpLockoutEnd);

        _output.WriteLine($"   Result: {result}");
        _output.WriteLine($"   TOTP Enabled: {user.IsTotpEnabled}");
        _output.WriteLine($"   Failure Count Reset: {user.TotpFailureCount}");

        _userManagerMock.Verify(x => x.UpdateAsync(user), Times.Once);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task SetupTotpAsync_WithInvalidUserId_ShouldReturnFalse(string userId)
    {
        // Arrange
        var secret = "JBSWY3DPEHPK3PXP";

        _output.WriteLine($"🔍 SetupTotpAsync_WithInvalidUserId_ShouldReturnFalse:");
        _output.WriteLine($"   User ID: '{userId}' (null/empty/whitespace)");
        _output.WriteLine($"   Secret: {secret}");

        // Act
        var result = await _userService.SetupTotpAsync(userId, secret);

        // Assert
        Assert.False(result);
        _output.WriteLine($"   Result: {result}");
        _output.WriteLine($"   Expected: False (invalid user ID should fail)");

        _userManagerMock.Verify(x => x.FindByIdAsync(It.IsAny<string>()), Times.Never);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task SetupTotpAsync_WithInvalidSecret_ShouldReturnFalse(string secret)
    {
        // Arrange
        var userId = "test-user-id";

        _output.WriteLine($"🔍 SetupTotpAsync_WithInvalidSecret_ShouldReturnFalse:");
        _output.WriteLine($"   User ID: {userId}");
        _output.WriteLine($"   Secret: '{secret}' (null/empty/whitespace)");

        // Act
        var result = await _userService.SetupTotpAsync(userId, secret);

        // Assert
        Assert.False(result);
        _output.WriteLine($"   Result: {result}");
        _output.WriteLine($"   Expected: False (invalid secret should fail)");

        _userManagerMock.Verify(x => x.FindByIdAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task SetupTotpAsync_WithNonExistentUser_ShouldReturnFalse()
    {
        // Arrange
        var userId = "non-existent-user";
        var secret = "JBSWY3DPEHPK3PXP";

        _userManagerMock.Setup(x => x.FindByIdAsync(userId))
            .ReturnsAsync((ApplicationUser?)null);

        _output.WriteLine($"🔍 SetupTotpAsync_WithNonExistentUser_ShouldReturnFalse:");
        _output.WriteLine($"   User ID: {userId}");
        _output.WriteLine($"   Secret: {secret}");

        // Act
        var result = await _userService.SetupTotpAsync(userId, secret);

        // Assert
        Assert.False(result);
        _output.WriteLine($"   Result: {result}");
        _output.WriteLine($"   Expected: False (non-existent user should fail)");

        _userManagerMock.Verify(x => x.FindByIdAsync(userId), Times.Once);
        _userManagerMock.Verify(x => x.UpdateAsync(It.IsAny<ApplicationUser>()), Times.Never);
    }

    [Fact]
    public async Task ValidateTotpAsync_WithValidCode_ShouldReturnTrueAndResetFailures()
    {
        // Arrange
        var userId = "test-user-id";
        var code = "123456";
        var user = new ApplicationUser
        {
            Id = userId,
            Email = "test@example.com",
            UserName = "testuser",
            TotpSecret = "JBSWY3DPEHPK3PXP",
            IsTotpEnabled = true,
            TotpFailureCount = 2,
            LastTotpFailure = DateTime.UtcNow.AddMinutes(-1)
        };

        _userManagerMock.Setup(x => x.FindByIdAsync(userId))
            .ReturnsAsync(user);
        _userManagerMock.Setup(x => x.UpdateAsync(It.IsAny<ApplicationUser>()))
            .ReturnsAsync(IdentityResult.Success);
        _totpServiceMock.Setup(x => x.ValidateTotp(user.TotpSecret, code))
            .Returns(true);

        _output.WriteLine($"✅ ValidateTotpAsync_WithValidCode_ShouldReturnTrueAndResetFailures:");
        _output.WriteLine($"   User ID: {userId}");
        _output.WriteLine($"   Code: {code}");
        _output.WriteLine($"   Initial Failure Count: {user.TotpFailureCount}");

        // Act
        var result = await _userService.ValidateTotpAsync(userId, code);

        // Assert
        Assert.True(result);
        Assert.Equal(0, user.TotpFailureCount);
        Assert.Null(user.LastTotpFailure);
        Assert.Null(user.TotpLockoutEnd);

        _output.WriteLine($"   Result: {result}");
        _output.WriteLine($"   Final Failure Count: {user.TotpFailureCount}");
        _output.WriteLine($"   Failures Reset: {user.LastTotpFailure == null}");

        _totpServiceMock.Verify(x => x.ValidateTotp(user.TotpSecret, code), Times.Once);
        _userManagerMock.Verify(x => x.UpdateAsync(user), Times.Once);
    }

    [Fact]
    public async Task ValidateTotpAsync_WithInvalidCode_ShouldReturnFalseAndIncrementFailures()
    {
        // Arrange
        var userId = "test-user-id";
        var code = "000000";
        var user = new ApplicationUser
        {
            Id = userId,
            Email = "test@example.com",
            UserName = "testuser",
            TotpSecret = "JBSWY3DPEHPK3PXP",
            IsTotpEnabled = true,
            TotpFailureCount = 1
        };

        _userManagerMock.Setup(x => x.FindByIdAsync(userId))
            .ReturnsAsync(user);
        _userManagerMock.Setup(x => x.UpdateAsync(It.IsAny<ApplicationUser>()))
            .ReturnsAsync(IdentityResult.Success);
        _totpServiceMock.Setup(x => x.ValidateTotp(user.TotpSecret, code))
            .Returns(false);

        _output.WriteLine($"🔍 ValidateTotpAsync_WithInvalidCode_ShouldReturnFalseAndIncrementFailures:");
        _output.WriteLine($"   User ID: {userId}");
        _output.WriteLine($"   Code: {code}");
        _output.WriteLine($"   Initial Failure Count: {user.TotpFailureCount}");

        // Act
        var result = await _userService.ValidateTotpAsync(userId, code);

        // Assert
        Assert.False(result);
        Assert.Equal(2, user.TotpFailureCount);
        Assert.NotNull(user.LastTotpFailure);
        Assert.Null(user.TotpLockoutEnd); // Should not be locked yet (max is 3)

        _output.WriteLine($"   Result: {result}");
        _output.WriteLine($"   Final Failure Count: {user.TotpFailureCount}");
        _output.WriteLine($"   Last Failure Time Set: {user.LastTotpFailure != null}");
        _output.WriteLine($"   Account Locked: {user.TotpLockoutEnd != null}");

        _totpServiceMock.Verify(x => x.ValidateTotp(user.TotpSecret, code), Times.Once);
        _userManagerMock.Verify(x => x.UpdateAsync(user), Times.Once);
    }

    [Fact]
    public async Task ValidateTotpAsync_WithThirdFailure_ShouldLockAccount()
    {
        // Arrange
        var userId = "test-user-id";
        var code = "000000";
        var user = new ApplicationUser
        {
            Id = userId,
            Email = "test@example.com",
            UserName = "testuser",
            TotpSecret = "JBSWY3DPEHPK3PXP",
            IsTotpEnabled = true,
            TotpFailureCount = 2 // One more failure will trigger lockout
        };

        _userManagerMock.Setup(x => x.FindByIdAsync(userId))
            .ReturnsAsync(user);
        _userManagerMock.Setup(x => x.UpdateAsync(It.IsAny<ApplicationUser>()))
            .ReturnsAsync(IdentityResult.Success);
        _totpServiceMock.Setup(x => x.ValidateTotp(user.TotpSecret, code))
            .Returns(false);

        _output.WriteLine($"🔍 ValidateTotpAsync_WithThirdFailure_ShouldLockAccount:");
        _output.WriteLine($"   User ID: {userId}");
        _output.WriteLine($"   Code: {code}");
        _output.WriteLine($"   Initial Failure Count: {user.TotpFailureCount}");
        _output.WriteLine($"   Max Failures Allowed: 3");

        // Act
        var result = await _userService.ValidateTotpAsync(userId, code);

        // Assert
        Assert.False(result);
        Assert.Equal(3, user.TotpFailureCount);
        Assert.NotNull(user.LastTotpFailure);
        Assert.NotNull(user.TotpLockoutEnd);
        Assert.True(user.TotpLockoutEnd > DateTime.UtcNow);

        _output.WriteLine($"   Result: {result}");
        _output.WriteLine($"   Final Failure Count: {user.TotpFailureCount}");
        _output.WriteLine($"   Account Locked: {user.TotpLockoutEnd != null}");
        _output.WriteLine($"   Lockout End: {user.TotpLockoutEnd}");

        _totpServiceMock.Verify(x => x.ValidateTotp(user.TotpSecret, code), Times.Once);
        _userManagerMock.Verify(x => x.UpdateAsync(user), Times.Once);
    }

    [Fact]
    public async Task ValidateTotpAsync_WithLockedAccount_ShouldReturnFalseWithoutValidation()
    {
        // Arrange
        var userId = "test-user-id";
        var code = "123456";
        var user = new ApplicationUser
        {
            Id = userId,
            Email = "test@example.com",
            UserName = "testuser",
            TotpSecret = "JBSWY3DPEHPK3PXP",
            IsTotpEnabled = true,
            TotpFailureCount = 3,
            TotpLockoutEnd = DateTime.UtcNow.AddMinutes(3) // Locked for 3 more minutes
        };

        _userManagerMock.Setup(x => x.FindByIdAsync(userId))
            .ReturnsAsync(user);

        _output.WriteLine($"🔍 ValidateTotpAsync_WithLockedAccount_ShouldReturnFalseWithoutValidation:");
        _output.WriteLine($"   User ID: {userId}");
        _output.WriteLine($"   Code: {code}");
        _output.WriteLine($"   Account Locked Until: {user.TotpLockoutEnd}");
        _output.WriteLine($"   Current Time: {DateTime.UtcNow}");

        // Act
        var result = await _userService.ValidateTotpAsync(userId, code);

        // Assert
        Assert.False(result);

        _output.WriteLine($"   Result: {result}");
        _output.WriteLine($"   Expected: False (locked account should not validate)");

        // Should not call TOTP validation for locked account
        _totpServiceMock.Verify(x => x.ValidateTotp(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task IsAccountLockedAsync_WithActivelyLockedAccount_ShouldReturnTrue()
    {
        // Arrange
        var userId = "test-user-id";
        var user = new ApplicationUser
        {
            Id = userId,
            TotpLockoutEnd = DateTime.UtcNow.AddMinutes(3)
        };

        _userManagerMock.Setup(x => x.FindByIdAsync(userId))
            .ReturnsAsync(user);

        _output.WriteLine($"✅ IsAccountLockedAsync_WithActivelyLockedAccount_ShouldReturnTrue:");
        _output.WriteLine($"   User ID: {userId}");
        _output.WriteLine($"   Lockout End: {user.TotpLockoutEnd}");
        _output.WriteLine($"   Current Time: {DateTime.UtcNow}");

        // Act
        var result = await _userService.IsAccountLockedAsync(userId);

        // Assert
        Assert.True(result);
        _output.WriteLine($"   Result: {result}");
        _output.WriteLine($"   Expected: True (account should be locked)");
    }

    [Fact]
    public async Task IsAccountLockedAsync_WithExpiredLockout_ShouldReturnFalseAndClearLockout()
    {
        // Arrange
        var userId = "test-user-id";
        var user = new ApplicationUser
        {
            Id = userId,
            TotpLockoutEnd = DateTime.UtcNow.AddMinutes(-1) // Expired 1 minute ago
        };

        _userManagerMock.Setup(x => x.FindByIdAsync(userId))
            .ReturnsAsync(user);
        _userManagerMock.Setup(x => x.UpdateAsync(It.IsAny<ApplicationUser>()))
            .ReturnsAsync(IdentityResult.Success);

        _output.WriteLine($"✅ IsAccountLockedAsync_WithExpiredLockout_ShouldReturnFalseAndClearLockout:");
        _output.WriteLine($"   User ID: {userId}");
        _output.WriteLine($"   Lockout End: {user.TotpLockoutEnd}");
        _output.WriteLine($"   Current Time: {DateTime.UtcNow}");

        // Act
        var result = await _userService.IsAccountLockedAsync(userId);

        // Assert
        Assert.False(result);
        Assert.Null(user.TotpLockoutEnd);

        _output.WriteLine($"   Result: {result}");
        _output.WriteLine($"   Lockout Cleared: {user.TotpLockoutEnd == null}");
        _output.WriteLine($"   Expected: False (expired lockout should be cleared)");

        _userManagerMock.Verify(x => x.UpdateAsync(user), Times.Once);
    }

    [Fact]
    public async Task LockAccountAsync_ShouldSetLockoutEndTime()
    {
        // Arrange
        var userId = "test-user-id";
        var user = new ApplicationUser
        {
            Id = userId,
            Email = "test@example.com",
            UserName = "testuser"
        };

        _userManagerMock.Setup(x => x.FindByIdAsync(userId))
            .ReturnsAsync(user);
        _userManagerMock.Setup(x => x.UpdateAsync(It.IsAny<ApplicationUser>()))
            .ReturnsAsync(IdentityResult.Success);

        var beforeLockTime = DateTime.UtcNow;

        _output.WriteLine($"✅ LockAccountAsync_ShouldSetLockoutEndTime:");
        _output.WriteLine($"   User ID: {userId}");
        _output.WriteLine($"   Before Lock Time: {beforeLockTime}");

        // Act
        await _userService.LockAccountAsync(userId);

        var afterLockTime = DateTime.UtcNow;

        // Assert
        Assert.NotNull(user.TotpLockoutEnd);
        Assert.True(user.TotpLockoutEnd > beforeLockTime);
        Assert.True(user.TotpLockoutEnd <= afterLockTime.AddMinutes(5)); // 5 minute lockout

        _output.WriteLine($"   Lockout End: {user.TotpLockoutEnd}");
        _output.WriteLine($"   After Lock Time: {afterLockTime}");
        _output.WriteLine($"   Lockout Duration: ~5 minutes");

        _userManagerMock.Verify(x => x.UpdateAsync(user), Times.Once);
    }

    [Fact]
    public async Task ResetTotpFailuresAsync_ShouldClearAllFailureData()
    {
        // Arrange
        var userId = "test-user-id";
        var user = new ApplicationUser
        {
            Id = userId,
            TotpFailureCount = 3,
            LastTotpFailure = DateTime.UtcNow.AddMinutes(-1),
            TotpLockoutEnd = DateTime.UtcNow.AddMinutes(3)
        };

        _userManagerMock.Setup(x => x.FindByIdAsync(userId))
            .ReturnsAsync(user);
        _userManagerMock.Setup(x => x.UpdateAsync(It.IsAny<ApplicationUser>()))
            .ReturnsAsync(IdentityResult.Success);

        _output.WriteLine($"✅ ResetTotpFailuresAsync_ShouldClearAllFailureData:");
        _output.WriteLine($"   User ID: {userId}");
        _output.WriteLine($"   Initial Failure Count: {user.TotpFailureCount}");
        _output.WriteLine($"   Initial Last Failure: {user.LastTotpFailure}");
        _output.WriteLine($"   Initial Lockout End: {user.TotpLockoutEnd}");

        // Act
        await _userService.ResetTotpFailuresAsync(userId);

        // Assert
        Assert.Equal(0, user.TotpFailureCount);
        Assert.Null(user.LastTotpFailure);
        Assert.Null(user.TotpLockoutEnd);

        _output.WriteLine($"   Final Failure Count: {user.TotpFailureCount}");
        _output.WriteLine($"   Final Last Failure: {user.LastTotpFailure}");
        _output.WriteLine($"   Final Lockout End: {user.TotpLockoutEnd}");

        _userManagerMock.Verify(x => x.UpdateAsync(user), Times.Once);
    }

    [Fact]
    public async Task GetRemainingLockoutTimeAsync_WithActivelyLockedAccount_ShouldReturnRemainingTime()
    {
        // Arrange
        var userId = "test-user-id";
        var lockoutEnd = DateTime.UtcNow.AddMinutes(3);
        var user = new ApplicationUser
        {
            Id = userId,
            TotpLockoutEnd = lockoutEnd
        };

        _userManagerMock.Setup(x => x.FindByIdAsync(userId))
            .ReturnsAsync(user);

        _output.WriteLine($"✅ GetRemainingLockoutTimeAsync_WithActivelyLockedAccount_ShouldReturnRemainingTime:");
        _output.WriteLine($"   User ID: {userId}");
        _output.WriteLine($"   Lockout End: {lockoutEnd}");
        _output.WriteLine($"   Current Time: {DateTime.UtcNow}");

        // Act
        var result = await _userService.GetRemainingLockoutTimeAsync(userId);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Value.TotalMinutes > 2.5); // Should be close to 3 minutes
        Assert.True(result.Value.TotalMinutes < 3.5);

        _output.WriteLine($"   Remaining Time: {result}");
        _output.WriteLine($"   Expected: ~3 minutes");
    }

    [Fact]
    public async Task GetRemainingLockoutTimeAsync_WithExpiredLockout_ShouldReturnNull()
    {
        // Arrange
        var userId = "test-user-id";
        var user = new ApplicationUser
        {
            Id = userId,
            TotpLockoutEnd = DateTime.UtcNow.AddMinutes(-1) // Expired
        };

        _userManagerMock.Setup(x => x.FindByIdAsync(userId))
            .ReturnsAsync(user);

        _output.WriteLine($"🔍 GetRemainingLockoutTimeAsync_WithExpiredLockout_ShouldReturnNull:");
        _output.WriteLine($"   User ID: {userId}");
        _output.WriteLine($"   Lockout End: {user.TotpLockoutEnd}");
        _output.WriteLine($"   Current Time: {DateTime.UtcNow}");

        // Act
        var result = await _userService.GetRemainingLockoutTimeAsync(userId);

        // Assert
        Assert.Null(result);
        _output.WriteLine($"   Result: {result}");
        _output.WriteLine($"   Expected: null (expired lockout)");
    }

    [Fact]
    public async Task GetUserByIdAsync_WithValidUserId_ShouldReturnUser()
    {
        // Arrange
        var userId = "test-user-id";
        var user = new ApplicationUser
        {
            Id = userId,
            Email = "test@example.com",
            UserName = "testuser"
        };

        _userManagerMock.Setup(x => x.FindByIdAsync(userId))
            .ReturnsAsync(user);

        _output.WriteLine($"✅ GetUserByIdAsync_WithValidUserId_ShouldReturnUser:");
        _output.WriteLine($"   User ID: {userId}");

        // Act
        var result = await _userService.GetUserByIdAsync(userId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(userId, result.Id);
        Assert.Equal("test@example.com", result.Email);
        Assert.Equal("testuser", result.UserName);

        _output.WriteLine($"   Result: {result?.UserName}");
        _output.WriteLine($"   Email: {result?.Email}");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetUserByIdAsync_WithInvalidUserId_ShouldReturnNull(string userId)
    {
        _output.WriteLine($"🔍 GetUserByIdAsync_WithInvalidUserId_ShouldReturnNull:");
        _output.WriteLine($"   User ID: '{userId}' (null/empty/whitespace)");

        // Act
        var result = await _userService.GetUserByIdAsync(userId);

        // Assert
        Assert.Null(result);
        _output.WriteLine($"   Result: {result}");
        _output.WriteLine($"   Expected: null (invalid user ID)");

        _userManagerMock.Verify(x => x.FindByIdAsync(It.IsAny<string>()), Times.Never);
    }
}