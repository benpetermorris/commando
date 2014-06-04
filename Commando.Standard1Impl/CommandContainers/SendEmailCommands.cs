using System.Diagnostics;
using twomindseye.Commando.API1.Commands;
using twomindseye.Commando.API1.EngineFacets;
using twomindseye.Commando.Standard1.FacetTypes;

namespace twomindseye.Commando.Standard1Impl.CommandContainers
{
    public sealed class SendEmailCommands : CommandContainer
    {
        [Command("Send Email", Aliases = "email")]
        public void SendEmail(
            [CommandParameter("To")] IEmailAddressFacet emailAddress,
            [CommandParameter("Text", Optional = true)] ITextFacet message)
        {
            Process.Start("mailto:" + emailAddress.Value);
        }

        [Command("Send Attachment", Aliases = "ea,attach")]
        public void SendAttachment(
            [CommandParameter("To")] IEmailAddressFacet emailAddress,
            [CommandParameter("Attachment")] [FilterExtraData("Type", "File")] IFileSystemItemFacet attachment,
            [CommandParameter("Text", Optional = true)] ITextFacet message)
        {
            Process.Start("mailto:" + emailAddress.Value);
        }
    }
}
