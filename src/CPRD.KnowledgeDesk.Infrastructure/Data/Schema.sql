PRAGMA foreign_keys = ON;
CREATE TABLE IF NOT EXISTS schema_info(version INTEGER NOT NULL);
INSERT INTO schema_info(version) SELECT 1 WHERE NOT EXISTS (SELECT 1 FROM schema_info);

CREATE TABLE IF NOT EXISTS folders(
  id TEXT PRIMARY KEY, parent_id TEXT NULL REFERENCES folders(id), name TEXT NOT NULL,
  sort_order INTEGER NOT NULL DEFAULT 0, is_archived INTEGER NOT NULL DEFAULT 0,
  created_at_utc TEXT NOT NULL, modified_at_utc TEXT NOT NULL
);
CREATE UNIQUE INDEX IF NOT EXISTS ux_folders_parent_name ON folders(IFNULL(parent_id,''), name COLLATE NOCASE);

CREATE TABLE IF NOT EXISTS notes(
  id TEXT PRIMARY KEY, title TEXT NOT NULL, content_package TEXT NOT NULL, plain_text TEXT NOT NULL,
  folder_id TEXT NOT NULL REFERENCES folders(id), note_type TEXT NOT NULL,
  is_favorite INTEGER NOT NULL DEFAULT 0, is_pinned INTEGER NOT NULL DEFAULT 0,
  is_archived INTEGER NOT NULL DEFAULT 0, created_at_utc TEXT NOT NULL, modified_at_utc TEXT NOT NULL,
  last_opened_at_utc TEXT NULL, deleted_at_utc TEXT NULL, source_type TEXT NULL,
  source_reference TEXT NULL, structured_json TEXT NOT NULL DEFAULT '{}'
);
CREATE INDEX IF NOT EXISTS ix_notes_folder ON notes(folder_id, deleted_at_utc, is_archived);

CREATE TABLE IF NOT EXISTS tags(id TEXT PRIMARY KEY, name TEXT NOT NULL, normalized_name TEXT NOT NULL UNIQUE);
CREATE TABLE IF NOT EXISTS note_tags(
  note_id TEXT NOT NULL REFERENCES notes(id) ON DELETE CASCADE,
  tag_id TEXT NOT NULL REFERENCES tags(id) ON DELETE CASCADE,
  PRIMARY KEY(note_id, tag_id)
);

CREATE VIRTUAL TABLE IF NOT EXISTS notes_fts USING fts5(
  note_id UNINDEXED, title, plain_text, tags_text, folder_path, note_type, structured_text,
  tokenize='unicode61 remove_diacritics 2'
);
