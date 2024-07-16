using Assignment.Data;
using Assignment.Models;
using Microsoft.Extensions.Logging;

namespace Assignment
{
    internal class CollaborativeBuilder
    {
        private readonly LeeegoooService _legoService;
        private readonly ILogger<CollaborativeBuilder> _logger;

        public CollaborativeBuilder(LeeegoooService legoService, ILogger<CollaborativeBuilder> logger)
        {
            _legoService = legoService;
            _logger = logger;
        }

        public async Task<List<User>> FindCollaboratorsAsync(string username, string setName, CancellationToken cancellationToken)
        {

            var user = await _legoService.GetUserByUsernameAsync(username, cancellationToken);
            if (user == null)
            {
                _logger.LogError($"User {username} not found");
                return new List<User>();
            }

            var set = await _legoService.GetSetByIdAsync(setName, cancellationToken);
            if (set == null)
            {
                _logger.LogError($"Set {setName} not found");
                return new List<User>();
            }

            var allUsers = await _legoService.GetAllUsersAsync(cancellationToken);
            var collaborators = new List<User>();

            foreach (var potentialCollaborator in allUsers)
            {
                if (potentialCollaborator.Username == username)
                {
                    continue;
                }

                if (CanCombineInventories(user.Inventory, potentialCollaborator.Inventory, set.Pieces))
                {
                    collaborators.Add(potentialCollaborator);
                }
            }

            return collaborators;
        }

        private bool CanCombineInventories(Inventory userInventory, Inventory collaboratorInventory, List<SetPiece> setPieces)
        {
            var combinedInventory = new Dictionary<(int, int), int>();

            foreach (var piece in userInventory.Pieces)
            {
                var key = (piece.DesignId, piece.ColourId);
                if (!combinedInventory.ContainsKey(key))
                {
                    combinedInventory[key] = 0;
                }
                combinedInventory[key] += piece.Quantity;
            }

            foreach (var piece in collaboratorInventory.Pieces)
            {
                var key = (piece.DesignId, piece.ColourId);
                if (!combinedInventory.ContainsKey(key))
                {
                    combinedInventory[key] = 0;
                }
                combinedInventory[key] += piece.Quantity;
            }

            foreach (var setPiece in setPieces)
            {
                var key = (setPiece.DesignId, setPiece.ColourId);
                if (!combinedInventory.ContainsKey(key) || combinedInventory[key] < setPiece.Quantity)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
