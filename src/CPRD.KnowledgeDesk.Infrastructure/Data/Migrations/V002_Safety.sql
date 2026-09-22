CREATE TABLE IF NOT EXISTS note_revisions(
  id INTEGER PRIMARY KEY AUTOINCREMENT,
  note_id TEXT NOT NULL REFERENCES notes(id) ON DELETE CASCADE,
  revision_number INTEGER NOT NULL,
  title_snapshot TEXT NOT NULL,
  content_package_snapshot TEXT NOT NULL,
  plain_text_snapshot TEXT NOT NULL,
  structured_json_snapshot TEXT NOT NULL,
  created_at_utc TEXT NOT NULL,
  UNIQUE(note_id, revision_number)
);
CREATE INDEX IF NOT EXISTS ix_note_revisions_note
ON note_revisions(note_id, revision_number DESC);
