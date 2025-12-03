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
        _fixture.AccountRepoMock.Reset();
        _fixture.TransactionRepoMock.Reset();
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
    public async Task DepositAsync_ThrowsArgumentException_AndDoesntCallRepository_WhenAmountIsZeroOrBelow(decimal amount)
    {  
        //Arrange
        const int accountId = 17;
        
        //Act and Assert
        await Assert.ThrowsAsync<ArgumentException>(()   
            => _fixture.Sut.DepositAsync(accountId, amount));  
        
        _fixture.AccountRepoMock
            .Verify(r => r.GetAccountByIdAsync(It.IsAny<int>()), Times.Never);

        _fixture.TransactionRepoMock
            .Verify(r => r.AddTransactionAsync(It.IsAny<Transaction>()), Times.Never);
    }
    
    
    [Fact]
    public async Task DepositAsync_CallsGetAccountByIdAsync()
    {
        //Arrange
        const int accountId = 17;
        Account testAccount = new Account("BusinessAccount",1000, 123456789, "TestUserId");
       
        _fixture.AccountRepoMock
            .Setup(r => r.GetAccountByIdAsync(accountId))
            .ReturnsAsync(testAccount);
       
        //Act
        await _fixture.Sut.DepositAsync(accountId, 5000);
       
        //Assert
        _fixture.AccountRepoMock.Verify(r => r.GetAccountByIdAsync(accountId),Times.Once);
    }
    
    
    [Fact]
    public async Task DepositAsync_ThrowsInvalidOperationException_AndDoesntCallTransactionRep_WhenAccountNotFound()
    {
        //Arrange
        const int accountId = 17;
        Decimal amount = 5000;
        _fixture.AccountRepoMock
            .Setup(x => x.GetAccountByIdAsync(accountId))
            .ReturnsAsync((Account)null!);
        
        //Act and Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _fixture.Sut.DepositAsync(accountId, amount));
        
        _fixture.TransactionRepoMock
            .Verify(r => r.AddTransactionAsync(It.IsAny<Transaction>()), Times.Never);
    }

    [Theory]
    [InlineData(1000, 500, 1500)]
    [InlineData(200, 300, 500)]
    [InlineData(0, 1000, 1000)]
    public async Task DepositAsync_IncreaseBalance(decimal balance, decimal deposit, decimal expected)
    {
        // Arrange
        const int accountId = 17;
        var testAccount = new Account("BusinessAccount",balance, 123456789, "TestUserId")
        {
            AccountId = accountId
        };
        
        _fixture.AccountRepoMock
            .Setup(r => r.GetAccountByIdAsync(accountId))
            .ReturnsAsync(testAccount);
        
         // Act
        await _fixture.Sut.DepositAsync(accountId, deposit);

        // Assert
        Assert.Equal(expected, testAccount.Balance);
    }

    [Fact]
    public async Task DepositAsync_CallsTransactionRepo_AndRegisterCorrectTransaction()
    {
        //Arrange
        const int AccountId = 17;
        decimal amount = 1000;
        Account testAccount = new Account("BusinessAccount", 1000, 123456789, "TestUserId");
        Transaction testTransaction = null;
        
        _fixture.AccountRepoMock
            .Setup(r => r.GetAccountByIdAsync(AccountId))
            .ReturnsAsync(testAccount);

        _fixture.TransactionRepoMock
            .Setup(r => r.AddTransactionAsync(It.IsAny<Transaction>()))
            .Callback<Transaction>(transaction => testTransaction = transaction)
            .Returns(Task.CompletedTask);

        //Act
        await _fixture.Sut.DepositAsync(AccountId, amount);
        
        //Assert
        Assert.NotNull(testTransaction);
        Assert.Equal(testTransaction.Amount, amount);
        Assert.Equal(testTransaction.AccountId, AccountId);
        Assert.Equal(TransactionType.Deposit, testTransaction.Type);
    }


    



    [Fact]
    public async Task GetAccountsForUserAsync_ShouldReturnAccounts_WhenAccountsExist()
    {
        // Arrange
        const string userId = "user123";
        var expectedAccounts = new List<Account>
    {
        new("Checking", 1000m, 123, userId) { AccountId = 1 },
        new("Savings", 5000m, 456, userId) { AccountId = 2 }
    };

        _fixture.AccountRepoMock
            .Setup(repo => repo.GetAccountsByUserIdAsync(userId))
            .ReturnsAsync(expectedAccounts);

        // Act
        var result = await _fixture.Sut.GetAccountsForUserAsync(userId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedAccounts.Count, result.Count());

        _fixture.AccountRepoMock.Verify(
            repo => repo.GetAccountsByUserIdAsync(userId),
            Times.Once);
    }

    [Fact]
    public async Task GetAccountsForUserAsync_ShouldReturnEmptyList_WhenNoAccountsExist()
    {
        // Arrange
        const string userId = "userid";
        var expectedAccounts = new List<Account>();

        _fixture.AccountRepoMock
            .Setup(repo => repo.GetAccountsByUserIdAsync(userId))
            .ReturnsAsync(expectedAccounts);

        // Act
        var result = await _fixture.Sut.GetAccountsForUserAsync(userId);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result); 

        _fixture.AccountRepoMock.Verify(
            repo => repo.GetAccountsByUserIdAsync(userId),
            Times.Once);
    }

    [Fact]
    public async Task GetAccountByIdAsync_ShouldReturnAccount_WhenAccountExists()
    {
        // Arrange
        const int accountId = 99;
        var expectedAccount = new Account("Checking", 1000m, 123, "user123") { AccountId = accountId };

        _fixture.AccountRepoMock
            .Setup(repo => repo.GetAccountByIdAsync(accountId))
            .ReturnsAsync(expectedAccount);

        // Act
        var result = await _fixture.Sut.GetAccountByIdAsync(accountId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(accountId, result.AccountId);

        _fixture.AccountRepoMock.Verify(
            repo => repo.GetAccountByIdAsync(accountId),
            Times.Once);
    }

    [Fact]
    public async Task GetAccountByIdAsync_ShouldReturnNull_WhenAccountDoesNotExist()
    {
        // Arrange
        const int accountId = 99;
        Account? expectedAccount = null;

        _fixture.AccountRepoMock
            .Setup(repo => repo.GetAccountByIdAsync(accountId))!
            .ReturnsAsync((Account?)expectedAccount);

        // Act
        var result = await _fixture.Sut.GetAccountByIdAsync(accountId);

        // Assert
        Assert.Null(result);

        _fixture.AccountRepoMock.Verify(
            repo => repo.GetAccountByIdAsync(accountId),
            Times.Once);
    }

    [Fact]
    public async Task GetTransactionsAsync_ShouldReturnTransactions_WhenTransactionsExist()
    {
        // Arrange
        const int accountId = 101;
        var expectedTransactions = new List<Transaction>
    {
        new(accountId, 50.00m, DateTime.Now.AddDays(-1), TransactionType.Deposit),
        new(accountId, -20.00m, DateTime.Now, TransactionType.Withdrawal),
        new(accountId, 10.00m, DateTime.Now, TransactionType.Deposit)
    };

        _fixture.TransactionRepoMock
            .Setup(repo => repo.GetTransactionsByAccountIdAsync(accountId))
            .ReturnsAsync(expectedTransactions);

        // Act
        var result = await _fixture.Sut.GetTransactionsAsync(accountId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedTransactions.Count, result.Count());

        _fixture.TransactionRepoMock.Verify(
            repo => repo.GetTransactionsByAccountIdAsync(accountId),
            Times.Once);
    }

    [Fact]
    public async Task GetTransactionsAsync_ShouldReturnEmptyList_WhenNoTransactionsExist()
    {
        // Arrange
        const int accountId = 88;
        var expectedTransactions = new List<Transaction>();

        _fixture.TransactionRepoMock
            .Setup(repo => repo.GetTransactionsByAccountIdAsync(accountId))
            .ReturnsAsync(expectedTransactions);

        // Act
        var result = await _fixture.Sut.GetTransactionsAsync(accountId);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result); 

        _fixture.TransactionRepoMock.Verify(
            repo => repo.GetTransactionsByAccountIdAsync(accountId),
            Times.Once);
    }
}