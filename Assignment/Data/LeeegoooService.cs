
using Assignment.Models;
using Microsoft.Extensions.Logging;
using RestSharp;
using System.Text.Json;
using System.Threading;

namespace Assignment.Data
{
    internal class LeeegoooService
    {
        private readonly IRestClient _client;
        private readonly ApiSettings _apiSettings;
        private readonly ILogger<LeeegoooService> _logger;
        public LeeegoooService(IRestClient client, ApiSettings apiSettings, ILogger<LeeegoooService> logger)
        {
            _client = client;
            _apiSettings = apiSettings;
            _logger = logger;                 
        }

        public async Task<User?> GetUserByUsernameAsync(string username, CancellationToken cancellationToken)
        {
            try
            {
                var request = new RestRequest($"{_apiSettings.BaseUrl}/api/user/by-username/{username}", Method.GET);
                request.AddHeader("Content-Type", "application/json");
                request.AddHeader("Accept-Encoding", "identity");

                var response = await CallWebApiAsync(request, cancellationToken);

                if (response == null || !response.IsSuccessful)
                {
                    _logger.LogError("Error fetching user by username: {Username}", username);
                    return null;
                }

                var user = JsonSerializer.Deserialize<User>(response.Content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return user;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching user by username: {Username}", username);
                return null;
            }
        }

        public async Task<List<Set>?> GetAllSetsAsync(CancellationToken cancellationToken)
        {
            try
            {
                var request = new RestRequest($"{_apiSettings.BaseUrl}/api/sets", Method.GET);
                request.AddHeader("Content-Type", "application/json");
                request.AddHeader("Accept-Encoding", "identity");

                var response = await CallWebApiAsync(request, cancellationToken);               

                if (!response.IsSuccessful)
                {
                    _logger.LogError("Error fetching all sets");
                    return null;
                }

                var sets = JsonSerializer.Deserialize<List<Set>>(response.Content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return sets;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching all sets");
                return null;
            }
        }

        public async Task<Set?> GetSetByIdAsync(string id, CancellationToken cancellationToken)
        {
            try
            {
                var request = new RestRequest($"/api/set/by-id/{id}", Method.GET);
                request.AddHeader("Content-Type", "application/json");
                request.AddHeader("Accept-Encoding", "identity");

                var response = await CallWebApiAsync(request, cancellationToken);

                if (response == null || !response.IsSuccessful)
                {
                    _logger.LogError("Error fetching set by id: {Id}", id);
                    return null;
                }

                var set = JsonSerializer.Deserialize<Set>(response.Content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return set;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching set by id: {Id}", id);
                return null;
            }
        }

        public async Task<List<User>?> GetAllUsersAsync(CancellationToken cancellationToken)
        {
            try
            {
                var request = new RestRequest("/api/users", Method.GET);
                request.AddHeader("Content-Type", "application/json");
                request.AddHeader("Accept-Encoding", "identity");

                var response = await CallWebApiAsync(request, cancellationToken);

                if (response == null || !response.IsSuccessful)
                {
                    _logger.LogError("Error fetching all users");
                    return null;
                }

                var users = JsonSerializer.Deserialize<List<User>>(response.Content, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                return users;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching all users");
                return null;
            }
        }

        private async Task<IRestResponse?> CallWebApiAsync(RestRequest request, CancellationToken cancellationToken)
        {
            IRestResponse? response = null;

            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                response = await _client.ExecuteAsync(request, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Request was cancelled.");
                throw;
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Error requesting data: {request.Resource}");
            }

            if (response is { IsSuccessful: true }) return response;

            _logger.LogError($"Error connecting to {request.Resource}");
            if (response == null)
            {
                _logger.LogError("Received null response from server");
                return null;
            }

            if (!string.IsNullOrEmpty(response.StatusDescription))
                _logger.LogError($"Error retrieving message: status code {response.StatusCode}, status message {response.StatusDescription}");
            if (!string.IsNullOrEmpty(response.ErrorMessage))
                _logger.LogError($"Error retrieving message: error source {response.ErrorException?.Source}, error message {response.ErrorMessage}");
            if (response.ErrorException?.InnerException != null)
                _logger.LogError(response.ErrorException.InnerException, $"Error retrieving message: inner exception {response.ErrorException.InnerException.Message}");

            return response;
        }

    }
}
