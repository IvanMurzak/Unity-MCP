# Contributing to Unity-MCP

We welcome contributions from the community! Unity-MCP is built with the vision of making game development as simple as never before with AI assistance. Whether you have ideas for new tools, features, bug fixes, or improvements, your contributions are highly appreciated.

## 🎯 Ways to Contribute

### 🐛 Bug Reports
Help us improve Unity-MCP by reporting issues you encounter:
- **Detailed bug reports** with reproduction steps
- **Performance issues** and optimization opportunities  
- **Documentation errors** or unclear instructions
- **Compatibility problems** with different Unity versions

### 💡 Feature Requests
Share your ideas for making Unity-MCP even better:
- **New AI tools** for specific Unity workflows
- **Integration improvements** with AI clients
- **Performance enhancements** and optimizations
- **Quality of life** features and usability improvements

### 🔧 Code Contributions
Direct contributions to the codebase:
- **Bug fixes** and stability improvements
- **New tool implementations** for Unity-MCP
- **Performance optimizations** and memory management
- **Test coverage** improvements and automation

### 📚 Documentation
Help make Unity-MCP more accessible:
- **Tutorial creation** and example projects
- **API documentation** improvements
- **Translation** of documentation to other languages
- **Video tutorials** and community guides

### 🤝 Community Support
Assist other community members:
- **Answer questions** in GitHub Discussions
- **Review pull requests** and provide feedback
- **Share your projects** built with Unity-MCP
- **Mentor newcomers** to the project

## 🚀 Getting Started

### Prerequisites

Before contributing, ensure you have:
- **Unity 2022.3 LTS** or newer installed
- **.NET 8.0 SDK** for server development
- **Git** for version control
- **GitHub account** for collaboration

### Setting Up Development Environment

#### 1. Fork and Clone the Repository
```bash
# Fork the repository on GitHub, then clone your fork
git clone https://github.com/YOUR_USERNAME/Unity-MCP.git
cd Unity-MCP

# Add upstream remote for staying updated
git remote add upstream https://github.com/IvanMurzak/Unity-MCP.git
```

#### 2. Set Up Unity Plugin Development
```bash
# Open the Unity plugin project
cd Unity-MCP-Plugin
# Open this folder in Unity Hub as a project
```

**Unity Project Setup**:
1. Open Unity Hub
2. Click "Open" and select `Unity-MCP-Plugin` folder
3. Let Unity import all assets and compile scripts
4. Verify no compilation errors in Console

#### 3. Set Up MCP Server Development
```bash
# Navigate to server directory
cd Unity-MCP-Server

# Restore dependencies
dotnet restore

# Build the project
dotnet build

# Run tests (if available)
dotnet test

# Run the server locally
dotnet run
```

#### 4. Verify Setup
1. **Start the MCP Server**: `dotnet run` in `Unity-MCP-Server` directory
2. **Open Unity Project**: Ensure plugin loads without errors
3. **Test Connection**: Verify Unity can connect to the server
4. **Run Basic Test**: Try creating a GameObject through AI

### Development Workflow

#### 1. Create a Feature Branch
```bash
# Update your fork
git fetch upstream
git checkout main
git merge upstream/main

# Create feature branch
git checkout -b feature/your-feature-name
# or
git checkout -b fix/bug-description
```

#### 2. Make Your Changes
Follow our coding standards and best practices:
- **Write clean, readable code** with proper comments
- **Follow C# naming conventions** (PascalCase for public members)
- **Add unit tests** for new functionality where applicable
- **Update documentation** for any API changes

#### 3. Test Your Changes
Before submitting:
- **Build successfully** without warnings
- **Test in Unity Editor** with various scenarios
- **Verify server compatibility** with different deployment methods
- **Test with different AI clients** if applicable

#### 4. Commit and Push
```bash
# Stage your changes
git add .

# Commit with descriptive message
git commit -m "feat: add new tool for procedural terrain generation"
# or
git commit -m "fix: resolve null reference exception in GameObject tool"

# Push to your fork
git push origin feature/your-feature-name
```

#### 5. Create Pull Request
1. **Navigate to GitHub** and open your fork
2. **Click "Pull Request"** and select your feature branch
3. **Fill out PR template** with detailed description
4. **Link related issues** using keywords like "Fixes #123"
5. **Request review** from maintainers

## 🛠️ Contribution Guidelines

### Code Standards

#### C# Coding Conventions
```csharp
// ✓ Good: PascalCase for public members
public class McpGameObjectTool
{
    public string ToolName { get; set; }
    
    // ✓ Good: Descriptive method names
    public string CreateGameObjectWithComponents(string name, Vector3 position)
    {
        // ✓ Good: Clear variable names
        var createdObject = new GameObject(name);
        createdObject.transform.position = position;
        
        return $"[Success] Created {name} at {position}";
    }
}

// ✗ Avoid: Poor naming and structure
public class tool1
{
    public string doStuff(string n, Vector3 p) { /* ... */ }
}
```

#### Tool Development Standards
```csharp
// ✓ Good: Complete tool implementation
[McpPluginToolType(Category = "GameObject Management")]
public class ExampleTool
{
    [McpPluginTool(
        "CreateDetailedObject",
        Title = "Create GameObject with detailed configuration"
    )]
    [Description("Creates a GameObject with specified components and properties, providing comprehensive customization options.")]
    public string CreateDetailedObject(
        [Description("Name for the new GameObject")]
        string name,
        
        [Description("World position for placement")]
        Vector3 position = default,
        
        [Description("Components to add (comma-separated list)")]
        string components = "",
        
        [Description("Whether to make object static for performance")]
        bool isStatic = false
    )
    {
        // Validate inputs
        if (string.IsNullOrWhiteSpace(name))
            return "[Error] GameObject name cannot be null or empty";
            
        return MainThread.Instance.Run(() =>
        {
            try
            {
                // Implementation with error handling
                var obj = new GameObject(name);
                obj.transform.position = position;
                obj.isStatic = isStatic;
                
                // Add components if specified
                if (!string.IsNullOrEmpty(components))
                {
                    AddComponentsFromString(obj, components);
                }
                
                return $"[Success] Created '{name}' at {position}";
            }
            catch (System.Exception ex)
            {
                return $"[Error] Failed to create GameObject: {ex.Message}";
            }
        });
    }
}
```

### Documentation Standards

#### Code Documentation
```csharp
/// <summary>
/// Creates a new GameObject in the Unity scene with specified configuration.
/// </summary>
/// <param name="name">The name to assign to the GameObject</param>
/// <param name="position">World position where the object should be placed</param>
/// <param name="components">Comma-separated list of component names to add</param>
/// <returns>Success message with object details, or error message if creation fails</returns>
[McpPluginTool("CreateGameObject")]
public string CreateGameObject(string name, Vector3 position, string components = "")
{
    // Implementation...
}
```

#### README and Wiki Updates
When adding new features:
1. **Update relevant README sections** with usage examples
2. **Add entries to AI Tools Reference** for new tools
3. **Create tutorial sections** for complex features
4. **Update API Reference** for interface changes

### Testing Guidelines

#### Unit Tests for Tools
```csharp
[Test]
public void CreateGameObject_WithValidParameters_ReturnsSuccessMessage()
{
    // Arrange
    var tool = new GameObjectTool();
    var objectName = "TestObject";
    var position = Vector3.zero;
    
    // Act
    var result = tool.CreateGameObject(objectName, position);
    
    // Assert
    Assert.IsTrue(result.Contains("[Success]"));
    Assert.IsTrue(result.Contains(objectName));
}

[Test]
public void CreateGameObject_WithNullName_ReturnsErrorMessage()
{
    // Arrange
    var tool = new GameObjectTool();
    
    // Act
    var result = tool.CreateGameObject(null, Vector3.zero);
    
    // Assert
    Assert.IsTrue(result.Contains("[Error]"));
}
```

#### Integration Tests
```csharp
[UnityTest]
public IEnumerator McpServer_ConnectsToUnityPlugin_Successfully()
{
    // Arrange
    var mcpPlugin = McpPlugin.Instance;
    
    // Act
    var connectTask = mcpPlugin.ConnectAsync();
    yield return new WaitUntil(() => connectTask.IsCompleted);
    
    // Assert
    Assert.IsTrue(connectTask.Result);
    Assert.IsTrue(mcpPlugin.IsConnected);
}
```

### Pull Request Guidelines

#### PR Description Template
```markdown
## Description
Brief description of what this PR accomplishes.

## Type of Change
- [ ] Bug fix (non-breaking change that fixes an issue)
- [ ] New feature (non-breaking change that adds functionality)
- [ ] Breaking change (fix or feature that would cause existing functionality to not work as expected)
- [ ] Documentation update

## Changes Made
- List specific changes made
- Include any new files or modified functionality
- Mention any breaking changes

## Testing
- [ ] I have tested these changes locally
- [ ] I have added unit tests for new functionality
- [ ] All existing tests pass
- [ ] I have tested with multiple Unity versions (if applicable)

## Screenshots/Examples
Include screenshots or code examples demonstrating the changes (if applicable).

## Related Issues
Fixes #(issue number)
Related to #(issue number)

## Checklist
- [ ] My code follows the project's coding standards
- [ ] I have performed a self-review of my code
- [ ] I have commented my code where necessary
- [ ] I have updated documentation as needed
- [ ] My changes generate no new warnings or errors
```

### Review Process

#### What Reviewers Look For
1. **Code Quality**: Clean, readable, maintainable code
2. **Functionality**: Features work as described and handle edge cases
3. **Performance**: No significant performance regressions
4. **Compatibility**: Works with supported Unity versions
5. **Documentation**: Adequate documentation for new features
6. **Testing**: Appropriate test coverage for changes

#### Addressing Review Feedback
```bash
# Make requested changes
git add .
git commit -m "address review feedback: improve error handling"

# Push updates to existing PR
git push origin feature/your-feature-name
```

## 🎨 Contribution Types

### 🔧 Adding New AI Tools

New tools extend Unity-MCP's capabilities. Great tool ideas:

#### Workflow Enhancement Tools
- **Asset optimization tools** (texture compression, mesh optimization)
- **Scene analysis tools** (performance profiling, hierarchy cleanup)
- **Build automation tools** (platform-specific build configurations)

#### Creative Tools
- **Procedural generation tools** (terrain, buildings, vegetation)
- **Animation utilities** (keyframe interpolation, curve editing)
- **Material generators** (procedural shaders, texture synthesis)

#### Development Tools
- **Code generation tools** (component templates, design patterns)
- **Testing utilities** (automated testing, mock data generation)
- **Documentation tools** (code documentation, asset catalogs)

### 🚀 Server Improvements

#### Performance Enhancements
- **Connection pooling** for better scalability
- **Request caching** to reduce Unity API calls
- **Memory optimization** for long-running servers

#### Protocol Extensions
- **WebSocket support** for real-time communication
- **Streaming responses** for large data transfers
- **Batch operations** for multiple tool executions

### 🎮 Unity Plugin Features

#### Editor Integration
- **Visual debugging tools** for MCP communication
- **Tool execution monitoring** and performance metrics
- **Configuration UI improvements** and settings management

#### Runtime Features
- **In-game AI assistance** for runtime tool execution
- **Security enhancements** for production deployments
- **Cross-platform compatibility** improvements

## 🏆 Recognition

### Contributor Recognition
We value all contributions and recognize them through:
- **Contributor list** in project README
- **Release notes** mentioning significant contributions
- **Community showcases** featuring contributor projects
- **Maintainer nominations** for exceptional contributors

### Contribution Rewards
- **Early access** to new features and betas
- **Direct collaboration** opportunities with maintainers
- **Conference speaking** opportunities to present your work
- **Professional networking** within the Unity and AI communities

## 📋 Code of Conduct

### Our Commitment
We are committed to providing a welcoming and inclusive environment for all contributors, regardless of background, experience level, or identity.

### Expected Behavior
- **Be respectful** in all interactions
- **Provide constructive feedback** and accept it gracefully
- **Help newcomers** learn and contribute effectively
- **Focus on what's best** for the community and project

### Unacceptable Behavior
- **Harassment** or discrimination of any kind
- **Inappropriate language** or personal attacks
- **Spam** or off-topic discussions
- **Violation of others' privacy** or confidentiality

### Reporting Issues
Report any Code of Conduct violations to the project maintainers through:
- **Private message** to maintainers
- **Email** to project contact addresses
- **GitHub's reporting tools** for serious violations

## 📞 Getting Help

### Before You Start
- **Read existing documentation** thoroughly
- **Search existing issues** for similar problems or requests
- **Check discussions** for community Q&A

### Ask for Help
- **GitHub Discussions**: General questions and community support
- **GitHub Issues**: Bug reports and feature requests
- **Discord/Chat**: Real-time community interaction (if available)
- **Email**: Direct contact for sensitive issues

### Mentorship Program
New contributors can request mentorship from experienced community members:
- **Pair programming** sessions for complex features
- **Code review** guidance and best practices
- **Project architecture** explanations and guidance
- **Career development** advice in open source

---

## 🎉 Thank You!

Your contributions help make Unity-MCP better for everyone. Whether you're fixing a small bug, adding a major feature, or helping other community members, every contribution matters.

**Ready to contribute?** 
1. **Start small** with a good first issue
2. **Join our discussions** to introduce yourself
3. **Fork the repository** and begin coding
4. **Don't hesitate to ask** for help when needed

Together, we're making game development as simple as never before! 💙💛

---

**Questions about contributing?** Open a discussion on [GitHub Discussions](https://github.com/IvanMurzak/Unity-MCP/discussions) or check our [FAQ](FAQ) for common questions.