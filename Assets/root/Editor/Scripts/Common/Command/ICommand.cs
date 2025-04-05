using System.Collections.Generic;

namespace com.IvanMurzak.UnityMCP.Common.API
{
    public interface ICommand
    {
        /// <summary>
        /// Executes the target static method with the provided arguments.
        /// </summary>
        /// <param name="parameters">The arguments to pass to the method.</param>
        /// <returns>The result of the method execution, or null if the method is void.</returns>
        ICommandResponseData Execute(params object[] parameters);

        /// <summary>
        /// Executes the target method with named parameters.
        /// Missing parameters will be filled with their default values or the type's default value if no default is defined.
        /// </summary>
        /// <param name="namedParameters">A dictionary mapping parameter names to their values.</param>
        /// <returns>The result of the method execution, or null if the method is void.</returns>
        ICommandResponseData Execute(IDictionary<string, object?> namedParameters);
    }
}