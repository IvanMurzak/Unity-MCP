# Deploy your package using `GitHub`

GitHub public repository could be used as a host for your package and directly imported into Unity project from GitHub repository.

> ⚠️ GitHub distribution method doesn't support automatic dependency resolving. Recommended to deploy package to OpenUPM if your package has dependencies. Also, you would need to use only OpenUPM-CLI to resolve dependencies automatically.

## Deploy

1. Increment package version in the file `Packages/com.ivanmurzak.unity.mcp.particlesystem/package.json`. It has `version` property.
   > Any further updates should be done by incrementing package version and making another GitHub release.
2. Create GitHub Release
   1. Go to your GitHub repository
   2. Click on `Releases`
   3. Click on `Draft a new release`
   4. Use `tag` that is equals to your package version name

# Installation

### Option 1: Using Package Manager (recommended)

- Open `Package Manager` in Unity Editor
- Click on the small `+` button in the top left corner
- Select `Add package from git URL`
- Paste URL to your GitHub repository with simple modification:
  `https://github.com/USER/REPO.git`
  Don't forget to replace **USER** and **REPO** to yours

#### **Or** you may use special version if you made one

> To make a version at GitHub, you may need to create Tag with the version name. Also, I would recommend to make a GitHub Release as well.

`https://github.com/USER/REPO.git#v1.0.0`
Don't forget to replace **USER** and **REPO** to yours

### Option 2: Manual

Modify `manifest.json` file. Need to add the line into your's `dependencies` object. Change `your.package.name` to the name of your package. And replace **USER** and **REPO** to yours.

```json
{
    "dependencies": {
        "your.package.name": "https://github.com/USER/REPO.git"
    }
}
```

#### **Or** you may use special version if you create one

Don't forget to replace **USER** and **REPO** to yours.

```json
{
    "dependencies": {
        "your.package.name": "https://github.com/USER/REPO.git#v1.0.0"
    }
}
```
