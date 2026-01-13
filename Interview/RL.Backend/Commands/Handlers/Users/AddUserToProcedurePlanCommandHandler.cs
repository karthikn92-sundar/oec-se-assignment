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
            if (request.PlanId < 1 || request.ProcedureId < 1 || request.UserId < 1)
                return ApiResponse<Unit>.Fail(
                    new BadRequestException("Invalid PlanId, ProcedureId or UserId"));

            var planProcedureExists = await _context.PlanProcedures
                .AnyAsync(pp => pp.PlanId == request.PlanId && pp.ProcedureId == request.ProcedureId, cancellationToken);

            if (!planProcedureExists)
                return ApiResponse<Unit>.Fail(new NotFoundException("PlanProcedure link does not exist."));

            var userExists = await _context.Users.AnyAsync(u => u.UserId == request.UserId, cancellationToken);
            if (!userExists)
                return ApiResponse<Unit>.Fail(new NotFoundException($"UserId {request.UserId} not found"));

            var alreadyAssigned = await _context.PlanProcedureUsers.AnyAsync(ppu =>
                ppu.PlanId == request.PlanId &&
                ppu.ProcedureId == request.ProcedureId &&
                ppu.UserId == request.UserId, cancellationToken);

            if (alreadyAssigned)
                return ApiResponse<Unit>.Succeed(Unit.Value);

            var utcNow = DateTime.UtcNow;

            // Assign the user to the plan-procedure
            _context.PlanProcedureUsers.Add(new PlanProcedureUser
            {
                PlanId = request.PlanId,
                ProcedureId = request.ProcedureId,
                UserId = request.UserId,
                CreateDate = utcNow,
                UpdateDate = utcNow
            });

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
