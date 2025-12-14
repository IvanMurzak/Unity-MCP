#!/usr/bin/env python3
"""
JSON Schema Validator
Validates a JSON schema file to ensure it's well-formed and follows JSON Schema specifications.
"""

import json
import sys
from pathlib import Path

try:
    from jsonschema import (
        Draft4Validator,
        Draft6Validator,
        Draft7Validator,
        Draft202012Validator,
        ValidationError
    )
    from jsonschema.exceptions import SchemaError
except ImportError:
    print("Error: jsonschema library not found.")
    print("Install it with: pip install jsonschema")
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
        ORANGE = Fore.YELLOW  # Orange is represented as yellow/bright yellow in terminals
        RESET = Style.RESET_ALL
    else:
        # Fallback to ANSI codes
        GREEN = '\033[92m'
        RED = '\033[91m'
        ORANGE = '\033[93m'  # Yellow/Orange
        RESET = '\033[0m'


def get_validator_for_schema(schema):
    """
    Determine the appropriate validator based on the $schema field.

    Args:
        schema: The JSON schema dictionary

    Returns:
        The appropriate validator class
    """
    schema_uri = schema.get('$schema', '')

    # Map schema URIs to validators
    if 'draft/2020-12' in schema_uri:
        return Draft202012Validator
    elif 'draft-07' in schema_uri or 'draft/07' in schema_uri:
        return Draft7Validator
    elif 'draft-06' in schema_uri or 'draft/06' in schema_uri:
        return Draft6Validator
    elif 'draft-04' in schema_uri or 'draft/04' in schema_uri:
        return Draft4Validator
    else:
        # Default to Draft 2020-12 (latest standard)
        return Draft202012Validator


def show_schema_preview(schema, missing_schema_field=False):
    """Show a preview of how the schema should be structured."""
    if missing_schema_field:
        print(f"\n  {Colors.ORANGE}Example - Your schema should start like this:{Colors.RESET}")
        print(f"  {Colors.GREEN}{{")
        print(f'    "$schema": "https://json-schema.org/draft/2020-12/schema",{Colors.RESET}')

        # Show first few fields of their current schema
        shown_fields = 0
        for key in list(schema.keys())[:3]:
            value = schema[key]
            if isinstance(value, (dict, list)):
                print(f'    "{key}": ...,')
            else:
                value_str = json.dumps(value)
                if len(value_str) > 40:
                    value_str = value_str[:40] + "..."
                print(f'    "{key}": {value_str},')
            shown_fields += 1

        if len(schema) > 3:
            print(f"    ...")
        print(f"  {Colors.GREEN}}}{Colors.RESET}\n")


def validate_schema_file(filename):
    """
    Validate a JSON schema file.

    Args:
        filename: Path to the JSON schema file

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
            schema = json.load(f)
    except json.JSONDecodeError as e:
        print(f"{Colors.RED}Error: Invalid JSON format in '{filename}'{Colors.RESET}")
        print(f"  {e}")
        return False
    except Exception as e:
        print(f"{Colors.RED}Error reading file: {e}{Colors.RESET}")
        return False

    # Get the appropriate validator for the schema
    validator_class = get_validator_for_schema(schema)

    # Validate the schema
    try:
        validator_class.check_schema(schema)
        print(f"{Colors.GREEN}✓ Schema in '{filename}' is valid!{Colors.RESET}")

        # Check schema type and print with warning color if not specified
        schema_type = schema.get('$schema', 'Not specified')
        if schema_type == 'Not specified':
            print(f"\n  {Colors.ORANGE}⚠ Warning: $schema field is missing from the root of your JSON schema{Colors.RESET}")
            print(f"  {Colors.ORANGE}├─ Location: Root level of the JSON object (typically line 1-2 in {filename}){Colors.RESET}")
            print(f"  {Colors.ORANGE}├─ Issue: Without $schema, validators may interpret your schema differently{Colors.RESET}")
            print(f"  {Colors.ORANGE}└─ Defaulting to: Draft 2020-12 for this validation{Colors.RESET}")

            # Show example of how to fix it
            show_schema_preview(schema, missing_schema_field=True)
        else:
            print(f"  Schema type: {schema_type}")

        # Show which validator was used
        validator_name = validator_class.__name__.replace('Validator', '')
        print(f"  Validated with: {validator_name}")

        if 'title' in schema:
            print(f"  Title: {schema['title']}")
        return True
    except SchemaError as e:
        print(f"{Colors.RED}✗ Invalid JSON Schema in '{filename}'{Colors.RESET}")
        print(f"  {e.message}")
        return False
    except Exception as e:
        print(f"{Colors.RED}Error validating schema: {e}{Colors.RESET}")
        return False


def main():
    """Main function to handle command line arguments and user input."""
    filename = None

    # Check if filename was provided as command line argument
    if len(sys.argv) > 1:
        filename = sys.argv[1]
    else:
        # Prompt user for filename
        print("JSON Schema Validator")
        print("-" * 40)
        filename = input("Enter the JSON schema filename: ").strip()

    if not filename:
        print(f"{Colors.RED}Error: No filename provided.{Colors.RESET}")
        sys.exit(1)

    # Validate the schema
    success = validate_schema_file(filename)

    # Exit with appropriate status code
    sys.exit(0 if success else 1)


if __name__ == "__main__":
    main()
