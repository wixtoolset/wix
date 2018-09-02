using WixToolset.Extensibility.Data;

namespace WixCop.Interfaces
{
    public interface IWixCopCommandLineParser
    {
        ICommandLineArguments Arguments { get; set; }

        ICommandLineCommand ParseWixCopCommandLine();
    }
}
