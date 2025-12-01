using Bankapp.Repositories.Interfaces;
using Bankapp.Services;
using Bankapp.Services.Interfaces;
using Moq;

namespace BankApp.Tests;

public class AccountServiceFixture
{
    public Mock<IAccountRepository> AccountRepoMock { get;}
    public Mock<ITransactionRepository> TransactionRepoMock { get;}
    public IAccountService Sut { get; }

    public AccountServiceFixture()
    {
        AccountRepoMock = new Mock<IAccountRepository>();
        TransactionRepoMock = new Mock <ITransactionRepository>();
        Sut = new AccountService(AccountRepoMock.Object, TransactionRepoMock.Object);
    }
}