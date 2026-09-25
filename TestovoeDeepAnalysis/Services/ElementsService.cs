using Npgsql;
using TestovoeDeepAnalysis.Models;

namespace TestovoeDeepAnalysis.Services;

public class ElementsService
{
    private readonly NpgsqlConnection _connection;

    public ElementsService(NpgsqlConnection connection)
    {
        _connection = connection;
    }

    public async Task<ElementsResponse> ProcessAsync(
        ElementsRequest request,
        CancellationToken cancellationToken)
    {
        await _connection.OpenAsync(cancellationToken);

        var response = new ElementsResponse
        {
            IsError = 0,
            ErrorCode = 0
        };

        return response;
    }
}