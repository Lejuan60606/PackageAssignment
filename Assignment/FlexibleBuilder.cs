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
            var userPiecesGroupedByDesign = userInventory.Pieces
                .GroupBy(piece => piece.DesignId)
                .ToDictionary(g => g.Key, g => g.ToList());

            if (CanBuildSetExactly(userPiecesGroupedByDesign, setPieces))
            {
                return true;
            }

            return CanBuildSetWithColorSubstitutionRecursive(userPiecesGroupedByDesign, setPieces, new Dictionary<int, int>());
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

        private bool CanBuildSetWithColorSubstitutionRecursive(
            Dictionary<int, List<Piece>> userPiecesGroupedByDesign,
            List<SetPiece> setPieces,
            Dictionary<int, int> colorMapping)
        {
            if (!setPieces.Any())
            {
                return true; // All set pieces have been successfully processed
            }

            var setPiece = setPieces.First();
            var remainingSetPieces = setPieces.Skip(1).ToList();

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
                    return CanBuildSetWithColorSubstitutionRecursive(userPiecesGroupedByDesign, remainingSetPieces, colorMapping);
                }
                requiredQuantity -= availableQuantity;
            }

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
                        continue; // The color is already used for other mapping
                    }

                    colorMapping[setPieceColor] = userPieceColor;
                }

                if (userPiecesGroupedByColor[userPieceColor] >= requiredQuantity)
                {
                    if (CanBuildSetWithColorSubstitutionRecursive(userPiecesGroupedByDesign, remainingSetPieces, new Dictionary<int, int>(colorMapping)))
                    {
                        return true;
                    }
                }
                else
                {
                    var newRequiredQuantity = requiredQuantity - userPiecesGroupedByColor[userPieceColor];
                    var newRemainingSetPieces = new List<SetPiece>(remainingSetPieces) { new SetPiece { DesignId = setPiece.DesignId, ColourId = setPieceColor, Quantity = newRequiredQuantity } };

                    if (CanBuildSetWithColorSubstitutionRecursive(userPiecesGroupedByDesign, newRemainingSetPieces, new Dictionary<int, int>(colorMapping)))
                    {
                        return true;
                    }
                }

                colorMapping.Remove(setPieceColor);
            }

            return false;
        }
    }

}
