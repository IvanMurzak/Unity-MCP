# Deploy your package to `OpenUPM`

OpenUPM is a registry of package made for Unity. It takes package sources from GitHub repository. That is why your package repository should be public. And your package should be manually submitted for the first time to OpenUPM to be registered in the index. Time to time OpenUPM would fetch fresh data from GitHub to identify package updates if any.

### Do just once

1. [Submit package to OpenUPM registry](https://openupm.com/packages/add/). Use link to your GitHub repository. **This should be done just once!**

### Do each update

⚠️ Make sure you done editing `package.json` and files in `Packages/com.ivanmurzak.unity.mcp.particlesystem` folder. Because it is going to be public with no ability to discard it.

1. Increment package version in the file `Packages/com.ivanmurzak.unity.mcp.particlesystem/package.json`. It has `version` property.
   > Any further updates should be done with incrementing package version and making another GitHub release.
   > Versions lower than `1.0.0` is represented in Unity as "preview".
2. Create GitHub Release
   1. Go to your GitHub repository
   2. Click on `Releases`
   3. Click on `Draft a new release`
   4. Use `tag` that is equals to your package version name

# Installation

When your package is distributed, you can install it into any Unity project.

- [Install OpenUPM-CLI](https://github.com/openupm/openupm-cli#installation)
- Open the command line in the Unity project folder
- Run the command

  ```bash
  openupm add YOUR_PACKAGE_NAME
  ```

### Alternative manual installation

If for any reason you don't want to use OpenUPM-CLI there is a manual approach.

Modify `manifest.json` file.

- Add the line into your's `dependencies` object
  - Change `your.package.name` to the name of your package
  - Replace **USER** and **REPO** to yours
- Add `scopedRegistries`
  - Change `your.package.name` to the name of your package

```json
{
    "dependencies": {
        "your.package.name": "1.0.0"
    },
    "scopedRegistries": [
        {
            "name": "OpenUPM",
            "url": "https://package.openupm.com",
            "scopes": [
                "your.package.name"
            ]
        }
    ],
}
```

---

### Good to know

> Make sure you made a new `Release` at GitHub
> Make sure the GitHub repository is public
> OpenUPM may take some time to index your new package or an update of your package. Usually up to 30 minutes.
