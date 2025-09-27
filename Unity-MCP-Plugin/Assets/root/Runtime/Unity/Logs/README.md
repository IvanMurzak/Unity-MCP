# File-Based Logging System

This implementation provides persistent logging that survives Unity domain reloads while maintaining memory efficiency.

## Architecture

### LogUtils.cs
- **Short-term Memory Cache**: Maintains last 100 log entries in memory for fast access
- **Immediate File Persistence**: Each log entry is saved to file immediately upon receipt
- **Memory Cleanup**: Automatically clears memory cache after writing to file
- **Efficient Querying**: `GetLastLogs()` method efficiently retrieves recent entries with filtering

### LogFileManager.cs
- **File Location**: `<project>/Temp/mcp-server/editor-logs.txt`
- **JSON Storage**: Each log entry stored as JSON for structured data
- **File Rotation**: Automatically rotates when file exceeds 10MB or 10,000 entries
- **Thread-Safe**: All file operations are protected with locks
- **Efficient Reading**: Reads from end of file backwards for recent entries

## Key Features

### Memory Efficiency
- Reduced from 5000 to 100 entries in memory cache
- Immediate cleanup after file write
- File-based storage prevents memory bloat

### Persistence
- Logs survive Unity domain reloads
- Immediate file writing ensures no data loss
- Structured JSON format for future extensibility

### Performance
- Reverse file reading for efficient "last N entries" queries
- Memory cache for fastest access to recent logs
- Duplicate detection when merging file and memory data

### File Management
- Automatic directory creation
- File rotation to prevent unlimited growth
- Graceful error handling for I/O operations
- Configurable size limits (10MB/10k entries)

## API Compatibility

All existing `Console.GetLogs` functionality is preserved:
- `maxEntries`: Limit number of returned entries
- `logTypeFilter`: Filter by LogType (Log, Warning, Error, etc.)
- `includeStackTrace`: Include stack traces in output
- `lastMinutes`: Time-based filtering

## Usage Example

```csharp
// Get last 50 log entries
var logs = LogUtils.GetLastLogs(50);

// Get last 20 error logs
var errorLogs = LogUtils.GetLastLogs(20, LogType.Error);

// Get logs from last 5 minutes
var recentLogs = LogUtils.GetLastLogs(100, null, DateTime.Now.AddMinutes(-5));

// Clear all logs (memory and file)
LogUtils.ClearLogs();
```

## File Format

Each log entry is stored as a single line of JSON:
```json
{"message":"Test log","stackTrace":"","logType":3,"timestamp":"2025-01-22T10:30:45.123Z","logTypeString":"Log"}
```

## Testing

Comprehensive test coverage includes:
- File persistence across domain reloads
- Memory cache management
- Log type filtering
- Time-based filtering
- Thread safety
- Error handling
- Integration with Console.GetLogs