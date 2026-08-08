namespace Buy2.Application.DTOs;

public record CreatePointsRuleDto(string RuleKey, string EventType, string ConditionExpression, string ActionType, int PointValue);
