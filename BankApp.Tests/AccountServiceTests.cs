using System.Threading.Tasks;
using Bankapp.Models;
using Bankapp.Services;
using Microsoft.Identity.Client;
using Moq;
using NuGet.ContentModel;
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
    
    
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-0.1)]
    public async Task DepositAsync_ThrowsArgumentException_WhenAmountIsZeroOrBelow(decimal amount)
    {  
        //Act and Assert
        await Assert.ThrowsAsync<ArgumentException>(()   
            => _fixture.Sut.DepositAsync(1, amount));  
    }
    
    
    [Fact]
    public async Task DepositAsync_ThrowsInvalidOperationException_When_AccountNotFound()
    {
        //Arrange
        _fixture.AccountRepoMock
            .Setup(x => x.GetAccountByIdAsync(1))
            .ReturnsAsync((Account)null!);
        
        //Act and Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _fixture.Sut.DepositAsync(1, 5000));
    }

    
    [Fact]
    public async Task DepositAsync_ShouldCall_AddAccountByIdAsync()
    {
         //Arrange
        _fixture.Sut.DepositAsync(1,5000);
        _fixture.AccountRepoMock .Setup(r => r.GetAccountByIdAsync(It.IsAny<int>()));
        
        //Act and Assert
        _fixture.AccountRepoMock.Verify(r => r.GetAccountByIdAsync(It.IsAny<int>()), Times.Once);
    }
    
    [Theory]
    [InlineData(1000, 500, 1500)]
    [InlineData(200, 300, 500)]
    [InlineData(0, 1000, 1000)]
    public async Task DepositAsync_IncreaseBalance(decimal balance, decimal deposit, decimal expected)
    {
        // Arrange
        
        var testAccount = new Account("BusinessAccount",balance, 123456789, "TestUserId");
        
        _fixture.AccountRepoMock
            .Setup(r => r.GetAccountByIdAsync(1))
            .ReturnsAsync(testAccount);
        
         // Act
        await _fixture.Sut.DepositAsync(1, deposit);

        // Assert
        Assert.Equal(expected, testAccount.Balance);
    }


    


}