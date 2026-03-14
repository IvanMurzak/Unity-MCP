#!/usr/bin/env python3
"""
MCP Client for connecting to Unity-MCP Server
Fetches and displays all tools with their JSON schemas using colored output
"""

import argparse
import asyncio
import json
import os
import sys
from pathlib import Path

import httpx
from dotenv import load_dotenv
from rich.console import Console
from rich.panel import Panel
from rich.syntax import Syntax
from rich.table import Table
from rich.tree import Tree

# Fix Windows encoding issues
if sys.platform == "win32":
    os.environ["PYTHONIOENCODING"] = "utf-8"
    # Force UTF-8 for stdout/stderr
    import io
    sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8')
    sys.stderr = io.TextIOWrapper(sys.stderr.buffer, encoding='utf-8')


class MCPClient:
    """MCP Client using HTTP transport with Bearer token auth"""

    def __init__(self, server_url: str, server_token: str):
        self.server_url = server_url.rstrip('/')
        self.server_token = server_token
        self.console = Console(force_terminal=True)
        self.headers = {
            "Authorization": f"Bearer {server_token}",
            "Content-Type": "application/json"
        }
        self.client = None
        self.request_id = 1

    async def connect(self) -> bool:
        """Establish connection to MCP server"""
        self.client = httpx.AsyncClient(headers=self.headers, timeout=30.0)
        try:
            # Test connection by sending an initialization request
            self.console.print(f"🔗 Connecting to MCP Server at [bold cyan]{self.server_url}[/bold cyan]...")

            # Try to get server info - MCP servers might use specific endpoints
            for endpoint in ["/", "/mcp", "/api", "/tools"]:
                try:
                    response = await self.client.get(f"{self.server_url}{endpoint}")
                    if response.status_code in [200, 400]:
                        # If we get 400, the endpoint exists but might need specific format
                        # We can still try to fetch tools
                        self.console.print("[green]✓ Connected to MCP Server[/green]")
                        return True
                except Exception:
                    continue

            # If no endpoint responded, assume we can still try to fetch tools
            self.console.print("[yellow]⚠ Could not verify connection, attempting to fetch tools...[/yellow]")
            return True

        except Exception as e:
            self.console.print(f"[red]✗ Connection error: {e}[/red]")
            return False

    async def fetch_tools(self) -> list:
        """Fetch all available MCP tools with their schemas"""
        try:
            # Try different endpoints and methods
            endpoints_to_try = [
                ("GET", f"{self.server_url}/tools"),
                ("GET", f"{self.server_url}/api/tools"),
                ("POST", f"{self.server_url}/mcp"),
            ]

            for method, url in endpoints_to_try:
                try:
                    if method == "GET":
                        response = await self.client.get(url)
                    else:
                        payload = {
                            "jsonrpc": "2.0",
                            "id": self.request_id,
                            "method": "tools/list",
                            "params": {}
                        }
                        self.request_id += 1
                        response = await self.client.post(url, json=payload)

                    if response.status_code == 200:
                        data = response.json()

                        # Try various key patterns
                        if "result" in data and "tools" in data["result"]:
                            return data["result"]["tools"]
                        elif "tools" in data:
                            return data["tools"]
                        elif isinstance(data, list):
                            return data
                        elif "data" in data and isinstance(data["data"], list):
                            return data["data"]
                except Exception as e:
                    continue

            self.console.print(f"[yellow]⚠ Could not find tools from any endpoint[/yellow]")
            return []

        except Exception as e:
            self.console.print(f"[red]Error fetching tools: {e}[/red]")
            return []

    def print_tools(self, tools: list, filter_str: str = None):
        """Print tools with full schemas using colored output

        Args:
            tools: List of tools to display
            filter_str: Optional substring to filter tools by name
        """
        if not tools:
            self.console.print("[yellow]No tools found[/yellow]")
            return

        # Filter tools if filter string provided
        if filter_str:
            tools = [t for t in tools if filter_str.lower() in t.get("name", "").lower()]
            if not tools:
                self.console.print(f"[yellow]No tools match filter: '{filter_str}'[/yellow]")
                return

        self.console.print(f"\n[bold magenta]📚 Available Tools ({len(tools)} total)[/bold magenta]\n")

        for i, tool in enumerate(tools, 1):
            # Create a panel for each tool
            tool_name = tool.get("name", "Unknown")
            tool_description = tool.get("description", "No description")

            # Create input schema display
            input_schema = tool.get("inputSchema", {})
            output_schema = tool.get("outputSchema", {})

            # Build tool info content
            content = f"[bold]Description:[/bold]\n{tool_description}\n\n"

            # Input Schema
            if input_schema:
                content += "[bold cyan]📥 Input Schema:[/bold cyan]\n"
                schema_json = json.dumps(input_schema, indent=2)
                content += f"[dim]{schema_json}[/dim]\n\n"
            else:
                content += "[dim]No input schema[/dim]\n\n"

            # Output Schema
            if output_schema:
                content += "[bold green]📤 Output Schema:[/bold green]\n"
                schema_json = json.dumps(output_schema, indent=2)
                content += f"[dim]{schema_json}[/dim]"
            else:
                content += "[dim]No output schema[/dim]"

            # Create panel with tool info
            panel = Panel(
                content,
                title=f"[bold yellow]{i}. {tool_name}[/bold yellow]",
                border_style="blue",
                expand=False
            )
            self.console.print(panel)

        # Summary table
        self.console.print("\n[bold magenta]📊 Tools Summary[/bold magenta]\n")
        table = Table(title="Tool Overview", show_header=True, header_style="bold magenta")
        table.add_column("Tool Name", style="cyan")
        table.add_column("Has Input Schema", style="green")
        table.add_column("Has Output Schema", style="blue")

        for tool in tools:
            tool_name = tool.get("name", "Unknown")
            has_input = "✓" if tool.get("inputSchema") else "✗"
            has_output = "✓" if tool.get("outputSchema") else "✗"
            table.add_row(tool_name, has_input, has_output)

        self.console.print(table)

    async def disconnect(self):
        """Close connection"""
        if self.client:
            await self.client.aclose()
            self.console.print("\n[green]✓ Disconnected from MCP Server[/green]")


async def main():
    """Main entry point"""

    # Parse command-line arguments
    parser = argparse.ArgumentParser(
        description="MCP Client for connecting to Unity-MCP Server"
    )
    parser.add_argument(
        "--filter",
        "-f",
        type=str,
        default=None,
        help="Filter tools by name substring (case-insensitive)"
    )
    args = parser.parse_args()

    # Load environment variables
    load_dotenv()

    server_url = os.getenv("SERVER_URL", "http://localhost:8080")
    server_token = os.getenv("SERVER_TOKEN", "")

    if not server_token:
        console = Console()
        console.print("[red]Error: SERVER_TOKEN not set in .env file[/red]")
        sys.exit(1)

    # Create and connect client
    client = MCPClient(server_url, server_token)

    if not await client.connect():
        sys.exit(1)

    try:
        # Fetch and display tools
        tools = await client.fetch_tools()
        client.print_tools(tools, filter_str=args.filter)

    except KeyboardInterrupt:
        print("\n")
    finally:
        await client.disconnect()


if __name__ == "__main__":
    asyncio.run(main())
