
using Assignment.Data;
using Assignment.Models;
using Microsoft.Extensions.Logging;

namespace Assignment
{
    internal class CustomBuildAdvisor
    {
        private readonly LeeegoooService _leeegoooService;
        private readonly ILogger<CustomBuildAdvisor> _logger;

        public CustomBuildAdvisor(LeeegoooService leeegoooService, ILogger<CustomBuildAdvisor> logger)
        {
            _leeegoooService = leeegoooService;
            _logger = logger;
        }

        public async Task<List<(int DesignId, int ColourId)>?> GetMaxPieceCountForCustomBuildAsync(string username)
        {
            try
            {
                var users = await _leeegoooService.GetAllUsersAsync(CancellationToken.None);
                if (users == null || users.Count == 0)
                {
                    _logger.LogError("No users found");
                    return null;
                }

                var pieceCount = CountPiecesAcrossUsers(users);
                var commonPieces = FindCommonPieces(pieceCount, users.Count);

                return commonPieces;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while calculating the maximum piece count for the custom build");
                return null;
            }
        }

        public Dictionary<(string userId, int DesignId, int ColourId), int> CountPiecesAcrossUsers(List<User> users)
        {
            var pieceCount = new Dictionary<(string userId, int DesignId, int ColourId), int>();

            foreach (var user in users)
            {
                foreach (var piece in user.Inventory.Pieces)
                {
                    var key = (user.Id, piece.DesignId, piece.ColourId);
                    if (pieceCount.ContainsKey(key))
                    {
                        pieceCount[key]++;
                    }
                    else
                    {
                        pieceCount[key] = 1;
                    }
                }
            }

            return pieceCount;
        }

        public List<(int DesignId, int ColourId)> FindCommonPieces(Dictionary<(string userId, int DesignId, int ColourId), int> pieceCount, int totalUsers)
        {
            var pieceUserCount = new Dictionary<(int DesignId, int ColourId), int>();

            foreach (var key in pieceCount.Keys)
            {
                var pieceKey = (key.DesignId, key.ColourId);
                if (pieceUserCount.ContainsKey(pieceKey))
                {
                    pieceUserCount[pieceKey]++;
                }
                else
                {
                    pieceUserCount[pieceKey] = 1;
                }
            }

            var commonPieces = new List<(int DesignId, int ColourId)>();

            foreach (var piece in pieceUserCount)
            {
                if (piece.Value >= totalUsers / 2.0)
                {
                    commonPieces.Add(piece.Key);
                }
            }

            return commonPieces;
        }
    }


}
