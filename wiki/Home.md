# ✨ AI Game Developer — Unity MCP

[![Docker Image](https://img.shields.io/docker/image-size/ivanmurzakdev/unity-mcp-server/latest?label=Docker%20Image&logo=docker&labelColor=333A41 'Docker Image')](https://hub.docker.com/r/ivanmurzakdev/unity-mcp-server)
[![MCP](https://badge.mcpx.dev?type=server 'MCP Server')](https://modelcontextprotocol.io/introduction)
[![Tests](https://github.com/IvanMurzak/Unity-MCP/workflows/release/badge.svg 'Tests Passed')](https://github.com/IvanMurzak/Unity-MCP/actions/workflows/release.yml)
[![Unity Asset Store](https://img.shields.io/badge/Asset%20Store-View-blue?logo=unity&labelColor=333A41 'Asset Store')](https://u3d.as/3wsw)
[![Unity Editor](https://img.shields.io/badge/Editor-X?style=flat&logo=unity&labelColor=333A41&color=49BC5C 'Unity Editor supported')](https://unity.com/releases/editor/archive)
[![Unity Runtime](https://img.shields.io/badge/Runtime-X?style=flat&logo=unity&labelColor=333A41&color=49BC5C 'Unity Runtime supported')](https://unity.com/releases/editor/archive)
[![OpenUPM](https://img.shields.io/npm/v/com.ivanmurzak.unity.mcp?label=OpenUPM&registry_uri=https://package.openupm.com&labelColor=333A41 'OpenUPM package')](https://openupm.com/packages/com.ivanmurzak.unity.mcp/)
[![Stars](https://img.shields.io/github/stars/IvanMurzak/Unity-MCP 'Stars')](https://github.com/IvanMurzak/Unity-MCP/stargazers)
[![License](https://img.shields.io/github/license/IvanMurzak/Unity-MCP?label=License&labelColor=333A41)](https://github.com/IvanMurzak/Unity-MCP/blob/main/LICENSE)

**Unity-MCP** is a revolutionary AI assistant that seamlessly integrates with Unity Editor and runtime games, providing powerful AI capabilities through the Model Context Protocol (MCP). It enables AI to perform a wide range of tasks in Unity projects, from simple object manipulation to complex game development workflows.

![AI Level Building](https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/level-building.gif)

## 🚀 What is Unity-MCP?

Unity-MCP bridges the gap between artificial intelligence and game development by providing:
- **Direct Unity Editor Integration** - AI can interact with your Unity projects in real-time
- **Runtime Game Support** - AI capabilities extend to compiled games on any platform
- **Flexible Communication** - Uses TCP connections for maximum compatibility
- **Model Context Protocol** - Industry-standard protocol for AI-tool interaction

## 🎯 Key Features

### For Developers
- **Real-time AI Assistance** - Chat with AI and see immediate results in Unity
- **Extensive Tool Library** - [100+ built-in AI tools](AI-Tools-Reference) for common Unity tasks
- **Custom Tool Development** - Create your own AI tools with simple C# attributes
- **Cross-Platform Support** - Works in Unity Editor and compiled games
- **Flexible Deployment** - Local or remote server options with Docker support

### For AI Models
- **Rich Unity API Access** - Complete access to Unity's GameObject hierarchy, components, and systems
- **Scene Management** - Create, modify, and navigate Unity scenes
- **Asset Management** - Handle textures, materials, prefabs, and other assets
- **Component Manipulation** - Add, remove, and configure Unity components
- **Real-time Reflection** - Dynamic discovery of available methods and properties

## 🏗️ Architecture

Unity-MCP consists of three main components:

```
AI Client (Claude, VS Code, etc.) ←→ MCP Server ←→ Unity Plugin
```

1. **AI Client** - Any MCP-compatible AI client (Claude Desktop, VS Code with MCP extension, etc.)
2. **MCP Server** - The Unity-MCP server that implements the Model Context Protocol
3. **Unity Plugin** - The Unity package that provides deep integration with Unity Editor and runtime

## 🎮 What Can You Build?

Unity-MCP enables AI to help create:

- **Interactive Games** - Complete game prototypes with AI assistance
- **Procedural Content** - AI-generated levels, textures, and game objects
- **Animation Systems** - Complex animations and state machines
- **UI/UX Elements** - Responsive interfaces and menu systems
- **Gameplay Mechanics** - Physics systems, player controllers, and game rules
- **Visual Effects** - Particle systems, shaders, and lighting setups

## 📚 Quick Navigation

### Getting Started
- [**Getting Started Guide**](Getting-Started) - Your first steps with Unity-MCP
- [**Installation Guide**](Installation-Guide) - Detailed setup instructions
- [**Configuration**](Configuration) - Essential configuration options

### Development
- [**AI Tools Reference**](AI-Tools-Reference) - Complete list of available tools
- [**Custom Tools Development**](Custom-Tools-Development) - Build your own AI tools
- [**API Reference**](API-Reference) - Technical documentation

### Deployment & Advanced
- [**Server Setup**](Server-Setup) - Advanced server configuration and Docker deployment
- [**Examples & Tutorials**](Examples-and-Tutorials) - Hands-on learning materials
- [**Troubleshooting**](Troubleshooting) - Solutions to common issues

### Community
- [**FAQ**](FAQ) - Frequently asked questions
- [**Contributing**](Contributing) - How to contribute to the project

## 🎨 Examples Gallery

<table>
  <tr>
    <td><img src="https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/flying-orbs.gif" alt="Animated Flying Orbs" /></td>
    <td><img src="https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/golden-sphere.gif" alt="Golden Sphere Animation" /></td>
  </tr>
  <tr>
    <td><img src="https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/runner.gif" alt="Runner Game" /></td>
    <td><img src="https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/procedural-terrain.gif" alt="Procedural Terrain" /></td>
  </tr>
  <tr>
    <td><img src="https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/create-material.gif" alt="Material Creation" /></td>
    <td><img src="https://github.com/IvanMurzak/Unity-MCP/blob/main/docs/img/playing-maze.gif" alt="Maze Game" /></td>
  </tr>
</table>

## 🌟 Community & Support

- ⭐ **Star the project** on [GitHub](https://github.com/IvanMurzak/Unity-MCP) to show your support
- 🐛 **Report issues** on our [Issue Tracker](https://github.com/IvanMurzak/Unity-MCP/issues)
- 💡 **Request features** through GitHub Issues
- 🤝 **Contribute** by following our [Contributing Guide](Contributing)

## 📄 License

Unity-MCP is released under the [Apache 2.0 License](https://github.com/IvanMurzak/Unity-MCP/blob/main/LICENSE).

---

**Ready to revolutionize your Unity development with AI?** Start with our [Getting Started Guide](Getting-Started) and join the future of game development!