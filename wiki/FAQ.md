# Frequently Asked Questions (FAQ)

Common questions and answers about Unity-MCP. Can't find what you're looking for? Check our [Troubleshooting guide](Troubleshooting) or ask in [GitHub Discussions](https://github.com/IvanMurzak/Unity-MCP/discussions).

## 🎯 General Questions

### What is Unity-MCP?
Unity-MCP is an AI assistant system that allows AI models like Claude to directly interact with Unity Editor and Unity games. It uses the Model Context Protocol (MCP) to provide AI with comprehensive access to Unity's API.

### How does Unity-MCP work?
Unity-MCP consists of three components:
1. **Unity Plugin** - Installed in your Unity project, provides deep integration
2. **MCP Server** - Bridges communication between AI and Unity  
3. **AI Client** - Your AI assistant (Claude Desktop, VS Code, etc.)

The AI sends commands through the MCP server to the Unity plugin, which executes them and returns results.

### Is Unity-MCP free to use?
Yes! Unity-MCP is open source and completely free. It's released under the Apache 2.0 License.

### Which AI models can I use with Unity-MCP?
Any AI client that supports the Model Context Protocol (MCP):
- **Claude Desktop** (recommended)
- **Claude Code** (CLI tool)
- **VS Code with MCP extension**
- **Custom MCP clients**

Future AI models that adopt MCP will automatically work with Unity-MCP.

## 🔧 Technical Questions

### What Unity versions are supported?
- **Unity 2022.3 LTS** and newer (recommended)
- **Unity 2021.3 LTS** (supported)
- **Unity 6000.2** (latest, fully supported)

### Can I use Unity-MCP in production games?
Yes! Unity-MCP works in both Unity Editor and compiled game builds. However, consider security implications when deploying to production.

### Does Unity-MCP work with Unity Cloud Build?
Unity-MCP Plugin works with Unity Cloud Build. However, you'll need to deploy the MCP Server separately since Cloud Build doesn't provide runtime server hosting.

### What platforms does Unity-MCP support?
**Unity Plugin**: All platforms Unity supports
**MCP Server**: 
- Windows (x64, ARM64)
- macOS (Intel, Apple Silicon)
- Linux (x64, ARM64)
- Docker containers

### Can multiple developers use one Unity-MCP server?
Yes, the server supports multiple concurrent connections. Configure `UNITY_MCP_MAX_CONNECTIONS` to control how many clients can connect simultaneously.

## 🚀 Getting Started

### How do I install Unity-MCP?
The quickest way:
1. **Unity Plugin**: Install from [Unity Asset Store](https://u3d.as/3wsw)
2. **MCP Server**: Run `docker run -p 8080:8080 ivanmurzakdev/unity-mcp-server:latest`
3. **Configure AI Client**: Add Unity-MCP to your Claude Desktop config

See our detailed [Installation Guide](Installation-Guide) for all methods.

### Do I need Docker to use Unity-MCP?
No, but it's recommended. You can also:
- Install as a .NET global tool
- Build from source
- Use package manager installations

Docker provides the most reliable deployment experience.

### Can I use Unity-MCP without an internet connection?
Yes! Unity-MCP works completely offline once installed:
- Unity Plugin runs locally in Unity Editor
- MCP Server runs on your local machine
- Only AI client might need internet (depends on the AI service)

### How much does it cost to run Unity-MCP?
Unity-MCP itself is free. Costs depend on your AI service:
- **Claude Desktop**: Free tier available
- **OpenAI API**: Pay per token usage
- **Local AI models**: No ongoing costs

## 💡 Usage Questions

### What can AI do with Unity-MCP?
AI can perform most Unity development tasks:
- Create and modify GameObjects
- Add and configure components
- Manage scenes and assets
- Write and modify C# scripts
- Create materials and textures
- Set up lighting and cameras
- Debug and test projects

See our [AI Tools Reference](AI-Tools-Reference) for the complete list.

### Can AI create complete games?
AI can help with many game development tasks, but complete game creation requires:
- Clear project vision and requirements
- Iterative feedback and refinement
- Human oversight for complex logic
- Art direction and polish

AI excels at rapid prototyping and implementing specific features.

### How do I ask AI to do complex tasks?
Be specific and break down complex tasks:

**Instead of**: "Make a platformer game"
**Try**: "Create a 2D player character with left/right movement, jumping physics, and ground detection"

**Instead of**: "Add enemies"  
**Try**: "Create an enemy GameObject with AI that patrols between two points and damages the player on contact"

### Can AI modify existing code?
Yes! AI can:
- Read existing C# scripts
- Add new methods and properties
- Modify existing functionality
- Refactor code structure
- Add comments and documentation

Always backup your project before letting AI make significant code changes.

## 🔧 Configuration & Setup

### What port does Unity-MCP use?
Default port is **8080** for both Unity Plugin and MCP Server. You can change this:
- Unity: Window → Unity MCP → Settings → Port
- Server: `unity-mcp-server --port 8081`

### Can I run Unity-MCP on a different port?
Yes, just ensure both Unity Plugin and MCP Server use the same port:
```bash
# Server on port 9000
unity-mcp-server --port 9000

# Unity Plugin: Change port in settings to 9000
```

### How do I configure Unity-MCP for my team?
1. **Shared Server**: Run one MCP Server for the team
2. **Individual Installs**: Each developer runs their own server
3. **Cloud Deployment**: Deploy server to cloud for remote access

See our [Configuration Guide](Configuration) for team setup details.

### Can I customize Unity-MCP's behavior?
Yes! You can:
- Create custom AI tools for project-specific tasks
- Configure performance and security settings
- Modify tool behavior through configuration
- Extend functionality with custom components

Check our [Custom Tools Development](Custom-Tools-Development) guide.

## 🛡️ Security Questions

### Is Unity-MCP secure?
Unity-MCP includes several security features:
- Local-only connections by default
- No external data transmission (unless configured)
- Configurable access controls
- Optional SSL/TLS encryption

For production use, review and configure security settings appropriately.

### Can AI access my computer files?
By default, AI can only access:
- Your Unity project files
- Unity Editor functionality
- Assets within the Unity project

AI cannot access files outside your Unity project unless you create custom tools that allow it.

### Should I use Unity-MCP in production games?
Consider security implications:
- **Development builds**: Generally safe for internal testing
- **Production builds**: Carefully evaluate if AI access is needed
- **Shipped games**: Usually disable Unity-MCP or restrict access

### Can I restrict what AI can do?
Yes, through several mechanisms:
- Custom tool permissions
- Server configuration limits
- Network access restrictions
- Component-level security

## 🚨 Troubleshooting

### Unity-MCP isn't working. Where do I start?
1. **Check Unity Console** for error messages
2. **Test server connection**: Visit `http://localhost:8080/health`
3. **Verify installation**: Ensure both Plugin and Server are installed
4. **Restart components**: Unity → Server → AI Client
5. **Check our [Troubleshooting guide](Troubleshooting)**

### Why can't AI see Unity tools?
Common causes:
- MCP Server not running
- Port mismatch between Unity and Server
- AI client not properly configured
- Server connection failed

Solution: Verify each component is running and properly configured.

### Performance is slow. How can I improve it?
Try these optimizations:
- **Enable threading** in Unity MCP settings
- **Reduce batch sizes** for large operations
- **Optimize Unity scenes** (fewer objects, simpler hierarchies)
- **Use faster server deployment** (local vs. remote)
- **Close unnecessary Unity windows**

### Why do some commands fail?
Common reasons:
- **Timing issues**: Operations happen too quickly
- **Missing dependencies**: Required components or objects don't exist
- **Unity state**: Editor in wrong mode (Edit vs Play)
- **Resource limits**: Memory or processing constraints

## 🔄 Updates & Maintenance

### How do I update Unity-MCP?
**Unity Plugin**:
- Asset Store: Re-download and import
- Package Manager: Update through Package Manager
- Git: Pull latest changes

**MCP Server**:
- Docker: `docker pull ivanmurzakdev/unity-mcp-server:latest`
- .NET Tool: `dotnet tool update -g Unity.MCP.Server`

### How often is Unity-MCP updated?
Unity-MCP follows semantic versioning:
- **Major releases**: New features, breaking changes
- **Minor releases**: New features, backward compatible  
- **Patch releases**: Bug fixes, stability improvements

Check [GitHub Releases](https://github.com/IvanMurzak/Unity-MCP/releases) for update notifications.

### What if my version stops working?
1. **Check compatibility**: Verify Unity version support
2. **Update components**: Ensure Plugin and Server versions match
3. **Check breaking changes**: Review release notes for changes
4. **Rollback if needed**: Use previous working versions temporarily

## 🤝 Development & Contributing

### Can I contribute to Unity-MCP?
Absolutely! Contributions are welcome:
- **Bug reports**: File issues on GitHub
- **Feature requests**: Discuss in GitHub Issues
- **Code contributions**: Submit pull requests
- **Documentation**: Improve guides and examples
- **Community support**: Help other users

See our [Contributing guide](Contributing) for details.

### How do I create custom tools?
Custom tools extend Unity-MCP's capabilities:

1. **Create tool class** with `[McpPluginToolType]` attribute
2. **Add tool methods** with `[McpPluginTool]` attribute  
3. **Use descriptive attributes** to help AI understand functionality
4. **Test thoroughly** before deployment

Check our [Custom Tools Development](Custom-Tools-Development) guide.

### Can I sell Unity-MCP integrations?
Unity-MCP is Apache 2.0 licensed, which allows commercial use. You can:
- **Create commercial tools** that integrate with Unity-MCP
- **Offer Unity-MCP services** to clients
- **Bundle Unity-MCP** in commercial products

Please include proper attribution as required by the Apache 2.0 license.

## 📚 Learning Resources

### Where can I learn more about Unity-MCP?
- **[Getting Started guide](Getting-Started)**: Quick introduction
- **[Examples & Tutorials](Examples-and-Tutorials)**: Hands-on learning
- **[API Reference](API-Reference)**: Technical documentation
- **[GitHub Repository](https://github.com/IvanMurzak/Unity-MCP)**: Source code and issues

### Are there video tutorials?
Check the [Examples & Tutorials](Examples-and-Tutorials) section for links to video content and community tutorials.

### How do I stay updated?
- **Star** the [GitHub repository](https://github.com/IvanMurzak/Unity-MCP)
- **Watch** for repository notifications
- **Follow** release announcements
- **Join** community discussions

## 🌟 Advanced Usage

### Can I use Unity-MCP for automated testing?
Yes! Unity-MCP enables AI-driven testing:
- **Automated UI testing**: AI navigates interfaces
- **Gameplay testing**: AI plays and evaluates games
- **Performance testing**: AI generates test scenarios
- **Regression testing**: AI validates functionality after changes

### How do I deploy Unity-MCP in the cloud?
Several deployment options:
- **Docker containers**: Deploy to any container platform
- **Kubernetes**: Scale across multiple nodes
- **Cloud services**: AWS ECS, Google Cloud Run, Azure Container Instances
- **Serverless**: Function-based deployments

See our [Server Setup guide](Server-Setup) for cloud deployment details.

### Can Unity-MCP work with version control?
Unity-MCP works normally with Git and other VCS:
- **Plugin files**: Commit to repository
- **Generated content**: Use .gitignore for temporary files
- **Configuration**: Version control settings files
- **Collaboration**: Multiple developers can use Unity-MCP simultaneously

---

**Have a question not covered here?** 
- Check our [Troubleshooting guide](Troubleshooting)
- Search [GitHub Issues](https://github.com/IvanMurzak/Unity-MCP/issues)
- Ask in [GitHub Discussions](https://github.com/IvanMurzak/Unity-MCP/discussions)
- Create a new issue if you found a bug