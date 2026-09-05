using System;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Buy2.Application.Common.Interfaces;
using Buy2.Infrastructure.Persistence;
using Buy2.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Xunit;

namespace Buy2.Domain.Tests.Transactions;

public class ExecutionStrategyTransactionReproductionTests
{
    private class FakeDbTransaction : DbTransaction
    {
        public FakeDbTransaction(DbConnection connection, IsolationLevel isolationLevel)
        {
            DbConnection = connection;
            IsolationLevel = isolationLevel;
        }

        public override IsolationLevel IsolationLevel { get; }
        protected override DbConnection DbConnection { get; }
        public override void Commit() { }
        public override void Rollback() { }
    }

    private class FakeDbCommand : DbCommand
    {
        [System.Diagnostics.CodeAnalysis.AllowNull]
        public override string CommandText { get; set; } = string.Empty;
        public override int CommandTimeout { get; set; }
        public override CommandType CommandType { get; set; }
        public override bool DesignTimeVisible { get; set; }
        public override UpdateRowSource UpdatedRowSource { get; set; }
        protected override DbConnection? DbConnection { get; set; }
        protected override DbParameterCollection DbParameterCollection => throw new NotImplementedException();
        protected override DbTransaction? DbTransaction { get; set; }
        public override void Cancel() { }
        public override int ExecuteNonQuery() => 0;
        public override object? ExecuteScalar() => null;
        public override void Prepare() { }
        protected override DbParameter CreateDbParameter() => throw new NotImplementedException();
        protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior) => throw new NotImplementedException();
    }

    private class FakeDbConnection : DbConnection
    {
        private ConnectionState _state = ConnectionState.Open;
        public override ConnectionState State => _state;
        public override string Database => "FakeDb";
        public override string DataSource => "FakeSource";
        public override string ServerVersion => "16.0.1000";
        [System.Diagnostics.CodeAnalysis.AllowNull]
        public override string ConnectionString { get; set; } = "Server=fake;Database=FakeDb;";
        public override void Open() { _state = ConnectionState.Open; }
        public override Task OpenAsync(CancellationToken cancellationToken)
        {
            _state = ConnectionState.Open;
            return Task.CompletedTask;
        }
        public override void Close() { _state = ConnectionState.Closed; }
        public override void ChangeDatabase(string databaseName) { }
        protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel) => new FakeDbTransaction(this, isolationLevel);
        protected override ValueTask<DbTransaction> BeginDbTransactionAsync(IsolationLevel isolationLevel, CancellationToken cancellationToken) =>
            new ValueTask<DbTransaction>(new FakeDbTransaction(this, isolationLevel));
        protected override DbCommand CreateDbCommand() => new FakeDbCommand();
    }

    public class TestRetryingExecutionStrategy : ExecutionStrategy
    {
        public TestRetryingExecutionStrategy(ExecutionStrategyDependencies dependencies)
            : base(dependencies, 5, TimeSpan.FromSeconds(10))
        {
        }

        public override bool RetriesOnFailure => true;

        protected override bool ShouldRetryOn(Exception exception) => true;
    }

    public class TestRetryingExecutionStrategyFactory : IExecutionStrategyFactory
    {
        private readonly ExecutionStrategyDependencies _dependencies;

        public TestRetryingExecutionStrategyFactory(ExecutionStrategyDependencies dependencies)
        {
            _dependencies = dependencies;
        }

        public IExecutionStrategy Create() => new TestRetryingExecutionStrategy(_dependencies);
    }

    private static Buy2DbContext CreateDbContextWithRetryingExecutionStrategy()
    {
        var options = new DbContextOptionsBuilder<Buy2DbContext>()
            .UseSqlServer(new FakeDbConnection(), sqlOptions =>
            {
                sqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(10),
                    errorNumbersToAdd: null);
            })
            .ReplaceService<IExecutionStrategyFactory, TestRetryingExecutionStrategyFactory>()
            .Options;

        return new Buy2DbContext(options);
    }

    [Fact]
    public async Task UnitOfWork_BeginTransactionAsync_WithSqlServerRetryingExecutionStrategy_ShouldSucceedWithoutThrowing()
    {
        // Arrange
        using var context = CreateDbContextWithRetryingExecutionStrategy();
        IUnitOfWork unitOfWork = new UnitOfWork(context);

        // Act & Assert
        // With ExecuteInTransactionAsync wrapping operations inside DbContext.Database.CreateExecutionStrategy().ExecuteAsync(...),
        // transactions succeed under SqlServerRetryingExecutionStrategy without throwing InvalidOperationException.
        await unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            await unitOfWork.SaveChangesAsync(CancellationToken.None);
        }, CancellationToken.None);

        var result = await unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            await unitOfWork.SaveChangesAsync(CancellationToken.None);
            return 42;
        }, CancellationToken.None);

        Assert.Equal(42, result);
    }

    [Fact]
    public async Task UnitOfWork_ExecuteInTransactionAsync_ExceptionInsideOperation_ThrowsAndRollsBack()
    {
        // Arrange
        using var context = CreateDbContextWithRetryingExecutionStrategy();
        IUnitOfWork unitOfWork = new UnitOfWork(context);

        // Act & Assert
        var ex = await Assert.ThrowsAnyAsync<Exception>(async () =>
        {
            await unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                await Task.Yield();
                throw new InvalidOperationException("Operation failure");
            }, CancellationToken.None);
        });

        Assert.True(ex is InvalidOperationException || ex.InnerException is InvalidOperationException);
    }
}
