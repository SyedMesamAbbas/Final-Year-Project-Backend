using HouseofTutorAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace HouseofTutorAPI.Services
{
    public class TutorRequestQueueService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<TutorRequestQueueService> _logger;

        public TutorRequestQueueService( IServiceScopeFactory scopeFactory, ILogger<TutorRequestQueueService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation(
                "Tutor Request Queue Service started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessExpiredRequests(
                        stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Error processing tutor request queue.");
                }

                await Task.Delay(
                    TimeSpan.FromSeconds(10),
                    stoppingToken);
            }
        }

        private async Task ProcessExpiredRequests(CancellationToken cancellationToken)
        {
            using var scope =
                _scopeFactory.CreateScope();

            var db = scope.ServiceProvider
                .GetRequiredService<HouseofTutorContext>();

            var now = DateTime.Now;

            // =================================================
            // Find Pending requests whose 2 minutes are over
            // =================================================

            var expiredRequests = await db.Requests
                .Where(r =>
                    r.Status == "Pending" &&
                    r.ResponseDeadline.HasValue &&
                    r.ResponseDeadline.Value <= now &&
                    r.RequestGroupId.HasValue)
                .ToListAsync(cancellationToken);

            foreach (var currentRequest in expiredRequests)
            {
                // =============================================
                // Get Group
                // =============================================

                var group = await db.RequestGroups .FirstOrDefaultAsync( g => g.RequestGroupId == currentRequest.RequestGroupId, cancellationToken);

                if (group == null)
                    continue;

                // =============================================
                // Group must still be searching
                // =============================================

                if (group.Status != "Searching")
                    continue;

                // =============================================
                // Make sure this is current request
                // =============================================

                if (group.CurrentRequestId !=
                    currentRequest.RequestId)
                {
                    continue;
                }

                // =============================================
                // Expire current tutor
                // =============================================

                currentRequest.Status = "Expired";

                currentRequest.ResponseDeadline = null;

                // =============================================
                // Find next queued tutor
                // =============================================

                var nextRequest = await db.Requests .Where(r => r.RequestGroupId == group.RequestGroupId && r.Status == "Queued") .OrderBy(r => r.TutorSequence) .FirstOrDefaultAsync(cancellationToken);

                // =============================================
                // No more tutors
                // =============================================

                if (nextRequest == null)
                {
                    group.Status = "Expired";

                    group.CurrentRequestId = null;

                    await db.SaveChangesAsync(cancellationToken);

                    continue;
                }

                // =============================================
                // Send request to next tutor
                // =============================================

                nextRequest.Status = "Pending";

                nextRequest.RequestDate = now;

                nextRequest.ResponseDeadline = now.AddMinutes(2);

                group.CurrentRequestId = nextRequest.RequestId;

                await db.SaveChangesAsync(cancellationToken);

                _logger.LogInformation(
                    "Request Group {GroupId}: Tutor {TutorId} " +
                    "expired. Request moved to Tutor {NextTutorId}.",
                    group.RequestGroupId,
                    currentRequest.TutorId,
                    nextRequest.TutorId);
            }
        }
    }
}