using Microsoft.Data.Sqlite;
using MonitorAgent.Models;

namespace MonitorAgent.Storage;

/// <summary>
/// Fila local em SQLite. Usada só quando a API está inacessível: o sinal é
/// enfileirado e reenviado nos ciclos seguintes, garantindo entrega (at-least-once).
/// </summary>
public sealed class LocalStore
{
    private readonly string _connectionString;

    public LocalStore(string databasePath)
    {
        var dir = Path.GetDirectoryName(databasePath);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = databasePath,
            Mode = SqliteOpenMode.ReadWriteCreate,
        }.ToString();
    }

    public void Initialize()
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS pending_signals (
                Id           INTEGER PRIMARY KEY AUTOINCREMENT,
                Hostname     TEXT NOT NULL,
                Usuario      TEXT NOT NULL,
                TimestampUtc TEXT NOT NULL,
                TituloJanela TEXT NOT NULL,
                Processo     TEXT NOT NULL
            );
            """;
        cmd.ExecuteNonQuery();
    }

    /// <summary>Enfileira um sinal que não pôde ser entregue à API.</summary>
    public void Enqueue(Signal signal)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO pending_signals (Hostname, Usuario, TimestampUtc, TituloJanela, Processo)
            VALUES ($host, $user, $ts, $title, $proc);
            """;
        cmd.Parameters.AddWithValue("$host", signal.Hostname);
        cmd.Parameters.AddWithValue("$user", signal.Usuario);
        cmd.Parameters.AddWithValue("$ts", signal.TimestampUtc.ToString("o"));
        cmd.Parameters.AddWithValue("$title", signal.TituloJanela);
        cmd.Parameters.AddWithValue("$proc", signal.Processo);
        cmd.ExecuteNonQuery();
    }

    /// <summary>Lê um lote de sinais pendentes (mais antigos primeiro).</summary>
    public IReadOnlyList<Signal> Dequeue(int limit)
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT Id, Hostname, Usuario, TimestampUtc, TituloJanela, Processo
            FROM pending_signals
            ORDER BY Id
            LIMIT $limit;
            """;
        cmd.Parameters.AddWithValue("$limit", limit);

        var result = new List<Signal>();
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            result.Add(new Signal
            {
                LocalId = reader.GetInt64(0),
                Hostname = reader.GetString(1),
                Usuario = reader.GetString(2),
                TimestampUtc = DateTimeOffset.Parse(reader.GetString(3)),
                TituloJanela = reader.GetString(4),
                Processo = reader.GetString(5),
            });
        }
        return result;
    }

    /// <summary>Remove sinais já entregues à API.</summary>
    public void Delete(IEnumerable<long> ids)
    {
        var idList = ids.ToList();
        if (idList.Count == 0)
            return;

        using var conn = Open();
        using var tx = conn.BeginTransaction();
        using var cmd = conn.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = "DELETE FROM pending_signals WHERE Id = $id;";
        var p = cmd.CreateParameter();
        p.ParameterName = "$id";
        cmd.Parameters.Add(p);

        foreach (var id in idList)
        {
            p.Value = id;
            cmd.ExecuteNonQuery();
        }
        tx.Commit();
    }

    /// <summary>Quantidade de sinais aguardando reenvio.</summary>
    public long PendingCount()
    {
        using var conn = Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM pending_signals;";
        return (long)(cmd.ExecuteScalar() ?? 0L);
    }

    private SqliteConnection Open()
    {
        var conn = new SqliteConnection(_connectionString);
        conn.Open();
        return conn;
    }
}
