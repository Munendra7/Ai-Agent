using Newtonsoft.Json.Linq;

namespace DocxProcessorLibrary.TemplateBasedDocGenerator
{
    /// <summary>
    /// Provides methods for extracting required payloads and populating content controls in DOCX templates.
    /// </summary>
    /// <author>Created by Munendra</author>
    public interface ITemplateBasedDocGenerator
    {
        /// <summary>
        /// Analyzes the provided DOCX template stream and extracts a JSON object
        /// representing the required data payload for populating the template.
        /// </summary>
        /// <param name="templateStream">A memory stream containing the DOCX template.</param>
        /// <returns>
        /// A <see cref="JObject"/> describing the required fields or structure needed to populate the template.
        /// </returns>
        JObject ExtractRequiredPayload(MemoryStream templateStream);

        /// <summary>
        /// Populates the content controls in the provided DOCX template stream using the specified JSON payload.
        /// </summary>
        /// <param name="templateStream">A memory stream containing the DOCX template.</param>
        /// <param name="jsonPayload">A JSON string with values to insert into the template's content controls.</param>
        /// <returns>
        /// A <see cref="MemoryStream"/> containing the populated DOCX document.
        /// </returns>
        MemoryStream PopulateContentControlsFromJson(MemoryStream templateStream, string jsonPayload);
    }
}