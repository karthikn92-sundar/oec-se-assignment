namespace RL.Backend.UnitTests;

[TestClass]
public class AddUserToProcedurePlanTests
{
    private RLContext _context = null!;
    private Mock<ILogger<AddUserToProcedurePlanCommandHandler>> _loggerMock = null!;
    private AddUserToProcedurePlanCommandHandler _handler = null!;

    [TestInitialize]
    public void Setup()
    {
        _context = DbContextHelper.CreateContext();
        _loggerMock = new Mock<ILogger<AddUserToProcedurePlanCommandHandler>>();
        _handler = new AddUserToProcedurePlanCommandHandler(_context, _loggerMock.Object);
    }

    [TestMethod]
    [DataRow(0, 1, 1)]
    [DataRow(-1, 1, 1)]
    [DataRow(int.MinValue, 1, 1)]
    [DataRow(1, 0, 1)]
    [DataRow(1, -1, 1)]
    [DataRow(1, 1, 0)]
    [DataRow(1, 1, -1)]
    public async Task Invalid_Ids_Returns_BadRequest(int planId, int procedureId, int userId)
    {
        // Arrange
        var request = new AddUserToProcedurePlanCommand
        {
            PlanId = planId,
            ProcedureId = procedureId,
            UserId = userId
        };

        // Act
        var result = await _handler.Handle(request, default);

        // Assert
        result.Succeeded.Should().BeFalse();
        result.Exception.Should().BeOfType<BadRequestException>();
    }

    [TestMethod]
    public async Task PlanProcedure_NotFound_Returns_NotFound()
    {
        // Arrange
        var request = new AddUserToProcedurePlanCommand
        {
            PlanId = 99,
            ProcedureId = 100,
            UserId = 1
        };

        _context.Users.Add(new User { UserId = 1, Name = "John" });
        await _context.SaveChangesAsync();

        // Act
        var result = await _handler.Handle(request, default);

        // Assert
        result.Succeeded.Should().BeFalse();
        result.Exception.Should().BeOfType<NotFoundException>();
    }

    [TestMethod]
    public async Task User_NotFound_Returns_NotFound()
    {
        // Arrange
        var request = new AddUserToProcedurePlanCommand
        {
            PlanId = 1,
            ProcedureId = 2,
            UserId = 999
        };

        _context.PlanProcedures.Add(new PlanProcedure { PlanId = 1, ProcedureId = 2 });
        await _context.SaveChangesAsync();

        // Act
        var result = await _handler.Handle(request, default);

        // Assert
        result.Succeeded.Should().BeFalse();
        result.Exception.Should().BeOfType<NotFoundException>();
    }

    [TestMethod]
    public async Task User_AlreadyAssigned_Returns_Success()
    {
        // Arrange
        var request = new AddUserToProcedurePlanCommand
        {
            PlanId = 1,
            ProcedureId = 2,
            UserId = 3
        };

        _context.PlanProcedures.Add(new PlanProcedure { PlanId = 1, ProcedureId = 2 });
        _context.Users.Add(new User { UserId = 3, Name = "AlreadyAssigned" });
        _context.PlanProcedureUsers.Add(new PlanProcedureUser
        {
            PlanId = 1,
            ProcedureId = 2,
            UserId = 3
        });
        await _context.SaveChangesAsync();

        // Act
        var result = await _handler.Handle(request, default);

        // Assert
        result.Succeeded.Should().BeTrue();
        result.Value.Should().Be(Unit.Value);
    }

    [TestMethod]
    public async Task User_Assigned_Successfully()
    {
        // Arrange
        var request = new AddUserToProcedurePlanCommand
        {
            PlanId = 5,
            ProcedureId = 10,
            UserId = 20
        };

        _context.PlanProcedures.Add(new PlanProcedure { PlanId = 5, ProcedureId = 10 });
        _context.Users.Add(new User { UserId = 20, Name = "NewUser" });
        await _context.SaveChangesAsync();

        // Act
        var result = await _handler.Handle(request, default);

        // Assert
        var assigned = await _context.PlanProcedureUsers
            .FirstOrDefaultAsync(x =>
                x.PlanId == request.PlanId &&
                x.ProcedureId == request.ProcedureId &&
                x.UserId == request.UserId);

        result.Succeeded.Should().BeTrue();
        result.Value.Should().Be(Unit.Value);
        assigned.Should().NotBeNull();
    }

    [TestMethod]
    public async Task Handler_LogsError_On_Exception()
    {
        // Arrange
        var contextMock = new Mock<RLContext>();
        var loggerMock = new Mock<ILogger<AddUserToProcedurePlanCommandHandler>>();
        var faultyHandler = new AddUserToProcedurePlanCommandHandler(contextMock.Object, loggerMock.Object);

        contextMock.Setup(x => x.PlanProcedures)
            .Throws(new Exception("DB error"));

        var request = new AddUserToProcedurePlanCommand
        {
            PlanId = 1,
            ProcedureId = 1,
            UserId = 1
        };

        // Act
        var result = await faultyHandler.Handle(request, default);

        // Assert
        result.Succeeded.Should().BeFalse();
        result.Exception.Should().BeOfType<Exception>();
        loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)),
            Times.Once);
    }
}
