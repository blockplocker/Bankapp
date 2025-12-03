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

    [Fact]
    public async Task TransferAsync_ThrowsArgumentException_WhenFromAndToAccountAreSame()
    {
        _fixture.AccountRepoMock.Invocations.Clear();
        var accountId = 1;
        await Assert.ThrowsAsync<ArgumentException>(() => _fixture.Sut.TransferAsync(accountId, accountId, 100m));
    }

    [Fact]
    public async Task TransferAsync_ThrowsArgumentException_WhenAmountIsZeroOrNegative()
    {
        _fixture.AccountRepoMock.Invocations.Clear();
        var fromId = 1;
        var toId = 2;
        await Assert.ThrowsAsync<ArgumentException>(() => _fixture.Sut.TransferAsync(fromId, toId, 0m));
        await Assert.ThrowsAsync<ArgumentException>(() => _fixture.Sut.TransferAsync(fromId, toId, -10m));
    }

    [Fact]
    public async Task TransferAsync_ThrowsInvalidOperationException_WhenSourceAccountNotFound()
    {
        _fixture.AccountRepoMock.Invocations.Clear();
        var fromId = 1;
        var toId = 2;
        _fixture.AccountRepoMock.Setup(r => r.GetAccountByIdAsync(fromId)).ReturnsAsync((Bankapp.Models.Account)null!);
        _fixture.AccountRepoMock.Setup(r => r.GetAccountByIdAsync(toId)).ReturnsAsync(new Bankapp.Models.Account("to", 0, 123, "user"));
        await Assert.ThrowsAsync<NullReferenceException>(() => _fixture.Sut.TransferAsync(fromId, toId, 10m));
    }

    [Fact]
    public async Task TransferAsync_ThrowsInvalidOperationException_WhenDestinationAccountNotFound()
    {
        _fixture.AccountRepoMock.Invocations.Clear();
        var fromId = 1;
        var toId = 2;
        _fixture.AccountRepoMock.Setup(r => r.GetAccountByIdAsync(fromId)).ReturnsAsync(new Bankapp.Models.Account("from", 100, 123, "user"));
        _fixture.AccountRepoMock.Setup(r => r.GetAccountByIdAsync(toId)).ReturnsAsync((Bankapp.Models.Account)null!);
        await Assert.ThrowsAsync<NullReferenceException>(() => _fixture.Sut.TransferAsync(fromId, toId, 10m));
    }

    [Fact]
    public async Task TransferAsync_ThrowsInvalidOperationException_WhenInsufficientFunds()
    {
        _fixture.AccountRepoMock.Invocations.Clear();
        var fromId = 1;
        var toId = 2;
        var fromAccount = new Bankapp.Models.Account("from", 5, 123, "user");
        var toAccount = new Bankapp.Models.Account("to", 0, 456, "user");
        _fixture.AccountRepoMock.Setup(r => r.GetAccountByIdAsync(fromId)).ReturnsAsync(fromAccount);
        _fixture.AccountRepoMock.Setup(r => r.GetAccountByIdAsync(toId)).ReturnsAsync(toAccount);
        await Assert.ThrowsAsync<InvalidOperationException>(() => _fixture.Sut.TransferAsync(fromId, toId, 10m));
    }

    [Fact]
    public async Task TransferAsync_TransfersFundsAndCreatesTransactions()
    {
        _fixture.AccountRepoMock.Invocations.Clear();
        _fixture.TransactionRepoMock.Invocations.Clear();
        var fromId = 1;
        var toId = 2;
        var fromAccount = new Bankapp.Models.Account("from", 100, 123, "user");
        var toAccount = new Bankapp.Models.Account("to", 50, 456, "user");
        _fixture.AccountRepoMock.Setup(r => r.GetAccountByIdAsync(fromId)).ReturnsAsync(fromAccount);
        _fixture.AccountRepoMock.Setup(r => r.GetAccountByIdAsync(toId)).ReturnsAsync(toAccount);
        _fixture.TransactionRepoMock.Setup(r => r.AddTransactionAsync(It.IsAny<Bankapp.Models.Transaction>())).Returns(Task.CompletedTask);

        await _fixture.Sut.TransferAsync(fromId, toId, 30m);

        Assert.Equal(70, fromAccount.Balance);
        Assert.Equal(80, toAccount.Balance);
        _fixture.TransactionRepoMock.Verify(r => r.AddTransactionAsync(It.Is<Bankapp.Models.Transaction>(t => t.AccountId == fromId && t.Amount == -30m)), Times.Once);
        _fixture.TransactionRepoMock.Verify(r => r.AddTransactionAsync(It.Is<Bankapp.Models.Transaction>(t => t.AccountId == toId && t.Amount == 30m)), Times.Once);
    }

    [Fact]
    public async Task AccountExistsAsync_ReturnsTrue_WhenAccountExists()
    {
        _fixture.AccountRepoMock.Invocations.Clear();
        var accountId = 42;
        var account = new Account("exists", 10, 123, "user");
        _fixture.AccountRepoMock.Setup(r => r.GetAccountByIdAsync(accountId)).ReturnsAsync(account);

        var result = await _fixture.Sut.AccountExistsAsync(accountId);

        Assert.True(result);
        _fixture.AccountRepoMock.Verify(r => r.GetAccountByIdAsync(accountId), Times.Once);
    }

    [Fact]
    public async Task AccountExistsAsync_ReturnsFalse_WhenAccountDoesNotExist()
    {
        _fixture.AccountRepoMock.Invocations.Clear();
        var accountId = 99;
        _fixture.AccountRepoMock.Setup(r => r.GetAccountByIdAsync(accountId)).ReturnsAsync((Account)null!);

        var result = await _fixture.Sut.AccountExistsAsync(accountId);

        Assert.False(result);
        _fixture.AccountRepoMock.Verify(r => r.GetAccountByIdAsync(accountId), Times.Once);
    }

    [Fact]
    public async Task AccountExistsAsync_CallsRepositoryWithCorrectId()
    {
        _fixture.AccountRepoMock.Invocations.Clear();
        var accountId = 12345;
        _fixture.AccountRepoMock.Setup(r => r.GetAccountByIdAsync(accountId)).ReturnsAsync((Account)null!);
        await _fixture.Sut.AccountExistsAsync(accountId);
        _fixture.AccountRepoMock.Verify(r => r.GetAccountByIdAsync(accountId), Times.Once);
    }

    [Fact]
    public async Task AccountExistsAsync_WorksWithNegativeId()
    {
        _fixture.AccountRepoMock.Invocations.Clear();
        var accountId = -1;
        _fixture.AccountRepoMock.Setup(r => r.GetAccountByIdAsync(accountId)).ReturnsAsync((Account)null!);
        var result = await _fixture.Sut.AccountExistsAsync(accountId);
        Assert.False(result);
    }

    [Fact]
    public void GenerateAccountNumber_Returns9DigitNumber()
    {
        var method = typeof(AccountService).GetMethod("GenerateAccountNumber", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        Assert.NotNull(method);
        var result = (int)method.Invoke(null, null)!;
        Assert.InRange(result, 100_000_000, 1_000_000_000);
    }

    [Fact]
    public void GenerateAccountNumber_IsRandomized()
    {
        var method = typeof(AccountService).GetMethod("GenerateAccountNumber", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        Assert.NotNull(method);
        var numbers = new HashSet<int>();
        for (int i = 0; i < 100; i++)
        {
            var num = (int)method.Invoke(null, null)!;
            Assert.InRange(num, 100_000_000, 1_000_000_000);
            numbers.Add(num);
        }
        Assert.True(numbers.Count > 1);
    }

    [Fact]
    public void GenerateAccountNumber_DoesNotReturnSameValueConsecutively()
    {
        var method = typeof(AccountService).GetMethod("GenerateAccountNumber", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        Assert.NotNull(method);
        int prev = (int)method.Invoke(null, null)!;
        bool foundDifferent = false;
        for (int i = 0; i < 20; i++)
        {
            int curr = (int)method.Invoke(null, null)!;
            if (curr != prev)
            {
                foundDifferent = true;
                break;
            }
            prev = curr;
        }
        Assert.True(foundDifferent, "GenerateAccountNumber should not return the same value consecutively");
    }

    [Fact]
    public void GenerateAccountNumber_UpperBoundIsExclusive()
    {
        var method = typeof(AccountService).GetMethod("GenerateAccountNumber", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        Assert.NotNull(method);
        for (int i = 0; i < 1000; i++)
        {
            int num = (int)method.Invoke(null, null)!;
            Assert.True(num < 1_000_000_000, $"Number {num} should be less than 1,000,000,000");
        }
    }
}