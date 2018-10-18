// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Xml;
    using System.Xml.Linq;
    using WixToolset.Core.Preprocess;
    using WixToolset.Data;
    using WixToolset.Extensibility;
    using WixToolset.Extensibility.Data;
    using WixToolset.Extensibility.Services;

    /// <summary>
    /// Preprocessor object
    /// </summary>
    internal class Preprocessor : IPreprocessor
    {
        private static readonly Regex DefineRegex = new Regex(@"^\s*(?<varName>.+?)\s*(=\s*(?<varValue>.+?)\s*)?$", RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.ExplicitCapture);
        private static readonly Regex PragmaRegex = new Regex(@"^\s*(?<pragmaName>.+?)(?<pragmaValue>[\s\(].+?)?$", RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.ExplicitCapture);

        private static readonly XmlReaderSettings DocumentXmlReaderSettings = new XmlReaderSettings()
        {
            ValidationFlags = System.Xml.Schema.XmlSchemaValidationFlags.None,
            XmlResolver = null,
        };

        private static readonly XmlReaderSettings FragmentXmlReaderSettings = new XmlReaderSettings()
        {
            ConformanceLevel = ConformanceLevel.Fragment,
            ValidationFlags = System.Xml.Schema.XmlSchemaValidationFlags.None,
            XmlResolver = null,
        };

        internal Preprocessor(IServiceProvider serviceProvider)
        {
            this.ServiceProvider = serviceProvider;

            this.Messaging = this.ServiceProvider.GetService<IMessaging>();
        }

        private IServiceProvider ServiceProvider { get; }

        private IMessaging Messaging { get; }

        private IPreprocessContext Context { get; set; }

        private Stack<string> CurrentFileStack { get; } = new Stack<string>();

        private Dictionary<string, IPreprocessorExtension> ExtensionsByPrefix { get; } = new Dictionary<string, IPreprocessorExtension>();

        private Stack<bool> IncludeNextStack { get; } = new Stack<bool>();

        private Stack<SourceLineNumber> SourceStack { get; } = new Stack<SourceLineNumber>();

        private IPreprocessHelper Helper { get; set; }

        /// <summary>
        /// Event for ifdef/ifndef directives.
        /// </summary>
        public event IfDefEventHandler IfDef;

        /// <summary>
        /// Event for included files.
        /// </summary>
        public event IncludedFileEventHandler IncludedFile;

        /// <summary>
        /// Event for preprocessed stream.
        /// </summary>
        public event ProcessedStreamEventHandler ProcessedStream;

        /// <summary>
        /// Event for resolved variables.
        /// </summary>
        public event ResolvedVariableEventHandler ResolvedVariable;

        /// <summary>
        /// Get the source line information for the current element.  The precompiler will insert
        /// special source line number information for each element that it encounters.
        /// </summary>
        /// <param name="node">Element to get source line information for.</param>
        /// <returns>
        /// The source line number used to author the element being processed or
        /// null if the preprocessor did not process the element or the node is
        /// not an element.
        /// </returns>
        public static SourceLineNumber GetSourceLineNumbers(XObject node)
        {
            return SourceLineNumber.GetFromXAnnotation(node);
        }

        /// <summary>
        /// Preprocesses a file.
        /// </summary>
        /// <param name="context">The preprocessing context.</param>
        /// <returns>XDocument with the postprocessed data.</returns>
        public XDocument Preprocess(IPreprocessContext context)
        {
            this.Context = context;
            this.Context.CurrentSourceLineNumber = new SourceLineNumber(context.SourcePath);
            this.Context.Variables = this.Context.Variables == null ? new Dictionary<string, string>() : new Dictionary<string, string>(this.Context.Variables);

            this.PreProcess();

            XDocument document;
            using (var reader = XmlReader.Create(this.Context.SourcePath, DocumentXmlReaderSettings))
            {
                document = this.Process(reader);
            }

            return this.PostProcess(document);
        }

        /// <summary>
        /// Preprocesses a file.
        /// </summary>
        /// <param name="context">The preprocessing context.</param>
        /// <param name="reader">XmlReader to processing the context.</param>
        /// <returns>XDocument with the postprocessed data.</returns>
        public XDocument Preprocess(IPreprocessContext context, XmlReader reader)
        {
            if (String.IsNullOrEmpty(context.SourcePath) && !String.IsNullOrEmpty(reader.BaseURI))
            {
                var uri = new Uri(reader.BaseURI);
                context.SourcePath = uri.AbsolutePath;
            }

            this.Context = context;
            this.Context.CurrentSourceLineNumber = new SourceLineNumber(context.SourcePath);
            this.Context.Variables = this.Context.Variables == null ? new Dictionary<string, string>() : new Dictionary<string, string>(this.Context.Variables);

            this.PreProcess();

            var document = this.Process(reader);

            return this.PostProcess(document);
        }

        /// <summary>
        /// Preprocesses a file.
        /// </summary>
        /// <param name="context">The preprocessing context.</param>
        /// <param name="reader">XmlReader to processing the context.</param>
        /// <returns>XDocument with the postprocessed data.</returns>
        private XDocument Process(XmlReader reader)
        {
            this.Helper = this.ServiceProvider.GetService<IPreprocessHelper>();

            this.CurrentFileStack.Clear();
            this.CurrentFileStack.Push(this.Helper.GetVariableValue(this.Context, "sys", "SOURCEFILEDIR"));

            // Process the reader into the output.
            var output = new XDocument();
            try
            {
                this.PreprocessReader(false, reader, output, 0);

                // Fire event with post-processed document.
                this.ProcessedStream?.Invoke(this, new ProcessedStreamEventArgs(this.Context.SourcePath, output));
            }
            catch (XmlException e)
            {
                this.UpdateCurrentLineNumber(reader, 0);
                throw new WixException(ErrorMessages.InvalidXml(this.Context.CurrentSourceLineNumber, "source", e.Message));
            }

            return this.Messaging.EncounteredError ? null : output;
        }

        /// <summary>
        /// Determins if string is an operator.
        /// </summary>
        /// <param name="operation">String to check.</param>
        /// <returns>true if string is an operator.</returns>
        private static bool IsOperator(string operation)
        {
            if (operation == null)
            {
                return false;
            }

            operation = operation.Trim();
            if (0 == operation.Length)
            {
                return false;
            }

            if ("=" == operation ||
                "!=" == operation ||
                "<" == operation ||
                "<=" == operation ||
                ">" == operation ||
                ">=" == operation ||
                "~=" == operation)
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Determines if expression is currently inside quotes.
        /// </summary>
        /// <param name="expression">Expression to evaluate.</param>
        /// <param name="index">Index to start searching in expression.</param>
        /// <returns>true if expression is inside in quotes.</returns>
        private static bool InsideQuotes(string expression, int index)
        {
            if (index == -1)
            {
                return false;
            }

            var numQuotes = 0;
            var tmpIndex = 0;
            while (-1 != (tmpIndex = expression.IndexOf('\"', tmpIndex, index - tmpIndex)))
            {
                numQuotes++;
                tmpIndex++;
            }

            // found an even number of quotes before the index, so we're not inside
            if (numQuotes % 2 == 0)
            {
                return false;
            }

            // found an odd number of quotes, so we are inside
            return true;
        }

        /// <summary>
        /// Tests expression to see if it starts with a keyword.
        /// </summary>
        /// <param name="expression">Expression to test.</param>
        /// <param name="operation">Operation to test for.</param>
        /// <returns>true if expression starts with a keyword.</returns>
        private static bool StartsWithKeyword(string expression, PreprocessorOperation operation)
        {
            expression = expression.ToUpperInvariant();
            switch (operation)
            {
            case PreprocessorOperation.Not:
                if (expression.StartsWith("NOT ", StringComparison.Ordinal) || expression.StartsWith("NOT(", StringComparison.Ordinal))
                {
                    return true;
                }
                break;
            case PreprocessorOperation.And:
                if (expression.StartsWith("AND ", StringComparison.Ordinal) || expression.StartsWith("AND(", StringComparison.Ordinal))
                {
                    return true;
                }
                break;
            case PreprocessorOperation.Or:
                if (expression.StartsWith("OR ", StringComparison.Ordinal) || expression.StartsWith("OR(", StringComparison.Ordinal))
                {
                    return true;
                }
                break;
            default:
                break;
            }
            return false;
        }

        /// <summary>
        /// Processes an xml reader into an xml writer.
        /// </summary>
        /// <param name="include">Specifies if reader is from an included file.</param>
        /// <param name="reader">Reader for the source document.</param>
        /// <param name="container">Node where content should be added.</param>
        /// <param name="offset">Original offset for the line numbers being processed.</param>
        private void PreprocessReader(bool include, XmlReader reader, XContainer container, int offset)
        {
            var currentContainer = container;
            var containerStack = new Stack<XContainer>();

            var ifContext = new IfContext(true, true, IfState.Unknown); // start by assuming we want to keep the nodes in the source code
            var ifStack = new Stack<IfContext>();

            // process the reader into the writer
            while (reader.Read())
            {
                // update information here in case an error occurs before the next read
                this.UpdateCurrentLineNumber(reader, offset);

                var sourceLineNumbers = this.Context.CurrentSourceLineNumber;

                // check for changes in conditional processing
                if (XmlNodeType.ProcessingInstruction == reader.NodeType)
                {
                    var ignore = false;
                    string name = null;

                    switch (reader.LocalName)
                    {
                    case "if":
                        ifStack.Push(ifContext);
                        if (ifContext.IsTrue)
                        {
                            ifContext = new IfContext(ifContext.IsTrue & ifContext.Active, this.EvaluateExpression(reader.Value), IfState.If);
                        }
                        else // Use a default IfContext object so we don't try to evaluate the expression if the context isn't true
                        {
                            ifContext = new IfContext();
                        }
                        ignore = true;
                        break;

                    case "ifdef":
                        ifStack.Push(ifContext);
                        name = reader.Value.Trim();
                        if (ifContext.IsTrue)
                        {
                            ifContext = new IfContext(ifContext.IsTrue & ifContext.Active, (null != this.Helper.GetVariableValue(this.Context, name, true)), IfState.If);
                        }
                        else // Use a default IfContext object so we don't try to evaluate the expression if the context isn't true
                        {
                            ifContext = new IfContext();
                        }
                        ignore = true;
                        this.IfDef?.Invoke(this, new IfDefEventArgs(sourceLineNumbers, true, ifContext.IsTrue, name));
                        break;

                    case "ifndef":
                        ifStack.Push(ifContext);
                        name = reader.Value.Trim();
                        if (ifContext.IsTrue)
                        {
                            ifContext = new IfContext(ifContext.IsTrue & ifContext.Active, (null == this.Helper.GetVariableValue(this.Context, name, true)), IfState.If);
                        }
                        else // Use a default IfContext object so we don't try to evaluate the expression if the context isn't true
                        {
                            ifContext = new IfContext();
                        }
                        ignore = true;
                        this.IfDef?.Invoke(this, new IfDefEventArgs(sourceLineNumbers, false, !ifContext.IsTrue, name));
                        break;

                    case "elseif":
                        if (0 == ifStack.Count)
                        {
                            throw new WixException(ErrorMessages.UnmatchedPreprocessorInstruction(sourceLineNumbers, "if", "elseif"));
                        }

                        if (IfState.If != ifContext.IfState && IfState.ElseIf != ifContext.IfState)
                        {
                            throw new WixException(ErrorMessages.UnmatchedPreprocessorInstruction(sourceLineNumbers, "if", "elseif"));
                        }

                        ifContext.IfState = IfState.ElseIf;   // we're now in an elseif
                        if (!ifContext.WasEverTrue)   // if we've never evaluated the if context to true, then we can try this test
                        {
                            ifContext.IsTrue = this.EvaluateExpression(reader.Value);
                        }
                        else if (ifContext.IsTrue)
                        {
                            ifContext.IsTrue = false;
                        }
                        ignore = true;
                        break;

                    case "else":
                        if (0 == ifStack.Count)
                        {
                            throw new WixException(ErrorMessages.UnmatchedPreprocessorInstruction(sourceLineNumbers, "if", "else"));
                        }

                        if (IfState.If != ifContext.IfState && IfState.ElseIf != ifContext.IfState)
                        {
                            throw new WixException(ErrorMessages.UnmatchedPreprocessorInstruction(sourceLineNumbers, "if", "else"));
                        }

                        ifContext.IfState = IfState.Else;   // we're now in an else
                        ifContext.IsTrue = !ifContext.WasEverTrue;   // if we were never true, we can be true now
                        ignore = true;
                        break;

                    case "endif":
                        if (0 == ifStack.Count)
                        {
                            throw new WixException(ErrorMessages.UnmatchedPreprocessorInstruction(sourceLineNumbers, "if", "endif"));
                        }

                        ifContext = ifStack.Pop();
                        ignore = true;
                        break;
                    }

                    if (ignore)   // ignore this node since we just handled it above
                    {
                        continue;
                    }
                }

                if (!ifContext.Active || !ifContext.IsTrue)   // if our context is not true then skip the rest of the processing and just read the next thing
                {
                    continue;
                }

                switch (reader.NodeType)
                {
                case XmlNodeType.XmlDeclaration:
                    var document = currentContainer as XDocument;
                    if (null != document)
                    {
                        document.Declaration = new XDeclaration(null, null, null);
                        while (reader.MoveToNextAttribute())
                        {
                            switch (reader.LocalName)
                            {
                            case "version":
                                document.Declaration.Version = reader.Value;
                                break;

                            case "encoding":
                                document.Declaration.Encoding = reader.Value;
                                break;

                            case "standalone":
                                document.Declaration.Standalone = reader.Value;
                                break;
                            }
                        }

                    }
                    //else
                    //{
                    //    display an error? Can this happen?
                    //}
                    break;

                case XmlNodeType.ProcessingInstruction:
                    switch (reader.LocalName)
                    {
                    case "define":
                        this.PreprocessDefine(reader.Value);
                        break;

                    case "error":
                        this.PreprocessError(reader.Value);
                        break;

                    case "warning":
                        this.PreprocessWarning(reader.Value);
                        break;

                    case "undef":
                        this.PreprocessUndef(reader.Value);
                        break;

                    case "include":
                        this.UpdateCurrentLineNumber(reader, offset);
                        this.PreprocessInclude(reader.Value, currentContainer);
                        break;

                    case "foreach":
                        this.PreprocessForeach(reader, currentContainer, offset);
                        break;

                    case "endforeach": // endforeach is handled in PreprocessForeach, so seeing it here is an error
                        throw new WixException(ErrorMessages.UnmatchedPreprocessorInstruction(sourceLineNumbers, "foreach", "endforeach"));

                    case "pragma":
                        this.PreprocessPragma(reader.Value, currentContainer);
                        break;

                    default:
                        // unknown processing instructions are currently ignored
                        break;
                    }
                    break;

                case XmlNodeType.Element:
                    if (0 < this.IncludeNextStack.Count && this.IncludeNextStack.Peek())
                    {
                        if ("Include" != reader.LocalName)
                        {
                            this.Messaging.Write(ErrorMessages.InvalidDocumentElement(sourceLineNumbers, reader.Name, "include", "Include"));
                        }

                        this.IncludeNextStack.Pop();
                        this.IncludeNextStack.Push(false);
                        break;
                    }

                    var empty = reader.IsEmptyElement;
                    var ns = XNamespace.Get(reader.NamespaceURI);
                    var element = new XElement(ns + reader.LocalName);
                    currentContainer.Add(element);

                    this.UpdateCurrentLineNumber(reader, offset);
                    element.AddAnnotation(sourceLineNumbers);

                    while (reader.MoveToNextAttribute())
                    {
                        var value = this.Helper.PreprocessString(this.Context, reader.Value);

                        var attribNamespace = XNamespace.Get(reader.NamespaceURI);
                        attribNamespace = XNamespace.Xmlns == attribNamespace && reader.LocalName.Equals("xmlns") ? XNamespace.None : attribNamespace;

                        element.Add(new XAttribute(attribNamespace + reader.LocalName, value));
                    }

                    if (!empty)
                    {
                        containerStack.Push(currentContainer);
                        currentContainer = element;
                    }
                    break;

                case XmlNodeType.EndElement:
                    if (0 < reader.Depth || !include)
                    {
                        currentContainer = containerStack.Pop();
                    }
                    break;

                case XmlNodeType.Text:
                    var postprocessedText = this.Helper.PreprocessString(this.Context, reader.Value);
                    currentContainer.Add(postprocessedText);
                    break;

                case XmlNodeType.CDATA:
                    var postprocessedValue = this.Helper.PreprocessString(this.Context, reader.Value);
                    currentContainer.Add(new XCData(postprocessedValue));
                    break;

                default:
                    break;
                }
            }

            if (0 != ifStack.Count)
            {
                throw new WixException(ErrorMessages.NonterminatedPreprocessorInstruction(this.Context.CurrentSourceLineNumber, "if", "endif"));
            }

            // TODO: can this actually happen?
            if (0 != containerStack.Count)
            {
                throw new WixException(ErrorMessages.NonterminatedPreprocessorInstruction(this.Context.CurrentSourceLineNumber, "nodes", "nodes"));
            }
        }

        /// <summary>
        /// Processes an error processing instruction.
        /// </summary>
        /// <param name="errorMessage">Text from source.</param>
        private void PreprocessError(string errorMessage)
        {
            // Resolve other variables in the error message.
            errorMessage = this.Helper.PreprocessString(this.Context, errorMessage);

            throw new WixException(ErrorMessages.PreprocessorError(this.Context.CurrentSourceLineNumber, errorMessage));
        }

        /// <summary>
        /// Processes a warning processing instruction.
        /// </summary>
        /// <param name="warningMessage">Text from source.</param>
        private void PreprocessWarning(string warningMessage)
        {
            // Resolve other variables in the warning message.
            warningMessage = this.Helper.PreprocessString(this.Context, warningMessage);

            this.Messaging.Write(WarningMessages.PreprocessorWarning(this.Context.CurrentSourceLineNumber, warningMessage));
        }

        /// <summary>
        /// Processes a define processing instruction and creates the appropriate parameter.
        /// </summary>
        /// <param name="originalDefine">Text from source.</param>
        private void PreprocessDefine(string originalDefine)
        {
            var match = DefineRegex.Match(originalDefine);

            if (!match.Success)
            {
                throw new WixException(ErrorMessages.IllegalDefineStatement(this.Context.CurrentSourceLineNumber, originalDefine));
            }

            var defineName = match.Groups["varName"].Value;
            var defineValue = match.Groups["varValue"].Value;

            // strip off the optional quotes
            if (1 < defineValue.Length &&
                   ((defineValue.StartsWith("\"", StringComparison.Ordinal) && defineValue.EndsWith("\"", StringComparison.Ordinal))
                || (defineValue.StartsWith("'", StringComparison.Ordinal) && defineValue.EndsWith("'", StringComparison.Ordinal))))
            {
                defineValue = defineValue.Substring(1, defineValue.Length - 2);
            }

            // resolve other variables in the variable value
            defineValue = this.Helper.PreprocessString(this.Context, defineValue);

            if (defineName.StartsWith("var.", StringComparison.Ordinal))
            {
                this.Helper.AddVariable(this.Context, defineName.Substring(4), defineValue);
            }
            else
            {
                this.Helper.AddVariable(this.Context, defineName, defineValue);
            }
        }

        /// <summary>
        /// Processes an undef processing instruction and creates the appropriate parameter.
        /// </summary>
        /// <param name="originalDefine">Text from source.</param>
        private void PreprocessUndef(string originalDefine)
        {
            var name = this.Helper.PreprocessString(this.Context, originalDefine.Trim());

            if (name.StartsWith("var.", StringComparison.Ordinal))
            {
                this.Helper.RemoveVariable(this.Context, name.Substring(4));
            }
            else
            {
                this.Helper.RemoveVariable(this.Context, name);
            }
        }

        /// <summary>
        /// Processes an included file.
        /// </summary>
        /// <param name="includePath">Path to included file.</param>
        /// <param name="parent">Parent container for included content.</param>
        private void PreprocessInclude(string includePath, XContainer parent)
        {
            var sourceLineNumbers = this.Context.CurrentSourceLineNumber;

            // Preprocess variables in the path.
            includePath = this.Helper.PreprocessString(this.Context, includePath);

            var includeFile = this.GetIncludeFile(includePath);

            if (null == includeFile)
            {
                throw new WixException(ErrorMessages.FileNotFound(sourceLineNumbers, includePath, "include"));
            }

            using (var reader = XmlReader.Create(includeFile, DocumentXmlReaderSettings))
            {
                this.PushInclude(includeFile);

                // process the included reader into the writer
                try
                {
                    this.PreprocessReader(true, reader, parent, 0);
                }
                catch (XmlException e)
                {
                    this.UpdateCurrentLineNumber(reader, 0);
                    throw new WixException(ErrorMessages.InvalidXml(sourceLineNumbers, "source", e.Message));
                }

                this.IncludedFile?.Invoke(this, new IncludedFileEventArgs(sourceLineNumbers, includeFile));

                this.PopInclude();
            }
        }

        /// <summary>
        /// Preprocess a foreach processing instruction.
        /// </summary>
        /// <param name="reader">The xml reader.</param>
        /// <param name="container">The container where to output processed data.</param>
        /// <param name="offset">Offset for the line numbers.</param>
        private void PreprocessForeach(XmlReader reader, XContainer container, int offset)
        {
            // Find the "in" token.
            var indexOfInToken = reader.Value.IndexOf(" in ", StringComparison.Ordinal);
            if (0 > indexOfInToken)
            {
                throw new WixException(ErrorMessages.IllegalForeach(this.Context.CurrentSourceLineNumber, reader.Value));
            }

            // parse out the variable name
            var varName = reader.Value.Substring(0, indexOfInToken).Trim();
            var varValuesString = reader.Value.Substring(indexOfInToken + 4).Trim();

            // preprocess the variable values string because it might be a variable itself
            varValuesString = this.Helper.PreprocessString(this.Context, varValuesString);

            var varValues = varValuesString.Split(';');

            // go through all the empty strings
            while (reader.Read() && XmlNodeType.Whitespace == reader.NodeType)
            {
            }

            // get the offset of this xml fragment (for some reason its always off by 1)
            var lineInfoReader = reader as IXmlLineInfo;
            if (null != lineInfoReader)
            {
                offset += lineInfoReader.LineNumber - 1;
            }

            var textReader = reader as XmlTextReader;
            // dump the xml to a string (maintaining whitespace if possible)
            if (null != textReader)
            {
                textReader.WhitespaceHandling = WhitespaceHandling.All;
            }

            var fragmentBuilder = new StringBuilder();
            var nestedForeachCount = 1;
            while (nestedForeachCount != 0)
            {
                if (reader.NodeType == XmlNodeType.ProcessingInstruction)
                {
                    switch (reader.LocalName)
                    {
                    case "foreach":
                        ++nestedForeachCount;
                        // Output the foreach statement
                        fragmentBuilder.AppendFormat("<?foreach {0}?>", reader.Value);
                        break;

                    case "endforeach":
                        --nestedForeachCount;
                        if (0 != nestedForeachCount)
                        {
                            fragmentBuilder.Append("<?endforeach ?>");
                        }
                        break;

                    default:
                        fragmentBuilder.AppendFormat("<?{0} {1}?>", reader.LocalName, reader.Value);
                        break;
                    }
                }
                else if (reader.NodeType == XmlNodeType.Element)
                {
                    fragmentBuilder.Append(reader.ReadOuterXml());
                    continue;
                }
                else if (reader.NodeType == XmlNodeType.Whitespace)
                {
                    // Or output the whitespace
                    fragmentBuilder.Append(reader.Value);
                }
                else if (reader.NodeType == XmlNodeType.None)
                {
                    throw new WixException(ErrorMessages.ExpectedEndforeach(this.Context.CurrentSourceLineNumber));
                }

                reader.Read();
            }

            using (var fragmentStream = new MemoryStream(Encoding.UTF8.GetBytes(fragmentBuilder.ToString())))
            {
                // process each iteration, updating the variable's value each time
                foreach (var varValue in varValues)
                {
                    using (var loopReader = XmlReader.Create(fragmentStream, FragmentXmlReaderSettings))
                    {
                        // Always overwrite foreach variables.
                        this.Helper.AddVariable(this.Context, varName, varValue, false);

                        try
                        {
                            this.PreprocessReader(false, loopReader, container, offset);
                        }
                        catch (XmlException e)
                        {
                            this.UpdateCurrentLineNumber(loopReader, offset);
                            throw new WixException(ErrorMessages.InvalidXml(this.Context.CurrentSourceLineNumber, "source", e.Message));
                        }

                        fragmentStream.Position = 0; // seek back to the beginning for the next loop.
                    }
                }
            }
        }

        /// <summary>
        /// Processes a pragma processing instruction
        /// </summary>
        /// <param name="pragmaText">Text from source.</param>
        private void PreprocessPragma(string pragmaText, XContainer parent)
        {
            var match = PragmaRegex.Match(pragmaText);

            if (!match.Success)
            {
                throw new WixException(ErrorMessages.InvalidPreprocessorPragma(this.Context.CurrentSourceLineNumber, pragmaText));
            }

            // resolve other variables in the pragma argument(s)
            var pragmaArgs = this.Helper.PreprocessString(this.Context, match.Groups["pragmaValue"].Value).Trim();

            try
            {
                this.Helper.PreprocessPragma(this.Context, match.Groups["pragmaName"].Value.Trim(), pragmaArgs, parent);
            }
            catch (Exception e)
            {
                throw new WixException(ErrorMessages.PreprocessorExtensionPragmaFailed(this.Context.CurrentSourceLineNumber, pragmaText, e.Message));
            }
        }

        /// <summary>
        /// Gets the next token in an expression.
        /// </summary>
        /// <param name="originalExpression">Expression to parse.</param>
        /// <param name="expression">Expression with token removed.</param>
        /// <param name="stringLiteral">Flag if token is a string literal instead of a variable.</param>
        /// <returns>Next token.</returns>
        private string GetNextToken(string originalExpression, ref string expression, out bool stringLiteral)
        {
            stringLiteral = false;
            var token = String.Empty;
            expression = expression.Trim();
            if (0 == expression.Length)
            {
                return String.Empty;
            }

            if (expression.StartsWith("\"", StringComparison.Ordinal))
            {
                stringLiteral = true;
                var endingQuotes = expression.IndexOf('\"', 1);
                if (-1 == endingQuotes)
                {
                    throw new WixException(ErrorMessages.UnmatchedQuotesInExpression(this.Context.CurrentSourceLineNumber, originalExpression));
                }

                // cut the quotes off the string
                token = this.Helper.PreprocessString(this.Context, expression.Substring(1, endingQuotes - 1));

                // advance past this string
                expression = expression.Substring(endingQuotes + 1).Trim();
            }
            else if (expression.StartsWith("$(", StringComparison.Ordinal))
            {
                // Find the ending paren of the expression
                var endingParen = -1;
                var openedCount = 1;
                for (var i = 2; i < expression.Length; i++)
                {
                    if ('(' == expression[i])
                    {
                        openedCount++;
                    }
                    else if (')' == expression[i])
                    {
                        openedCount--;
                    }

                    if (openedCount == 0)
                    {
                        endingParen = i;
                        break;
                    }
                }

                if (-1 == endingParen)
                {
                    throw new WixException(ErrorMessages.UnmatchedParenthesisInExpression(this.Context.CurrentSourceLineNumber, originalExpression));
                }
                token = expression.Substring(0, endingParen + 1);

                // Advance past this variable
                expression = expression.Substring(endingParen + 1).Trim();
            }
            else
            {
                // Cut the token off at the next equal, space, inequality operator,
                // or end of string, whichever comes first
                var space = expression.IndexOf(" ", StringComparison.Ordinal);
                var equals = expression.IndexOf("=", StringComparison.Ordinal);
                var lessThan = expression.IndexOf("<", StringComparison.Ordinal);
                var lessThanEquals = expression.IndexOf("<=", StringComparison.Ordinal);
                var greaterThan = expression.IndexOf(">", StringComparison.Ordinal);
                var greaterThanEquals = expression.IndexOf(">=", StringComparison.Ordinal);
                var notEquals = expression.IndexOf("!=", StringComparison.Ordinal);
                var equalsNoCase = expression.IndexOf("~=", StringComparison.Ordinal);
                int closingIndex;

                if (space == -1)
                {
                    space = Int32.MaxValue;
                }

                if (equals == -1)
                {
                    equals = Int32.MaxValue;
                }

                if (lessThan == -1)
                {
                    lessThan = Int32.MaxValue;
                }

                if (lessThanEquals == -1)
                {
                    lessThanEquals = Int32.MaxValue;
                }

                if (greaterThan == -1)
                {
                    greaterThan = Int32.MaxValue;
                }

                if (greaterThanEquals == -1)
                {
                    greaterThanEquals = Int32.MaxValue;
                }

                if (notEquals == -1)
                {
                    notEquals = Int32.MaxValue;
                }

                if (equalsNoCase == -1)
                {
                    equalsNoCase = Int32.MaxValue;
                }

                closingIndex = Math.Min(space, Math.Min(equals, Math.Min(lessThan, Math.Min(lessThanEquals, Math.Min(greaterThan, Math.Min(greaterThanEquals, Math.Min(equalsNoCase, notEquals)))))));

                if (Int32.MaxValue == closingIndex)
                {
                    closingIndex = expression.Length;
                }

                // If the index is 0, we hit an operator, so return it
                if (0 == closingIndex)
                {
                    // Length 2 operators
                    if (closingIndex == lessThanEquals || closingIndex == greaterThanEquals || closingIndex == notEquals || closingIndex == equalsNoCase)
                    {
                        closingIndex = 2;
                    }
                    else // Length 1 operators
                    {
                        closingIndex = 1;
                    }
                }

                // Cut out the new token
                token = expression.Substring(0, closingIndex).Trim();
                expression = expression.Substring(closingIndex).Trim();
            }

            return token;
        }

        /// <summary>
        /// Gets the value for a variable.
        /// </summary>
        /// <param name="originalExpression">Original expression for error message.</param>
        /// <param name="variable">Variable to evaluate.</param>
        /// <returns>Value of variable.</returns>
        private string EvaluateVariable(string originalExpression, string variable)
        {
            // By default it's a literal and will only be evaluated if it
            // matches the variable format
            var varValue = variable;

            if (variable.StartsWith("$(", StringComparison.Ordinal))
            {
                try
                {
                    varValue = this.Helper.PreprocessString(this.Context, variable);
                }
                catch (ArgumentNullException)
                {
                    // non-existent variables are expected
                    varValue = null;
                }
            }
            else if (variable.IndexOf("(", StringComparison.Ordinal) != -1 || variable.IndexOf(")", StringComparison.Ordinal) != -1)
            {
                // make sure it doesn't contain parenthesis
                throw new WixException(ErrorMessages.UnmatchedParenthesisInExpression(this.Context.CurrentSourceLineNumber, originalExpression));
            }
            else if (variable.IndexOf("\"", StringComparison.Ordinal) != -1)
            {
                // shouldn't contain quotes
                throw new WixException(ErrorMessages.UnmatchedQuotesInExpression(this.Context.CurrentSourceLineNumber, originalExpression));
            }

            return varValue;
        }

        /// <summary>
        /// Gets the left side value, operator, and right side value of an expression.
        /// </summary>
        /// <param name="originalExpression">Original expression to evaluate.</param>
        /// <param name="expression">Expression modified while processing.</param>
        /// <param name="leftValue">Left side value from expression.</param>
        /// <param name="operation">Operation in expression.</param>
        /// <param name="rightValue">Right side value from expression.</param>
        private void GetNameValuePair(string originalExpression, ref string expression, out string leftValue, out string operation, out string rightValue)
        {
            leftValue = this.GetNextToken(originalExpression, ref expression, out var stringLiteral);

            // If it wasn't a string literal, evaluate it
            if (!stringLiteral)
            {
                leftValue = this.EvaluateVariable(originalExpression, leftValue);
            }

            // Get the operation
            operation = this.GetNextToken(originalExpression, ref expression, out stringLiteral);
            if (IsOperator(operation))
            {
                if (stringLiteral)
                {
                    throw new WixException(ErrorMessages.UnmatchedQuotesInExpression(this.Context.CurrentSourceLineNumber, originalExpression));
                }

                rightValue = this.GetNextToken(originalExpression, ref expression, out stringLiteral);

                // If it wasn't a string literal, evaluate it
                if (!stringLiteral)
                {
                    rightValue = this.EvaluateVariable(originalExpression, rightValue);
                }
            }
            else
            {
                // Prepend the token back on the expression since it wasn't an operator
                // and put the quotes back on the literal if necessary

                if (stringLiteral)
                {
                    operation = "\"" + operation + "\"";
                }
                expression = (operation + " " + expression).Trim();

                // If no operator, just check for existence
                operation = "";
                rightValue = "";
            }
        }

        /// <summary>
        /// Evaluates an expression.
        /// </summary>
        /// <param name="originalExpression">Original expression to evaluate.</param>
        /// <param name="expression">Expression modified while processing.</param>
        /// <returns>true if expression evaluates to true.</returns>
        private bool EvaluateAtomicExpression(string originalExpression, ref string expression)
        {
            // Quick test to see if the first token is a variable
            var startsWithVariable = expression.StartsWith("$(", StringComparison.Ordinal);
            this.GetNameValuePair(originalExpression, ref expression, out var leftValue, out var operation, out var rightValue);

            var expressionValue = false;

            // If the variables don't exist, they were evaluated to null
            if (null == leftValue || null == rightValue)
            {
                if (operation.Length > 0)
                {
                    throw new WixException(ErrorMessages.ExpectedVariable(this.Context.CurrentSourceLineNumber, originalExpression));
                }

                // false expression
            }
            else if (operation.Length == 0)
            {
                // There is no right side of the equation.
                // If the variable was evaluated, it exists, so the expression is true
                if (startsWithVariable)
                {
                    expressionValue = true;
                }
                else
                {
                    throw new WixException(ErrorMessages.UnexpectedLiteral(this.Context.CurrentSourceLineNumber, originalExpression));
                }
            }
            else
            {
                leftValue = leftValue.Trim();
                rightValue = rightValue.Trim();
                if ("=" == operation)
                {
                    if (leftValue == rightValue)
                    {
                        expressionValue = true;
                    }
                }
                else if ("!=" == operation)
                {
                    if (leftValue != rightValue)
                    {
                        expressionValue = true;
                    }
                }
                else if ("~=" == operation)
                {
                    if (String.Equals(leftValue, rightValue, StringComparison.OrdinalIgnoreCase))
                    {
                        expressionValue = true;
                    }
                }
                else
                {
                    // Convert the numbers from strings
                    int rightInt;
                    int leftInt;
                    try
                    {
                        rightInt = Int32.Parse(rightValue, CultureInfo.InvariantCulture);
                        leftInt = Int32.Parse(leftValue, CultureInfo.InvariantCulture);
                    }
                    catch (FormatException)
                    {
                        throw new WixException(ErrorMessages.IllegalIntegerInExpression(this.Context.CurrentSourceLineNumber, originalExpression));
                    }
                    catch (OverflowException)
                    {
                        throw new WixException(ErrorMessages.IllegalIntegerInExpression(this.Context.CurrentSourceLineNumber, originalExpression));
                    }

                    // Compare the numbers
                    if ("<" == operation && leftInt < rightInt ||
                        "<=" == operation && leftInt <= rightInt ||
                        ">" == operation && leftInt > rightInt ||
                        ">=" == operation && leftInt >= rightInt)
                    {
                        expressionValue = true;
                    }
                }
            }

            return expressionValue;
        }

        /// <summary>
        /// Gets a sub-expression in parenthesis.
        /// </summary>
        /// <param name="originalExpression">Original expression to evaluate.</param>
        /// <param name="expression">Expression modified while processing.</param>
        /// <param name="endSubExpression">Index of end of sub-expression.</param>
        /// <returns>Sub-expression in parenthesis.</returns>
        private string GetParenthesisExpression(string originalExpression, string expression, out int endSubExpression)
        {
            endSubExpression = 0;

            // if the expression doesn't start with parenthesis, leave it alone
            if (!expression.StartsWith("(", StringComparison.Ordinal))
            {
                return expression;
            }

            // search for the end of the expression with the matching paren
            var openParenIndex = 0;
            var closeParenIndex = 1;
            while (openParenIndex != -1 && openParenIndex < closeParenIndex)
            {
                closeParenIndex = expression.IndexOf(')', closeParenIndex);
                if (closeParenIndex == -1)
                {
                    throw new WixException(ErrorMessages.UnmatchedParenthesisInExpression(this.Context.CurrentSourceLineNumber, originalExpression));
                }

                if (InsideQuotes(expression, closeParenIndex))
                {
                    // ignore stuff inside quotes (it's a string literal)
                }
                else
                {
                    // Look to see if there is another open paren before the close paren
                    // and skip over the open parens while they are in a string literal
                    do
                    {
                        openParenIndex++;
                        openParenIndex = expression.IndexOf('(', openParenIndex, closeParenIndex - openParenIndex);
                    }
                    while (InsideQuotes(expression, openParenIndex));
                }

                // Advance past the closing paren
                closeParenIndex++;
            }

            endSubExpression = closeParenIndex;

            // Return the expression minus the parenthesis
            return expression.Substring(1, closeParenIndex - 2);
        }

        /// <summary>
        /// Updates expression based on operation.
        /// </summary>
        /// <param name="currentValue">State to update.</param>
        /// <param name="operation">Operation to apply to current value.</param>
        /// <param name="prevResult">Previous result.</param>
        private void UpdateExpressionValue(ref bool currentValue, PreprocessorOperation operation, bool prevResult)
        {
            switch (operation)
            {
            case PreprocessorOperation.And:
                currentValue = currentValue && prevResult;
                break;
            case PreprocessorOperation.Or:
                currentValue = currentValue || prevResult;
                break;
            case PreprocessorOperation.Not:
                currentValue = !currentValue;
                break;
            default:
                throw new WixException(ErrorMessages.UnexpectedPreprocessorOperator(this.Context.CurrentSourceLineNumber, operation.ToString()));
            }
        }

        /// <summary>
        /// Evaluate an expression.
        /// </summary>
        /// <param name="expression">Expression to evaluate.</param>
        /// <returns>Boolean result of expression.</returns>
        private bool EvaluateExpression(string expression)
        {
            var tmpExpression = expression;
            return this.EvaluateExpressionRecurse(expression, ref tmpExpression, PreprocessorOperation.And, true);
        }

        /// <summary>
        /// Recurse through the expression to evaluate if it is true or false.
        /// The expression is evaluated left to right. 
        /// The expression is case-sensitive (converted to upper case) with the
        /// following exceptions: variable names and keywords (and, not, or).
        /// Comparisons with = and != are string comparisons.  
        /// Comparisons with inequality operators must be done on valid integers.
        /// 
        /// The operator precedence is:
        ///    ""
        ///    ()
        ///    &lt;, &gt;, &lt;=, &gt;=, =, !=
        ///    Not
        ///    And, Or
        ///    
        /// Valid expressions include:
        ///   not $(var.B) or not $(var.C)
        ///   (($(var.A))and $(var.B) ="2")or Not((($(var.C))) and $(var.A))
        ///   (($(var.A)) and $(var.B) = " 3 ") or $(var.C)
        ///   $(var.A) and $(var.C) = "3" or $(var.C) and $(var.D) = $(env.windir)
        ///   $(var.A) and $(var.B)>2 or $(var.B) &lt;= 2
        ///   $(var.A) != "2" 
        /// </summary>
        /// <param name="originalExpression">The original expression</param>
        /// <param name="expression">The expression currently being evaluated</param>
        /// <param name="prevResultOperation">The operation to apply to this result</param>
        /// <param name="prevResult">The previous result to apply to this result</param>
        /// <returns>Boolean to indicate if the expression is true or false</returns>
        private bool EvaluateExpressionRecurse(string originalExpression, ref string expression, PreprocessorOperation prevResultOperation, bool prevResult)
        {
            var expressionValue = false;
            expression = expression.Trim();
            if (expression.Length == 0)
            {
                throw new WixException(ErrorMessages.UnexpectedEmptySubexpression(this.Context.CurrentSourceLineNumber, originalExpression));
            }

            // If the expression starts with parenthesis, evaluate it
            if (expression.IndexOf('(') == 0)
            {
                var subExpression = this.GetParenthesisExpression(originalExpression, expression, out var endSubExpressionIndex);
                expressionValue = this.EvaluateExpressionRecurse(originalExpression, ref subExpression, PreprocessorOperation.And, true);

                // Now get the rest of the expression that hasn't been evaluated
                expression = expression.Substring(endSubExpressionIndex).Trim();
            }
            else
            {
                // Check for NOT
                if (StartsWithKeyword(expression, PreprocessorOperation.Not))
                {
                    expression = expression.Substring(3).Trim();
                    if (expression.Length == 0)
                    {
                        throw new WixException(ErrorMessages.ExpectedExpressionAfterNot(this.Context.CurrentSourceLineNumber, originalExpression));
                    }

                    expressionValue = this.EvaluateExpressionRecurse(originalExpression, ref expression, PreprocessorOperation.Not, true);
                }
                else // Expect a literal
                {
                    expressionValue = this.EvaluateAtomicExpression(originalExpression, ref expression);

                    // Expect the literal that was just evaluated to already be cut off
                }
            }
            this.UpdateExpressionValue(ref expressionValue, prevResultOperation, prevResult);

            // If there's still an expression left, it must start with AND or OR.
            if (expression.Trim().Length > 0)
            {
                if (StartsWithKeyword(expression, PreprocessorOperation.And))
                {
                    expression = expression.Substring(3);
                    return this.EvaluateExpressionRecurse(originalExpression, ref expression, PreprocessorOperation.And, expressionValue);
                }
                else if (StartsWithKeyword(expression, PreprocessorOperation.Or))
                {
                    expression = expression.Substring(2);
                    return this.EvaluateExpressionRecurse(originalExpression, ref expression, PreprocessorOperation.Or, expressionValue);
                }
                else
                {
                    throw new WixException(ErrorMessages.InvalidSubExpression(this.Context.CurrentSourceLineNumber, expression, originalExpression));
                }
            }

            return expressionValue;
        }

        /// <summary>
        /// Update the current line number with the reader's current state.
        /// </summary>
        /// <param name="reader">The xml reader for the preprocessor.</param>
        /// <param name="offset">This is the artificial offset of the line numbers from the reader.  Used for the foreach processing.</param>
        private void UpdateCurrentLineNumber(XmlReader reader, int offset)
        {
            var lineInfoReader = reader as IXmlLineInfo;
            if (null != lineInfoReader)
            {
                var newLine = lineInfoReader.LineNumber + offset;

                if (this.Context.CurrentSourceLineNumber.LineNumber != newLine)
                {
                    this.Context.CurrentSourceLineNumber = new SourceLineNumber(this.Context.CurrentSourceLineNumber.FileName, newLine);
                }
            }
        }

        /// <summary>
        /// Pushes a file name on the stack of included files.
        /// </summary>
        /// <param name="fileName">Name to push on to the stack of included files.</param>
        private void PushInclude(string fileName)
        {
            if (1023 < this.CurrentFileStack.Count)
            {
                throw new WixException(ErrorMessages.TooDeeplyIncluded(this.Context.CurrentSourceLineNumber, this.CurrentFileStack.Count));
            }

            this.CurrentFileStack.Push(fileName);
            this.SourceStack.Push(this.Context.CurrentSourceLineNumber);
            this.Context.CurrentSourceLineNumber = new SourceLineNumber(fileName);
            this.IncludeNextStack.Push(true);
        }

        /// <summary>
        /// Pops a file name from the stack of included files.
        /// </summary>
        private void PopInclude()
        {
            this.Context.CurrentSourceLineNumber = this.SourceStack.Pop();

            this.CurrentFileStack.Pop();
            this.IncludeNextStack.Pop();
        }

        /// <summary>
        /// Go through search paths, looking for a matching include file.
        /// Start the search in the directory of the source file, then go
        /// through the search paths in the order given on the command line
        /// (leftmost first, ...).
        /// </summary>
        /// <param name="includePath">User-specified path to the included file (usually just the file name).</param>
        /// <returns>Returns a FileInfo for the found include file, or null if the file cannot be found.</returns>
        private string GetIncludeFile(string includePath)
        {
            string finalIncludePath = null;

            includePath = includePath.Trim();

            // remove quotes (only if they match)
            if ((includePath.StartsWith("\"", StringComparison.Ordinal) && includePath.EndsWith("\"", StringComparison.Ordinal)) ||
                (includePath.StartsWith("'", StringComparison.Ordinal) && includePath.EndsWith("'", StringComparison.Ordinal)))
            {
                includePath = includePath.Substring(1, includePath.Length - 2);
            }

            // check if the include file is a full path
            if (Path.IsPathRooted(includePath))
            {
                if (File.Exists(includePath))
                {
                    finalIncludePath = includePath;
                }
            }
            else // relative path
            {
                // build a string to test the directory containing the source file first
                var currentFolder = this.CurrentFileStack.Peek();
                var includeTestPath = Path.Combine(Path.GetDirectoryName(currentFolder), includePath);

                // test the source file directory
                if (File.Exists(includeTestPath))
                {
                    finalIncludePath = includeTestPath;
                }
                else // test all search paths in the order specified on the command line
                {
                    foreach (var includeSearchPath in this.Context.IncludeSearchPaths)
                    {
                        // if the path exists, we have found the final string
                        includeTestPath = Path.Combine(includeSearchPath, includePath);
                        if (File.Exists(includeTestPath))
                        {
                            finalIncludePath = includeTestPath;
                            break;
                        }
                    }
                }
            }

            return finalIncludePath;
        }

        private void PreProcess()
        {
            foreach (var extension in this.Context.Extensions)
            {
                if (extension.Prefixes != null)
                {
                    foreach (var prefix in extension.Prefixes)
                    {
                        if (!this.ExtensionsByPrefix.TryGetValue(prefix, out var collidingExtension))
                        {
                            this.ExtensionsByPrefix.Add(prefix, extension);
                        }
                        else
                        {
                            this.Messaging.Write(ErrorMessages.DuplicateExtensionPreprocessorType(extension.GetType().ToString(), prefix, collidingExtension.GetType().ToString()));
                        }
                    }
                }

                extension.PrePreprocess(this.Context);
            }
        }

        private XDocument PostProcess(XDocument document)
        {
            foreach (var extension in this.Context.Extensions)
            {
                extension.PostPreprocess(document);
            }

            return document;
        }
    }
}
