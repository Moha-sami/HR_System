using Buy2.Application.Common.Interfaces;
using Buy2.Domain.Entities;
using MediatR;

namespace Buy2.Application.Features.Points.CreateRule;

public record CreatePointsRuleCommand(
    string RuleName,
    int PointsValue,
    string TriggerType
) : IRequest<int>;

public class CreatePointsRuleCommandHandler : IRequestHandler<CreatePointsRuleCommand, int>
{
    private readonly IRepository<PointsRule> _pointsRuleRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreatePointsRuleCommandHandler(IRepository<PointsRule> pointsRuleRepository, IUnitOfWork unitOfWork)
    {
        _pointsRuleRepository = pointsRuleRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<int> Handle(CreatePointsRuleCommand request, CancellationToken cancellationToken)
    {
        var rule = new PointsRule
        {
            RuleKey = request.RuleName,
            EventType = request.TriggerType,
            ConditionExpression = string.Empty,
            ActionType = "AddPoints",
            PointValue = request.PointsValue
        };

        await _pointsRuleRepository.AddAsync(rule, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return rule.Id;
    }
}
