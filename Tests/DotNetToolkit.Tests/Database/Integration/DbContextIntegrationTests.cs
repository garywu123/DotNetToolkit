#region License

// Author:      Gary Wu
// Project:     DotNetToolkit Integration Tests
// Date:        December 1, 2025
// Description: Integration tests for DbContext to verify command execution,
//              query operations, and stored procedure calls against SQL Server LocalDB.

#endregion

using System.Data;
using DotNetToolkit.Database.Abstractions;
using DotNetToolkit.Database.Configuration;
using DotNetToolkit.Database.Services;
using DotNetToolkit.Tests.Database.Extensions;
using DotNetToolkit.Tests.Database.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DotNetToolkit.Tests.Database.Integration;

/// <summary>
/// Integration tests for <see cref="DbContext"/> using SQL Server LocalDB.
/// </summary>
/// <remarks>
/// These tests verify command execution, query operations, and stored procedure interactions
/// against a real SQL Server LocalDB instance.
/// <code><![CDATA[
/// // Example test execution:
/// dotnet test --filter "FullyQualifiedName~DbContextIntegrationTests"
/// ]]></code>
/// </remarks>
[Collection("Database")]
public class DbContextIntegrationTests : IAsyncLifetime
{
    private readonly DatabaseFixture _fixture;
    private IDbContext _dbContext = null!;
    private IServiceProvider _serviceProvider = null!;

    /// <summary>
    /// Initializes a new instance of the <see cref="DbContextIntegrationTests"/> class.
    /// </summary>
    /// <param name="fixture">The shared database fixture.</param>
    public DbContextIntegrationTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    /// <summary>
    /// Sets up the test by resetting the database and creating a DbContext instance.
    /// </summary>
    /// <returns>A task representing the asynchronous initialization.</returns>
    public async Task InitializeAsync()
    {
        await _fixture.ResetDatabaseAsync();

        var services = new ServiceCollection();
        
        services.AddLogging(builder => builder.AddConsole());
        
        var settings = new DatabaseSettings
        {
            ProviderName = "Microsoft.Data.SqlClient",
            ConnectionString = _fixture.ConnectionString,
            CommandTimeoutSeconds = 30
        };
        
        services.AddSingleton(Options.Create(settings));
        services.AddSingleton<IDbConnectionFactory, DbConnectionFactory>();
        services.AddScoped<IDbContext, DbContext>();
        
        _serviceProvider = services.BuildServiceProvider();
        _dbContext = _serviceProvider.GetRequiredService<IDbContext>();
    }

    /// <summary>
    /// Cleans up after each test.
    /// </summary>
    /// <returns>A task representing the asynchronous cleanup.</returns>
    public Task DisposeAsync()
    {
        _dbContext?.Dispose();
        (_serviceProvider as IDisposable)?.Dispose();
        return Task.CompletedTask;
    }

    #region CreateCommand Tests

    /// <summary>
    /// Verifies that CreateCommand with stored procedure name returns a valid command wrapper.
    /// </summary>
    [Fact]
    public void CreateCommand_WithStoredProcedureName_ShouldReturnValidWrapper()
    {
        // Act
        var command = _dbContext.CreateCommand("usp_GetAllUsers");

        // Assert
        Assert.NotNull(command);
        Assert.Equal("usp_GetAllUsers", command.CommandText);
        Assert.Equal(CommandType.StoredProcedure, command.CommandType);
    }

    /// <summary>
    /// Verifies that CreateCommand with command text and type returns a valid command wrapper.
    /// </summary>
    [Fact]
    public void CreateCommand_WithCommandTextAndType_ShouldReturnValidWrapper()
    {
        // Act
        var command = _dbContext.CreateCommand("SELECT * FROM Users", CommandType.Text);

        // Assert
        Assert.NotNull(command);
        Assert.Equal("SELECT * FROM Users", command.CommandText);
        Assert.Equal(CommandType.Text, command.CommandType);
    }

    #endregion

    #region ExecuteQueryAsync Tests

    /// <summary>
    /// Verifies that ExecuteQueryAsync can retrieve all users.
    /// </summary>
    [Fact]
    public async Task ExecuteQueryAsync_GetAllUsers_ShouldReturnUsers()
    {
        // Arrange
        var command = _dbContext.CreateCommand("usp_GetAllUsers");

        // Act
        var users = await _dbContext.ExecuteQueryAsync<UserDto>(command);

        // Assert
        Assert.NotNull(users);
        Assert.True(users.Count >= 3, "Expected at least 3 seeded users");
        Assert.All(users, user =>
        {
            Assert.True(user.UserId > 0);
            Assert.False(string.IsNullOrEmpty(user.Username));
            Assert.False(string.IsNullOrEmpty(user.Email));
        });
    }

    /// <summary>
    /// Verifies that ExecuteQueryAsync can retrieve a user by ID.
    /// </summary>
    [Fact]
    public async Task ExecuteQueryAsync_GetUserById_ShouldReturnSingleUser()
    {
        // Arrange
        var command = _dbContext.CreateCommand("usp_GetUserById");
        command.AddParameter("@UserId", 1);

        // Act
        var users = await _dbContext.ExecuteQueryAsync<UserDto>(command);

        // Assert
        Assert.NotNull(users);
        Assert.Single(users);
        Assert.Equal(1, users[0].UserId);
        Assert.Equal("jdoe", users[0].Username);
    }

    /// <summary>
    /// Verifies that ExecuteQueryAsync returns empty list for non-existent user.
    /// </summary>
    [Fact]
    public async Task ExecuteQueryAsync_GetNonExistentUser_ShouldReturnEmptyList()
    {
        // Arrange
        var command = _dbContext.CreateCommand("usp_GetUserById");
        command.AddParameter("@UserId", 99999);

        // Act
        var users = await _dbContext.ExecuteQueryAsync<UserDto>(command);

        // Assert
        Assert.NotNull(users);
        Assert.Empty(users);
    }

    /// <summary>
    /// Verifies that ExecuteQueryAsync can retrieve products by category.
    /// </summary>
    [Fact]
    public async Task ExecuteQueryAsync_GetProductsByCategory_ShouldReturnProducts()
    {
        // Arrange
        var command = _dbContext.CreateCommand("usp_GetProductsByCategory");
        command.AddParameter("@CategoryId", 1); // Electronics

        // Act
        var products = await _dbContext.ExecuteQueryAsync<ProductDto>(command);

        // Assert
        Assert.NotNull(products);
        Assert.True(products.Count >= 3, "Expected at least 3 electronics products");
        Assert.All(products, product =>
        {
            Assert.Equal(1, product.CategoryId);
            Assert.Equal("Electronics", product.CategoryName);
        });
    }

    /// <summary>
    /// Verifies that ExecuteQueryAsync can retrieve orders by user ID with joins.
    /// </summary>
    [Fact]
    public async Task ExecuteQueryAsync_GetOrdersByUserId_ShouldReturnOrdersWithUserInfo()
    {
        // Arrange - First create an order
        var createOrderCommand = _dbContext.CreateCommand("usp_CreateOrder");
        createOrderCommand.AddParameter("@UserId", 1);
        createOrderCommand.AddParameter("@Status", "Pending");
        var outputParam = createOrderCommand.AddOutputParameter("@OrderId", DbType.Int32);
        await _dbContext.ExecuteNonQueryAsync(createOrderCommand);

        var command = _dbContext.CreateCommand("usp_GetOrdersByUserId");
        command.AddParameter("@UserId", 1);

        // Act
        var orders = await _dbContext.ExecuteQueryAsync<OrderDto>(command);

        // Assert
        Assert.NotNull(orders);
        Assert.True(orders.Count >= 1);
        Assert.All(orders, order =>
        {
            Assert.Equal(1, order.UserId);
            Assert.Equal("jdoe", order.Username);
        });
    }

    #endregion

    #region ExecuteNonQueryAsync Tests

    /// <summary>
    /// Verifies that ExecuteNonQueryAsync can create a new user.
    /// </summary>
    [Fact]
    public async Task ExecuteNonQueryAsync_CreateUser_ShouldReturnUserId()
    {
        // Arrange
        var command = _dbContext.CreateCommand("usp_CreateUser");
        command.AddParameter("@Username", "testuser");
        command.AddParameter("@Email", "test@example.com");
        command.AddParameter("@FirstName", "Test");
        command.AddParameter("@LastName", "User");
        var outputParam = command.AddOutputParameter("@UserId", DbType.Int32);

        // Act
        var result = await _dbContext.ExecuteNonQueryAsync(command);

        // Assert
        Assert.True(result >= 0);
        
        // Verify user was created
        var verifyCommand = _dbContext.CreateCommand("usp_GetUserById");
        verifyCommand.AddParameter("@UserId", outputParam.Value);
        var users = await _dbContext.ExecuteQueryAsync<UserDto>(verifyCommand);
        
        Assert.Single(users);
        Assert.Equal("testuser", users[0].Username);
        Assert.Equal("test@example.com", users[0].Email);
    }

    /// <summary>
    /// Verifies that ExecuteNonQueryAsync can update a user.
    /// </summary>
    [Fact]
    public async Task ExecuteNonQueryAsync_UpdateUser_ShouldModifyUser()
    {
        // Arrange
        var command = _dbContext.CreateCommand("usp_UpdateUser");
        command.AddParameter("@UserId", 1);
        command.AddParameter("@Email", "newemail@example.com");
        command.AddParameter("@FirstName", "Johnny");
        command.AddParameter("@LastName", "Doe");

        // Act
        var result = await _dbContext.ExecuteNonQueryAsync(command);

        // Assert
        Assert.True(result >= 0);
        
        // Verify changes
        var verifyCommand = _dbContext.CreateCommand("usp_GetUserById");
        verifyCommand.AddParameter("@UserId", 1);
        var users = await _dbContext.ExecuteQueryAsync<UserDto>(verifyCommand);
        
        Assert.Single(users);
        Assert.Equal("newemail@example.com", users[0].Email);
        Assert.Equal("Johnny", users[0].FirstName);
    }

    /// <summary>
    /// Verifies that ExecuteNonQueryAsync can soft delete a user.
    /// </summary>
    [Fact]
    public async Task ExecuteNonQueryAsync_DeleteUser_ShouldMarkUserInactive()
    {
        // Arrange
        var command = _dbContext.CreateCommand("usp_DeleteUser");
        command.AddParameter("@UserId", 2);

        // Act
        var result = await _dbContext.ExecuteNonQueryAsync(command);

        // Assert
        Assert.True(result >= 0);
        
        // Verify user is not in active list
        var verifyCommand = _dbContext.CreateCommand("usp_GetAllUsers");
        var users = await _dbContext.ExecuteQueryAsync<UserDto>(verifyCommand);
        
        Assert.DoesNotContain(users, u => u.UserId == 2);
    }

    /// <summary>
    /// Verifies that ExecuteNonQueryAsync can create orders and add items.
    /// </summary>
    [Fact]
    public async Task ExecuteNonQueryAsync_CreateOrderWithItems_ShouldUpdateTotalAndStock()
    {
        // Arrange - Create order
        var createOrderCommand = _dbContext.CreateCommand("usp_CreateOrder");
        createOrderCommand.AddParameter("@UserId", 1);
        createOrderCommand.AddParameter("@Status", "Pending");
        var orderIdParam = createOrderCommand.AddOutputParameter("@OrderId", DbType.Int32);
        
        await _dbContext.ExecuteNonQueryAsync(createOrderCommand);
        var orderId = (int)orderIdParam.Value!;

        // Act - Add order item
        var addItemCommand = _dbContext.CreateCommand("usp_AddOrderItem");
        addItemCommand.AddParameter("@OrderId", orderId);
        addItemCommand.AddParameter("@ProductId", 1); // Laptop Pro 15
        addItemCommand.AddParameter("@Quantity", 2);
        var orderItemIdParam = addItemCommand.AddOutputParameter("@OrderItemId", DbType.Int32);
        
        var result = await _dbContext.ExecuteNonQueryAsync(addItemCommand);

        // Assert
        Assert.True(result >= 0);
        Assert.True((int)orderItemIdParam.Value! > 0);

        // Verify order details
        var detailsCommand = _dbContext.CreateCommand("usp_GetOrderDetails");
        detailsCommand.AddParameter("@OrderId", orderId);
        var orders = await _dbContext.ExecuteQueryAsync<OrderHeaderDto>(detailsCommand);
        
        Assert.Single(orders);
        Assert.True(orders[0].TotalAmount > 0);
    }

    /// <summary>
    /// Verifies that ExecuteNonQueryAsync can update product price.
    /// </summary>
    [Fact]
    public async Task ExecuteNonQueryAsync_UpdateProductPrice_ShouldChangePrice()
    {
        // Arrange
        var command = _dbContext.CreateCommand("usp_UpdateProductPrice");
        command.AddParameter("@ProductId", 2); // Wireless Mouse
        command.AddParameter("@NewPrice", 34.99m);

        // Act
        var result = await _dbContext.ExecuteNonQueryAsync(command);

        // Assert
        Assert.True(result >= 0);
        
        // Verify price change
        var verifyCommand = _dbContext.CreateCommand("usp_GetProductsByCategory");
        verifyCommand.AddParameter("@CategoryId", 1);
        var products = await _dbContext.ExecuteQueryAsync<ProductDto>(verifyCommand);
        
        var updatedProduct = products.FirstOrDefault(p => p.ProductId == 2);
        Assert.NotNull(updatedProduct);
        Assert.Equal(34.99m, updatedProduct.Price);
    }

    #endregion

    #region DTO Classes

    /// <summary>
    /// Data transfer object for User entity.
    /// </summary>
    public class UserDto
    {
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
        public bool IsActive { get; set; }
    }

    /// <summary>
    /// Data transfer object for Product entity with category information.
    /// </summary>
    public class ProductDto
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int CategoryId { get; set; }
        public decimal Price { get; set; }
        public int StockQty { get; set; }
        public DateTime CreatedDate { get; set; }
        public bool IsActive { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public string? Description { get; set; }
    }

    /// <summary>
    /// Data transfer object for Order entity with user information.
    /// </summary>
    public class OrderDto
    {
        public int OrderId { get; set; }
        public int UserId { get; set; }
        public DateTime OrderDate { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
    }

    /// <summary>
    /// Data transfer object for Order header information.
    /// </summary>
    public class OrderHeaderDto
    {
        public int OrderId { get; set; }
        public int UserId { get; set; }
        public DateTime OrderDate { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
    }

    #endregion
}
