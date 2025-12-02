using System.Threading.Tasks;
using Bankapp.Models;
using Bankapp.Services;
using Moq;
using Xunit;

namespace BankApp.Tests;

public class AccountServiceTests : IClassFixture<AccountServiceFixture>
{
    private readonly AccountServiceFixture _fixture;

    public AccountServiceTests(AccountServiceFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task CreateAccountAsync_CreatesAccountWithCorrectProperties_AndCallsRepository()
    {
        _fixture.AccountRepoMock.Invocations.Clear();
        // Arrange
        var userId = "user1";
        var accountName = "Test Account";
        var initialDeposit = 100m;
        Account? capturedAccount = null;
        _fixture.AccountRepoMock.Setup(r => r.AddAccountAsync(It.IsAny<Account>()))
            .Callback<Account>(acc => capturedAccount = acc)
            .Returns(Task.CompletedTask);

        // Act
        var result = await _fixture.Sut.CreateAccountAsync(userId, accountName, initialDeposit);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(accountName, result.AccountName);
        Assert.Equal(initialDeposit, result.Balance);
        Assert.Equal(userId, result.UserId);
        Assert.True(result.AccountNumber >= 100_000_000 && result.AccountNumber < 1_000_000_000);
        Assert.Empty(result.Transactions);
        _fixture.AccountRepoMock.Verify(r => r.AddAccountAsync(It.IsAny<Account>()), Times.Once);
        Assert.Same(result, capturedAccount);
    }

    [Fact]
    public async Task CreateAccountAsync_DefaultsInitialDepositToZero()
    {
        _fixture.AccountRepoMock.Invocations.Clear();
        // Arrange
        var userId = "user2";
        var accountName = "NoDepositAccount";
        _fixture.AccountRepoMock.Setup(r => r.AddAccountAsync(It.IsAny<Account>())).Returns(Task.CompletedTask);

        // Act
        var result = await _fixture.Sut.CreateAccountAsync(userId, accountName);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(0m, result.Balance);
        Assert.Equal(accountName, result.AccountName);
        Assert.Equal(userId, result.UserId);
        _fixture.AccountRepoMock.Verify(r => r.AddAccountAsync(It.IsAny<Account>()), Times.Once);
    }

    [Fact]
    public async Task CreateAccountAsync_ThrowsArgumentException_WhenAccountNameIsNullOrEmpty()
    {
        _fixture.AccountRepoMock.Invocations.Clear();
        var userId = "user3";
        await Assert.ThrowsAsync<ArgumentException>(() => _fixture.Sut.CreateAccountAsync(userId, null!));
        await Assert.ThrowsAsync<ArgumentException>(() => _fixture.Sut.CreateAccountAsync(userId, ""));
    }

    [Fact]
    public async Task CreateAccountAsync_ThrowsArgumentException_WhenUserIdIsNullOrEmpty()
    {
        _fixture.AccountRepoMock.Invocations.Clear();
        var accountName = "TestAccount";
        await Assert.ThrowsAsync<ArgumentException>(() => _fixture.Sut.CreateAccountAsync(null!, accountName));
        await Assert.ThrowsAsync<ArgumentException>(() => _fixture.Sut.CreateAccountAsync("", accountName));
    }

    [Fact]
    public async Task CreateAccountAsync_ThrowsArgumentException_WhenInitialDepositIsNegative()
    {
        _fixture.AccountRepoMock.Invocations.Clear();
        var userId = "user4";
        var accountName = "NegativeDepositAccount";
        await Assert.ThrowsAsync<ArgumentException>(() => _fixture.Sut.CreateAccountAsync(userId, accountName, -10m));
    }

    [Fact]
    public async Task CreateAccountAsync_CallsRepositoryWithNewAccountInstance()
    {
        _fixture.AccountRepoMock.Invocations.Clear();
        var userId = "user5";
        var accountName = "RepoCallAccount";
        _fixture.AccountRepoMock.Setup(r => r.AddAccountAsync(It.IsAny<Account>())).Returns(Task.CompletedTask);

        var result = await _fixture.Sut.CreateAccountAsync(userId, accountName, 50m);

        _fixture.AccountRepoMock.Verify(r => r.AddAccountAsync(It.Is<Account>(a =>
            a.AccountName == accountName &&
            a.UserId == userId &&
            a.Balance == 50m
        )), Times.Once);
    }
}