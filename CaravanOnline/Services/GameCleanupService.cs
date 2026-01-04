using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CaravanOnline.Services
{
    public class GameCleanupService : BackgroundService
    {
        private readonly OnlineGameStateService _gameStateService;
        private readonly ILogger<GameCleanupService> _logger;
        private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(5);
        private readonly TimeSpan _abandonedThreshold = TimeSpan.FromMinutes(30);

        public GameCleanupService(
            OnlineGameStateService gameStateService,
            ILogger<GameCleanupService> logger)
        {
            _gameStateService = gameStateService;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Game Cleanup Service started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CleanupAbandonedGames();
                    await Task.Delay(_checkInterval, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    // Expected when stopping
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred during game cleanup");
                    // Continue running even if cleanup fails
                    await Task.Delay(_checkInterval, stoppingToken);
                }
            }

            _logger.LogInformation("Game Cleanup Service stopped.");
        }

        private async Task CleanupAbandonedGames()
        {
            var abandonedRooms = _gameStateService.GetAbandonedRooms(_abandonedThreshold);
            
            if (abandonedRooms.Any())
            {
                _logger.LogInformation($"Found {abandonedRooms.Count} abandoned game(s) to clean up");
                
                foreach (var room in abandonedRooms)
                {
                    _logger.LogInformation(
                        $"Cleaning up abandoned game: RoomId={room.RoomId}, " +
                        $"Created={room.CreatedAt:yyyy-MM-dd HH:mm:ss} UTC, " +
                        $"LastActivity={room.LastActivityAt:yyyy-MM-dd HH:mm:ss} UTC, " +
                        $"Players={room.Player1Name}/{room.Player2Name ?? "none"}"
                    );

                    _gameStateService.RemoveRoom(room.RoomId);
                    
                    // Clear large collections to help GC
                    room.Player1Cards?.Clear();
                    room.Player2Cards?.Clear();
                    room.Lanes?.Clear();
                }
                
                _logger.LogInformation($"Successfully cleaned up {abandonedRooms.Count} abandoned game(s)");
            }

            await Task.CompletedTask;
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Game Cleanup Service is stopping...");
            await base.StopAsync(cancellationToken);
        }
    }
}
