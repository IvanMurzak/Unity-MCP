#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using com.IvanMurzak.Unity.MCP.Common.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace com.IvanMurzak.Unity.MCP.Common
{
    public static partial class ConnectorBuilderExtensions
    {
        public static IConnectorBuilder WithResources(this IConnectorBuilder builder, params Type[] targetTypes)
            => WithResources(builder, targetTypes.ToArray());

        public static IConnectorBuilder WithResources(this IConnectorBuilder builder, IEnumerable<Type> targetTypes)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));
            if (targetTypes == null)
                throw new ArgumentNullException(nameof(targetTypes));

            foreach (var targetType in targetTypes)
                WithResources(builder, targetType);

            return builder;
        }

        public static IConnectorBuilder WithResources<T>(this IConnectorBuilder builder)
            => WithResources(builder, typeof(T));

        public static IConnectorBuilder WithResources(this IConnectorBuilder builder, Type targetType)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));
            if (targetType == null)
                throw new ArgumentNullException(nameof(targetType));

            var logger = builder.Services.BuildServiceProvider().GetRequiredService<ILoggerFactory>().CreateLogger<RunResourceContent>();

            foreach (var method in targetType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance))
            {
                var attribute = method.GetCustomAttribute<ResourceAttribute>();
                if (attribute == null)
                    continue;

                if (!method.ReturnType.IsArray ||
                    !typeof(IResponseResourceContent).IsAssignableFrom(method.ReturnType.GetElementType()))
                    throw new InvalidOperationException($"Method {targetType.FullName}{method.Name} must return {nameof(IResponseResourceContent)} array.");

                var listContextMethodName = attribute.ListResources ?? throw new InvalidOperationException($"Method {method.Name} does not have a 'ListResources'.");
                var listContextMethod = targetType.GetMethod(listContextMethodName);
                if (listContextMethod == null)
                    throw new InvalidOperationException($"Method {targetType.FullName}{listContextMethodName} not found in type {targetType.Name}.");

                if (!listContextMethod.ReturnType.IsArray ||
                    !typeof(IResponseListResource).IsAssignableFrom(listContextMethod.ReturnType.GetElementType()))
                    throw new InvalidOperationException($"Method {targetType.FullName}{listContextMethod.Name} must return {nameof(IResponseListResource)} array.");

                var resourceParams = new RunResource
                (
                    route: attribute!.Route ?? throw new InvalidOperationException($"Method {targetType.FullName}{method.Name} does not have a 'routing'."),
                    name: attribute.Name ?? throw new InvalidOperationException($"Method {targetType.FullName}{method.Name} does not have a 'name'."),
                    description: attribute.Description,
                    mimeType: attribute.MimeType,
                    runnerGetContent: method.IsStatic
                        ? RunResourceContent.CreateFromStaticMethod(logger, method)
                        : RunResourceContent.CreateFromClassMethod(logger, targetType, method),
                    runnerListContext: listContextMethod.IsStatic
                        ? RunResourceContext.CreateFromStaticMethod(logger, listContextMethod)
                        : RunResourceContext.CreateFromClassMethod(logger, targetType, listContextMethod)
                );

                builder.AddResource(resourceParams!);
            }
            return builder;
        }

        public static IConnectorBuilder WithResourcesFromAssembly(this IConnectorBuilder builder, Assembly? assembly = null)
        {
            if (builder == null)
                throw new ArgumentNullException(nameof(builder));

            assembly ??= Assembly.GetCallingAssembly();

            return builder.WithResources(
                from t in assembly.GetTypes()
                where t.GetCustomAttribute<ResourceTypeAttribute>() is not null
                select t);
        }
    }
}