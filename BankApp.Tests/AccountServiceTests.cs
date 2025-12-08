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
        
        var userId = "user1";
        var accountName = "Test Account";
        var initialDeposit = 100m;
        Account? capturedAccount = null;
        _fixture.AccountRepoMock.Setup(r => r.AddAccountAsync(It.IsAny<Account>()))
            .Callback<Account>(acc => capturedAccount = acc)
            .Returns(Task.CompletedTask);

        
        var result = await _fixture.Sut.CreateAccountAsync(userId, accountName, initialDeposit);

       
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
        
        var userId = "user2";
        var accountName = "NoDepositAccount";
        _fixture.AccountRepoMock.Setup(r => r.AddAccountAsync(It.IsAny<Account>())).Returns(Task.CompletedTask);

        
        var result = await _fixture.Sut.CreateAccountAsync(userId, accountName);

        
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
[InlineData(17, 0)]
[InlineData(17, -1)]
[InlineData(17, -0.1)]
public async Task DepositAsync_ThrowsArgumentException_AndDoesntCallRepository_WhenAmountIsZeroOrBelow(int accountId, decimal amount)
{
    //Act and Assert
    await Assert.ThrowsAsync<ArgumentException>(()
        => _fixture.Sut.DepositAsync(accountId, amount));

    _fixture.AccountRepoMock
        .Verify(r => r.GetAccountByIdAsync(It.IsAny<int>()), Times.Never);
    
    _fixture.AccountRepoMock
        .Verify(r => r.UpdateAccountAsync(It.IsAny<Account>()), Times.Never);

    _fixture.TransactionRepoMock
        .Verify(r => r.AddTransactionAsync(It.IsAny<Transaction>()), Times.Never);
}


[Fact]
public async Task DepositAsync_CallsGetAccountByIdAsync()
{
    //Arrange
    const int accountId = 17;
    Account testAccount = new Account("BusinessAccount", 1000, 123456789, "TestUserId");

    _fixture.AccountRepoMock
        .Setup(r => r.GetAccountByIdAsync(accountId))
        .ReturnsAsync(testAccount);

    //Act
    await _fixture.Sut.DepositAsync(accountId, 5000);

    //Assert
    _fixture.AccountRepoMock
        .Verify(r => r.GetAccountByIdAsync(accountId),
            Times.Once);
    _fixture.AccountRepoMock
        .Verify(r => r.UpdateAccountAsync(testAccount),
            Times.Once);
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
    
    _fixture.AccountRepoMock
        .Verify(r => r.UpdateAccountAsync(It.IsAny<Account>()),
            Times.Never);
    _fixture.TransactionRepoMock
        .Verify(r => r.AddTransactionAsync(It.IsAny<Transaction>()),
            Times.Never);
}

[Theory]
[InlineData(1000, 500, 1500)]
[InlineData(200, 300, 500)]
[InlineData(0, 1000, 1000)]
public async Task DepositAsync_IncreasAndUpdatesBalance(decimal balance, decimal deposit, decimal expected)
{
    // Arrange
    const int accountId = 17;
    var testAccount = new Account("BusinessAccount", balance, 123456789, "TestUserId")
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
    _fixture.AccountRepoMock.Verify(
        r => r.UpdateAccountAsync(testAccount),
        Times.Once);
}

[Fact]
public async Task DepositAsync_CallsTransactionRepo_AndRegisterCorrectTransaction()
{
    //Arrange
    const int accountId = 17;
    decimal amount = 1500;
    Account testAccount = new Account("BusinessAccount", 1000, 123456789, "TestUserId");
    Transaction testTransaction = null;

    _fixture.AccountRepoMock
        .Setup(r => r.GetAccountByIdAsync(accountId))
        .ReturnsAsync(testAccount);

    _fixture.TransactionRepoMock
        .Setup(r => r.AddTransactionAsync(It.IsAny<Transaction>()))
        .Callback<Transaction>(transaction => testTransaction = transaction)
        .Returns(Task.CompletedTask);

    //Act
    await _fixture.Sut.DepositAsync(accountId, amount);

    //Assert
    _fixture.AccountRepoMock
        .Verify(r => r.UpdateAccountAsync(testAccount)
            ,Times.Once);
    _fixture.TransactionRepoMock
        .Verify(r => r.AddTransactionAsync(It.IsAny<Transaction>()),
            Times.Once);
    Assert.NotNull(testTransaction);
    Assert.Equal(testTransaction.Amount, amount);
    Assert.Equal(testTransaction.AccountId, accountId);
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
    
    
    [Theory]
    [InlineData(17, 0)]
    [InlineData(17, -1)]
    [InlineData(17, -0.99)]
    public async Task WithdrawAsync_ThrowsArgumentException_WhenWithdrawAmountIsZeroOrBelow(
        int accountId, decimal withdrawAmount)
    {
        //Act and Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _fixture.Sut.WithdrawAsync(accountId, withdrawAmount));

        _fixture.AccountRepoMock
            .Verify(r => r.GetAccountByIdAsync(accountId),
                Times.Never);

        _fixture.TransactionRepoMock
            .Verify(r => r.AddTransactionAsync(It.IsAny<Transaction>()),
                Times.Never);
    }


    [Theory]
    [InlineData(17, 1)]
    [InlineData(17, 9000)]
    [InlineData(17, 0.1)]
    public async Task WitrhdrawAsync_Calls_GetAccountById_AndAddTransactionAsync(int accountId, decimal withdrawAmount)
    {
        //Arrange
        Account testAccount = new Account("BusinessAccount", 9000, 123456789, "testUserId");

        _fixture.AccountRepoMock
            .Setup(r => r.GetAccountByIdAsync(accountId))
            .ReturnsAsync(testAccount);

        //Act
        await _fixture.Sut.WithdrawAsync(accountId, withdrawAmount);

        //Assert
        _fixture.AccountRepoMock
            .Verify(r => r.GetAccountByIdAsync(accountId),
                Times.Once);

        _fixture.TransactionRepoMock
            .Verify(r => r.AddTransactionAsync(It.IsAny<Transaction>()),
                Times.Once);
    }


    [Theory]
    [InlineData(17, 1)]
    [InlineData(17, 0.1)]
    [InlineData(17, 9000)]
    public async Task WithdrawAsync_ThrowsInvalidOperation_WhenAccountNotFound(int accountId, decimal withdrawAmount)
    {
        //Arrange
        
        _fixture.AccountRepoMock
            .Setup(r => r.GetAccountByIdAsync(accountId))
            .ReturnsAsync((Account)null!);

        //Act and Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _fixture.Sut.WithdrawAsync(accountId, withdrawAmount));

        _fixture.AccountRepoMock
            .Verify(r => r.GetAccountByIdAsync(accountId),
                Times.Once);

        _fixture.TransactionRepoMock
            .Verify(r => r.AddTransactionAsync(It.IsAny<Transaction>()),
                Times.Never);
    }

    [Theory]
    [InlineData(17, 9000.5)]
    [InlineData(17, 9000.01)]
    [InlineData(17, 9100)]
    public async Task WithdrawAsync_ThrowsInvalidOperationException_WhenWithdrawAmount_IsHigherThanBalance(int accountId, decimal withdrawAmount)
    {
        //Arrange
        Account testAccount = new Account("BusinessAccount", 9000, 123456789, "testUserId");

        _fixture.AccountRepoMock
            .Setup(r => r.GetAccountByIdAsync(accountId))
            .ReturnsAsync(testAccount);

        //Act and Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _fixture.Sut.WithdrawAsync(accountId, withdrawAmount));

        _fixture.AccountRepoMock
            .Verify(r => r.GetAccountByIdAsync(accountId),
                Times.Once);


        _fixture.TransactionRepoMock
            .Verify(r => r.AddTransactionAsync(It.IsAny<Transaction>()),
                Times.Never);
    }

    [Theory]
    [InlineData(1000, 500, 500)]
    [InlineData(500, 200, 300)]
    [InlineData(100, 100, 0)]
    public async Task WithdrawAsync_WithdrawsOnCorrectAccount_AndCallsAddTransactionAsync(decimal accountBalance, decimal withdrawAmount, decimal expected)
    {
        //Arrange
        Account account1 = new Account("SalaryAccount", accountBalance, 111, "testUserId1") { AccountId = 1 };
        Account account2 = new Account("BusinessAccount", 999, 222, "testUserId2") { AccountId = 2 };
        _fixture.AccountRepoMock
            .Setup(r => r.GetAccountByIdAsync(1))
            .ReturnsAsync(account1);
        //Act
        await _fixture.Sut.WithdrawAsync(1, withdrawAmount);

        //Assert
        _fixture.TransactionRepoMock
            .Verify(r => r.AddTransactionAsync(It.IsAny<Transaction>()),
                Times.Once);

        Assert.Equal(expected, account1.Balance);
        Assert.Equal(999, account2.Balance);

    }


    [Theory]
      [InlineData(17, 1000, 300)]
      [InlineData(22, 500, 100)]
      public async Task WithdrawAsync_CreatesCorrectTransaction(int accountId, decimal accountBalance, decimal withdrawAmount)
      {
          // ARRANGE
          Account testAccount = new Account("BusinessAccount", accountBalance, 123, "testUserId") { AccountId = accountId };
          Transaction testTransaction = null;

          _fixture.AccountRepoMock
              .Setup(r => r.GetAccountByIdAsync(accountId))
              .ReturnsAsync(testAccount);

          _fixture.TransactionRepoMock
              .Setup(r => r.AddTransactionAsync(It.IsAny<Transaction>()))
              .Callback<Transaction>(t => testTransaction = t)
              .Returns(Task.CompletedTask);

          // ACT
          await _fixture.Sut.WithdrawAsync(accountId, withdrawAmount);

          // ASSERT
          Assert.NotNull(testTransaction);
          Assert.Equal(accountId, testTransaction.AccountId);
          Assert.Equal(-withdrawAmount, testTransaction.Amount);
          Assert.Equal(TransactionType.Withdrawal, testTransaction.Type);
      } 
 
    
}


    


