using MediatR;
using Microsoft.EntityFrameworkCore;
using RL.Backend.Exceptions;
using RL.Backend.Models;
using RL.Data;
using RL.Data.DataModels;

namespace RL.Backend.Commands.Handlers.Users;

public class AddUserToProcedurePlanCommandHandler : IRequestHandler<AddUserToProcedurePlanCommand, ApiResponse<Unit>>
{
    private readonly RLContext _context;
    private readonly ILogger<AddUserToProcedurePlanCommandHandler> _logger;
    public AddUserToProcedurePlanCommandHandler(RLContext context, ILogger<AddUserToProcedurePlanCommandHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<ApiResponse<Unit>> Handle(AddUserToProcedurePlanCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Validate input
            if (request.PlanId < 1)
                return ApiResponse<Unit>.Fail(new BadRequestException("Invalid PlanId"));
            if (request.ProcedureId < 1)
                return ApiResponse<Unit>.Fail(new BadRequestException("Invalid ProcedureId"));
            if (request.UserId < 1)
                return ApiResponse<Unit>.Fail(new BadRequestException("Invalid UserId"));

            var planProcedure = await _context.PlanProcedures
                .FirstOrDefaultAsync(pp => pp.PlanId == request.PlanId && pp.ProcedureId == request.ProcedureId);

            if (planProcedure == null)
                return ApiResponse<Unit>.Fail(new NotFoundException("PlanProcedure link does not exist."));

            var userExists = await _context.Users.AnyAsync(u => u.UserId == request.UserId);
            if (!userExists)
                return ApiResponse<Unit>.Fail(new NotFoundException($"UserId {request.UserId} not found"));

            var alreadyAssigned = await _context.PlanProcedureUsers.AnyAsync(ppu =>
                ppu.PlanId == request.PlanId &&
                ppu.ProcedureId == request.ProcedureId &&
                ppu.UserId == request.UserId);

            if (alreadyAssigned)
                return ApiResponse<Unit>.Succeed(Unit.Value);

            // Assign the user to the plan-procedure
            var newAssignment = new PlanProcedureUser
            {
                PlanId = request.PlanId,
                ProcedureId = request.ProcedureId,
                UserId = request.UserId,
                CreateDate = DateTime.UtcNow,
                UpdateDate = DateTime.UtcNow
            };

            _context.PlanProcedureUsers.Add(newAssignment);
            await _context.SaveChangesAsync(cancellationToken);

            return ApiResponse<Unit>.Succeed(Unit.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding user to procedure plan. PlanId: {PlanId}, ProcedureId: {ProcedureId}, UserId: {UserId}",
                 request.PlanId, request.ProcedureId, request.UserId);
            return ApiResponse<Unit>.Fail(ex);
        }
    }
}
