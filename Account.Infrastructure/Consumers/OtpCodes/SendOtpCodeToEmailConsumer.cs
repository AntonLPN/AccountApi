using Account.Contracts.Saga.TwoFactor.Commands;
using Account.Contracts.Saga.TwoFactor.Events;
using Account.Domain.Interfaces;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Account.Infrastructure.Consumers.TwoFactor;

public class SendOtpCodeToEmailConsumer(ILogger<SendOtpCodeToEmailConsumer> logger, IEmail emailService)
    : IConsumer<SendOtpCodeIntegrationCommand>
{
    public async Task Consume(ConsumeContext<SendOtpCodeIntegrationCommand> context)
    {
        var isOtpCodeSent = await emailService.SendOtpCodeAsync(context.Message.Email, context.Message.OtpCode,
            context.CancellationToken);
        if (!isOtpCodeSent)
        {
            logger.LogError("Otp Code Send Failed");
            await context.Publish(new TwoFactorFailedIntegrationEvent()
            {
                CorrelationId = context.Message.CorrelationId,
                FailureReason = "Failed to send Otp Code Send",
                UserId = context.Message.UserId,
            });
        }

        logger.LogInformation("Consumed  Otp Code Send for user = {UserId}", context.Message.UserId);
        await context.Publish(new OtpCodeSentIntegrationEvent()
        {
            CorrelationId = context.Message.CorrelationId,
            UserId = context.Message.UserId,
        });
    }
}