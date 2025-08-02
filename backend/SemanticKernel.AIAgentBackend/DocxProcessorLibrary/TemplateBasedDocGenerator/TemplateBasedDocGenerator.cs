using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Newtonsoft.Json.Linq;

namespace DocxProcessorLibrary.TemplateBasedDocGenerator
{
    public class TemplateBasedDocGenerator : ITemplateBasedDocGenerator
    {
        public JObject ExtractRequiredPayload(MemoryStream templateStream)
        {
            using (WordprocessingDocument wordDoc = WordprocessingDocument.Open(templateStream, false))
            {
                var body = wordDoc?.MainDocumentPart?.Document.Body;
                return ExtractPayloadFromSdtElements(body?.Descendants<SdtElement>());
            }
        }

        private static JObject ExtractPayloadFromSdtElements(IEnumerable<SdtElement>? sdtElements)
        {
            var payload = new JObject();
            var childTagsInsideRepeaters = new HashSet<string>();
            var repeatingSectionTags = new HashSet<string>();
            var repeatingSectionStructures = new Dictionary<string, List<SdtElement>>();

            // Identify repeaters and their children
            foreach (var sdt in sdtElements ?? Enumerable.Empty<SdtElement>())
            {
                var tag = sdt?.SdtProperties?.GetFirstChild<Tag>()?.Val?.Value;
                if (string.IsNullOrWhiteSpace(tag)) continue;

                if (sdt is SdtBlock || sdt is SdtRow)
                {
                    var innerSdts = sdt.Descendants<SdtElement>()
                        .Where(x => x != sdt && x?.SdtProperties?.GetFirstChild<Tag>()?.Val?.Value != null)
                        .ToList();

                    if (innerSdts.Any())
                    {
                        repeatingSectionTags.Add(tag);
                        repeatingSectionStructures[tag] = innerSdts;
                        foreach (var childSdt in innerSdts)
                            childTagsInsideRepeaters.Add(childSdt?.SdtProperties?.GetFirstChild<Tag>()?.Val?.Value ?? string.Empty);
                    }
                }
            }

            // Build payload recursively
            foreach (var sdt in sdtElements ?? Enumerable.Empty<SdtElement>())
            {
                var tag = sdt?.SdtProperties?.GetFirstChild<Tag>()?.Val?.Value;
                if (string.IsNullOrWhiteSpace(tag)) continue;

                if (repeatingSectionTags.Contains(tag))
                {
                    if (!payload.ContainsKey(tag))
                    {
                        var arr = new JArray();
                        var item = ExtractPayloadFromSdtElements(repeatingSectionStructures[tag] ?? new List<SdtElement>());
                        arr.Add(item);
                        payload[tag] = arr;
                    }
                }
                else if (!childTagsInsideRepeaters.Contains(tag))
                {
                    if (!payload.ContainsKey(tag))
                    {
                        if (sdt?.SdtProperties?.GetFirstChild<CheckBox>() != null)
                            payload[tag] = false;
                        else if (sdt?.SdtProperties?.GetFirstChild<SdtContentDropDownList>() != null)
                            payload[tag] = "";
                        else
                            payload[tag] = "";
                    }
                }
            }

            return payload;
        }

        public MemoryStream PopulateContentControlsFromJson(MemoryStream templateStream, string jsonPayload)
        {
            var payload = JObject.Parse(jsonPayload);

            using (WordprocessingDocument wordDoc = WordprocessingDocument.Open(templateStream, true))
            {
                var doc = wordDoc?.MainDocumentPart?.Document;
                var body = doc?.Body;
                PopulateSdtElements(body, payload);
                doc?.Save();
            }

            templateStream.Position = 0;
            return templateStream;
        }

        private static void PopulateSdtElements(OpenXmlElement? parent, JObject payload)
        {
            var sdtElements = parent?.Descendants<SdtElement>()?.ToList() ?? new List<SdtElement>();

            foreach (var sdt in sdtElements)
            {
                var tag = sdt?.SdtProperties?.GetFirstChild<Tag>()?.Val?.Value;
                if (string.IsNullOrWhiteSpace(tag)) continue;
                if (!payload.TryGetValue(tag ?? string.Empty, out var token) || token == null) continue;

                if (token.Type == JTokenType.String || token.Type == JTokenType.Integer)
                {
                    SetSingleText(sdt, token?.ToString() ?? string.Empty);
                }
                else if (token.Type == JTokenType.Boolean && sdt?.SdtProperties?.GetFirstChild<CheckBox>() != null)
                {
                    var isChecked = token?.Value<bool>() ?? false;
                    var val = isChecked ? "☒" : "☐";
                    SetSingleText(sdt, val);
                }
                else if (token.Type == JTokenType.String && sdt?.SdtProperties?.GetFirstChild<SdtContentDropDownList>() != null)
                {
                    SetSingleText(sdt, token?.ToString() ?? string.Empty);
                }
                else if (token.Type == JTokenType.Array && token is JArray arr)
                {
                    var prototype = sdt?.CloneNode(true);
                    var parentElement = sdt?.Parent;
                    sdt?.Remove();

                    foreach (var obj in arr)
                    {
                        if (prototype is SdtElement proto)
                        {
                            var newSdt = proto.CloneNode(true) as SdtElement;
                            var objFields = obj as JObject;
                            if (newSdt != null && objFields != null && parentElement != null)
                            {
                                // Recursively fill nested SDTs
                                PopulateSdtElements(newSdt, objFields);
                                parentElement.AppendChild(newSdt);
                            }
                        }
                    }
                }
            }
        }

        // Helper method to set only the first <Text> and clear the rest
        private static void SetSingleText(SdtElement? sdt, string value)
        {
            var textElements = sdt?.Descendants<Text>()?.ToList() ?? new List<Text>();
            if (textElements.Count > 0)
            {
                if (textElements[0] != null)
                    textElements[0].Text = value ?? string.Empty;
                for (int i = 1; i < textElements.Count; i++)
                {
                    if (textElements[i] != null)
                        textElements[i].Text = string.Empty;
                }
            }
        }
    }
}
