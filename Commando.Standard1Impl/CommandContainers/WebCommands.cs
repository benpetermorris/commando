using System;
using System.Diagnostics;
using twomindseye.Commando.API1.Commands;
using twomindseye.Commando.API1.EngineFacets;

namespace twomindseye.Commando.Standard1Impl.CommandContainers
{
    public sealed class WebCommands : CommandContainer
    {
        [Command("Search Web", Aliases = "ws,searchweb")]
        public void SearchWeb(ITextFacet text)
        {
            Process.Start("http://www.google.com/search?q=" + Uri.EscapeDataString(text.Text));
        }
    }
}
