using Microsoft.AspNetCore.Html;

namespace SwamiSamarthSociety.Web.Helpers
{
    // Small inline-SVG icon set (Feather-style) used across the layout and views.
    // Kept dependency-free (no icon-font/CDN) so the app renders identically offline.
    public static class Icons
    {
        private static readonly Dictionary<string, string> Paths = new()
        {
            ["home"] = "<path d=\"M3 10.5 12 3l9 7.5\"/><path d=\"M5 9.5V20a1 1 0 0 0 1 1h4v-6h4v6h4a1 1 0 0 0 1-1V9.5\"/>",
            ["users"] = "<path d=\"M17 21v-2a4 4 0 0 0-4-4H5a4 4 0 0 0-4 4v2\"/><circle cx=\"9\" cy=\"7\" r=\"4\"/><path d=\"M23 21v-2a4 4 0 0 0-3-3.87\"/><path d=\"M16 3.13a4 4 0 0 1 0 7.75\"/>",
            ["user-check"] = "<path d=\"M16 21v-2a4 4 0 0 0-4-4H5a4 4 0 0 0-4 4v2\"/><circle cx=\"8.5\" cy=\"7\" r=\"4\"/><polyline points=\"17 11 19 13 23 9\"/>",
            ["user-x"] = "<path d=\"M16 21v-2a4 4 0 0 0-4-4H5a4 4 0 0 0-4 4v2\"/><circle cx=\"8.5\" cy=\"7\" r=\"4\"/><line x1=\"18\" y1=\"8\" x2=\"23\" y2=\"13\"/><line x1=\"23\" y1=\"8\" x2=\"18\" y2=\"13\"/>",
            ["credit-card"] = "<rect x=\"1\" y=\"4\" width=\"22\" height=\"16\" rx=\"2.5\"/><line x1=\"1\" y1=\"10\" x2=\"23\" y2=\"10\"/>",
            ["calendar"] = "<rect x=\"3\" y=\"4\" width=\"18\" height=\"18\" rx=\"2.5\"/><line x1=\"16\" y1=\"2\" x2=\"16\" y2=\"6\"/><line x1=\"8\" y1=\"2\" x2=\"8\" y2=\"6\"/><line x1=\"3\" y1=\"10\" x2=\"21\" y2=\"10\"/>",
            ["clock"] = "<circle cx=\"12\" cy=\"12\" r=\"10\"/><polyline points=\"12 6 12 12 16 14\"/>",
            ["bell"] = "<path d=\"M18 8a6 6 0 0 0-12 0c0 7-3 9-3 9h18s-3-2-3-9\"/><path d=\"M13.73 21a2 2 0 0 1-3.46 0\"/>",
            ["shield"] = "<path d=\"M12 22s8-4 8-10V5l-8-3-8 3v7c0 6 8 10 8 10z\"/>",
            ["log-out"] = "<path d=\"M9 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h4\"/><polyline points=\"16 17 21 12 16 7\"/><line x1=\"21\" y1=\"12\" x2=\"9\" y2=\"12\"/>",
            ["menu"] = "<line x1=\"3\" y1=\"6\" x2=\"21\" y2=\"6\"/><line x1=\"3\" y1=\"12\" x2=\"21\" y2=\"12\"/><line x1=\"3\" y1=\"18\" x2=\"21\" y2=\"18\"/>",
            ["x"] = "<line x1=\"18\" y1=\"6\" x2=\"6\" y2=\"18\"/><line x1=\"6\" y1=\"6\" x2=\"18\" y2=\"18\"/>",
            ["trending-up"] = "<polyline points=\"23 6 13.5 15.5 8.5 10.5 1 18\"/><polyline points=\"17 6 23 6 23 12\"/>",
            ["percent"] = "<line x1=\"19\" y1=\"5\" x2=\"5\" y2=\"19\"/><circle cx=\"6.5\" cy=\"6.5\" r=\"2.5\"/><circle cx=\"17.5\" cy=\"17.5\" r=\"2.5\"/>",
            ["layers"] = "<polygon points=\"12 2 2 7 12 12 22 7 12 2\"/><polyline points=\"2 17 12 22 22 17\"/><polyline points=\"2 12 12 17 22 12\"/>",
            ["check-circle"] = "<path d=\"M22 11.08V12a10 10 0 1 1-5.93-9.14\"/><polyline points=\"22 4 12 14.01 9 11.01\"/>",
            ["plus"] = "<line x1=\"12\" y1=\"5\" x2=\"12\" y2=\"19\"/><line x1=\"5\" y1=\"12\" x2=\"19\" y2=\"12\"/>",
            ["download"] = "<path d=\"M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4\"/><polyline points=\"7 10 12 15 17 10\"/><line x1=\"12\" y1=\"15\" x2=\"12\" y2=\"3\"/>",
            ["file-text"] = "<path d=\"M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z\"/><polyline points=\"14 2 14 8 20 8\"/><line x1=\"16\" y1=\"13\" x2=\"8\" y2=\"13\"/><line x1=\"16\" y1=\"17\" x2=\"8\" y2=\"17\"/>",
            ["arrow-right"] = "<line x1=\"5\" y1=\"12\" x2=\"19\" y2=\"12\"/><polyline points=\"12 5 19 12 12 19\"/>",
            ["send"] = "<line x1=\"22\" y1=\"2\" x2=\"11\" y2=\"13\"/><polygon points=\"22 2 15 22 11 13 2 9 22 2\"/>",
        };

        public static IHtmlContent Render(string name, string? cssClass = null)
        {
            var body = Paths.TryGetValue(name, out var p) ? p : "";
            var cls = string.IsNullOrEmpty(cssClass) ? "icon" : $"icon {cssClass}";
            var svg = $"<svg class=\"{cls}\" viewBox=\"0 0 24 24\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.8\" stroke-linecap=\"round\" stroke-linejoin=\"round\" aria-hidden=\"true\">{body}</svg>";
            return new HtmlString(svg);
        }
    }
}
