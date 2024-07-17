using Assignment.Data;
using Assignment.Models;
using Microsoft.Extensions.Logging;


namespace Assignment
{
    internal class CustomBuildAdvisor2
    {
        private readonly LeeegoooService _leeegoooService;
        private readonly ILogger<CustomBuildAdvisor> _logger;

        public CustomBuildAdvisor2(LeeegoooService leeegoooService, ILogger<CustomBuildAdvisor> logger)
        {
            _leeegoooService = leeegoooService;
            _logger = logger;
        }

        public async Task<Dictionary<(int DesignId, int ColourId), int>> GetMaxPieceSetForCustomBuildAsync()
        {
            try
            {
                var users = await _leeegoooService.GetAllUsersAsync(CancellationToken.None);
                if (users == null || users.Count == 0)
                {
                    _logger.LogError("No users found");
                    return new Dictionary<(int DesignId, int ColourId), int>();
                }

                var pieceQuantities = CountPieceQuantities(users);
                var medianQuantities = CalculateMedianQuantities(pieceQuantities, users.Count);
                var maxPieceSet = ConstructInitialMaxSet(medianQuantities);

                return AdjustMaxSetForCompletion(users, maxPieceSet);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while calculating the maximum piece set for the custom build");
                return new Dictionary<(int DesignId, int ColourId), int>();
            }
        }

        private Dictionary<(string UserId, int DesignId, int ColourId), int> CountPieceQuantities(List<User> users)
        {
            var pieceQuantities = new Dictionary<(string UserId, int DesignId, int ColourId), int>();

            foreach (var user in users)
            {
                foreach (var piece in user.Inventory.Pieces)
                {
                    var key = (user.Id, piece.DesignId, piece.ColourId);
                    pieceQuantities[key] = piece.Quantity;
                }
            }

            return pieceQuantities;
        }

        private Dictionary<(int DesignId, int ColourId), List<int>> GroupPieceQuantitiesByDesignAndColor(Dictionary<(string UserId, int DesignId, int ColourId), int> pieceQuantities)
        {
            var groupedQuantities = new Dictionary<(int DesignId, int ColourId), List<int>>();

            foreach (var kvp in pieceQuantities)
            {
                var key = (kvp.Key.DesignId, kvp.Key.ColourId);
                if (!groupedQuantities.ContainsKey(key))
                {
                    groupedQuantities[key] = new List<int>();
                }
                groupedQuantities[key].Add(kvp.Value);
            }

            return groupedQuantities;
        }

        private Dictionary<(int DesignId, int ColourId), int> CalculateMedianQuantities(Dictionary<(string UserId, int DesignId, int ColourId), int> pieceQuantities, int userCount)
        {
            var groupedQuantities = GroupPieceQuantitiesByDesignAndColor(pieceQuantities);

            var medianQuantities = new Dictionary<(int DesignId, int ColourId), int>();
            foreach (var kvp in groupedQuantities)
            {
                var quantities = kvp.Value.OrderBy(q => q).ToList();
                int median = quantities[userCount / 2];
                medianQuantities[kvp.Key] = median;
            }

            return medianQuantities;
        }

        private Dictionary<(int DesignId, int ColourId), int> ConstructInitialMaxSet(Dictionary<(int DesignId, int ColourId), int> medianQuantities)
        {
            return new Dictionary<(int DesignId, int ColourId), int>(medianQuantities);
        }

        private Dictionary<(int DesignId, int ColourId), int> AdjustMaxSetForCompletion(List<User> users, Dictionary<(int DesignId, int ColourId), int> maxPieceSet)
        {
            foreach (var piece in maxPieceSet.Keys.ToList())
            {
                int completionCount = users.Count(user => user.Inventory.Pieces.Any(p => p.DesignId == piece.DesignId && p.ColourId == piece.ColourId && p.Quantity >= maxPieceSet[piece]));
                while (completionCount < users.Count / 2 && maxPieceSet[piece] > 0)
                {
                    maxPieceSet[piece]--;
                    completionCount = users.Count(user => user.Inventory.Pieces.Any(p => p.DesignId == piece.DesignId && p.ColourId == piece.ColourId && p.Quantity >= maxPieceSet[piece]));
                }

                if (maxPieceSet[piece] == 0)
                {
                    maxPieceSet.Remove(piece);
                }
            }

            var finalSet = new Dictionary<(int DesignId, int ColourId), int>();
            foreach (var piece in maxPieceSet)
            {
                int completionCount = users.Count(user => user.Inventory.Pieces.Any(p => p.DesignId == piece.Key.DesignId && p.ColourId == piece.Key.ColourId && p.Quantity >= piece.Value));
                if (completionCount >= users.Count / 2)
                {
                    finalSet[piece.Key] = piece.Value;
                }
            }

            return finalSet;
        }

    }
}
