
using Assignment.Models;

namespace Assignment
{
    internal class SetChecker
    {
        public bool CanBuildSet(User user, Set set)
        {
            if (user?.Inventory?.Pieces == null || set?.Pieces == null)
            {
                return false;
            }

            var inventoryPieces = user.Inventory.Pieces;
            foreach (var setPiece in set.Pieces)
            {
                var matchingPiece = inventoryPieces
                    .Find(p => p.DesignId == setPiece.DesignId && p.ColourId == setPiece.ColourId);
                if (matchingPiece == null || matchingPiece.Quantity < setPiece.Quantity)
                {
                    return false;
                }
            }
            return true;
        }

        public List<Set> GetBuildableSets(User user, List<Set> sets)
        {
            if (user == null || sets == null)
            {
                return new List<Set>();
            }

            var buildableSets = new List<Set>();
            foreach (var set in sets)
            {
                if (CanBuildSet(user, set))
                {
                    buildableSets.Add(set);
                }
            }
            return buildableSets;
        }
    }
}
