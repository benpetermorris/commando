using System.Diagnostics;
using twomindseye.Commando.API1.Commands;
using twomindseye.Commando.API1.EngineFacets;
using twomindseye.Commando.Standard1.FacetTypes;

namespace twomindseye.Commando.Standard1Impl.CommandContainers
{
    public sealed class FilesystemItemCommands : CommandContainer
    {
        [Command("Open File", Aliases = "open")]
        public void OpenInExplorer(
            [CommandParameter("File or Folder"), FilterExtraData("Type", "File", "Folder")] IFileSystemItemFacet item)
        {
            Process.Start((string) item.Path);
        }

        [Command("Open Program", Aliases = "open")]
        public void OpenProgram(
            [CommandParameter("Program"), FilterExtraData("Type", "Program")] IFileSystemItemFacet program,
            [CommandParameter("File", Optional = true), FilterExtraData("Type", "File")] IFileSystemItemFacet file,
            [CommandParameter("Text", Optional = true)] ITextFacet commandline)
        {
            if (file != null)
            {
                Process.Start(program.Path, '"' + file.Path + '"');
            }
            else
            {
                Process.Start(program.Path);
            }
        }

        [Command("Open Url", Aliases = "open")]
        public void OpenUrl(
            [CommandParameter("Url")] IUrlFacet url)
        {
            Process.Start(url.Url);
        }

        [Command("Open Command Prompt", Aliases = "cmd")]
        public void OpenCommandPrompt(
            [CommandParameter("Location", Optional = true), FilterExtraData("Type", "Folder")] IFileSystemItemFacet folder)
        {
            Process.Start("cmd.exe", string.Format(@"/k ""cd ""{0}""""", folder.Path));
        }
    }
}
