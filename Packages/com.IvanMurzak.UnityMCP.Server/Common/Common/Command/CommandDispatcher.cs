#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
using System;
using System.Collections.Generic;
using com.IvanMurzak.UnityMCP.Common.Data;
using Microsoft.Extensions.Logging;

namespace com.IvanMurzak.UnityMCP.Common
{
    public partial class CommandDispatcher : ICommandDispatcher
    {
        readonly ILogger<CommandDispatcher> _logger;
        readonly IDictionary<string, ICommand> _commands;

        public CommandDispatcher(ILogger<CommandDispatcher> logger, IDictionary<string, ICommand> commands)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _logger.LogTrace("Ctor.");

            _commands = commands ?? throw new ArgumentNullException(nameof(commands));
        }

        /// <summary>
        /// Executes a command based on the provided CommandData.
        /// </summary>
        /// <param name="data">The CommandData containing the command name and parameters.</param>
        public IResponseData Dispatch(ICommandData data)
        {
            if (data == null)
                return ResponseData.Error("Command data is null.")
                    .Log(_logger);

            if (data.Name == null)
                return ResponseData.Error("Command.Name is null.")
                    .Log(_logger);

            if (!_commands.TryGetValue(data.Name, out var command))
                return ResponseData.Error($"Command with name '{data.Name}' not found.")
                    .Log(_logger);

            try
            {
                var message = data.Parameters == null
                    ? $"Executing command '{data.Name}' with no parameters."
                    : $"Executing command '{data.Name}' with parameters[{data.Parameters.Count}]:\n{string.Join(",\n", data.Parameters)}";
                _logger.LogInformation(message);

                // Execute the command with the parameters from CommandData
                return command.Execute(data.Parameters)
                    .Log(_logger);
            }
            catch (Exception ex)
            {
                // Handle or log the exception as needed
                return ResponseData.Error($"Failed to execute command '{data.Name}'. Exception: {ex}")
                    .Log(_logger);
            }
        }

        public void Dispose()
        {
            _commands.Clear();
        }
    }
}