using CPRD.KnowledgeDesk.Infrastructure.Data;
using Microsoft.Data.Sqlite;

namespace CPRD.KnowledgeDesk.Infrastructure.Repositories;

public sealed class SearchRepository
{
    public SearchRepository(KnowledgeDb db) { }

    public async Task RefreshAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        Guid noteId,
        CancellationToken cancellationToken)
    {
        var id = noteId.ToString("D");
        var folderPath = await GetFolderPathAsync(connection, transaction, id, cancellationToken);

        await using (var delete = connection.CreateCommand())
        {
            delete.Transaction = transaction;
            delete.CommandText = "DELETE FROM notes_fts WHERE note_id=$id";
            delete.Parameters.AddWithValue("$id", id);
            await delete.ExecuteNonQueryAsync(cancellationToken);
        }

        await using var insert = connection.CreateCommand();
        insert.Transaction = transaction;
        insert.CommandText = """
            INSERT INTO notes_fts(note_id,title,plain_text,tags_text,folder_path,note_type,structured_text)
            SELECT n.id,n.title,n.plain_text,
                   COALESCE((SELECT group_concat(t.name,' ') FROM note_tags nt JOIN tags t ON t.id=nt.tag_id WHERE nt.note_id=n.id),''),
                   $folderPath,n.note_type,n.structured_json
              FROM notes n
             WHERE n.id=$id AND n.deleted_at_utc IS NULL
            """;
        insert.Parameters.AddWithValue("$id", id);
        insert.Parameters.AddWithValue("$folderPath", folderPath);
        await insert.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task RefreshFolderSubtreeAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        Guid folderId,
        CancellationToken cancellationToken)
    {
        var noteIds = new List<Guid>();
        await using (var command = connection.CreateCommand())
        {
            command.Transaction = transaction;
            command.CommandText = """
                WITH RECURSIVE subtree(id) AS (
                  SELECT $folderId
                  UNION ALL
                  SELECT f.id FROM folders f JOIN subtree s ON f.parent_id=s.id
                )
                SELECT n.id
                  FROM notes n
                 WHERE n.folder_id IN (SELECT id FROM subtree)
                   AND n.deleted_at_utc IS NULL
                """;
            command.Parameters.AddWithValue("$folderId", folderId.ToString("D"));
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
                noteIds.Add(Guid.Parse(reader.GetString(0)));
        }

        foreach (var noteId in noteIds)
            await RefreshAsync(connection, transaction, noteId, cancellationToken);
    }

    private static async Task<string> GetFolderPathAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        string noteId,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            WITH RECURSIVE path(id,parent_id,name,depth) AS (
              SELECT f.id,f.parent_id,f.name,0
                FROM folders f JOIN notes n ON n.folder_id=f.id
               WHERE n.id=$noteId
              UNION ALL
              SELECT f.id,f.parent_id,f.name,p.depth+1
                FROM folders f JOIN path p ON p.parent_id=f.id
            )
            SELECT COALESCE(group_concat(name,' > '),'')
              FROM (SELECT name FROM path ORDER BY depth DESC)
            """;
        command.Parameters.AddWithValue("$noteId", noteId);
        return Convert.ToString(await command.ExecuteScalarAsync(cancellationToken)) ?? string.Empty;
    }
}
