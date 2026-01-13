namespace RL.Backend.UnitTests;

[TestClass]
public class RemoveUserFromProcedurePlanTests
{
    private RLContext _context = null!;
    private RemoveUserFromProcedurePlanCommandHandler _handler = null!;

    [TestInitialize]
    public void Setup()
    {
        _context = DbContextHelper.CreateContext();
        var loggerMock = new Mock<ILogger<RemoveUserFromProcedurePlanCommandHandler>>();
        _handler = new RemoveUserFromProcedurePlanCommandHandler(_context, loggerMock.Object);
    }

    [TestMethod]
    [DataRow(-1)]
    [DataRow(0)]
    [DataRow(int.MinValue)]
    public async Task RemoveUserFromProcedurePlan_InvalidPlanId_ReturnsBadRequest(int planId)
    {
        // Arrange
        var request = new RemoveUserFromProcedurePlanCommand
        {
            PlanId = planId,
            ProcedureId = 1
        };

        // Act
        var result = await _handler.Handle(request, default);

        // Assert
        result.Succeeded.Should().BeFalse();
        result.Exception.Should().BeOfType<BadRequestException>();
    }

    [TestMethod]
    [DataRow(-1)]
    [DataRow(0)]
    [DataRow(int.MinValue)]
    public async Task RemoveUserFromProcedurePlan_InvalidProcedureId_ReturnsBadRequest(int procedureId)
    {
        // Arrange
        var request = new RemoveUserFromProcedurePlanCommand
        {
            PlanId = 1,
            ProcedureId = procedureId
        };

        // Act
        var result = await _handler.Handle(request, default);

        // Assert
        result.Succeeded.Should().BeFalse();
        result.Exception.Should().BeOfType<BadRequestException>();
    }

    [TestMethod]
    public async Task RemoveUserFromProcedurePlan_RemoveAllUsers_Succeeds()
    {
        // Arrange
        var planId = 1;
        var procedureId = 2;

        _context.PlanProcedureUsers.AddRange(
            new PlanProcedureUser { PlanId = planId, ProcedureId = procedureId, UserId = 100 },
            new PlanProcedureUser { PlanId = planId, ProcedureId = procedureId, UserId = 101 }
        );
        await _context.SaveChangesAsync();

        var request = new RemoveUserFromProcedurePlanCommand
        {
            PlanId = planId,
            ProcedureId = procedureId,
            UserId = null // remove all users
        };

        // Act
        var result = await _handler.Handle(request, default);

        // Assert
        var remaining = await _context.PlanProcedureUsers
            .Where(ppu => ppu.PlanId == planId && ppu.ProcedureId == procedureId)
            .ToListAsync();

        result.Succeeded.Should().BeTrue();
        result.Value.Should().Be(Unit.Value);
        remaining.Should().BeEmpty();
    }

    [TestMethod]
    public async Task RemoveUserFromProcedurePlan_RemoveSpecificUser_Succeeds()
    {
        // Arrange
        var planId = 2;
        var procedureId = 3;
        var userId = 200;

        _context.PlanProcedureUsers.AddRange(
            new PlanProcedureUser { PlanId = planId, ProcedureId = procedureId, UserId = userId },
            new PlanProcedureUser { PlanId = planId, ProcedureId = procedureId, UserId = 201 }
        );
        await _context.SaveChangesAsync();

        var request = new RemoveUserFromProcedurePlanCommand
        {
            PlanId = planId,
            ProcedureId = procedureId,
            UserId = userId
        };

        // Act
        var result = await _handler.Handle(request, default);

        // Assert
        var remaining = await _context.PlanProcedureUsers
            .Where(ppu => ppu.PlanId == planId && ppu.ProcedureId == procedureId)
            .ToListAsync();

        result.Succeeded.Should().BeTrue();
        result.Value.Should().Be(Unit.Value);
        remaining.Should().ContainSingle(x => x.UserId == 201);
    }

    [TestMethod]
    public async Task RemoveUserFromProcedurePlan_NoUserExists_StillSucceeds()
    {
        // Arrange
        var planId = 10;
        var procedureId = 20;

        var request = new RemoveUserFromProcedurePlanCommand
        {
            PlanId = planId,
            ProcedureId = procedureId,
            UserId = 999 // nonexistent
        };

        // Act
        var result = await _handler.Handle(request, default);

        // Assert
        result.Succeeded.Should().BeTrue();
        result.Value.Should().Be(Unit.Value);
    }
}
