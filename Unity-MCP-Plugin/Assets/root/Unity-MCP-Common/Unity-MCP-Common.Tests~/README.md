# Unity-MCP-Common Tests

This project contains unit tests for the Unity-MCP-Common library.

## Test Framework

- **xUnit** - Primary testing framework
- **Moq** - Mocking framework for creating test doubles
- **FluentAssertions** - Assertion library for more readable test assertions

## Running Tests

### Using .NET CLI

```bash
# Run all tests
dotnet test

# Run tests with verbose output
dotnet test --verbosity detailed

# Run tests with code coverage
dotnet test --collect:"XPlat Code Coverage"
```

### Using Visual Studio

1. Open the solution in Visual Studio
2. Open Test Explorer (Test > Test Explorer)
3. Click "Run All" to execute all tests

### Using Visual Studio Code

1. Install the ".NET Core Test Explorer" extension
2. Tests will appear in the Test Explorer sidebar
3. Click the play button to run tests

## Test Structure

Tests are organized by namespace to mirror the source code structure:

```txt
Tests/
├── Data/                    # Tests for data models
│   ├── ResponseDataTests.cs
│   └── VersionTests.cs
├── Extensions/              # Tests for extension methods
│   └── ExtensionsStringTests.cs
└── ...
```

## Writing Tests

### Test Naming Convention

Tests follow the pattern: `MethodName_Scenario_ExpectedBehavior`

Example:

```csharp
[Fact]
public void Join_WithDefaultSeparator_JoinsStringsWithComma()
```

### Test Structure (Arrange-Act-Assert)

```csharp
[Fact]
public void ExampleTest()
{
    // Arrange - Set up test data and dependencies
    var input = "test";

    // Act - Execute the method under test
    var result = SomeMethod(input);

    // Assert - Verify the expected outcome
    result.Should().Be("expected");
}
```

### Using FluentAssertions

FluentAssertions provides more readable assertions:

```csharp
// Instead of:
Assert.Equal("expected", actual);

// Use:
actual.Should().Be("expected");

// More examples:
result.Should().NotBeNull();
collection.Should().BeEmpty();
value.Should().BeGreaterThan(10);
```

## Code Coverage

To generate code coverage reports:

```bash
# Install report generator
dotnet tool install --global dotnet-reportgenerator-globaltool

# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage"

# Generate HTML report
reportgenerator -reports:"**/coverage.cobertura.xml" -targetdir:"coveragereport"
```

## Contributing

When adding new features to Unity-MCP-Common, please ensure:

1. New functionality has corresponding unit tests
2. Tests follow the existing naming conventions
3. Tests are properly organized in the correct namespace
4. All tests pass before submitting changes
