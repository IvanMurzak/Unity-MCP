#if !UNITY_5_3_OR_NEWER
using ModelContextProtocol.Protocol.Types;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Threading.Tasks;

namespace com.IvanMurzak.Unity.MCP.Server.API
{
    public partial class Tool_Assets
    {
        [McpServerTool
        (
            Name = "Assets_Find",
            Title = "Find assets in the project"
        )]
        [Description(@"Search the asset database using the search filter string.
Available types:
t:AnimationClip
t:AudioClip
t:AudioMixer
t:ComputeShader
t:Font
t:GUISkin
t:Material
t:Mesh
t:Model
t:PhysicMaterial
t:Prefab
t:Scene
t:Script
t:Shader
t:Sprite
t:Texture
t:VideoClip
t:VisualEffectAsset
t:VisualEffectSubgraph")]
        public Task<CallToolResponse> Search
        (
            [Description("The folders where the search will start. If null, the search will be performed in all folders.")]
            string[]? searchInFolders = null,
// <ref>https://docs.unity3d.com/ScriptReference/AssetDatabase.FindAssets.html</ref>
[Description(@"Searching filter. Could be empty.
Name: Filter assets by their filename (without extension). Words separated by whitespace are treated as separate name searches. For example, 'test asset' is a name of an Asset which will be searched for. Note that the name can be used to identify an asset. Further, the name used in the filter string can be specified as a subsection. For example, the 'test asset' example above can be matched using 'test'.
Labels (l:): Assets can have labels attached to them. Assets with particular labels can be found using the keyword 'l:' before each label. This indicates that the string is searching for labels.
Types (t:): Find assets based on explicitly identified types. The keyword 't:' is used as a way to specify that typed assets are being looked for. If more than one type is included in the filter string, then assets that match one class will be returned. Types can either be built-in types such as Texture2D or user-created script classes. User-created classes are assets created from a ScriptableObject class in the project. If all assets are wanted, use Object as all assets derive from Object. Specifying one or more folders using the searchInFolders argument will limit the searching to these folders and their child folders. This is faster than searching all assets in all folders.
AssetBundles (b:): Find assets which are part of an Asset bundle. The keyword 'b:' is used to determine that Asset bundle names should be part of the query.
Area (a:): Find assets in a specific area of a project. Valid values are 'all', 'assets', and 'packages'. Use this to make your query more specific using the 'a:' keyword followed by the area name to speed up searching.
Globbing (glob:): Use globbing to match specific rules. The keyword 'glob:' is followed by the query. For example, 'glob:Editor' will find all Editor folders in a project, 'glob:(Editor|Resources)' will find all Editor and Resources folders in a project, 'glob:Editor/*' will return all Assets inside Editor folders in a project, while 'glob:Editor/**' will return all Assets within Editor folders recursively.

Note:
Searching is case insensitive.")]
            string? filter = null
        )
        {
            return ToolRouter.Call("Assets_Find", arguments =>
            {
                if (filter != null && filter.Length > 0)
                    arguments[nameof(filter)] = filter;

                if (searchInFolders != null && searchInFolders.Length > 0)
                    arguments[nameof(searchInFolders)] = searchInFolders;
            });
        }
    }
}
#endif