using Assignment.Data;
using Assignment.Models;
using Microsoft.Extensions.Logging;

namespace Assignment
{
    internal class FlexibleBuilder
    {
        private readonly LeeegoooService _leeegoooService;
        private readonly ILogger<FlexibleBuilder> _logger;

        public FlexibleBuilder(LeeegoooService leeegoooService, ILogger<FlexibleBuilder> logger)
        {
            _leeegoooService = leeegoooService;
            _logger = logger;
        }

        public async Task<List<Set>> GetBuildableSetsWithColorSubstitutionAsync(string username, CancellationToken cancellationToken)
        {
            var user = await _leeegoooService.GetUserByUsernameAsync(username, cancellationToken);
            if (user == null)
            {
                _logger.LogError($"User {username} not found");
                return new List<Set>();
            }

            var sets = await _leeegoooService.GetAllSetsAsync(cancellationToken);
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
            var userPiecesGroupedByDesign = userInventory.Pieces
                .GroupBy(piece => piece.DesignId)
                .ToDictionary(g => g.Key, g => g.ToList());
      
            if (CanBuildSetExactly(userPiecesGroupedByDesign, setPieces))
            {
                return true;
            }

            var colorMapping = new Dictionary<int, int>();

            foreach (var setPiece in setPieces)
            {
                if (!userPiecesGroupedByDesign.TryGetValue(setPiece.DesignId, out var userPieces))
                {
                    return false;
                }

                var userPiecesGroupedByColor = userPieces
                    .GroupBy(p => p.ColourId)
                    .ToDictionary(g => g.Key, g => g.Sum(p => p.Quantity));

                var requiredQuantity = setPiece.Quantity;
                var setPieceColor = setPiece.ColourId;

                if (userPiecesGroupedByColor.TryGetValue(setPieceColor, out var availableQuantity))
                {
                    if (availableQuantity >= requiredQuantity)
                    {
                        continue;
                    }

                    requiredQuantity -= availableQuantity;
                }

                var colorSubstituted = false;
                foreach (var userPieceColor in userPiecesGroupedByColor.Keys)
                {
                    if (colorMapping.TryGetValue(setPieceColor, out var mappedColor))
                    {
                        if (mappedColor != userPieceColor)
                        {
                            continue;
                        }
                    }
                    else
                    {
                        if (colorMapping.ContainsValue(userPieceColor))
                        {
                            continue; //the color is already used 
                        }

                        colorMapping[setPieceColor] = userPieceColor;
                    }

                    if (userPiecesGroupedByColor[userPieceColor] >= requiredQuantity)
                    {
                        colorSubstituted = true;
                        break;
                    }

                    requiredQuantity -= userPiecesGroupedByColor[userPieceColor];
                }

                if (!colorSubstituted && requiredQuantity > 0)
                {
                    return false;
                }
            }

            return true;
        }

        private bool CanBuildSetExactly(Dictionary<int, List<Piece>> userPiecesGroupedByDesign, List<SetPiece> setPieces)
        {
            foreach (var setPiece in setPieces)
            {
                if (!userPiecesGroupedByDesign.TryGetValue(setPiece.DesignId, out var userPieces))
                {
                    return false;
                }

                var requiredQuantity = setPiece.Quantity;
                var matchingPieces = userPieces.Where(p => p.ColourId == setPiece.ColourId);

                foreach (var userPiece in matchingPieces)
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
