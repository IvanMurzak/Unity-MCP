/*
┌──────────────────────────────────────────────────────────────────┐
│  Author: Ivan Murzak (https://github.com/IvanMurzak)             │
│  Repository: GitHub (https://github.com/IvanMurzak/Unity-MCP)    │
│  Copyright (c) 2025 Ivan Murzak                                  │
│  Licensed under the Apache License, Version 2.0.                 │
│  See the LICENSE file in the project root for more information.  │
└──────────────────────────────────────────────────────────────────┘
*/
#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using com.IvanMurzak.ReflectorNet;
using com.IvanMurzak.ReflectorNet.Utils;
using com.IvanMurzak.Unity.MCP.Common.Model;
using Microsoft.Extensions.Logging;
using R3;

namespace com.IvanMurzak.Unity.MCP.Common
{
    public class McpRunner : IMcpRunner
    {
        static readonly JsonElement EmptyInputSchema = JsonDocument.Parse("{\"type\":\"object\"}").RootElement;

        protected readonly ILogger<McpRunner> _logger;
        protected readonly Reflector _reflector;
        readonly ToolRunnerCollection _tools;
        readonly PromptRunnerCollection _prompts;
        readonly ResourceRunnerCollection _resources;
        readonly Subject<Unit> _onToolsUpdated = new();
        readonly Subject<Unit> _onPromptsUpdated = new();
        readonly Subject<Unit> _onResourcesUpdated = new();

        public Reflector Reflector => _reflector;
        public Observable<Unit> OnToolsUpdated => _onToolsUpdated;
        public Observable<Unit> OnResourcesUpdated => _onResourcesUpdated;
        public Observable<Unit> OnPromptsUpdated => _onPromptsUpdated;

        public McpRunner(ILogger<McpRunner> logger, Reflector reflector, ToolRunnerCollection tools, PromptRunnerCollection prompts, ResourceRunnerCollection resources)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _logger.LogTrace("Ctor");
            _reflector = reflector ?? throw new ArgumentNullException(nameof(reflector));
            _tools = tools ?? throw new ArgumentNullException(nameof(tools));
            _prompts = prompts ?? throw new ArgumentNullException(nameof(prompts));
            _resources = resources ?? throw new ArgumentNullException(nameof(resources));

            if (_logger.IsEnabled(LogLevel.Trace))
            {
                _logger.LogTrace("Registered tools [{0}]:", tools.Count);
                foreach (var kvp in tools)
                    _logger.LogTrace("Tool: {0}", kvp.Key);
            }

            if (_logger.IsEnabled(LogLevel.Trace))
            {
                _logger.LogTrace("Registered resources [{0}]:", resources.Count);
                foreach (var kvp in resources)
                    _logger.LogTrace("Resource: {Name}. Route: {Route}", kvp.Key, kvp.Value.Route);
            }
        }

        #region Tools
        public int EnabledToolsCount => _tools.Count(kvp => kvp.Value.Enabled);
        public int TotalToolsCount => _tools.Count;
        public bool HasTool(string name) => _tools.ContainsKey(name);
        public bool AddTool(string name, IRunTool runner)
        {
            if (HasTool(name))
            {
                _logger.LogWarning("Tool with Name '{0}' already exists. Skipping addition.", name);
                return false;
            }

            _tools[name] = runner;
            _onToolsUpdated.OnNext(Unit.Default);
            return true;
        }
        public bool RemoveTool(string name)
        {
            if (!HasTool(name))
            {
                _logger.LogWarning("Tool with Name '{0}' not found. Cannot remove.", name);
                return false;
            }

            var removed = _tools.Remove(name);
            if (removed)
                _onToolsUpdated.OnNext(Unit.Default);

            return removed;
        }
        public bool IsToolEnabled(string name)
        {
            if (!_tools.TryGetValue(name, out var runner))
            {
                _logger.LogWarning("Tool with Name '{0}' not found.", name);
                return false;
            }

            return runner.Enabled;
        }
        public bool SetToolEnabled(string name, bool enabled)
        {
            if (!_tools.TryGetValue(name, out var runner))
            {
                _logger.LogWarning("Tool with Name '{0}' not found.", name);
                return false;
            }

            runner.Enabled = enabled;
            _onToolsUpdated.OnNext(Unit.Default);

            return true;
        }

        public async Task<IResponseData<ResponseCallTool>> RunCallTool(IRequestCallTool data, CancellationToken cancellationToken = default)
        {
            if (data == null)
                return ResponseData<ResponseCallTool>.Error(Consts.Guid.Zero, "Tool data is null.")
                    .Log(_logger);

            if (string.IsNullOrEmpty(data.Name))
                return ResponseData<ResponseCallTool>.Error(data.RequestID, "Tool.Name is null.")
                    .Log(_logger);

            if (!_tools.TryGetValue(data.Name, out var runner))
                return ResponseData<ResponseCallTool>.Error(data.RequestID, $"Tool with Name '{data.Name}' not found.")
                    .Log(_logger);
            try
            {
                if (_logger.IsEnabled(LogLevel.Information))
                {
                    var message = data.Arguments == null
                        ? $"Run tool '{data.Name}' with no parameters."
                        : $"Run tool '{data.Name}' with parameters[{data.Arguments.Count}]:\n{string.Join(",\n", data.Arguments)}\n";
                    _logger.LogInformation(message);
                }

                var result = await runner.Run(data.RequestID, data.Arguments, cancellationToken);
                if (result == null)
                    return ResponseData<ResponseCallTool>.Error(data.RequestID, $"Tool '{data.Name}' returned null result.")
                        .Log(_logger);

                result.Log(_logger);

                return result.Pack(data.RequestID);
            }
            catch (Exception ex)
            {
                // Handle or log the exception as needed
                return ResponseData<ResponseCallTool>.Error(data.RequestID, $"Failed to run tool '{data.Name}'. Exception: {ex}")
                    .Log(_logger, $"RunCallTool[{data.Name}]", ex);
            }
        }

        public Task<IResponseData<ResponseListTool[]>> RunListTool(IRequestListTool data, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogDebug("Listing tools.");
                var result = _tools
                    .Select(kvp =>
                    {
                        var response = new ResponseListTool()
                        {
                            Name = kvp.Key,
                            Title = kvp.Value.Title,
                            Description = kvp.Value.Description,
                            InputSchema = kvp.Value.InputSchema.ToJsonElement() ?? EmptyInputSchema
                        };
                        if (kvp.Value.OutputSchema == null)
                            return response;

                        if (kvp.Value.OutputSchema is not JsonNode jn)
                            return response;

                        if (jn.GetValueKind() != JsonValueKind.Object)
                            return response;

                        if (jn[JsonSchema.Type]?.GetValue<string>() != JsonSchema.Object)
                            return response;

                        response.OutputSchema = jn.ToJsonElement();
                        return response;
                    })
                    .ToArray();
                _logger.LogDebug("{0} Tools listed.", result.Length);

                return result
                    .Log(_logger)
                    .Pack(data.RequestID)
                    .TaskFromResult();
            }
            catch (Exception ex)
            {
                // Handle or log the exception as needed
                return ResponseData<ResponseListTool[]>.Error(data.RequestID, $"Failed to list tools. Exception: {ex}")
                    .Log(_logger, "RunListTool", ex)
                    .TaskFromResult();
            }
        }
        #endregion

        #region Resources
        public int EnabledResourcesCount => _resources.Count(kvp => kvp.Value.Enabled);
        public int TotalResourcesCount => _resources.Count;
        public bool HasResource(string name) => _resources.ContainsKey(name);
        public bool AddResource(IRunResource resourceParams)
        {
            if (resourceParams == null)
                throw new ArgumentNullException(nameof(resourceParams));

            if (HasResource(resourceParams.Name))
            {
                _logger.LogWarning("Resource with Name '{0}' already exists. Skipping addition.", resourceParams.Name);
                return false;
            }
            _resources[resourceParams.Name] = resourceParams;
            _onResourcesUpdated.OnNext(Unit.Default);

            return true;
        }
        public bool RemoveResource(string name)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("Resource name is null or empty.", nameof(name));

            if (!HasResource(name))
            {
                _logger.LogWarning("Resource with Name '{0}' does not exist. Skipping removal.", name);
                return false;
            }

            _resources.Remove(name);
            _onResourcesUpdated.OnNext(Unit.Default);

            return true;
        }
        public bool IsResourceEnabled(string name)
        {
            if (!_resources.TryGetValue(name, out var runner))
            {
                _logger.LogWarning("Resource with Name '{0}' not found.", name);
                return false;
            }

            return runner.Enabled;
        }
        public bool SetResourceEnabled(string name, bool enabled)
        {
            if (!_resources.TryGetValue(name, out var runner))
            {
                _logger.LogWarning("Resource with Name '{0}' not found.", name);
                return false;
            }

            runner.Enabled = enabled;
            _onResourcesUpdated.OnNext(Unit.Default);
            return true;
        }
        public async Task<IResponseData<ResponseResourceContent[]>> RunResourceContent(IRequestResourceContent data, CancellationToken cancellationToken = default)
        {
            if (data == null)
                throw new ArgumentException("Resource data is null.");

            if (data.Uri == null)
                throw new ArgumentException("Resource.Uri is null.");

            var runner = FindResourceContentRunner(data.Uri, _resources, out var uriTemplate)?.RunGetContent;
            if (runner == null || uriTemplate == null)
                throw new ArgumentException($"No route matches the URI: {data.Uri}");

            _logger.LogInformation("Executing resource '{0}'.", data.Uri);

            var parameters = ParseUriParameters(uriTemplate!, data.Uri);
            PrintParameters(parameters);

            // Execute the resource with the parameters from Uri
            var result = await runner.Run(parameters);
            return result.Pack(data.RequestID);
        }
        public async Task<IResponseData<ResponseListResource[]>> RunListResources(IRequestListResources data, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Listing resources. [{Count}]", _resources.Count);
            var tasks = _resources.Values
                .Select(resource => resource.RunListContext.Run());

            await Task.WhenAll(tasks);

            return tasks
                .SelectMany(x => x.Result)
                .ToArray()
                .Pack(data.RequestID);
        }
        public Task<IResponseData<ResponseResourceTemplate[]>> RunResourceTemplates(IRequestListResourceTemplates data, CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Listing resource templates. [{Count}]", _resources.Count);
            return _resources.Values
                .Select(resource => new ResponseResourceTemplate(resource.Route, resource.Name, resource.Description, resource.MimeType))
                .ToArray()
                .Pack(data.RequestID)
                .TaskFromResult();
        }
        IRunResource? FindResourceContentRunner(string uri, IDictionary<string, IRunResource> resources, out string? uriTemplate)
        {
            foreach (var route in resources)
            {
                if (IsMatch(route.Value.Route, uri))
                {
                    uriTemplate = route.Value.Route;
                    return route.Value;
                }
            }
            uriTemplate = null;
            return null;
        }
        #endregion

        #region Prompts
        public int EnabledPromptsCount => _prompts.Count(kvp => kvp.Value.Enabled);
        public int TotalPromptsCount => _prompts.Count;
        public bool HasPrompt(string name) => _prompts.ContainsKey(name);
        public bool AddPrompt(IRunPrompt runner)
        {
            if (runner == null)
                throw new ArgumentNullException(nameof(runner));

            if (HasPrompt(runner.Name))
            {
                _logger.LogWarning("Prompt with Name '{0}' already exists. Skipping addition.", runner.Name);
                return false;
            }

            _prompts[runner.Name] = runner;
            _onPromptsUpdated.OnNext(Unit.Default);
            return true;
        }
        public bool RemovePrompt(string name)
        {
            if (!HasPrompt(name))
            {
                _logger.LogWarning("Prompt with Name '{0}' not found. Cannot remove.", name);
                return false;
            }

            var removed = _prompts.Remove(name);
            if (removed)
                _onPromptsUpdated.OnNext(Unit.Default);

            return removed;
        }
        public bool IsPromptEnabled(string name)
        {
            if (!_prompts.TryGetValue(name, out var runner))
            {
                _logger.LogWarning("Prompt with Name '{0}' not found.", name);
                return false;
            }

            return runner.Enabled;
        }
        public bool SetPromptEnabled(string name, bool enabled)
        {
            if (!_prompts.TryGetValue(name, out var runner))
            {
                _logger.LogWarning("Prompt with Name '{0}' not found.", name);
                return false;
            }

            _logger.LogInformation("Setting Prompt '{0}' enabled state to {1}.", name, enabled);

            runner.Enabled = enabled;
            _onPromptsUpdated.OnNext(Unit.Default);

            return true;
        }
        public async Task<IResponseData<ResponseGetPrompt>> RunGetPrompt(IRequestGetPrompt request, CancellationToken cancellationToken = default)
        {
            if (!_prompts.TryGetValue(request.Name, out var runner))
            {
                return ResponseData<ResponseGetPrompt>
                    .Error(request.RequestID, $"Prompt with Name '{request.Name}' not found.")
                    .Log(_logger);
            }

            var result = await runner.Run(request.RequestID, request.Arguments, cancellationToken);

            result.Log(_logger);

            return result.Pack(request.RequestID);
        }

        public Task<IResponseData<ResponseListPrompts>> RunListPrompts(IRequestListPrompts request, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogDebug("Listing prompts. [{Count}]", _prompts.Count);
                var result = new ResponseListPrompts()
                {
                    Prompts = _prompts.Values
                        .Select(p => new ResponsePrompt()
                        {
                            Name = p.Name,
                            Title = p.Title,
                            Description = p.Description,
                            Arguments = p.InputSchema.ToResponsePromptArguments()
                        })
                        .ToList()
                };
                _logger.LogDebug("{0} Prompts listed.", result.Prompts.Count);

                return result
                    .Log(_logger)
                    .Pack(request.RequestID)
                    .TaskFromResult();
            }
            catch (Exception ex)
            {
                // Handle or log the exception as needed
                return ResponseData<ResponseListPrompts>.Error(request.RequestID, $"Failed to list tools. Exception: {ex}")
                    .Log(_logger, "RunListTool", ex)
                    .TaskFromResult();
            }
        }
        #endregion

        #region Utils
        bool IsMatch(string uriTemplate, string uri)
        {
            // Convert pattern to regex
            var regexPattern = "^" + Regex.Replace(uriTemplate, @"\{(\w+)\}", @"(?<$1>[^/]+)") + "(?:/.*)?$";

            return Regex.IsMatch(uri, regexPattern);
        }

        IDictionary<string, object?> ParseUriParameters(string pattern, string uri)
        {
            var parameters = new Dictionary<string, object?>()
            {
                { "uri", uri }
            };

            // Convert pattern to regex
            var regexPattern = "^" + Regex.Replace(pattern, @"\{(\w+)\}", @"(?<$1>.+)") + "(?:/.*)?$";

            var regex = new Regex(regexPattern);
            var match = regex.Match(uri);

            if (match.Success)
            {
                foreach (var groupName in regex.GetGroupNames())
                {
                    if (groupName != "0") // Skip the entire match group
                    {
                        parameters[groupName] = match.Groups[groupName].Value;
                    }
                }
            }

            return parameters;
        }

        void PrintParameters(IDictionary<string, object?> parameters)
        {
            if (!_logger.IsEnabled(LogLevel.Debug))
                return;

            var parameterLogs = string.Join(Environment.NewLine, parameters.Select(kvp => $"{kvp.Key} = {kvp.Value ?? "null"}"));
            _logger.LogDebug("Parsed Parameters [{0}]:\n{1}", parameters.Count, parameterLogs);
        }
        #endregion

        public void Dispose()
        {
            _tools.Clear();
            _prompts.Clear();
            _resources.Clear();
        }
    }
}
