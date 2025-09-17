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
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using com.IvanMurzak.ReflectorNet;
using com.IvanMurzak.Unity.MCP.Common.Model;
using Microsoft.Extensions.Logging;

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

        public Reflector Reflector => _reflector;

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
                    _logger.LogTrace("Resource: {0}", kvp.Key);
            }
        }

        public bool HasTool(string name) => _tools.ContainsKey(name);
        public bool HasResource(string name) => _resources.ContainsKey(name);

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
                    .Select(kvp => new ResponseListTool()
                    {
                        Name = kvp.Key,
                        Title = kvp.Value.Title,
                        Description = kvp.Value.Description,
                        InputSchema = kvp.Value.InputSchema.ToJsonElement() ?? EmptyInputSchema,
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
            var tasks = _resources.Values
                .Select(resource => resource.RunListContext.Run());

            await Task.WhenAll(tasks);

            return tasks
                .SelectMany(x => x.Result)
                .ToArray()
                .Pack(data.RequestID);
        }

        public Task<IResponseData<ResponseResourceTemplate[]>> RunResourceTemplates(IRequestListResourceTemplates data, CancellationToken cancellationToken = default)
            => _resources.Values
                .Select(resource => new ResponseResourceTemplate(resource.Route, resource.Name, resource.Description, resource.MimeType))
                .ToArray()
                .Pack(data.RequestID)
                .TaskFromResult();

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
            throw new NotImplementedException();
        }

        IRunResource? FindResourceContentRunner(string uri, IDictionary<string, IRunResource> resources, out string? uriTemplate)
        {
            foreach (var route in resources)
            {
                if (IsMatch(route.Key, uri))
                {
                    uriTemplate = route.Key;
                    return route.Value;
                }
            }
            uriTemplate = null;
            return null;
        }

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

        public void Dispose()
        {
            _resources.Clear();
            _tools.Clear();
        }
    }
}
