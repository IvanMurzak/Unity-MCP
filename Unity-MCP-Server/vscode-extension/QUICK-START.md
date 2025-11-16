# Quick Start Guide - Unity MCP VS Code Extension

This guide will get you from zero to published extension in minutes!

## 🚀 Fast Track (First Time Setup)

### Step 1: Install Prerequisites (5 minutes)

```powershell
# Check if Node.js is installed
node --version  # Should be v20.x or higher

# If not installed, download from: https://nodejs.org/
```

### Step 2: Install Dependencies (2 minutes)

```powershell
cd vscode-extension
npm install
```

### Step 3: Build Locally (2 minutes)

```powershell
# Build everything and create .vsix package
.\deploy.ps1 -SkipPublish
```

This will:
- ✅ Build the .NET MCP server for all platforms
- ✅ Copy binaries to extension folder
- ✅ Compile TypeScript
- ✅ Create `unity-mcp-0.22.1.vsix` file

### Step 4: Test Locally (1 minute)

```powershell
# Install in VS Code
code --install-extension unity-mcp-0.22.1.vsix
```

Then in VS Code:
1. Press `F1` or `Ctrl+Shift+P`
2. Type "Unity MCP: Start Server"
3. Check status bar (bottom right) - should show "▶ Unity MCP"

## 📦 Publishing to Marketplace (One-Time Setup)

### Step 1: Get Personal Access Token (5 minutes)

1. Go to https://dev.azure.com/
2. Sign in with Microsoft account
3. Click on your profile → "Personal access tokens"
4. Click "New Token"
5. Settings:
   - **Name**: `vscode-publishing`
   - **Organization**: All accessible organizations
   - **Scopes**: Check **"Marketplace (Manage)"**
6. Click "Create" and **COPY THE TOKEN** (you won't see it again!)

### Step 2: Create Publisher (3 minutes)

```powershell
# Install vsce globally
npm install -g @vscode/vsce

# Create publisher (if you haven't already)
vsce create-publisher IvanMurzak
```

Or create via web: https://marketplace.visualstudio.com/manage/createpublisher

### Step 3: Publish! (2 minutes)

```powershell
# Set your token (optional, can pass as parameter)
$env:VSCE_PAT = "your-token-here"

# Build and publish
.\deploy.ps1
```

Or without setting environment variable:

```powershell
.\deploy.ps1 -Token "your-token-here"
```

## 🎯 Common Commands

```powershell
# Full build and publish
.\deploy.ps1

# Build only (no publish)
.\deploy.ps1 -SkipPublish

# Skip .NET build (use existing binaries)
.\deploy.ps1 -SkipBuild

# Just package, no build or publish
.\deploy.ps1 -SkipBuild -SkipPublish
```

## 🐛 Troubleshooting

### "npm not found"
👉 Install Node.js from https://nodejs.org/

### "Cannot find module 'vscode'"
👉 Run `npm install` in the `vscode-extension` folder

### "Publisher not found"
👉 Create publisher at https://marketplace.visualstudio.com/manage

### "401 Unauthorized" during publish
👉 Check your PAT token:
- Is it valid?
- Does it have "Marketplace > Manage" scope?
- Did you set it correctly?

### Extension doesn't start
👉 Check VS Code Output panel:
1. View > Output
2. Select "Unity MCP" from dropdown
3. Look for error messages

## 📝 Before Publishing Checklist

- [ ] Update version in [package.json](package.json)
- [ ] Update [CHANGELOG.md](CHANGELOG.md) with changes
- [ ] Test locally with `.vsix` file
- [ ] Add icon.png (128x128 or 256x256)
- [ ] Review [README.md](README.md) for accuracy
- [ ] Verify all platform binaries are included

## 🎨 Adding an Icon

1. Create a 256x256 PNG icon
2. Save as `icon.png` in the `vscode-extension` folder
3. The `package.json` already references it:
   ```json
   "icon": "icon.png"
   ```

## 🔄 Updating the Extension

When you make changes:

1. **Increment version** in `package.json`:
   ```json
   {
     "version": "0.22.2"  // Increment this
   }
   ```

2. **Update CHANGELOG.md**:
   ```markdown
   ## [0.22.2] - 2025-01-17
   ### Fixed
   - Bug fix description
   ```

3. **Publish**:
   ```powershell
   .\deploy.ps1
   ```

## 📚 Next Steps

- Read [SETUP.md](SETUP.md) for detailed development guide
- Review [package.json](package.json) to customize extension
- Check [extension.ts](src/extension.ts) to understand the code
- Visit [VS Code Extension Docs](https://code.visualstudio.com/api)

## 🆘 Need Help?

- GitHub Issues: https://github.com/IvanMurzak/Unity-MCP/issues
- Discord: https://discord.gg/cfbdMZX99G
- VS Code Docs: https://code.visualstudio.com/api

---

**You're all set!** 🎉 Happy publishing!
