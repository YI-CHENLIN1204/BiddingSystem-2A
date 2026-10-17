using Npgsql;

var cs = Environment.GetEnvironmentVariable("TEST_POSTGRES_CONNECTION")
    ?? throw new InvalidOperationException("TEST_POSTGRES_CONNECTION required");
await using var setup = new NpgsqlConnection(cs);
await setup.OpenAsync();
await using (var cmd = new NpgsqlCommand("CREATE TABLE IF NOT EXISTS fault_harness (operation_id text PRIMARY KEY, value int NOT NULL); TRUNCATE fault_harness;", setup))
    await cmd.ExecuteNonQueryAsync();

static async Task<int> Count(NpgsqlConnection db, string operation) {
    await using var cmd = new NpgsqlCommand("SELECT count(*) FROM fault_harness WHERE operation_id=@op", db);
    cmd.Parameters.AddWithValue("op", operation);
    return Convert.ToInt32(await cmd.ExecuteScalarAsync());
}

// Case A: kill writer's session before COMMIT. Transaction must not persist.
await using (var writer = new NpgsqlConnection(cs)) {
    await writer.OpenAsync();
    var pid = writer.ProcessID;
    await using (var tx = await writer.BeginTransactionAsync()) {
        await using (var insert = new NpgsqlCommand("INSERT INTO fault_harness VALUES ('uncommitted',1)", writer, tx))
            await insert.ExecuteNonQueryAsync();
        await using var killer = new NpgsqlConnection(cs);
        await killer.OpenAsync();
        await using (var kill = new NpgsqlCommand("SELECT pg_terminate_backend(@pid)", killer)) {
            kill.Parameters.AddWithValue("pid", pid);
            if (!Convert.ToBoolean(await kill.ExecuteScalarAsync())) throw new Exception("Could not terminate writer");
        }
        try { await tx.CommitAsync(); throw new Exception("Unexpected commit after termination"); }
        catch (Exception e) when (e is NpgsqlException or InvalidOperationException or System.IO.IOException) {
            Console.WriteLine("Expected writer connection failure: " + e.GetType().Name);
        }
    }
}
await using (var check = new NpgsqlConnection(cs)) {
    await check.OpenAsync();
    if (await Count(check, "uncommitted") != 0) throw new Exception("FAIL: uncommitted row persisted");
}
Console.WriteLine("PASS: connection dropped before commit; no partial row");

// Case B: simulate COMMIT acknowledged by DB but HTTP response lost.
// Durable unique operation ID lets client reconcile rather than blindly insert again.
await using (var writer = new NpgsqlConnection(cs)) {
    await writer.OpenAsync();
    await using var tx = await writer.BeginTransactionAsync();
    await using var insert = new NpgsqlCommand("INSERT INTO fault_harness VALUES ('ack-lost',2)", writer, tx);
    await insert.ExecuteNonQueryAsync();
    await tx.CommitAsync();
}
try { throw new System.IO.IOException("Simulated HTTP response lost after successful commit"); }
catch (System.IO.IOException) { Console.WriteLine("Expected simulated lost response"); }
await using (var verify = new NpgsqlConnection(cs)) {
    await verify.OpenAsync();
    if (await Count(verify, "ack-lost") != 1) throw new Exception("FAIL: missing committed operation");
    await using var upsert = new NpgsqlCommand("INSERT INTO fault_harness VALUES ('ack-lost',2) ON CONFLICT (operation_id) DO NOTHING", verify);
    await upsert.ExecuteNonQueryAsync();
    if (await Count(verify, "ack-lost") != 1) throw new Exception("FAIL: duplicated operation");
}
Console.WriteLine("PASS: lost response can be reconciled by durable operation key");
Console.WriteLine("SCOPE: isolated Npgsql harness; DOES NOT prove application API is idempotent.");
