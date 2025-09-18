using System.CommandLine;

namespace YouTubeToPlex.SubPrograms
{
    internal interface ISubProgram
    {
        Command GetCommand();
    }
}
