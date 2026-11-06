using Newtonsoft.Json;

namespace SuHatScada.Domain.Models
{
    public class SekilJson
    {
        [JsonProperty("id")] public string Id { get; set; }
        [JsonProperty("type")] public string? Type { get; set; }
        /// <summary>
        /// card,gauge vb. tanımlama
        /// </summary>
        [JsonProperty("type2")] public string? Type2 { get; set; }
        [JsonProperty("x")] public int? X { get; set; }
        [JsonProperty("y")] public int? Y { get; set; }
        [JsonProperty("cX")] public int? CX { get; set; }
        [JsonProperty("cY")] public int? CY { get; set; }
        [JsonProperty("r")] public int? R { get; set; }
        [JsonProperty("rX")] public string? RX { get; set; } = "0";
        [JsonProperty("rY")] public string? RY { get; set; } = "0";
        [JsonProperty("width")] public int? Width { get; set; }
        [JsonProperty("height")] public int? Height { get; set; }
        [JsonProperty("yuzde")] public string? Yuzde { get; set; }
        [JsonProperty("fill")] public string? Fill { get; set; }
        [JsonProperty("text")] public string? Text { get; set; }
        [JsonProperty("fontSize")] public int? FontSize { get; set; }
        [JsonProperty("baslikFontSize")] public int? BaslikFontSize { get; set; }
        [JsonProperty("fontFamily")] public string? FontFamily { get; set; }
        [JsonProperty("fontWeight")] public string? FontWeight { get; set; }
        [JsonProperty("textDecoration")] public string? TextDecoration { get; set; }
        [JsonProperty("href")] public string? Href { get; set; }
        [JsonProperty("stroke")] public string? Stroke { get; set; }
        [JsonProperty("strokeWidth")] public string? StrokeWidth { get; set; }
        [JsonProperty("opacity")] public string? Opacity { get; set; }
        [JsonProperty("strokeDasharray")] public string? StrokeDasharray { get; set; }
        [JsonProperty("rotation")] public int? Rotation { get; set; }
        [JsonProperty("zIndex")] public int? ZIndex { get; set; }
        [JsonProperty("value")] public string? Value { get; set; }
        [JsonProperty("d")] public string? D { get; set; }
        [JsonProperty("visible")] public string? Visible { get; set; }
        [JsonProperty("blink")] public string? Blink { get; set; }
        [JsonProperty("animationFrom")] public string? AnimationFrom { get; set; }
        [JsonProperty("animationTo")] public string? AnimationTo { get; set; }
        [JsonProperty("animationDuration")] public string? AnimationDuration { get; set; }
        [JsonProperty("baslik")] public string? Baslik { get; set; }
        [JsonProperty("icerik")] public List<string>? Icerik { get; set; }
        [JsonProperty("birim")] public List<string>? Birim { get; set; }
        [JsonProperty("rowBasliklar")] public List<string>? RowBasliklar { get; set; }
        [JsonProperty("icerikDbl")] public List<List<string>>? IcerikDbl { get; set; }
        [JsonProperty("basliklar")] public List<string>? Basliklar { get; set; }
        [JsonProperty("resimUrls")] public List<string>? ResimUrls { get; set; }
        [JsonProperty("borderTopColor")] public string? BorderTopColor { get; set; }
        [JsonProperty("cardWidth")] public string? CardWidth { get; set; }
        [JsonProperty("cardHeight")] public string? CardHeight { get; set; }
        [JsonProperty("textColor")] public string? TextColor { get; set; }
        [JsonProperty("textColorArr")] public List<string>? TextColorArr { get; set; }
        [JsonProperty("children")] public List<SekilJson>? Children { get; set; }
        [JsonProperty("data")] public dynamic? Data { get; set; }
        [JsonProperty("onClick")] public dynamic? OnClick { get; set; }
        [JsonProperty("elementAttr")] public string? ElementAttr { get; set; }
        [JsonProperty("buttonText")] public string? ButtonText { get; set; }
        [JsonProperty("targetId")] public int? TargetId { get; set; }
        [JsonProperty("draggable")] public int? Draggable { get; set; }
        [JsonProperty("cursor")] public string? Cursor { get; set; }
        [JsonProperty("modTuru")] public string? ModTuru { get; set; }
        [JsonProperty("modTuruTargetId")] public string? ModTuruTargetId { get; set; }

        public string GetPropertyValue(string PropName)
        {
            var asd = this.GetType().GetProperty(PropName).GetValue(this, null);
            return this.GetType().GetProperty(PropName).GetValue(this, null) == null ? "" : this.GetType().GetProperty(PropName).GetValue(this, null).ToString();
        }

        public void SetPropertyValue(string propName, string value)
        {
            var property = this.GetType().GetProperty(propName);
            if (property != null && property.CanWrite)
            {
                if (property.PropertyType == typeof(int) && int.TryParse(value, out int intValue))
                {
                    property.SetValue(this, intValue);
                }
                else
                {
                    property.SetValue(this, value);
                }
            }
        }
    }
}