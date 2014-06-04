/// imports
/// Commando.API1, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null

/// usings
/// System
/// System.Diagnostics
/// twomindseye.Commando.API1

/// command
/// function searchVideo
/// title EasyNews: Search for Videos
/// aliases esv
/// param Keywords
/// paramtype EngineFacets.ITextFacet

function searchVideo(keywords) {
    Process.Start(StringFormat("https://secure.members.easynews.com/global4/search.html?gps={0}&fty[]=VIDEO", [encodeURIComponent(keywords.Text)]));
}