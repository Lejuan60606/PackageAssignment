
using Assignment.Data;
using Assignment.Models;
using Microsoft.Extensions.Logging;

namespace Assignment
{
    internal class FlexibleBuilder
    {
        private readonly LeeegoooService _legoService;
        private readonly ILogger<FlexibleBuilder> _logger;

        public FlexibleBuilder(LeeegoooService legoService, ILogger<FlexibleBuilder> logger)
        {
            _legoService = legoService;
            _logger = logger;
        }

        public async Task<List<Set>> GetBuildableSetsWithColorSubstitutionAsync(string username, CancellationToken cancellationToken)
        {
            var user = await _legoService.GetUserByUsernameAsync(username, cancellationToken);
            if (user == null)
            {
                _logger.LogError($"User {username} not found");
                return new List<Set>();
            }

            var sets = await _legoService.GetAllSetsAsync(cancellationToken);
            var buildableSets = new List<Set>();

            foreach (var set in sets)
            {
                if (CanBuildSetWithColorSubstitution(user.Inventory, set.Pieces))
                {
                    buildableSets.Add(set);
                }
            }

            return buildableSets;
        }

        private bool CanBuildSetWithColorSubstitution(Inventory userInventory, List<SetPiece> setPieces)
        {
            var userPiecesGroupedByDesign = userInventory.Pieces.GroupBy(piece => piece.DesignId)
                                                                .ToDictionary(g => g.Key, g => g.ToList());

            foreach (var setPiece in setPieces)
            {
                if (!userPiecesGroupedByDesign.ContainsKey(setPiece.DesignId))
                {
                    return false;
                }

                var userPieces = userPiecesGroupedByDesign[setPiece.DesignId];
                var requiredQuantity = setPiece.Quantity;

                foreach (var userPiece in userPieces)
                {
                    if (userPiece.Quantity >= requiredQuantity)
                    {
                        requiredQuantity = 0;
                        break;
                    }

                    requiredQuantity -= userPiece.Quantity;
                }

                if (requiredQuantity > 0)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
