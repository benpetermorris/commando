/// imports
/// Commando.Standard1, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null

/// usings
/// System
/// System.Diagnostics
/// Microsoft.Win32
/// StandardFacets = twomindseye.Commando.Standard1.FacetTypes
/// EngineFacets = twomindseye.Commando.API1.EngineFacets

/// command
/// function startRepoBrowser
/// title Tortoise Repo-browser
/// aliases repo
/// param Repository Url
/// paramtype StandardFacets.IUrlFacet
/// paramoptional false

function startRepoBrowser(url) {
    Process.Start(getProcPath(), StringFormat("/command:repobrowser \"/path:{0}\"", [url.Url]));
}

function getProcPath() {
    var key = Registry.LocalMachine.OpenSubKey("SOFTWARE\\TortoiseSVN");
    var procPath = key.GetValue("ProcPath").ToString();
    key.Dispose();
    return procPath;
} 
