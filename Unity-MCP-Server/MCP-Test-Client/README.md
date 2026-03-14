# Unity MCP Test Client

Python MCP client for connecting to the Unity-MCP Server with colored output and full schema inspection.

## Installation

### Prerequisites

- Python 3.9+
- [UV package manager](https://docs.astral.sh/uv/getting-started/installation/) (fast, written in Rust)

### Setup

```bash
# Install dependencies (creates virtual environment in .venv)
uv sync
```

## Configuration

Edit the `.env` file with your MCP server credentials:

```env
SERVER_URL=http://localhost:9099
SERVER_TOKEN=your-token-here
```

## Usage

### Basic Usage

Display all 56 available MCP tools with their full JSON schemas:

```bash
uv run python mcp_client.py
```

### Filter Tools

Search for tools by name (case-insensitive):

```bash
# Show only gameobject-related tools
uv run python mcp_client.py --filter gameobject

# Show only asset-related tools (short flag)
uv run python mcp_client.py -f assets

# Show scene tools
uv run python mcp_client.py -f scene

# Show component tools
uv run python mcp_client.py --filter component
```

### Get Help

```bash
uv run python mcp_client.py --help
```

## Output

The client displays tools in organized panels showing:

- **Tool Name** — The kebab-case identifier
- **Description** — What the tool does
- **Input Schema** — Complete JSON schema with all properties, types, and requirements
- **Output Schema** — Complete JSON schema of the tool's response
- **Summary Table** — Overview of all tools with schema availability

## Features

| Feature | Description |
|---------|-------------|
| 🔗 **Bearer Auth** | Connects via HTTP with Bearer token authentication |
| 📚 **56 Tools** | Fetches all available Unity-MCP tools |
| 📥 **Input Schemas** | Displays complete JSON schemas for tool inputs |
| 📤 **Output Schemas** | Shows response schemas with type definitions |
| 🎨 **Colored Output** | Rich formatting with panels, tables, and syntax highlighting |
| 🔍 **Filtering** | Search tools by name substring |

## Examples

### View all tools
```bash
uv run python mcp_client.py
```

### Find gameobject tools
```bash
uv run python mcp_client.py -f gameobject
```

Output shows 11 matching tools:
- gameobject-component-add
- gameobject-component-list-all
- gameobject-component-remove
- gameobject-create
- gameobject-delete
- gameobject-find
- gameobject-instantiate
- gameobject-move
- gameobject-rename
- gameobject-set-active
- gameobject-set-layer

### Find asset tools
```bash
uv run python mcp_client.py -f assets
```

Output shows 16 matching tools for asset management.

## UV Commands Reference

```bash
# Install dependencies
uv sync

# Add a new package
uv add package-name

# Add a dev dependency
uv add --dev package-name

# Activate the virtual environment
source .venv/bin/activate     # Linux/macOS
.venv\Scripts\activate        # Windows PowerShell

# Run Python directly in venv
uv run python script.py

# Specify Python version
uv run --python 3.11 python mcp_client.py

# View lock file
cat uv.lock

# Update dependencies
uv lock --upgrade
```

## About UV

UV is a fast Python package manager written in Rust:

- ⚡ **10-100x faster** than pip/poetry
- 📦 **Lock files** for reproducible builds
- 🐍 **Python version management** built-in
- 🔒 **Secure** by design
- 🌍 **Compatible** with pip/poetry/venv

[Learn more →](https://docs.astral.sh/uv/)

## Project Structure

```
MCP-Test-Client/
├── mcp_client.py       # Main MCP client script
├── pyproject.toml      # Project config and dependencies
├── uv.lock             # Dependency lock file
├── .env                # Server configuration
├── .venv/              # Virtual environment (auto-created)
└── README.md           # This file
```

## Troubleshooting

### Issue: "SERVER_TOKEN not set"
**Fix:** Update your `.env` file with valid credentials.

### Issue: "Connection refused"
**Fix:** Ensure MCP server is running at the URL specified in `.env`.

### Issue: "No tools found"
**Fix:** Verify Bearer token is correct and server is responding.

## License

MIT
