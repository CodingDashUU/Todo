namespace Washu.Framework.Identity;

public record CronJobRequest(string JobFunction, string CronExpression);