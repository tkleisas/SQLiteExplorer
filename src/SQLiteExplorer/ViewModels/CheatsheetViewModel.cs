using CommunityToolkit.Mvvm.ComponentModel;

namespace SQLiteExplorer.ViewModels;

public partial class CheatsheetViewModel : ObservableObject
{
    [ObservableProperty]
    private bool _isVisible;

    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    private string _content = string.Empty;
}

public static class Cheatsheets
{
    public static readonly string Sqlite = """
# SQLite Cheatsheet

## Data Types
- `INTEGER` - Signed integer (1, 2, 3, 4, 6, or 8 bytes)
- `TEXT` - Text string (UTF-8, UTF-16BE, UTF-16LE)
- `REAL` - Floating point value (8-byte IEEE)
- `BLOB` - Binary data
- `NUMERIC` - Can contain INTEGER or REAL

## Creating Tables
```sql
CREATE TABLE users (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    name TEXT NOT NULL,
    email TEXT UNIQUE,
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP
);
```

## Basic Queries
```sql
SELECT * FROM table_name;
SELECT col1, col2 FROM table_name WHERE condition;
INSERT INTO table_name (col1, col2) VALUES (val1, val2);
UPDATE table_name SET col1 = val1 WHERE condition;
DELETE FROM table_name WHERE condition;
```

## Filtering
```sql
SELECT * FROM table WHERE col LIKE '%pattern%';
SELECT * FROM table WHERE col IN (1, 2, 3);
SELECT * FROM table WHERE col BETWEEN 10 AND 20;
SELECT * FROM table WHERE col IS NULL;
SELECT DISTINCT col FROM table;
```

## Sorting & Limiting
```sql
SELECT * FROM table ORDER BY col ASC;
SELECT * FROM table ORDER BY col1, col2 DESC;
SELECT * FROM table LIMIT 10;
SELECT * FROM table LIMIT 10 OFFSET 20;
```

## Joins
```sql
SELECT * FROM t1 INNER JOIN t2 ON t1.id = t2.id;
SELECT * FROM t1 LEFT JOIN t2 ON t1.id = t2.id;
SELECT * FROM t1 CROSS JOIN t2;
```

## Aggregation
```sql
SELECT COUNT(*) FROM table;
SELECT SUM(col), AVG(col), MIN(col), MAX(col) FROM table;
SELECT col, COUNT(*) FROM table GROUP BY col;
SELECT col, COUNT(*) FROM table GROUP BY col HAVING COUNT(*) > 1;
```

## Table Info
```sql
.tables                    -- List all tables
.schema table_name         -- Show table schema
PRAGMA table_info(table);  -- Column details
PRAGMA index_list(table);  -- List indexes
```

## Alter Table
```sql
ALTER TABLE table ADD COLUMN col TEXT;
ALTER TABLE table RENAME TO new_table;
ALTER TABLE table RENAME COLUMN old TO new;
```

## Indexes
```sql
CREATE INDEX idx_name ON table(col);
CREATE UNIQUE INDEX idx_name ON table(col);
DROP INDEX idx_name;
```

## Transactions
```sql
BEGIN TRANSACTION;
-- statements
COMMIT;
-- or
ROLLBACK;
```
""";

    public static readonly string Postgres = """
# PostgreSQL Cheatsheet

## Data Types
- `SMALLINT`, `INTEGER`, `BIGINT` - Integer types
- `SERIAL`, `BIGSERIAL` - Auto-incrementing
- `DECIMAL(p,s)`, `NUMERIC(p,s)` - Exact numeric
- `REAL`, `DOUBLE PRECISION` - Floating point
- `TEXT`, `VARCHAR(n)`, `CHAR(n)` - Character types
- `BOOLEAN` - true/false
- `DATE`, `TIME`, `TIMESTAMP` - Date/time
- `UUID` - Universally unique identifier
- `JSONB` - Binary JSON
- `ARRAY` - Array type

## Creating Tables
```sql
CREATE TABLE users (
    id SERIAL PRIMARY KEY,
    name VARCHAR(255) NOT NULL,
    email VARCHAR(255) UNIQUE,
    created_at TIMESTAMP DEFAULT NOW()
);
```

## Basic Queries
```sql
SELECT * FROM table_name;
SELECT col1, col2 FROM table_name WHERE condition;
INSERT INTO table_name (col1, col2) VALUES (val1, val2);
UPDATE table_name SET col1 = val1 WHERE condition;
DELETE FROM table_name WHERE condition;
```

## Returning Values (PostgreSQL-specific)
```sql
INSERT INTO users (name) VALUES ('John') RETURNING id;
UPDATE users SET name = 'Jane' RETURNING *;
DELETE FROM users WHERE id = 1 RETURNING *;
```

## Filtering
```sql
SELECT * FROM table WHERE col ILIKE '%pattern%';  -- Case-insensitive
SELECT * FROM table WHERE col ~ '^pattern';       -- Regex match
SELECT * FROM table WHERE col !~ 'pattern';       -- Regex not match
SELECT * FROM table WHERE col IN (1, 2, 3);
SELECT * FROM table WHERE col BETWEEN 10 AND 20;
SELECT * FROM table WHERE col IS NULL;
SELECT DISTINCT col FROM table;
```

## Sorting & Limiting
```sql
SELECT * FROM table ORDER BY col ASC NULLS FIRST;
SELECT * FROM table ORDER BY col DESC NULLS LAST;
SELECT * FROM table LIMIT 10;
SELECT * FROM table LIMIT 10 OFFSET 20;
SELECT * FROM table FETCH FIRST 10 ROWS ONLY;
```

## Joins
```sql
SELECT * FROM t1 INNER JOIN t2 ON t1.id = t2.id;
SELECT * FROM t1 LEFT JOIN t2 ON t1.id = t2.id;
SELECT * FROM t1 RIGHT JOIN t2 ON t1.id = t2.id;
SELECT * FROM t1 FULL OUTER JOIN t2 ON t1.id = t2.id;
SELECT * FROM t1 CROSS JOIN t2;
SELECT * FROM t1 NATURAL JOIN t2;
```

## Aggregation
```sql
SELECT COUNT(*) FROM table;
SELECT SUM(col), AVG(col), MIN(col), MAX(col) FROM table;
SELECT col, COUNT(*) FROM table GROUP BY col;
SELECT col, COUNT(*) FROM table GROUP BY col HAVING COUNT(*) > 1;
SELECT STRING_AGG(col, ',') FROM table;  -- Like GROUP_CONCAT
```

## Common Table Expressions
```sql
WITH cte_name AS (
    SELECT * FROM table WHERE condition
)
SELECT * FROM cte_name;

-- Recursive CTE
WITH RECURSIVE cte AS (
    SELECT 1 AS n
    UNION ALL
    SELECT n + 1 FROM cte WHERE n < 10
)
SELECT * FROM cte;
```

## JSON Operations
```sql
SELECT data->>'key' FROM table;           -- Get JSON field as text
SELECT data->'key' FROM table;            -- Get JSON field as JSON
SELECT data#>'{key1,key2}' FROM table;   -- Nested path
UPDATE table SET data = data || '{"k":"v"}'::jsonb;
```

## Window Functions
```sql
SELECT col, ROW_NUMBER() OVER (ORDER BY col) FROM table;
SELECT col, RANK() OVER (PARTITION BY grp ORDER BY val) FROM table;
SELECT col, LAG(col) OVER (ORDER BY col) FROM table;
SELECT col, LEAD(col) OVER (ORDER BY col) FROM table;
```

## Table Info
```sql
\dt                        -- List tables (psql)
\d table_name              -- Describe table (psql)
SELECT * FROM information_schema.tables;
SELECT * FROM information_schema.columns WHERE table_name = 'table';
```

## Alter Table
```sql
ALTER TABLE table ADD COLUMN col VARCHAR(255);
ALTER TABLE table DROP COLUMN col;
ALTER TABLE table RENAME COLUMN old TO new;
ALTER TABLE table ALTER COLUMN col TYPE VARCHAR(500);
ALTER TABLE table RENAME TO new_table;
```

## Indexes
```sql
CREATE INDEX idx_name ON table(col);
CREATE UNIQUE INDEX idx_name ON table(col);
CREATE INDEX idx_name ON table USING gin(col);  -- For JSONB, arrays
DROP INDEX idx_name;
```

## Transactions
```sql
BEGIN;
-- statements
COMMIT;
-- or
ROLLBACK;
-- Savepoints
SAVEPOINT my_savepoint;
ROLLBACK TO my_savepoint;
```
""";
}
