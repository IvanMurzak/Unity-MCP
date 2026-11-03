# Deploy your package to `npmjs.com` using `npm`

`npmjs.com` is a very popular registry for wide range of packages. It was not designed to host Unity related packages, but it works. If for any reason still need to use it, there is the tutorial how to make it work.

### Preparation (just once)

- Install [NPM](https://nodejs.org/en/download/)
- Create [NPMJS](https://npmjs.com) account
- Open `Commands` folder
- Run the script `npm_add_user.bat` and sign-in to your account

<details>
  <summary><code>npm_add_user.bat</code> script content</summary>

  It executes `npm adduser` command in the package root folder

  ```bash
cd ..\Assets\root
npm adduser
  ```

</details>

### Deploy

⚠️ Make sure you done editing `package.json` and files in `Packages/com.ivanmurzak.unity.mcp.particlesystem` folder. Because it is going to be public with no ability to discard it.

1. Increment `version` in `package.json` file
    > Any further updates should be done with incrementing package version.
    > Versions lower than `1.0.0` is represented in Unity as "preview".
2. Open `Commands` folder
3. Run the script `npm_deploy.bat` to publish your package to the public

<details>
  <summary><code>npm_deploy.bat</code> script content</summary>

  The first line in the script copies the `README.md` file to the package root. Because the README should be in a package also, that is a part of the package format.
  It executes `npm publish` command in the package root folder. The command publishes your package to the NPMJS platform automatically

  ```bash
xcopy ..\README.md ..\Assets\root\README.md* /Y
xcopy ..\README.md ..\Assets\root\Documentation~\README.md* /Y
cd ..\Assets\root
npm publish
pause
  ```

</details>

# Installation

When your package is distributed, you can install it into any Unity project.

- [Install OpenUPM-CLI](https://github.com/openupm/openupm-cli#installation)
- Open the command line in the Unity project folder
- Run the command

  ```bash
  openupm --registry https://registry.npmjs.org add YOUR_PACKAGE_NAME
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
            "name": "npmjs",
            "url": "https://registry.npmjs.org",
            "scopes": [
                "your.package.name"
            ]
        }
    ],
}
```
