using FastMember;
using System.Data;
using Microsoft.Data.SqlClient;

public interface IDbService
{
    Task<List<T>> ExecuteQueryAsync<T>(string queryOrProc, Dictionary<string, object>? parameters = null, bool isStoredProc = false);
    Task<int> ExecuteNonQueryAsync(string queryOrProc, Dictionary<string, object>? parameters = null, bool isStoredProc = false);
}

public class DbService : IDbService
{
    private readonly string _connectionString;

    public DbService(IConfiguration configuration)
    {
        var db = Environment.GetEnvironmentVariable("DB");
        var user = Environment.GetEnvironmentVariable("DB_USER");
        var password = Environment.GetEnvironmentVariable("DB_PASSWORD");
        _connectionString = $"Server=localhost;Database={db};User Id={user};Password={password};";


#if DEBUG
        _connectionString = configuration.GetConnectionString("DefaultConnection");
#endif
    }

    public async Task<List<T>> ExecuteQueryAsync<T>(string queryOrProc, Dictionary<string, object>? parameters = null, bool isStoredProc = false)
    {
        var result = new List<T>();
        using var conn = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand(queryOrProc, conn);

        if (isStoredProc)
            cmd.CommandType = CommandType.StoredProcedure;

        if (parameters != null)
            foreach (var param in parameters)
                cmd.Parameters.AddWithValue(param.Key, param.Value ?? DBNull.Value);

        await conn.OpenAsync();
        using var reader = await cmd.ExecuteReaderAsync();

        var type = typeof(T);
        bool isPrimitive = type.IsPrimitive || type == typeof(string) || type == typeof(decimal);
        bool isNullablePrimitive = Nullable.GetUnderlyingType(type) is Type underlying && (underlying.IsPrimitive || underlying == typeof(decimal));
        bool isValueTuple = type.FullName?.StartsWith("System.ValueTuple") == true;

        var accessor = (!isPrimitive && !isNullablePrimitive && !isValueTuple)
            ? TypeAccessor.Create(type)
            : null;

        var members = accessor?.GetMembers().Select(m => m.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);

        while (await reader.ReadAsync())
        {
            if (isPrimitive || isNullablePrimitive)
            {
                var value = await reader.IsDBNullAsync(0) ? default : reader.GetFieldValue<T>(0);
                result.Add(value!);
            }
            else if (isValueTuple)
            {
                var values = new object[reader.FieldCount];
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    values[i] = await reader.IsDBNullAsync(i) ? null! : reader.GetValue(i);
                }

                var tuple = Activator.CreateInstance(type, values);
                result.Add((T)tuple!);
            }
            else
            {
                var obj = Activator.CreateInstance<T>();
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    var colName = reader.GetName(i);
                    if (members!.Contains(colName) && !await reader.IsDBNullAsync(i))
                    {
                        accessor![obj, colName] = reader.GetValue(i);
                    }
                }
                result.Add(obj);
            }
        }

        return result;
    }

    public async Task<int> ExecuteNonQueryAsync(string queryOrProc, Dictionary<string, object>? parameters = null, bool isStoredProc = false)
    {
        using var conn = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand(queryOrProc, conn);

        if (isStoredProc)
            cmd.CommandType = CommandType.StoredProcedure;

        if (parameters != null)
            foreach (var param in parameters)
                cmd.Parameters.AddWithValue(param.Key, param.Value ?? DBNull.Value);

        await conn.OpenAsync();
        return await cmd.ExecuteNonQueryAsync();
    }
}