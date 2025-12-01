using Bankapp.Repositories;
using Bankapp.Services;
using Moq;

namespace BankApp.Tests;

public class AccountServiceFixture
{
    public Mock<IAccountRepository> AccountRepoMock { get;}
    public Mock<ITransactionRepository> TransactionRepoMock { get;}
    public AccountService Sut { get; }

    public AccountServiceFixture()
    {
        AccountRepoMock = new Mock<IAccountRepository>();
        TransactionRepoMock = new Mock <ITransactionRepository>();
        Sut = new AccountService(AccountRepoMock.Object, TransactionRepoMock.Object);
    }
}