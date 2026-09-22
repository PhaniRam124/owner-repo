CREATE TABLE IF NOT EXISTS attachments(
  id TEXT PRIMARY KEY,
  note_id TEXT NOT NULL REFERENCES notes(id) ON DELETE CASCADE,
  filename TEXT NOT NULL,
  stored_path TEXT NOT NULL UNIQUE,
  mime_type TEXT NOT NULL,
  size_bytes INTEGER NOT NULL,
  hash_sha256 TEXT NOT NULL,
  created_at_utc TEXT NOT NULL
);
CREATE INDEX IF NOT EXISTS ix_attachments_note ON attachments(note_id);
