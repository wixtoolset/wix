// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.ExtensibilityServices
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using System.Xml.Linq;
    using WixToolset.Data;
    using WixToolset.Extensibility;
    using WixToolset.Extensibility.Data;
    using WixToolset.Extensibility.Services;

    internal class PreprocessHelper : IPreprocessHelper
    {
        private static readonly char[] VariableSplitter = new char[] { '.' };
        private static readonly char[] ArgumentSplitter = new char[] { ',' };

        public PreprocessHelper(IServiceProvider serviceProvider)
        {
            this.ServiceProvider = serviceProvider;

            this.Messaging = this.ServiceProvider.GetService<IMessaging>();
        }

        private IServiceProvider ServiceProvider { get; }

        private IMessaging Messaging { get; }

        private Dictionary<string, IPreprocessorExtension> ExtensionsByPrefix { get; set; }

        public void AddVariable(IPreprocessContext context, string name, string value)
        {
            this.AddVariable(context, name, value, true);
        }

        public void AddVariable(IPreprocessContext context, string name, string value, bool showWarning)
        {
            var currentValue = this.GetVariableValue(context, "var", name);

            if (null == currentValue)
            {
                context.Variables.Add(name, value);
            }
            else
            {
                if (showWarning && value != currentValue)
                {
                    this.Messaging.Write(WarningMessages.VariableDeclarationCollision(context.CurrentSourceLineNumber, name, value, currentValue));
                }

                context.Variables[name] = value;
            }
        }

        public string EvaluateFunction(IPreprocessContext context, string function)
        {
            var prefixParts = function.Split(VariableSplitter, 2);

            // Check to make sure there are 2 parts and neither is an empty string.
            if (2 != prefixParts.Length || 0 >= prefixParts[0].Length || 0 >= prefixParts[1].Length)
            {
                throw new WixException(ErrorMessages.InvalidPreprocessorFunction(context.CurrentSourceLineNumber, function));
            }

            var prefix = prefixParts[0];
            var functionParts = prefixParts[1].Split(new char[] { '(' }, 2);

            // Check to make sure there are 2 parts, neither is an empty string, and the second part ends with a closing paren.
            if (2 != functionParts.Length || 0 >= functionParts[0].Length || 0 >= functionParts[1].Length || !functionParts[1].EndsWith(")", StringComparison.Ordinal))
            {
                throw new WixException(ErrorMessages.InvalidPreprocessorFunction(context.CurrentSourceLineNumber, function));
            }

            var functionName = functionParts[0];

            // Remove the trailing closing paren.
            var allArgs = functionParts[1].Substring(0, functionParts[1].Length - 1);

            // Parse the arguments and preprocess them.
            var args = allArgs.Split(ArgumentSplitter);
            for (var i = 0; i < args.Length; i++)
            {
                args[i] = this.PreprocessString(context, args[i].Trim());
            }

            var result = this.EvaluateFunction(context, prefix, functionName, args);

            // If the function didn't evaluate, try to evaluate the original value as a variable to support 
            // the use of open and closed parens inside variable names. Example: $(env.ProgramFiles(x86)) should resolve.
            if (result == null)
            {
                result = this.GetVariableValue(context, function, true);
            }

            return result;
        }

        public string EvaluateFunction(IPreprocessContext context, string prefix, string function, string[] args)
        {
            if (String.IsNullOrEmpty(prefix))
            {
                throw new ArgumentNullException("prefix");
            }

            if (String.IsNullOrEmpty(function))
            {
                throw new ArgumentNullException("function");
            }

            switch (prefix)
            {
                case "fun":
                    switch (function)
                    {
                        case "AutoVersion":
                            // Make sure the base version is specified
                            if (args.Length == 0 || String.IsNullOrEmpty(args[0]))
                            {
                                throw new WixException(ErrorMessages.InvalidPreprocessorFunctionAutoVersion(context.CurrentSourceLineNumber));
                            }

                            // Build = days since 1/1/2000; Revision = seconds since midnight / 2
                            var now = DateTime.UtcNow;
                            var build = now - new DateTime(2000, 1, 1);
                            var revision = now - new DateTime(now.Year, now.Month, now.Day);

                            return String.Join(".", args[0], (int)build.TotalDays, (int)(revision.TotalSeconds / 2));

                        default:
                            return null;
                    }

                default:
                    var extensionsByPrefix = this.GetExtensionsByPrefix(context);
                    if (extensionsByPrefix.TryGetValue(prefix, out var extension))
                    {
                        try
                        {
                            return extension.EvaluateFunction(prefix, function, args);
                        }
                        catch (Exception e)
                        {
                            throw new WixException(ErrorMessages.PreprocessorExtensionEvaluateFunctionFailed(context.CurrentSourceLineNumber, prefix, function, String.Join(",", args), e.Message));
                        }
                    }
                    else
                    {
                        return null;
                    }
            }
        }

        public string GetVariableValue(IPreprocessContext context, string variable, bool allowMissingPrefix)
        {
            // Strip the "$(" off the front.
            if (variable.StartsWith("$(", StringComparison.Ordinal))
            {
                variable = variable.Substring(2);
            }

            var parts = variable.Split(VariableSplitter, 2);

            if (1 == parts.Length) // missing prefix
            {
                if (allowMissingPrefix)
                {
                    return this.GetVariableValue(context, "var", parts[0]);
                }
                else
                {
                    throw new WixException(ErrorMessages.InvalidPreprocessorVariable(context.CurrentSourceLineNumber, variable));
                }
            }
            else
            {
                // check for empty variable name
                if (0 < parts[1].Length)
                {
                    string result = this.GetVariableValue(context, parts[0], parts[1]);

                    // If we didn't find it and we allow missing prefixes and the variable contains a dot, perhaps the dot isn't intended to indicate a prefix
                    if (null == result && allowMissingPrefix && variable.Contains("."))
                    {
                        result = this.GetVariableValue(context, "var", variable);
                    }

                    return result;
                }
                else
                {
                    throw new WixException(ErrorMessages.InvalidPreprocessorVariable(context.CurrentSourceLineNumber, variable));
                }
            }
        }

        public string GetVariableValue(IPreprocessContext context, string prefix, string name)
        {
            if (String.IsNullOrEmpty(prefix))
            {
                throw new ArgumentNullException("prefix");
            }

            if (String.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException("name");
            }

            switch (prefix)
            {
                case "env":
                    return Environment.GetEnvironmentVariable(name);

                case "sys":
                    switch (name)
                    {
                        case "CURRENTDIR":
                            return String.Concat(Directory.GetCurrentDirectory(), Path.DirectorySeparatorChar);

                        case "SOURCEFILEDIR":
                            return String.Concat(Path.GetDirectoryName(context.CurrentSourceLineNumber.FileName), Path.DirectorySeparatorChar);

                        case "SOURCEFILEPATH":
                            return context.CurrentSourceLineNumber.FileName;

                        case "PLATFORM":
                            this.Messaging.Write(WarningMessages.DeprecatedPreProcVariable(context.CurrentSourceLineNumber, "$(sys.PLATFORM)", "$(sys.BUILDARCH)"));

                            goto case "BUILDARCH";

                        case "BUILDARCH":
                            switch (context.Platform)
                            {
                                case Platform.X86:
                                    return "x86";

                                case Platform.X64:
                                    return "x64";

                                case Platform.IA64:
                                    return "ia64";

                                case Platform.ARM:
                                    return "arm";

                                default:
                                    throw new ArgumentException("Unknown platform enumeration '{0}' encountered.", context.Platform.ToString());
                            }

                        case "WIXMAJORVERSION":
                            return ThisAssembly.AssemblyFileVersion.Split('.')[0];

                        case "WIXVERSION":
                            return ThisAssembly.AssemblyFileVersion;

                        default:
                            return null;
                    }

                case "var":
                    return context.Variables.TryGetValue(name, out var result) ? result : null;

                default:
                    var extensionsByPrefix = this.GetExtensionsByPrefix(context);
                    if (extensionsByPrefix.TryGetValue(prefix, out var extension))
                    {
                        try
                        {
                            return extension.GetVariableValue(prefix, name);
                        }
                        catch (Exception e)
                        {
                            throw new WixException(ErrorMessages.PreprocessorExtensionGetVariableValueFailed(context.CurrentSourceLineNumber, prefix, name, e.Message));
                        }
                    }
                    else
                    {
                        return null;
                    }
            }
        }

        public void PreprocessPragma(IPreprocessContext context, string pragmaName, string args, XContainer parent)
        {
            var prefixParts = pragmaName.Split(VariableSplitter, 2);

            // Check to make sure there are 2 parts and neither is an empty string.
            if (2 != prefixParts.Length)
            {
                throw new WixException(ErrorMessages.InvalidPreprocessorPragma(context.CurrentSourceLineNumber, pragmaName));
            }

            var prefix = prefixParts[0];
            var pragma = prefixParts[1];

            if (String.IsNullOrEmpty(prefix) || String.IsNullOrEmpty(pragma))
            {
                throw new WixException(ErrorMessages.InvalidPreprocessorPragma(context.CurrentSourceLineNumber, pragmaName));
            }

            switch (prefix)
            {
                case "wix":
                    switch (pragma)
                    {
                        // Add any core defined pragmas here
                        default:
                            this.Messaging.Write(WarningMessages.PreprocessorUnknownPragma(context.CurrentSourceLineNumber, pragmaName));
                            break;
                    }
                    break;

                default:
                    var extensionsByPrefix = this.GetExtensionsByPrefix(context);
                    if (extensionsByPrefix.TryGetValue(prefix, out var extension))
                    {
                        if (!extension.ProcessPragma(prefix, pragma, args, parent))
                        {
                            this.Messaging.Write(WarningMessages.PreprocessorUnknownPragma(context.CurrentSourceLineNumber, pragmaName));
                        }
                    }
                    break;
            }
        }

        public string PreprocessString(IPreprocessContext context, string value)
        {
            var sb = new StringBuilder();
            var currentPosition = 0;
            var end = 0;

            while (-1 != (currentPosition = value.IndexOf('$', end)))
            {
                if (end < currentPosition)
                {
                    sb.Append(value, end, currentPosition - end);
                }

                end = currentPosition + 1;

                var remainder = value.Substring(end);
                if (remainder.StartsWith("$", StringComparison.Ordinal))
                {
                    sb.Append("$");
                    end++;
                }
                else if (remainder.StartsWith("(loc.", StringComparison.Ordinal))
                {
                    currentPosition = remainder.IndexOf(')');
                    if (-1 == currentPosition)
                    {
                        this.Messaging.Write(ErrorMessages.InvalidPreprocessorVariable(context.CurrentSourceLineNumber, remainder));
                        break;
                    }

                    sb.Append("$");   // just put the resource reference back as was
                    sb.Append(remainder, 0, currentPosition + 1);

                    end += currentPosition + 1;
                }
                else if (remainder.StartsWith("(", StringComparison.Ordinal))
                {
                    var openParenCount = 1;
                    var closingParenCount = 0;
                    var isFunction = false;
                    var foundClosingParen = false;

                    // find the closing paren
                    int closingParenPosition;
                    for (closingParenPosition = 1; closingParenPosition < remainder.Length; closingParenPosition++)
                    {
                        switch (remainder[closingParenPosition])
                        {
                            case '(':
                                openParenCount++;
                                isFunction = true;
                                break;

                            case ')':
                                closingParenCount++;
                                break;
                        }

                        if (openParenCount == closingParenCount)
                        {
                            foundClosingParen = true;
                            break;
                        }
                    }

                    // move the currentPosition to the closing paren
                    currentPosition += closingParenPosition;

                    if (!foundClosingParen)
                    {
                        if (isFunction)
                        {
                            this.Messaging.Write(ErrorMessages.InvalidPreprocessorFunction(context.CurrentSourceLineNumber, remainder));
                            break;
                        }
                        else
                        {
                            this.Messaging.Write(ErrorMessages.InvalidPreprocessorVariable(context.CurrentSourceLineNumber, remainder));
                            break;
                        }
                    }

                    var subString = remainder.Substring(1, closingParenPosition - 1);
                    string result = null;
                    if (isFunction)
                    {
                        result = this.EvaluateFunction(context, subString);
                    }
                    else
                    {
                        result = this.GetVariableValue(context, subString, true);
                    }

                    if (null == result)
                    {
                        if (isFunction)
                        {
                            this.Messaging.Write(ErrorMessages.UndefinedPreprocessorFunction(context.CurrentSourceLineNumber, subString));
                            break;
                        }
                        else
                        {
                            this.Messaging.Write(ErrorMessages.UndefinedPreprocessorVariable(context.CurrentSourceLineNumber, subString));
                            break;
                        }
                    }
                    else
                    {
                        if (!isFunction)
                        {
                            //this.OnResolvedVariable(new ResolvedVariableEventArgs(context.CurrentSourceLineNumber, subString, result));
                        }
                    }

                    sb.Append(result);
                    end += closingParenPosition + 1;
                }
                else   // just a floating "$" so put it in the final string (i.e. leave it alone) and keep processing
                {
                    sb.Append('$');
                }
            }

            if (end < value.Length)
            {
                sb.Append(value.Substring(end));
            }

            return sb.ToString();
        }

        public void RemoveVariable(IPreprocessContext context, string name)
        {
            if (!context.Variables.Remove(name))
            {
                this.Messaging.Write(ErrorMessages.CannotReundefineVariable(context.CurrentSourceLineNumber, name));
            }
        }

        private Dictionary<string, IPreprocessorExtension> GetExtensionsByPrefix(IPreprocessContext context)
        {
            if (this.ExtensionsByPrefix == null)
            {
                this.ExtensionsByPrefix = new Dictionary<string, IPreprocessorExtension>();

                var extensionManager = this.ServiceProvider.GetService<IExtensionManager>();

                var extensions = extensionManager.GetServices<IPreprocessorExtension>();

                foreach (var extension in extensions)
                {
                    if (null != extension.Prefixes)
                    {
                        foreach (string prefix in extension.Prefixes)
                        {
                            if (!this.ExtensionsByPrefix.ContainsKey(prefix))
                            {
                                this.ExtensionsByPrefix.Add(prefix, extension);
                            }
                        }
                    }
                }
            }

            return this.ExtensionsByPrefix;
        }
    }
}
