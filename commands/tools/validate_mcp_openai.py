#!/usr/bin/env python3
"""
MCP Tool JSON Validator using OpenAI API
Validates MCP tool JSON by actually injecting it into OpenAI API.
Tests if the tool definition is accepted by OpenAI's function calling system.
This is a real "battle test" - if OpenAI API accepts it, it's valid!
"""

import json
import sys
import os
from pathlib import Path

# Try to load .env file from script directory
try:
    from dotenv import load_dotenv
    # Get the directory where this script is located
    script_dir = Path(__file__).parent
    env_path = script_dir / '.env'
    load_dotenv(dotenv_path=env_path)
except ImportError:
    # python-dotenv not installed, will use system environment variables only
    pass

try:
    from openai import OpenAI
except ImportError:
    print("Error: openai library not found.")
    print("Install it with: pip install openai")
    sys.exit(1)

# Try to import colorama for Windows color support
try:
    from colorama import init, Fore, Style
    init(autoreset=True)
    HAS_COLORAMA = True
except ImportError:
    HAS_COLORAMA = False

# ANSI color codes
class Colors:
    if HAS_COLORAMA:
        GREEN = Fore.GREEN
        RED = Fore.RED
        ORANGE = Fore.YELLOW
        BLUE = Fore.CYAN
        RESET = Style.RESET_ALL
        BOLD = Style.BRIGHT
    else:
        GREEN = '\033[92m'
        RED = '\033[91m'
        ORANGE = '\033[93m'
        BLUE = '\033[96m'
        RESET = '\033[0m'
        BOLD = '\033[1m'


def get_openai_client():
    """Get OpenAI client with API key from environment or .env file."""
    api_key = os.getenv('OPENAI_API_KEY')
    if not api_key:
        script_dir = Path(__file__).parent
        print(f"{Colors.RED}Error: OPENAI_API_KEY not found.{Colors.RESET}")
        print(f"\nOption 1: Create a .env file in the script directory:")
        print(f"  Location: {script_dir}/.env")
        print(f"  Content:  OPENAI_API_KEY=sk-your-api-key-here")
        print(f"\nOption 2: Set environment variable:")
        print(f"  Linux/Mac: export OPENAI_API_KEY='sk-your-api-key-here'")
        print(f"  Windows:   set OPENAI_API_KEY=sk-your-api-key-here")
        sys.exit(1)
    return OpenAI(api_key=api_key)


def convert_mcp_to_openai_tool(mcp_tool):
    """
    Convert MCP tool definition to OpenAI function calling format.

    Args:
        mcp_tool: MCP tool definition from the JSON

    Returns:
        OpenAI tool definition
    """
    return {
        "type": "function",
        "function": {
            "name": mcp_tool.get("name", "unknown"),
            "description": mcp_tool.get("description", ""),
            "parameters": mcp_tool.get("inputSchema", {})
        }
    }


def validate_with_openai(json_content, filename):
    """
    Validate MCP tool JSON by actually injecting it into OpenAI API.

    Args:
        json_content: The parsed JSON content
        filename: Name of the file being validated

    Returns:
        Validation result dictionary
    """
    client = get_openai_client()

    validation_results = {
        "isValid": True,
        "errors": [],
        "warnings": [],
        "info": [],
        "summary": ""
    }

    try:
        # Extract tools from MCP JSON
        tools_list = []

        # Check if it's wrapped in a result object (like the example)
        if "result" in json_content and "tools" in json_content["result"]:
            mcp_tools = json_content["result"]["tools"]
        elif "tools" in json_content:
            mcp_tools = json_content["tools"]
        elif isinstance(json_content, list):
            mcp_tools = json_content
        else:
            # Assume single tool
            mcp_tools = [json_content]

        print(f"{Colors.BLUE}🔍 Injecting {len(mcp_tools)} tool(s) into OpenAI API...{Colors.RESET}")

        # Convert each MCP tool to OpenAI format
        for i, mcp_tool in enumerate(mcp_tools):
            try:
                openai_tool = convert_mcp_to_openai_tool(mcp_tool)
                tools_list.append(openai_tool)
                print(f"{Colors.BLUE}  ├─ Tool {i+1}: {openai_tool['function']['name']}{Colors.RESET}")
            except Exception as e:
                validation_results["isValid"] = False
                validation_results["errors"].append({
                    "severity": "error",
                    "location": f"tools[{i}]",
                    "message": f"Failed to convert MCP tool to OpenAI format: {str(e)}",
                    "suggestion": "Check that the tool has required fields: name, description, inputSchema"
                })

        if not tools_list:
            validation_results["isValid"] = False
            validation_results["errors"].append({
                "severity": "error",
                "location": "root",
                "message": "No valid tools found in the JSON",
                "suggestion": "Ensure the JSON contains a 'tools' array with tool definitions"
            })
            return validation_results

        # Try to make a test API call with the tools to validate them
        print(f"{Colors.BLUE}  └─ Testing with OpenAI API...{Colors.RESET}")

        response = client.chat.completions.create(
            model="gpt-4o-mini",  # Use cheaper model for validation
            messages=[
                {
                    "role": "user",
                    "content": "Hello"
                }
            ],
            tools=tools_list,
            tool_choice="none"  # Don't actually call the tools
        )

        # If we got here, the tools are valid!
        validation_results["isValid"] = True
        validation_results["summary"] = f"Successfully validated {len(tools_list)} tool(s) with OpenAI API"
        validation_results["info"].append({
            "severity": "info",
            "message": f"All {len(tools_list)} tool definition(s) accepted by OpenAI API"
        })

        # Check for missing descriptions or other best practices
        for i, mcp_tool in enumerate(mcp_tools):
            tool_name = mcp_tool.get("name", f"tool_{i}")

            if not mcp_tool.get("description"):
                validation_results["warnings"].append({
                    "severity": "warning",
                    "location": f"tools[{i}].description",
                    "message": f"Tool '{tool_name}' is missing a description",
                    "suggestion": "Add a description to help the AI understand when to use this tool"
                })

        return validation_results

    except Exception as e:
        # Parse OpenAI API error for specific validation issues
        error_message = str(e)

        validation_results["isValid"] = False

        # Try to extract specific error details
        if "Invalid schema" in error_message or "invalid" in error_message.lower():
            validation_results["errors"].append({
                "severity": "error",
                "location": "inputSchema",
                "message": f"OpenAI API rejected the schema: {error_message}",
                "suggestion": "Check that inputSchema follows JSON Schema Draft 7 specification"
            })
        else:
            validation_results["errors"].append({
                "severity": "error",
                "location": "API call",
                "message": f"OpenAI API error: {error_message}",
                "suggestion": "Review the full error message and check your tool definition"
            })

        validation_results["summary"] = "Validation failed - OpenAI API rejected the tool definition"

        return validation_results


def print_validation_results(result, filename):
    """Print validation results in a colorized, readable format."""
    if not result:
        return False

    is_valid = result.get('isValid', False)
    errors = result.get('errors', [])
    warnings = result.get('warnings', [])
    info = result.get('info', [])
    summary = result.get('summary', '')

    # Print header
    print("\n" + "="*70)
    if is_valid:
        print(f"{Colors.GREEN}{Colors.BOLD}✓ VALIDATION PASSED{Colors.RESET}")
    else:
        print(f"{Colors.RED}{Colors.BOLD}✗ VALIDATION FAILED{Colors.RESET}")
    print("="*70)

    # Print summary
    if summary:
        print(f"\n{Colors.BOLD}Summary:{Colors.RESET}")
        print(f"  {summary}")

    # Print errors
    if errors:
        print(f"\n{Colors.RED}{Colors.BOLD}ERRORS ({len(errors)}):{Colors.RESET}")
        for i, error in enumerate(errors, 1):
            print(f"\n  {Colors.RED}[{i}] {error.get('message', 'Unknown error')}{Colors.RESET}")
            if 'location' in error:
                print(f"      {Colors.BOLD}Location:{Colors.RESET} {error['location']}")
            if 'suggestion' in error:
                print(f"      {Colors.BOLD}Fix:{Colors.RESET} {error['suggestion']}")

    # Print warnings
    if warnings:
        print(f"\n{Colors.ORANGE}{Colors.BOLD}WARNINGS ({len(warnings)}):{Colors.RESET}")
        for i, warning in enumerate(warnings, 1):
            print(f"\n  {Colors.ORANGE}[{i}] {warning.get('message', 'Unknown warning')}{Colors.RESET}")
            if 'location' in warning:
                print(f"      {Colors.BOLD}Location:{Colors.RESET} {warning['location']}")
            if 'suggestion' in warning:
                print(f"      {Colors.BOLD}Recommendation:{Colors.RESET} {warning['suggestion']}")

    # Print info
    if info:
        print(f"\n{Colors.BLUE}{Colors.BOLD}INFORMATION ({len(info)}):{Colors.RESET}")
        for i, item in enumerate(info, 1):
            print(f"  {Colors.BLUE}• {item.get('message', '')}{Colors.RESET}")

    # Print footer
    print("\n" + "="*70)
    print(f"File: {filename}")
    print(f"Status: {Colors.GREEN}VALID{Colors.RESET}" if is_valid else f"Status: {Colors.RED}INVALID{Colors.RESET}")
    if errors or warnings:
        print(f"Issues: {Colors.RED}{len(errors)} error(s){Colors.RESET}, {Colors.ORANGE}{len(warnings)} warning(s){Colors.RESET}")
    print("="*70 + "\n")

    return is_valid


def validate_mcp_tool_file(filename):
    """
    Validate an MCP tool JSON file.

    Args:
        filename: Path to the MCP tool JSON file

    Returns:
        True if valid, False otherwise
    """
    file_path = Path(filename)

    # Check if file exists
    if not file_path.exists():
        print(f"{Colors.RED}Error: File '{filename}' not found.{Colors.RESET}")
        return False

    # Read and parse the JSON file
    try:
        with open(file_path, 'r', encoding='utf-8') as f:
            json_content = json.load(f)
    except json.JSONDecodeError as e:
        print(f"{Colors.RED}Error: Invalid JSON format in '{filename}'{Colors.RESET}")
        print(f"  Line {e.lineno}, Column {e.colno}: {e.msg}")
        return False
    except Exception as e:
        print(f"{Colors.RED}Error reading file: {e}{Colors.RESET}")
        return False

    print(f"{Colors.BLUE}Validating MCP Tool JSON: {filename}{Colors.RESET}")

    # Validate using OpenAI
    result = validate_with_openai(json_content, filename)

    if result:
        return print_validation_results(result, filename)
    else:
        return False


def main():
    """Main function to handle command line arguments and user input."""
    filename = None

    # Check if filename was provided as command line argument
    if len(sys.argv) > 1:
        filename = sys.argv[1]
    else:
        # Prompt user for filename
        print(f"{Colors.BOLD}MCP Tool JSON Validator (OpenAI-powered){Colors.RESET}")
        print("-" * 50)
        filename = input("Enter the MCP tool JSON filename: ").strip()

    if not filename:
        print(f"{Colors.RED}Error: No filename provided.{Colors.RESET}")
        sys.exit(1)

    # Validate the MCP tool JSON
    success = validate_mcp_tool_file(filename)

    # Exit with appropriate status code
    sys.exit(0 if success else 1)


if __name__ == "__main__":
    main()
