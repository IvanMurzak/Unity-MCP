#!/usr/bin/env python3
"""
MCP Tool JSON Validator using OpenAI API
Validates MCP tool JSON files using OpenAI's API for comprehensive schema validation.
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


def validate_with_openai(json_content, filename):
    """
    Validate MCP tool JSON using OpenAI API.

    Args:
        json_content: The parsed JSON content
        filename: Name of the file being validated

    Returns:
        Validation result dictionary
    """
    client = get_openai_client()

    validation_prompt = """You are a JSON schema validator specializing in MCP (Model Context Protocol) tool definitions.

Analyze the following MCP tool JSON and validate it thoroughly. Check for:

1. **Structure Compliance**: Verify it follows the MCP tool specification format
2. **Schema Validity**: Check if inputSchema and outputSchema are valid JSON schemas
3. **Reference Integrity**: Verify all $ref references point to existing definitions in $defs
4. **Type Consistency**: Ensure all type declarations are valid and consistent
5. **Required Fields**: Check that all required fields are present
6. **Circular References**: Detect any circular reference issues
7. **Best Practices**: Identify any deviations from JSON schema best practices
8. **Descriptions**: Check if important fields have helpful descriptions

Respond ONLY with a JSON object in this exact format:
{
    "isValid": true/false,
    "errors": [
        {
            "severity": "error",
            "location": "path.to.field or line number",
            "message": "Description of the error",
            "suggestion": "How to fix it"
        }
    ],
    "warnings": [
        {
            "severity": "warning",
            "location": "path.to.field",
            "message": "Description of the warning",
            "suggestion": "Recommendation"
        }
    ],
    "info": [
        {
            "severity": "info",
            "message": "General information or best practice suggestion"
        }
    ],
    "summary": "Brief summary of the validation result"
}

MCP Tool JSON to validate:
"""

    try:
        print(f"{Colors.BLUE}🔍 Sending to OpenAI API for validation...{Colors.RESET}")

        response = client.chat.completions.create(
            model="gpt-4o",
            messages=[
                {
                    "role": "system",
                    "content": "You are a precise JSON schema validator. Always respond with valid JSON only."
                },
                {
                    "role": "user",
                    "content": validation_prompt + "\n```json\n" + json.dumps(json_content, indent=2) + "\n```"
                }
            ],
            temperature=0,
            response_format={"type": "json_object"}
        )

        result_text = response.choices[0].message.content
        result = json.loads(result_text)

        return result

    except Exception as e:
        print(f"{Colors.RED}Error communicating with OpenAI API: {e}{Colors.RESET}")
        return None


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
