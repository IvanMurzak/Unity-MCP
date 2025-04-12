#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using com.IvanMurzak.Unity.MCP.Common.Data;
using Microsoft.Extensions.Logging;

namespace com.IvanMurzak.Unity.MCP.Common
{
    public class LocalApp : ILocalApp
    {
        protected readonly ILogger<LocalApp> _logger;
        protected readonly IConnectionManager _connectionManager;
        readonly IDictionary<string, IRunTool> _tools;
        readonly IDictionary<string, IRunResource> _resources;

        public LocalApp(ILogger<LocalApp> logger, IDictionary<string, IRunTool> tools, IDictionary<string, IRunResource> resources, IConnectionManager connectionManager)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _logger.LogTrace("Ctor.");
            _tools = tools ?? throw new ArgumentNullException(nameof(tools));
            _resources = resources ?? throw new ArgumentNullException(nameof(resources));
            _connectionManager = connectionManager ?? throw new ArgumentNullException(nameof(connectionManager));

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

        public async Task<IResponseData<IResponseCallTool>> RunCallTool(IRequestCallTool data, CancellationToken cancellationToken = default)
        {
            if (data == null)
                return ResponseData<IResponseCallTool>.Error("Tool data is null.")
                    .Log(_logger);

            if (string.IsNullOrEmpty(data.Name))
                return ResponseData<IResponseCallTool>.Error("Tool.Name is null.")
                    .Log(_logger);

            if (!_tools.TryGetValue(data.Name, out var runner))
                return ResponseData<IResponseCallTool>.Error($"Tool with Name '{data.Name}' not found.")
                    .Log(_logger);
            try
            {
                if (_logger.IsEnabled(LogLevel.Information))
                {
                    var message = data.Arguments == null
                        ? $"Run tool '{data.Name}' with no parameters."
                        : $"Run tool '{data.Name}' with parameters[{data.Arguments.Count}]:\n{string.Join(",\n", data.Arguments)}";
                    _logger.LogInformation(message);
                }

                var result = await runner.Run(data.Arguments);
                if (result == null)
                    return ResponseData<IResponseCallTool>.Error($"Tool '{data.Name}' returned null result.")
                        .Log(_logger);

                return result.Log(_logger).Pack();
            }
            catch (Exception ex)
            {
                // Handle or log the exception as needed
                return ResponseData<IResponseCallTool>.Error($"Failed to run tool '{data.Name}'. Exception: {ex}")
                    .Log(_logger, ex);
            }
        }

        public Task<IResponseData<List<IResponseListTool>>> RunListTool(IRequestListTool data, CancellationToken cancellationToken = default)
        {
            try
            {
                var result = _tools
                    .Select(kvp => new ResponseListTool()
                    {
                        Name = kvp.Key,
                        Description = kvp.Value.Description,
                        InputSchema = kvp.Value.InputSchema.ToJsonElement() ?? new()
                    })
                    .Cast<IResponseListTool>()
                    .ToList();

                return result
                    .Log(_logger)
                    .Pack()
                    .TaskFromResult();
            }
            catch (Exception ex)
            {
                // Handle or log the exception as needed
                return ResponseData<List<IResponseListTool>>.Error($"Failed to list tools. Exception: {ex}")
                    .Log(_logger, ex)
                    .TaskFromResult();
            }
        }

        public async Task<IResponseData<List<IResponseResourceContent>>> RunResourceContent(IRequestResourceContent data, CancellationToken cancellationToken = default)
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
            return result.Pack();
        }

        public async Task<IResponseData<List<IResponseListResource>>> RunListResources(IRequestListResources data, CancellationToken cancellationToken = default)
        {
            var tasks = _resources.Values
                .Select(resource => resource.RunListContext.Run());

            await Task.WhenAll(tasks);

            return tasks
                .SelectMany(x => x.Result)
                .ToList()
                .Pack();
        }

        public Task<IResponseData<List<IResponseResourceTemplate>>> RunResourceTemplates(IRequestListResourceTemplates data, CancellationToken cancellationToken = default)
            => _resources.Values
                .Select(resource => new ResponseResourceTemplate(resource.Route, resource.Name, resource.Description, resource.MimeType))
                .Cast<IResponseResourceTemplate>()
                .ToList()
                .Pack()
                .TaskFromResult();

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