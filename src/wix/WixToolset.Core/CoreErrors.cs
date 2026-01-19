// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core
{
    using System;
    using System.Collections.Generic;
    using WixToolset.Data;

    internal static class CoreErrors
    {
        public static Message UnableToCopyFile(SourceLineNumber sourceLineNumbers, string source, string destination, string detail)
        {
            return Message(sourceLineNumbers, Ids.UnableToCopyFile, "Unable to copy file from: {0}, to: {1}. Error detail: {2}", source, destination, detail);
        }

        public static Message UnableToDeleteFile(SourceLineNumber sourceLineNumbers, string path, string detail)
        {
            return Message(sourceLineNumbers, Ids.UnableToDeleteFile, "Unable to delete file: {0}. Error detail: {1}", path, detail);
        }

        public static Message UnableToMoveFile(SourceLineNumber sourceLineNumbers, string source, string destination, string detail)
        {
            return Message(sourceLineNumbers, Ids.UnableToMoveFile, "Unable to move file from: {0}, to: {1}. Error detail: {2}", source, destination, detail);
        }

        public static Message UnableToOpenFile(SourceLineNumber sourceLineNumbers, string path, string detail)
        {
            return Message(sourceLineNumbers, Ids.UnableToOpenFile, "Unable to open file: {0}. Error detail: {1}", path, detail);
        }

        public static Message BackendNotFound(string outputType, string outputPath)
        {
            return Message(null, Ids.BackendNotFound, "Unable to find a backend to process output type: {0} for output file: {1}. Specify a different output type or output file extension.", outputType, outputPath);
        }

        public static Message AdditionalArgumentUnexpected(string argument)
        {
            return Message(null, Ids.AdditionalArgumentUnexpected, "Additional argument '{0}' was unexpected. Remove the argument and add the '-?' switch for more information.", argument);
        }

        public static Message IntermediatesMustBeCompiled(string invalidIntermediates)
        {
            return Message(null, Ids.IntermediatesMustBeCompiled, "Intermediates being linked must have been compiled. Intermediates with these ids were not compiled: {0}", invalidIntermediates);
        }

        public static Message WixiplSourceFileIsExclusive()
        {
            return Message(null, Ids.WixiplSourceFileIsExclusive, "When an intermediate post link source file is specified, it must be the only source file provided.");
        }

        public static Message CannotReundefineVariable(SourceLineNumber sourceLineNumbers, string variableName)
        {
            return Message(sourceLineNumbers, Ids.CannotReundefineVariable, "The variable '{0}' cannot be undefined because its already undefined.", variableName);
        }

        public static Message ComponentExpectedFeature(SourceLineNumber sourceLineNumbers, string component, string type, string target)
        {
            return Message(sourceLineNumbers, Ids.ComponentExpectedFeature, "The component '{0}' is not assigned to a feature. The component's {1} '{2}' requires it to be assigned to at least one feature.", component, type, target);
        }

        public static Message ComponentReferencedTwice(SourceLineNumber sourceLineNumbers, string crefChildId)
        {
            return Message(sourceLineNumbers, Ids.ComponentReferencedTwice, "Component {0} cannot be contained in a Module twice.", crefChildId);
        }

        public static Message DisallowedMsiProperty(SourceLineNumber sourceLineNumbers, string property, string illegalValueList)
        {
            return Message(sourceLineNumbers, Ids.DisallowedMsiProperty, "The '{0}' MsiProperty is controlled by the bootstrapper and cannot be authored. (Illegal properties are: {1}.) Remove the MsiProperty element.", property, illegalValueList);
        }

        public static Message DuplicatedUiLocalization(SourceLineNumber sourceLineNumbers, string controlName, string dialogName)
        {
            return Message(sourceLineNumbers, Ids.DuplicatedUiLocalization, "The localization for control {0} in dialog {1} is duplicated. Only one localization per control is allowed.", controlName, dialogName);
        }

        public static Message DuplicatedUiLocalization(SourceLineNumber sourceLineNumbers, string dialogName)
        {
            return Message(sourceLineNumbers, Ids.DuplicatedUiLocalization, "The localization for dialog {0} is duplicated. Only one localization per dialog is allowed.", dialogName);
        }

        public static Message DuplicateExtensionPreprocessorType(string extension, string variablePrefix, string collidingExtension)
        {
            return Message(null, Ids.DuplicateExtensionPreprocessorType, "The extension '{0}' uses the same preprocessor variable prefix, '{1}', as previously loaded extension '{2}'. Please remove one of the extensions or rename the prefix to avoid the collision.", extension, variablePrefix, collidingExtension);
        }

        public static Message DuplicateLocalizationIdentifier(SourceLineNumber sourceLineNumbers, string localizationId)
        {
            return Message(sourceLineNumbers, Ids.DuplicateLocalizationIdentifier, "The localization identifier '{0}' has been duplicated in multiple locations. A common cause is a bundle .wixproj that automatically loads .wxl files that are intended for the bootstrapper application. You can turn off that behavior by setting the EnableDefaultEmbeddedResourceItems property to false.", localizationId);
        }

        public static Message DuplicateVariableDefinition(string variableName, string variableValue, string variableCollidingValue)
        {
            return Message(null, Ids.DuplicateVariableDefinition, "The variable '{0}' with value '{1}' was previously declared with value '{2}'.", variableName, variableValue, variableCollidingValue);
        }

        public static Message ExampleGuid(SourceLineNumber sourceLineNumbers, string elementName, string attributeName, string value)
        {
            return Message(sourceLineNumbers, Ids.ExampleGuid, "The {0}/@{1} attribute's value, '{2}', is not a legal Guid value. A Guid needs to be generated and put in place of '{2}' in the source file.", elementName, attributeName, value);
        }

        public static Message ExpectedArgument(string argument)
        {
            return Message(null, Ids.ExpectedArgument, "{0} is expected to be followed by a value. See -? for additional detail.", argument);
        }

        public static Message ExpectedDirectory(string directory)
        {
            return Message(null, Ids.ExpectedDirectory, "The directory '{0}' could not be found.", directory);
        }

        public static Message ExpectedDirectoryGotFile(string option, string path)
        {
            return Message(null, Ids.ExpectedDirectoryGotFile, "The {0} option requires a directory, but the provided path is a file: {1}", option, path);
        }

        public static Message ExpectedEndforeach(SourceLineNumber sourceLineNumbers)
        {
            return Message(sourceLineNumbers, Ids.ExpectedEndforeach, "A <?foreach?> statement was found that had no matching <?endforeach?>.");
        }

        public static Message ExpectedExpressionAfterNot(SourceLineNumber sourceLineNumbers, string expression)
        {
            return Message(sourceLineNumbers, Ids.ExpectedExpressionAfterNot, "Expecting an argument for 'NOT' in expression '{0}'.", expression);
        }

        public static Message ExpectedFileGotDirectory(string option, string path)
        {
            return Message(null, Ids.ExpectedFileGotDirectory, "The {0} option requires a file, but the provided path is a directory: {1}", option, path);
        }

        public static Message ExpectedVariable(SourceLineNumber sourceLineNumbers, string expression)
        {
            return Message(sourceLineNumbers, Ids.ExpectedVariable, "A required variable was missing in the expression '{0}'.", expression);
        }

        public static Message FileInUse(SourceLineNumber sourceLineNumbers, string file)
        {
            return Message(sourceLineNumbers, Ids.FileInUse, "The process can not access the file '{0}' because it is being used by another process.", file);
        }

        public static Message IllegalAttributeValueWithIllegalList(SourceLineNumber sourceLineNumbers, string elementName, string attributeName, string value, string illegalValueList)
        {
            return Message(sourceLineNumbers, Ids.IllegalAttributeValueWithIllegalList, "The {0}/@{1} attribute's value, '{2}', is one of the illegal options: {3}.", elementName, attributeName, value, illegalValueList);
        }

        public static Message IllegalDefineStatement(SourceLineNumber sourceLineNumbers, string defineStatement)
        {
            return Message(sourceLineNumbers, Ids.IllegalDefineStatement, "The define statement '<?define {0}?>' is not well-formed. Define statements should be in the form <?define variableName = \"variable value\"?>.", defineStatement);
        }

        public static Message IllegalForeach(SourceLineNumber sourceLineNumbers, string foreachStatement)
        {
            return Message(sourceLineNumbers, Ids.IllegalForeach, "The foreach statement '{0}' is illegal. The proper format for foreach is <?foreach varName in valueList?>.", foreachStatement);
        }

        public static Message IllegalIdentifierLooksLikeFormatted(SourceLineNumber sourceLineNumbers, string elementName, string attributeName, string value)
        {
            return Message(sourceLineNumbers, Ids.IllegalIdentifierLooksLikeFormatted, "The {0}/@{1} attribute's value, '{2}', is not a legal identifier. The {0}/@{1} attribute does not support formatted string values, such as property names enclosed in brackets ([LIKETHIS]). The value must be the identifier of another element, such as the Directory/@Id attribute value.", elementName, attributeName, value);
        }

        public static Message IllegalInlineLocVariable(SourceLineNumber sourceLineNumbers, string variableName, string variableValue)
        {
            return Message(sourceLineNumbers, Ids.IllegalInlineLocVariable, "The localization variable '{0}' specifies an illegal inline default value of '{1}'. Localization variables cannot specify default values inline, instead the value should be specified in a WiX localization (.wxl) file.", variableName, variableValue);
        }

        public static Message IllegalIntegerInExpression(SourceLineNumber sourceLineNumbers, string expression)
        {
            return Message(sourceLineNumbers, Ids.IllegalIntegerInExpression, "An illegal number was found in the expression '{0}'.", expression);
        }

        public static Message IllegalLongValue(SourceLineNumber sourceLineNumbers, string elementName, string attributeName, string value)
        {
            return Message(sourceLineNumbers, Ids.IllegalLongValue, "The {0}/@{1} attribute's value, '{2}', is not a legal long value. Legal long values are from -9,223,372,036,854,775,808 to 9,223,372,036,854,775,807.", elementName, attributeName, value);
        }

        public static Message IllegalSuppressWarningId(string suppressedId)
        {
            return Message(null, Ids.IllegalSuppressWarningId, "Illegal value '{0}' for the -sw<N> command line option. Specify a particular warning number, like '-sw6' to suppress the warning with ID 6, or '-sw' alone to suppress all warnings.", suppressedId);
        }

        public static Message IllegalWarningIdAsError(string warningId)
        {
            return Message(null, Ids.IllegalWarningIdAsError, "Illegal value '{0}' for the -wx<N> command line option. Specify a particular warning number, like '-wx6' to display the warning with ID 6 as an error, or '-wx' alone to suppress all warnings.", warningId);
        }

        public static Message IllegalYesNoDefaultValue(SourceLineNumber sourceLineNumbers, string elementName, string attributeName, string value)
        {
            return Message(sourceLineNumbers, Ids.IllegalYesNoDefaultValue, "The {0}/@{1} attribute's value, '{2}', is not a legal yes/no/default value. The only legal values are 'default', 'no' or 'yes'.", elementName, attributeName, value);
        }

        public static Message InvalidCommandLineFileName(string fileName, string error)
        {
            return Message(null, Ids.InvalidCommandLineFileName, "Invalid file name specified on the command line: '{0}'. Error message: '{1}'", fileName, error);
        }

        public static Message InvalidBundleCondition(SourceLineNumber sourceLineNumbers, string elementName, string attributeName, string condition)
        {
            return Message(sourceLineNumbers, Ids.InvalidBundleCondition, "The {0}/@{1} attribute's value '{2}' is not a valid bundle condition.", elementName, attributeName, condition);
        }

        public static Message CouldNotFindExtensionInPaths(string extensionPath, IEnumerable<string> checkedPaths)
        {
            return Message(null, Ids.InvalidExtension, "The extension '{0}' could not be found. Checked paths: {1}", extensionPath, String.Join(", ", checkedPaths));
        }

        public static Message InvalidExtension(string extension)
        {
            return Message(null, Ids.InvalidExtension, "The extension '{0}' could not be loaded.", extension);
        }

        public static Message InvalidExtension(string extension, string invalidReason)
        {
            return Message(null, Ids.InvalidExtension, "The extension '{0}' could not be loaded because of the following reason: {1}", extension, invalidReason);
        }

        public static Message InvalidExtension(string extension, string extensionType, string expectedType)
        {
            return Message(null, Ids.InvalidExtension, "The extension '{0}' is the wrong type: '{1}'. The expected type was '{2}'.", extension, extensionType, expectedType);
        }

        public static Message InvalidExtension(string extension, string extensionType, string expectedType1, string expectedType2)
        {
            return Message(null, Ids.InvalidExtension, "The extension '{0}' is the wrong type: '{1}'. The expected type was '{2}' or '{3}'.", extension, extensionType, expectedType1, expectedType2);
        }

        public static Message InvalidPreprocessorFunction(SourceLineNumber sourceLineNumbers, string variable)
        {
            return Message(sourceLineNumbers, Ids.InvalidPreprocessorFunction, "Ill-formed preprocessor function '${0}'. Functions must have a prefix (like 'fun.'), a name at least 1 character long, and matching opening and closing parentheses.", variable);
        }

        public static Message InvalidPreprocessorFunctionAutoVersion(SourceLineNumber sourceLineNumbers)
        {
            return Message(sourceLineNumbers, Ids.InvalidPreprocessorFunctionAutoVersion, "Invalid AutoVersion template specified.");
        }

        public static Message InvalidPreprocessorPragma(SourceLineNumber sourceLineNumbers, string variable)
        {
            return Message(sourceLineNumbers, Ids.InvalidPreprocessorPragma, "Malformed preprocessor pragma '{0}'. Pragmas must have a prefix, a name of at least 1 character long, and be followed by optional arguments.", variable);
        }

        public static Message InvalidPreprocessorVariable(SourceLineNumber sourceLineNumbers, string variable)
        {
            return Message(sourceLineNumbers, Ids.InvalidPreprocessorVariable, "Ill-formed preprocessor variable '$({0})'. Variables must have a prefix (like 'var.', 'env.', or 'sys.') and a name at least 1 character long. If the literal string '$({0})' is desired, use '$$({0})'.", variable);
        }

        public static Message InvalidSubExpression(SourceLineNumber sourceLineNumbers, string subExpression, string expression)
        {
            return Message(sourceLineNumbers, Ids.InvalidSubExpression, "Found invalid subexpression '{0}' in expression '{1}'.", subExpression, expression);
        }

        public static Message InvalidSummaryInfoCodePage(SourceLineNumber sourceLineNumbers, int codePage)
        {
            return Message(sourceLineNumbers, Ids.InvalidSummaryInfoCodePage, "The code page '{0}' is invalid for summary information. You must specify an ANSI code page.", codePage);
        }

        public static Message LocalizationVariableUnknown(SourceLineNumber sourceLineNumbers, string variableId)
        {
            return Message(sourceLineNumbers, Ids.LocalizationVariableUnknown, "The localization variable !(loc.{0}) is unknown. Please ensure the variable is defined.", variableId);
        }

        public static Message MergeModuleExpectedFeature(SourceLineNumber sourceLineNumbers, string mergeId)
        {
            return Message(sourceLineNumbers, Ids.MergeModuleExpectedFeature, "The merge module '{0}' is not assigned to a feature. All merge modules must be assigned to at least one feature.", mergeId);
        }

        public static Message MissingEntrySection()
        {
            return Message(null, Ids.MissingEntrySection, "Could not find entry section in provided list of intermediates. Supported entry section types are: Package, Bundle, Patch, Module.");
        }

        public static Message MissingEntrySection(string sectionType)
        {
            return Message(null, Ids.MissingEntrySection, "Could not find entry section in provided list of intermediates. Expected section of type '{0}'.", sectionType);
        }

        public static Message MultipleEntrySections(SourceLineNumber sourceLineNumbers, string sectionName1, string sectionName2)
        {
            return Message(sourceLineNumbers, Ids.MultipleEntrySections, "Multiple entry sections '{0}' and '{1}' found. Only one entry section may be present in a single target.", sectionName1, sectionName2);
        }

        public static Message MultipleEntrySections2(SourceLineNumber sourceLineNumbers)
        {
            return Message(sourceLineNumbers, Ids.MultipleEntrySections2, "Location of entry section related to previous error.");
        }

        public static Message MultiplePrimaryReferences(SourceLineNumber sourceLineNumbers, string crefChildType, string crefChildId, string crefParentType, string crefParentId, string conflictParentType, string conflictParentId)
        {
            return Message(sourceLineNumbers, Ids.MultiplePrimaryReferences, "Multiple primary references were found for {0} '{1}' in {2} '{3}' and {4} '{5}'.", crefChildType, crefChildId, crefParentType, crefParentId, conflictParentType, conflictParentId);
        }

        public static Message MustSpecifyOutputWithMoreThanOneInput()
        {
            return Message(null, Ids.MustSpecifyOutputWithMoreThanOneInput, "You must specify an output file using the \"-o\" or \"-out\" switch when you provide more than one input file.");
        }

        public static Message NonterminatedPreprocessorInstruction(SourceLineNumber sourceLineNumbers, string beginInstruction, string endInstruction)
        {
            return Message(sourceLineNumbers, Ids.NonterminatedPreprocessorInstruction, "Found a <?{0}?> processing instruction without a matching <?{1}?> after it.", beginInstruction, endInstruction);
        }

        public static Message OrderingReferenceLoopDetected(SourceLineNumber sourceLineNumbers, string loopList)
        {
            return Message(sourceLineNumbers, Ids.OrderingReferenceLoopDetected, "A circular reference of ordering dependencies was detected. The infinite loop includes: {0}. Ordering dependency references must form a directed acyclic graph.", loopList);
        }

        public static Message OrphanedComponent(SourceLineNumber sourceLineNumbers, string componentName)
        {
            return Message(sourceLineNumbers, Ids.OrphanedComponent, "Found orphaned Component '{0}'. If this is a Package, every Component must have at least one parent Feature. To include a Component in a Module, you must include it directly as a Component element of the Module element or indirectly via ComponentRef, ComponentGroup, or ComponentGroupRef elements.", componentName);
        }

        public static Message PathCannotContainQuote(string fileName)
        {
            return Message(null, Ids.PathCannotContainQuote, "Path '{0}' contains a literal quote character. Quotes are often accidentally introduced when trying to refer to a directory path with spaces in it, such as \"C:\\Out Directory\\\" -- the backslash before the quote acts an escape character. The correct representation for that path is: \"C:\\Out Directory\\\\\".", fileName);
        }

        public static Message PathTooLong(SourceLineNumber sourceLineNumbers, string fileName)
        {
            return Message(sourceLineNumbers, Ids.PathTooLong, "'{0}' is too long, the fully qualified file name must be less than 260 characters, and the directory name must be less than 248 characters.", fileName);
        }

        public static Message PreprocessorError(SourceLineNumber sourceLineNumbers, string message)
        {
            return Message(sourceLineNumbers, Ids.PreprocessorError, "{0}", message);
        }

        public static Message PreprocessorExtensionEvaluateFunctionFailed(SourceLineNumber sourceLineNumbers, string prefix, string function, string args, string message)
        {
            return Message(sourceLineNumbers, Ids.PreprocessorExtensionEvaluateFunctionFailed, "In the preprocessor extension that handles prefix '{0}' while trying to call function '{1}({2})' and exception has occurred : {3}", prefix, function, args, message);
        }

        public static Message PreprocessorExtensionGetVariableValueFailed(SourceLineNumber sourceLineNumbers, string prefix, string variable, string message)
        {
            return Message(sourceLineNumbers, Ids.PreprocessorExtensionGetVariableValueFailed, "In the preprocessor extension that handles prefix '{0}' while trying to get the value for variable '{1}' and exception has occured : {2}", prefix, variable, message);
        }

        public static Message PreprocessorExtensionPragmaFailed(SourceLineNumber sourceLineNumbers, string pragma, string message)
        {
            return Message(sourceLineNumbers, Ids.PreprocessorExtensionPragmaFailed, "Exception thrown while processing pragma '{0}'. The exception's message is: {1}", pragma, message);
        }

        public static Message ReferenceLoopDetected(SourceLineNumber sourceLineNumbers, string loopList)
        {
            return Message(sourceLineNumbers, Ids.ReferenceLoopDetected, "A circular reference of groups was detected. The infinite loop includes: {0}. Group references must form a directed acyclic graph.", loopList);
        }

        public static Message TooDeeplyIncluded(SourceLineNumber sourceLineNumbers, int depth)
        {
            return Message(sourceLineNumbers, Ids.TooDeeplyIncluded, "Include files cannot be nested more deeply than {0} times. Make sure included files don't accidentally include themselves.", depth);
        }

        public static Message UnauthorizedAccess(string filePath)
        {
            return Message(null, Ids.UnauthorizedAccess, "Access to the path '{0}' is denied.", filePath);
        }

        public static Message UndefinedPreprocessorFunction(SourceLineNumber sourceLineNumbers, string variableName)
        {
            return Message(sourceLineNumbers, Ids.UndefinedPreprocessorFunction, "Undefined preprocessor function '$({0})'.", variableName);
        }

        public static Message UndefinedPreprocessorVariable(SourceLineNumber sourceLineNumbers, string variableName)
        {
            return Message(sourceLineNumbers, Ids.UndefinedPreprocessorVariable, "Undefined preprocessor variable '$({0})'.", variableName);
        }

        public static Message UnexpectedEmptySubexpression(SourceLineNumber sourceLineNumbers, string expression)
        {
            return Message(sourceLineNumbers, Ids.UnexpectedEmptySubexpression, "The empty subexpression is unexpected in the expression '{0}'.", expression);
        }

        public static Message UnexpectedLiteral(SourceLineNumber sourceLineNumbers, string expression)
        {
            return Message(sourceLineNumbers, Ids.UnexpectedLiteral, "An unexpected literal was found in the expression '{0}'.", expression);
        }

        public static Message UnexpectedPreprocessorOperator(SourceLineNumber sourceLineNumbers, string op)
        {
            return Message(sourceLineNumbers, Ids.UnexpectedPreprocessorOperator, "The operator '{0}' is unexpected.", op);
        }

        public static Message UnhandledExtensionAttribute(SourceLineNumber sourceLineNumbers, string elementName, string extensionAttributeName, string extensionNamespace)
        {
            return Message(sourceLineNumbers, Ids.UnhandledExtensionAttribute, "The {0} element contains an unhandled extension attribute '{1}'. Please ensure that the extension for attributes in the '{2}' namespace has been provided.", elementName, extensionAttributeName, extensionNamespace);
        }

        public static Message UnhandledExtensionElement(SourceLineNumber sourceLineNumbers, string elementName, string extensionElementName, string extensionNamespace)
        {
            return Message(sourceLineNumbers, Ids.UnhandledExtensionElement, "The {0} element contains an unhandled extension element '{1}'. Please ensure that the extension for elements in the '{2}' namespace has been provided.", elementName, extensionElementName, extensionNamespace);
        }

        public static Message UnmatchedParenthesisInExpression(SourceLineNumber sourceLineNumbers, string expression)
        {
            return Message(sourceLineNumbers, Ids.UnmatchedParenthesisInExpression, "The parenthesis don't match in the expression '{0}'.", expression);
        }

        public static Message UnmatchedPreprocessorInstruction(SourceLineNumber sourceLineNumbers, string beginInstruction, string endInstruction)
        {
            return Message(sourceLineNumbers, Ids.UnmatchedPreprocessorInstruction, "Found a <?{1}?> processing instruction without a matching <?{0}?> before it.", beginInstruction, endInstruction);
        }

        public static Message UnmatchedQuotesInExpression(SourceLineNumber sourceLineNumbers, string expression)
        {
            return Message(sourceLineNumbers, Ids.UnmatchedQuotesInExpression, "The quotes don't match in the expression '{0}'.", expression);
        }

        public static Message UnresolvedBindReference(SourceLineNumber sourceLineNumbers, string BindRef)
        {
            return Message(sourceLineNumbers, Ids.UnresolvedBindReference, "Unresolved bind-time variable {0}.", BindRef);
        }

        public static Message UnresolvedReference(SourceLineNumber sourceLineNumbers, string symbolName)
        {
            return Message(sourceLineNumbers, Ids.UnresolvedReference, "The identifier '{0}' could not be found. Ensure you have typed the reference correctly and that all the necessary inputs are provided to the linker.", symbolName);
        }

        public static Message UnresolvedReference(SourceLineNumber sourceLineNumbers, string symbolName, WixToolset.Data.AccessModifier accessModifier)
        {
            return Message(sourceLineNumbers, Ids.UnresolvedReference, "The identifier '{0}' is inaccessible due to its protection level.", symbolName, accessModifier);
        }

        public static Message UnsupportedExtensionAttribute(SourceLineNumber sourceLineNumbers, string elementName, string extensionElementName)
        {
            return Message(sourceLineNumbers, Ids.UnsupportedExtensionAttribute, "The {0} element contains an unsupported extension attribute '{1}'. The {0} element does not currently support extension attributes. Is the {1} attribute using the correct XML namespace?", elementName, extensionElementName);
        }

        public static Message UnsupportedExtensionElement(SourceLineNumber sourceLineNumbers, string elementName, string extensionElementName)
        {
            return Message(sourceLineNumbers, Ids.UnsupportedExtensionElement, "The {0} element contains an unsupported extension element '{1}'. The {0} element does not currently support extension elements. Is the {1} element using the correct XML namespace?", elementName, extensionElementName);
        }

        public static Message BindVariableCollision(SourceLineNumber sourceLineNumbers, string variableId)
        {
            return Message(sourceLineNumbers, Ids.BindVariableCollision, "The bind variable '{0}' is declared in more than one location. Please remove one of the declarations.", variableId);
        }

        public static Message BindVariableUnknown(SourceLineNumber sourceLineNumbers, string variableId)
        {
            return Message(sourceLineNumbers, Ids.BindVariableUnknown, "The bind variable !(wix.{0}) is unknown. Please ensure the variable is declared on the command line for wix.exe, via a WixVariable element, or inline using the syntax !(wix.{0}=some value which doesn't contain parentheses).", variableId);
        }

        public static Message NoSourceFiles()
        {
            return Message(null, Ids.NoSourceFiles, "No source files specified.");
        }

        public static Message IllegalInnerText(SourceLineNumber sourceLineNumbers, string elementName, string innerText)
        {
            return Message(sourceLineNumbers, Ids.IllegalInnerText, "The {0} element contains illegal inner text: '{1}'.", elementName, innerText);
        }

        public static Message IllegalInnerText(SourceLineNumber sourceLineNumbers, string elementName, string /*innerText*/_, string attributeName)
        {
            return Message(sourceLineNumbers, Ids.IllegalInnerText, "The {0} element contains inner text which is obsolete. Use the {1} attribute instead.", elementName, attributeName);
        }

        public static Message ActionScheduledRelativeToItself(SourceLineNumber sourceLineNumbers, string elementName, string attributeName, string attributeValue)
        {
            return Message(sourceLineNumbers, Ids.ActionScheduledRelativeToItself, "The {0}/@{1} attribute's value '{2}' is invalid because it would make this action dependent upon itself. Please change the value to the name of a different action.", elementName, attributeName, attributeValue);
        }

        public static Message AdvertiseStateMustMatch(SourceLineNumber sourceLineNumbers, string advertiseState, string parentAdvertiseState)
        {
            return Message(sourceLineNumbers, Ids.AdvertiseStateMustMatch, "The advertise state of this element: '{0}', does not match the advertise state set on the parent element: '{1}'.", advertiseState, parentAdvertiseState);
        }

        public static Message AppIdIncompatibleAdvertiseState(SourceLineNumber sourceLineNumbers, string elementName, string attributeName, string value, string parentValue)
        {
            return Message(sourceLineNumbers, Ids.AppIdIncompatibleAdvertiseState, "The {0}/@(1) attribute's value, '{2}' does not match the advertise state on its parent element: '{3}'. (Note: AppIds nested under Fragment, Module, or Package elements must be advertised.)", elementName, attributeName, value, parentValue);
        }

        public static Message CannotAuthorSpecialProperties(SourceLineNumber sourceLineNumbers, string propertyName)
        {
            return Message(sourceLineNumbers, Ids.CannotAuthorSpecialProperties, "The {0} property was specified. Special MSI properties cannot be authored. Use the attributes on the Property element instead.", propertyName);
        }

        public static Message CannotDefaultComponentId(SourceLineNumber sourceLineNumbers)
        {
            return Message(sourceLineNumbers, Ids.CannotDefaultComponentId, "The Component/@Id attribute was not found; it is required when there is no valid keypath to use as the default id value.");
        }

        public static Message CannotDefaultMismatchedAdvertiseStates(SourceLineNumber sourceLineNumbers)
        {
            return Message(sourceLineNumbers, Ids.CannotDefaultMismatchedAdvertiseStates, "MIME element cannot be marked as the default when its advertise state differs from its parent element. Ensure that the advertise state of the MIME element matches its parents element or remove the Mime/@Advertise attribute completely.");
        }

        public static Message CanNotHaveTwoParents(SourceLineNumber sourceLineNumbers, string directorySearch, string parentAttribute, string parentElement)
        {
            return Message(sourceLineNumbers, Ids.CanNotHaveTwoParents, "The DirectorySearchRef {0} can not have a Parent attribute {1} and also be nested under parent element {2}", directorySearch, parentAttribute, parentElement);
        }

        public static Message ComponentMultipleKeyPaths(SourceLineNumber sourceLineNumbers, string elementName, string attributeName, string value, string fileElementName, string registryElementName, string odbcDataSourceElementName)
        {
            return Message(sourceLineNumbers, Ids.ComponentMultipleKeyPaths, "The {0} element has multiple key paths set. The key path may only be set to '{2}' in extension elements that support it or one of the following locations: {0}/@{1}, {3}/@{1}, {4}/@{1}, or {5}/@{1}.", elementName, attributeName, value, fileElementName, registryElementName, odbcDataSourceElementName);
        }

        public static Message CustomActionMultipleSources(SourceLineNumber sourceLineNumbers, string elementName, string attributeName, string attributeName1, string attributeName2, string attributeName3, string attributeName4, string attributeName5)
        {
            return Message(sourceLineNumbers, Ids.CustomActionMultipleSources, "The {0}/@{1} attribute cannot coexist with a previously specified attribute on this element. The {0} element may only have one of the following source attributes specified at a time: {2}, {3}, {4}, {5}, or {6}.", elementName, attributeName, attributeName1, attributeName2, attributeName3, attributeName4, attributeName5);
        }

        public static Message CustomActionMultipleTargets(SourceLineNumber sourceLineNumbers, string elementName, string attributeName, string attributeName1, string attributeName2, string attributeName3, string attributeName4, string attributeName5, string attributeName6, string attributeName7)
        {
            return Message(sourceLineNumbers, Ids.CustomActionMultipleTargets, "The {0}/@{1} attribute cannot coexist with a previously specified attribute on this element. The {0} element may only have one of the following target attributes specified at a time: {2}, {3}, {4}, {5}, {6}, {7}, or {8}.", elementName, attributeName, attributeName1, attributeName2, attributeName3, attributeName4, attributeName5, attributeName6, attributeName7);
        }

        public static Message CustomTableIllegalColumnWidth(SourceLineNumber sourceLineNumbers, string elementName, string attributeName, int value)
        {
            return Message(sourceLineNumbers, Ids.CustomTableIllegalColumnWidth, "The {0}/@{1} attribute's value, '{2}', is not a valid column width. Valid column widths are 2 or 4.", elementName, attributeName, value);
        }

        public static Message CustomTableMissingPrimaryKey(SourceLineNumber sourceLineNumbers)
        {
            return Message(sourceLineNumbers, Ids.CustomTableMissingPrimaryKey, "The CustomTable is missing a Column element with the PrimaryKey attribute set to 'yes'. At least one column must be marked as the primary key.");
        }

        public static Message CustomTableNameTooLong(SourceLineNumber sourceLineNumbers, string elementName, string attributeName, string value)
        {
            return Message(sourceLineNumbers, Ids.CustomTableNameTooLong, "The {0}/@{1} attribute's value, '{2}', is too long for a table name. It cannot be more than than 31 characters long.", elementName, attributeName, value);
        }

        public static Message DuplicateContextValue(SourceLineNumber sourceLineNumbers, string contextValue)
        {
            return Message(sourceLineNumbers, Ids.DuplicateContextValue, "The context value '{0}' was duplicated. Context values must be distinct.", contextValue);
        }

        public static Message DuplicateExtensionXmlSchemaNamespace(string extension, string extensionXmlSchemaNamespace, string collidingExtension)
        {
            return Message(null, Ids.DuplicateExtensionXmlSchemaNamespace, "The extension '{0}' uses the same xml schema namespace, '{1}', as previously loaded extension '{2}'. Please either remove one of the extensions or rename the xml schema namespace to avoid the collision.", extension, extensionXmlSchemaNamespace, collidingExtension);
        }

        public static Message ExpectedBinaryCategory(SourceLineNumber sourceLineNumbers)
        {
            return Message(sourceLineNumbers, Ids.ExpectedBinaryCategory, "The Column element specifies a binary column but does not have the correct Category specified. Windows Installer requires binary columns to specify their category as binary. Please set the Category attribute's value to 'Binary'.");
        }

        public static Message ExpectedSignedCabinetName(SourceLineNumber sourceLineNumbers)
        {
            return Message(sourceLineNumbers, Ids.ExpectedSignedCabinetName, "The Media/@Cabinet attribute was not found; it is required when this element contains a DigitalSignature child element. This is because Windows Installer can only verify the digital signatures of external cabinets. Please either remove the DigitalSignature element or specify a valid external cabinet name via the Cabinet attribute.");
        }

        public static Message FeatureCannotFavorAndDisallowAdvertise(SourceLineNumber sourceLineNumbers, string elementName, string attributeName, string value, string otherAttributeName, string otherValue)
        {
            return Message(sourceLineNumbers, Ids.FeatureCannotFavorAndDisallowAdvertise, "The {0}/@{1} attribute's value, '{2}', cannot coexist with the {3} attribute's value of '{4}'. These options would ask the installer to disallow the advertised state for this feature while at the same time favoring it.", elementName, attributeName, value, otherAttributeName, otherValue);
        }

        public static Message FeatureConfigurableDirectoryNotUppercase(SourceLineNumber sourceLineNumbers, string elementName, string attributeName, string value)
        {
            return Message(sourceLineNumbers, Ids.FeatureConfigurableDirectoryNotUppercase, "The {0}/@{1} attribute's value, '{2}', contains lowercase characters. Since this directory is user-configurable, it needs to be a public property. This means the value must be completely uppercase.", elementName, attributeName, value);
        }

        public static Message FeatureNameTooLong(SourceLineNumber sourceLineNumbers, string elementName, string attributeName, string attributeValue)
        {
            return Message(sourceLineNumbers, Ids.FeatureNameTooLong, "The {0}/@{1} attribute with value '{2}', is too long for a feature name. Due to limitations in the Windows Installer, feature names cannot be longer than 38 characters in length.", elementName, attributeName, attributeValue);
        }

        public static Message IllegalAttributeInMergeModule(SourceLineNumber sourceLineNumbers, string elementName, string attributeName)
        {
            return Message(sourceLineNumbers, Ids.IllegalAttributeInMergeModule, "The {0}/@{1} attribute cannot be specified in a merge module.", elementName, attributeName);
        }

        public static Message IllegalAttributeWhenAdvertised(SourceLineNumber sourceLineNumbers, string elementName, string attributeName)
        {
            return Message(sourceLineNumbers, Ids.IllegalAttributeWhenAdvertised, "The {0}/@{1} attribute cannot be specified because the element is advertised.", elementName, attributeName);
        }

        public static Message IllegalCodepageAttribute(SourceLineNumber sourceLineNumbers, string codepage, string elementName, string attributeName)
        {
            return Message(sourceLineNumbers, Ids.IllegalCodepageAttribute, "The code page '{0}' is not a valid Windows code page. Please check the {1}/@{2} attribute value in your source file.", codepage, elementName, attributeName);
        }

        public static Message IllegalCompressionLevel(SourceLineNumber sourceLineNumbers, string compressionLevel)
        {
            return Message(sourceLineNumbers, Ids.IllegalCompressionLevel, "The compression level '{0}' is not valid. Valid values are 'none', 'low', 'medium', 'high', and 'mszip'.", compressionLevel);
        }

        public static Message IllegalModuleExclusionLanguageAttributes(SourceLineNumber sourceLineNumbers)
        {
            return Message(sourceLineNumbers, Ids.IllegalModuleExclusionLanguageAttributes, "Cannot set both ExcludeLanguage and ExcludeExceptLanguage attributes on a ModuleExclusion element.");
        }

        public static Message IllegalPropertyCustomActionAttributes(SourceLineNumber sourceLineNumbers)
        {
            return Message(sourceLineNumbers, Ids.IllegalPropertyCustomActionAttributes, "The CustomAction sets a property but its Execute attribute is not 'immediate' (the default). Property-setting custom actions cannot be deferred.\"");
        }

        public static Message IllegalSearchIdForParentDepth(SourceLineNumber sourceLineNumbers, string id, string parentId)
        {
            return Message(sourceLineNumbers, Ids.IllegalSearchIdForParentDepth, "When the parent DirectorySearch/@Depth attribute is greater than 1 for the DirectorySearch '{1}', the FileSearch/@Id attribute must be absent for FileSearch '{0}' unless the parent DirectorySearch/@AssignToProperty attribute value is 'yes'. Remove the FileSearch/@Id attribute for '{0}' to resolve this issue.", id, parentId);
        }

        public static Message IllegalShortFilename(SourceLineNumber sourceLineNumbers, string elementName, string attributeName, string value)
        {
            return Message(sourceLineNumbers, Ids.IllegalShortFilename, "The {0}/@{1} attribute's value, '{2}', is not a valid 8.3-compliant name. Legal names contain no more than 8 non-period characters followed by an optional period and extension of no more than 3 non-period characters. Any character except for the follow may be used: \\ ? | > < : / * \" + , ; = [ ] (space).", elementName, attributeName, value);
        }

        public static Message IllegalTargetDirDefaultDir(SourceLineNumber sourceLineNumbers, string defaultDir)
        {
            return Message(sourceLineNumbers, Ids.IllegalTargetDirDefaultDir, "The 'TARGETDIR' directory has an illegal DefaultDir value of '{0}'. The DefaultDir value is created from the *Name attributes of the Directory element. The TARGETDIR directory is a special directory which must have its Name attribute set to 'SourceDir'.", defaultDir);
        }

        public static Message IllegalTerminalServerCustomActionAttributes(SourceLineNumber sourceLineNumbers)
        {
            return Message(sourceLineNumbers, Ids.IllegalTerminalServerCustomActionAttributes, "The CustomAction/@TerminalServerAware attribute's value is 'yes' but the Execute attribute is not 'deferred,' 'rollback,' or 'commit.' Terminal-Server-aware custom actions must be deferred, rollback, or commit custom actions. For more information, see https://learn.microsoft.com/en-us/windows/win32/msi/terminalserver .\"");
        }

        public static Message ImplicitComponentKeyPath(SourceLineNumber sourceLineNumbers, string componentId)
        {
            return Message(sourceLineNumbers, Ids.ImplicitComponentKeyPath, "The component '{0}' does not have an explicit key path specified. If the ordering of the elements under the Component element changes, the key path will also change. To prevent accidental changes, the key path should be set to 'yes' in one of the following locations: Component/@KeyPath, File/@KeyPath, ODBCDataSource/@KeyPath, or Registry/@KeyPath.", componentId);
        }

        public static Message InsufficientVersion(SourceLineNumber sourceLineNumbers, Version currentVersion, Version requiredVersion)
        {
            return Message(sourceLineNumbers, Ids.InsufficientVersion, "The current version of the toolset is {0}, but version {1} is required.", currentVersion, requiredVersion);
        }

        public static Message InsufficientVersion(SourceLineNumber sourceLineNumbers, Version currentVersion, Version requiredVersion, string extension)
        {
            return Message(sourceLineNumbers, Ids.InsufficientVersion, "The current version of the extension '{2}' is {0}, but version {1} is required.", currentVersion, requiredVersion, extension);
        }

        public static Message InvalidCabinetTemplate(SourceLineNumber sourceLineNumbers, string cabinetTemplate)
        {
            return Message(sourceLineNumbers, Ids.InvalidCabinetTemplate, "CabinetTemplate attribute's value '{0}' must contain '{{0}}' and should contain no more than 8 characters followed by an optional extension of no more than 3 characters. Any character except for the follow may be used: \\ ? | > < : / * \" + , ; = [ ] (space). The Windows Installer team has recommended following the 8.3 format for external cabinet files and any other naming scheme is officially unsupported (which means it is not guaranteed to work on all platforms).", cabinetTemplate);
        }

        public static Message InvalidDateTimeFormat(SourceLineNumber sourceLineNumbers, string elementName, string attributeName, string value)
        {
            return Message(sourceLineNumbers, Ids.InvalidDateTimeFormat, "The {0}/@{1} attribute's value '{2}' is not a valid date/time value. A date/time value should follow the format YYYY-MM-DDTHH:mm:ss and be a valid date and time between 1980 and 2043, inclusive.", elementName, attributeName, value);
        }

        public static Message InvalidEmbeddedUIFileName(SourceLineNumber sourceLineNumbers, string codepage)
        {
            return Message(sourceLineNumbers, Ids.InvalidEmbeddedUIFileName, "The EmbeddedUI/@Name attribute value, '{0}', does not contain an extension. Windows Installer will not load an embedded UI DLL without an extension. Include an extension or just omit the Name attribute so it defaults to the file name portion of the Source attribute value.", codepage);
        }

        public static Message MediaEmbeddedCabinetNameTooLong(SourceLineNumber sourceLineNumbers, string elementName, string attributeName, string value, int length)
        {
            return Message(sourceLineNumbers, Ids.MediaEmbeddedCabinetNameTooLong, "The {0}/@{1} attribute's value, '{2}', is {3} characters long. The name is too long for an embedded cabinet. It cannot be more than than 62 characters long.", elementName, attributeName, value, length);
        }

        public static Message MissingTypeLibFile(SourceLineNumber sourceLineNumbers, string elementName, string fileElementName)
        {
            return Message(sourceLineNumbers, Ids.MissingTypeLibFile, "The {0} element is non-advertised and therefore requires a parent {1} element.", elementName, fileElementName);
        }

        public static Message MultipleIdentifiersFound(SourceLineNumber sourceLineNumbers, string elementName, string identifier, string mismatchIdentifier)
        {
            return Message(sourceLineNumbers, Ids.MultipleIdentifiersFound, "Under a '{0}' element, multiple identifiers were found: '{1}' and '{2}'. All search elements under this element must have the same id.", elementName, identifier, mismatchIdentifier);
        }

        public static Message NeedSequenceBeforeOrAfter(SourceLineNumber sourceLineNumbers, string elementName)
        {
            return Message(sourceLineNumbers, Ids.NeedSequenceBeforeOrAfter, "A {0} element must have a Before attribute, After attribute, or a Sequence attribute.", elementName);
        }

        public static Message NoFirstControlSpecified(SourceLineNumber sourceLineNumbers, string dialogName)
        {
            return Message(sourceLineNumbers, Ids.NoFirstControlSpecified, "The '{0}' dialog element does not have a valid tabbable control. You must either have a tabbable control that is not marked TabSkip='yes', or you must mark a control TabSkip='no'. If you have a page with no tabbable controls (a progress page, for example), you might want to set the first Text control to be TabSkip='no'.", dialogName);
        }

        public static Message ParentElementAttributeRequired(SourceLineNumber sourceLineNumbers, string parentElement, string parentAttribute, string childElement)
        {
            return Message(sourceLineNumbers, Ids.ParentElementAttributeRequired, "The parent {0} element is missing the {1} attribute that is required for the {2} child element.", parentElement, parentAttribute, childElement);
        }

        public static Message ProgIdNestedTooDeep(SourceLineNumber sourceLineNumbers)
        {
            return Message(sourceLineNumbers, Ids.ProgIdNestedTooDeep, "ProgId elements may not be nested more than 1 level deep.");
        }

        public static Message RadioButtonBitmapAndIconDisallowed(SourceLineNumber sourceLineNumbers)
        {
            return Message(sourceLineNumbers, Ids.RadioButtonBitmapAndIconDisallowed, "RadioButtonGroup elements that contain RadioButton elements with Bitmap or Icon attributes set to \"yes\" can only be specified under a Control element. Move your RadioButtonGroup element as a child of the appropriate Control element.");
        }

        public static Message RadioButtonTypeInconsistent(SourceLineNumber sourceLineNumbers)
        {
            return Message(sourceLineNumbers, Ids.RadioButtonTypeInconsistent, "All RadioButton elements in a RadioButtonGroup must be consistent with their use of the Bitmap, Icon, and Text attributes. Ensure all of the RadioButton elements in this group have the same attribute specified.");
        }

        public static Message RegistryMultipleValuesWithoutMultiString(SourceLineNumber sourceLineNumbers, string registryElementName, string valueAttributeName, string registryValueElementName, string typeAttributeName)
        {
            return Message(sourceLineNumbers, Ids.RegistryMultipleValuesWithoutMultiString, "The {0}/@{1} attribute and a {0}/{2} element cannot both be specified. Only one may be specified if the {3} attribute's value is not 'multiString'.", registryElementName, valueAttributeName, registryValueElementName, typeAttributeName);
        }

        public static Message RegistryNameValueIncorrect(SourceLineNumber sourceLineNumbers, string elementName, string attributeName, string value)
        {
            return Message(sourceLineNumbers, Ids.RegistryNameValueIncorrect, "The {0}/@{1} attribute's value, '{2}', is incorrect. It should not contain values of '+', '-', or '*' when the {0}/@Value attribute is empty. Instead, use the proper element and attributes: for Name='+' use RegistryKey/@Action='createKey', for Name='-' use RemoveRegistryKey/@Action='removeOnUninstall', for Name='*' use RegistryKey/@Action='createAndRemoveOnUninstall'.", elementName, attributeName, value);
        }

        public static Message RegistryRootInvalid(SourceLineNumber sourceLineNumbers)
        {
            return Message(sourceLineNumbers, Ids.RegistryRootInvalid, "Registry/@Root attribute is invalid on a nested Registry element. Either remove the Root attribute or move the Registry element so it is not nested under another Registry element.");
        }

        public static Message RelativePathForRegistryElement(SourceLineNumber sourceLineNumbers)
        {
            return Message(sourceLineNumbers, Ids.RelativePathForRegistryElement, "Cannot convert RelativePath into Registry elements.");
        }

        public static Message RootFeatureCannotFollowParent(SourceLineNumber sourceLineNumbers)
        {
            return Message(sourceLineNumbers, Ids.RootFeatureCannotFollowParent, "The Feature element specifies a root feature with an illegal InstallDefault value of 'followParent'. Root features cannot follow their parent feature's install state because they don't have a parent feature. Please remove or change the value of the InstallDefault attribute.");
        }

        public static Message SearchElementRequired(SourceLineNumber sourceLineNumbers, string elementName)
        {
            return Message(sourceLineNumbers, Ids.SearchElementRequired, "A '{0}' element must have a search element as a child.", elementName);
        }

        public static Message SearchElementRequiredWithAttribute(SourceLineNumber sourceLineNumbers, string elementName, string attributeName, string attributeValue)
        {
            return Message(sourceLineNumbers, Ids.SearchElementRequiredWithAttribute, "A {0} element must have a search element as a child when the {0}/@{1} attribute has the value '{2}'.", elementName, attributeName, attributeValue);
        }

        public static Message SearchPropertyNotUppercase(SourceLineNumber sourceLineNumbers, string elementName, string attributeName, string value)
        {
            return Message(sourceLineNumbers, Ids.SearchPropertyNotUppercase, "The {0}/@{1} attribute's value, '{2}', cannot contain lowercase characters. Since this is a search property, it must also be a public property. This means the Property/@Id value must be completely uppercase.", elementName, attributeName, value);
        }

        public static Message SecurePropertyNotUppercase(SourceLineNumber sourceLineNumbers, string elementName, string attributeName, string propertyId)
        {
            return Message(sourceLineNumbers, Ids.SecurePropertyNotUppercase, "The {0}/@{1} attribute's value, '{2}', cannot contain lowercase characters. Since this is a secure property, it must also be a public property. This means the Property/@Id value must be completely uppercase.", elementName, attributeName, propertyId);
        }

        public static Message SignedEmbeddedCabinet(SourceLineNumber sourceLineNumbers)
        {
            return Message(sourceLineNumbers, Ids.SignedEmbeddedCabinet, "The DigitalSignature element cannot be nested under a Media element which specifies EmbedCab='yes'. This is because Windows Installer can only verify the digital signatures of external cabinets. Please either remove the DigitalSignature element or change the value of the Media/@EmbedCab attribute to 'no'.");
        }

        public static Message TabbableControlNotAllowedInBillboard(SourceLineNumber sourceLineNumbers, string elementName, string controlType)
        {
            return Message(sourceLineNumbers, Ids.TabbableControlNotAllowedInBillboard, "A {0} element was specified with Type='{1}' and TabSkip='no'. Tabbable controls are not allowed in Billboards.", elementName, controlType);
        }

        public static Message TableNameTooLong(SourceLineNumber sourceLineNumbers, string elementName, string attributeName, string value)
        {
            return Message(sourceLineNumbers, Ids.TableNameTooLong, "The {0}/@{1} attribute's value, '{2}', is too long for a table name. It cannot be more than than 31 characters long.", elementName, attributeName, value);
        }

        public static Message TooManyChildren(SourceLineNumber sourceLineNumbers, string elementName, string childElementName)
        {
            return Message(sourceLineNumbers, Ids.TooManyChildren, "The {0} element contains multiple {1} child elements. There can only be one {1} child element per {0} element.", elementName, childElementName);
        }

        public static Message TooManySearchElements(SourceLineNumber sourceLineNumbers, string elementName)
        {
            return Message(sourceLineNumbers, Ids.TooManySearchElements, "Only one search element can appear under a '{0}' element.", elementName);
        }

        public static Message UniqueFileSearchIdRequired(SourceLineNumber sourceLineNumbers, string id, string elementName)
        {
            return Message(sourceLineNumbers, Ids.UniqueFileSearchIdRequired, "The DirectorySearch element '{0}' requires that the child {1} element has a unique Id when the DirectorySearch/@AssignToProperty attribute is set to 'yes'.", id, elementName);
        }

        public static Message ValueAndMaskMustBeSameLength(SourceLineNumber sourceLineNumbers)
        {
            return Message(sourceLineNumbers, Ids.ValueAndMaskMustBeSameLength, "The FileTypeMask/@Value and FileTypeMask/@Mask attributes must be the same length.");
        }

        public static Message ValueNotSupported(SourceLineNumber sourceLineNumbers, string elementName, string attributeName, string attributeValue)
        {
            return Message(sourceLineNumbers, Ids.ValueNotSupported, "The {0}/@{1} attribute's value, '{2}, is not supported by the Windows Installer.", elementName, attributeName, attributeValue);
        }

        public static Message VersionIndependentProgIdsCannotHaveIcons(SourceLineNumber sourceLineNumbers)
        {
            return Message(sourceLineNumbers, Ids.VersionIndependentProgIdsCannotHaveIcons, "Version independent ProgIds cannot have Icons. Remove the Icon and/or IconIndex attributes from your ProgId element.");
        }

        private static Message Message(SourceLineNumber sourceLineNumber, Ids id, string format, params object[] args)
        {
            return new Message(sourceLineNumber, MessageLevel.Error, (int)id, format, args);
        }

        public enum Ids
        {
            InsufficientVersion = 7,
            SecurePropertyNotUppercase = 11,
            SearchPropertyNotUppercase = 12,
            AppIdIncompatibleAdvertiseState = 18,
            IllegalAttributeWhenAdvertised = 19,
            CustomActionMultipleSources = 22,
            CustomActionMultipleTargets = 23,
            IllegalShortFilename = 26,
            TableNameTooLong = 28,
            FeatureConfigurableDirectoryNotUppercase = 29,
            FeatureCannotFavorAndDisallowAdvertise = 30,
            MediaEmbeddedCabinetNameTooLong = 32,
            RegistryMultipleValuesWithoutMultiString = 34,
            ExampleGuid = 40,
            TooManyChildren = 41,
            ComponentMultipleKeyPaths = 42,
            MissingTypeLibFile = 47,
            UnauthorizedAccess = 50,
            IllegalModuleExclusionLanguageAttributes = 51,
            NoFirstControlSpecified = 52,
            ValueAndMaskMustBeSameLength = 54,
            TooManySearchElements = 55,
            SearchElementRequired = 57,
            MultipleIdentifiersFound = 58,
            AdvertiseStateMustMatch = 59,
            DuplicateContextValue = 60,
            RelativePathForRegistryElement = 61,
            RegistryRootInvalid = 64,
            IllegalYesNoDefaultValue = 65,
            IllegalAttributeInMergeModule = 66,
            SearchElementRequiredWithAttribute = 69,
            CannotAuthorSpecialProperties = 70,
            NeedSequenceBeforeOrAfter = 72,
            ValueNotSupported = 73,
            TabbableControlNotAllowedInBillboard = 74,
            RadioButtonTypeInconsistent = 77,
            RadioButtonBitmapAndIconDisallowed = 78,
            IllegalSuppressWarningId = 79,
            ReferenceLoopDetected = 86,
            InvalidDateTimeFormat = 88,
            MultipleEntrySections = 89,
            MultipleEntrySections2 = 90,
            MissingEntrySection = 93,
            UnresolvedReference = 94,
            MultiplePrimaryReferences = 95,
            ComponentReferencedTwice = 96,
            ImplicitComponentKeyPath = 99,
            DuplicateLocalizationIdentifier = 100,
            LocalizationVariableUnknown = 102,
            ProgIdNestedTooDeep = 105,
            CanNotHaveTwoParents = 106,
            CustomTableNameTooLong = 109,
            CustomTableIllegalColumnWidth = 110,
            CustomTableMissingPrimaryKey = 111,
            PathCannotContainQuote = 117,
            AdditionalArgumentUnexpected = 118,
            RegistryNameValueIncorrect = 119,
            IllegalLongValue = 122,
            DuplicateExtensionXmlSchemaNamespace = 125,
            DuplicateExtensionPreprocessorType = 127,
            FileInUse = 128,
            ExpectedDirectory = 138,
            ComponentExpectedFeature = 139,
            InvalidExtension = 144,
            InvalidSubExpression = 145,
            UnmatchedPreprocessorInstruction = 146,
            NonterminatedPreprocessorInstruction = 147,
            ExpectedExpressionAfterNot = 148,
            InvalidPreprocessorVariable = 149,
            UndefinedPreprocessorVariable = 150,
            IllegalDefineStatement = 151,
            CannotReundefineVariable = 153,
            IllegalForeach = 154,
            ExpectedEndforeach = 156,
            UnmatchedQuotesInExpression = 158,
            UnmatchedParenthesisInExpression = 159,
            ExpectedVariable = 160,
            UnexpectedLiteral = 161,
            IllegalIntegerInExpression = 162,
            UnexpectedPreprocessorOperator = 163,
            UnexpectedEmptySubexpression = 164,
            ActionScheduledRelativeToItself = 181,
            CannotDefaultMismatchedAdvertiseStates = 191,
            VersionIndependentProgIdsCannotHaveIcons = 192,
            BindVariableCollision = 195,
            BindVariableUnknown = 197,
            UnhandledExtensionElement = 200,
            UnhandledExtensionAttribute = 201,
            UnsupportedExtensionAttribute = 202,
            UnsupportedExtensionElement = 203,
            IllegalTargetDirDefaultDir = 206,
            ExpectedBinaryCategory = 208,
            RootFeatureCannotFollowParent = 209,
            FeatureNameTooLong = 210,
            SignedEmbeddedCabinet = 211,
            ExpectedSignedCabinetName = 212,
            IllegalInlineLocVariable = 213,
            MergeModuleExpectedFeature = 215,
            IllegalTerminalServerCustomActionAttributes = 232,
            IllegalPropertyCustomActionAttributes = 233,
            InvalidPreprocessorFunction = 234,
            UndefinedPreprocessorFunction = 235,
            PreprocessorExtensionEvaluateFunctionFailed = 236,
            PreprocessorExtensionGetVariableValueFailed = 237,
            PreprocessorError = 250,
            ExpectedArgument = 251,
            PathTooLong = 262,
            OrphanedComponent = 267,
            IllegalIdentifierLooksLikeFormatted = 275,
            IllegalCodepageAttribute = 276,
            IllegalCompressionLevel = 277,
            ExpectedDirectoryGotFile = 280,
            ExpectedFileGotDirectory = 281,
            InvalidCommandLineFileName = 284,
            IllegalWarningIdAsError = 286,
            DuplicateVariableDefinition = 288,
            UnresolvedBindReference = 298,
            MustSpecifyOutputWithMoreThanOneInput = 302,
            IllegalSearchIdForParentDepth = 303,
            InvalidEmbeddedUIFileName = 312,
            UniqueFileSearchIdRequired = 313,
            CannotDefaultComponentId = 330,
            ParentElementAttributeRequired = 331,
            PreprocessorExtensionPragmaFailed = 333,
            InvalidPreprocessorPragma = 334,
            OrderingReferenceLoopDetected = 343,
            IllegalAttributeValueWithIllegalList = 348,
            InvalidSummaryInfoCodePage = 349,
            InvalidCabinetTemplate = 358,
            DisallowedMsiProperty = 365,
            DuplicatedUiLocalization = 374,
            InvalidPreprocessorFunctionAutoVersion = 379,
            TooDeeplyIncluded = 385,
            NoSourceFiles = 391,
            WixiplSourceFileIsExclusive = 392,
            IntermediatesMustBeCompiled = 395,
            IllegalInnerText = 400,
            InvalidBundleCondition = 409,
            UnableToCopyFile = 7010,
            UnableToDeleteFile = 7011,
            UnableToMoveFile = 7012,
            UnableToOpenFile = 7013,
            BackendNotFound = 7014,
        } // last available is 7099. 7100 is WindowsInstallerBackendWarnings.
    }
}

