namespace TablesAndSymbols
{
    public enum ColumnCategory
    {
        Unknown,
        Text,
        UpperCase,
        LowerCase,
        Integer,
        DoubleInteger,
        TimeDate,
        Identifier,
        Property,
        Filename,
        WildCardFilename,
        Path,
        Paths,
        AnyPath,
        DefaultDir,
        RegPath,
        Formatted,
        Template,
        Condition,
        Guid,
        Version,
        Language,
        Binary,
        CustomSource,
        Cabinet,
        Shortcut,
        FormattedSDDLText,
    }

    public enum ColumnModularizeType
    {
        None,
        Column,
        Icon,
        CompanionFile,
        Condition,
        ControlEventArgument,
        ControlText,
        Property,
        SemicolonDelimited,
    }

    public enum ColumnType
    {
        Unknown,
        String,
        Localized,
        Number,
        Object,
        Preserved,
    }
}
