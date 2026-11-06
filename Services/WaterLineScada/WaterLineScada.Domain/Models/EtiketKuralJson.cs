namespace WaterLineScada.Domain.Models
{
    public class EtiketKuralJson
    {
        public class Rule1
        {
            public string? Id { get; set; }
            public List<PropertyRule>? Rules { get; set; }
            public List<Rule1>? Children { get; set; }
            //public string LogicalOperator { get; set; } = "and"; // "and" veya "or"
        }

        public class PropertyRule
        {
            public string PropertyName { get; set; }
            public string? BelowMin { get; set; }
            public string? AboveMax { get; set; }
            public string? WithinRange { get; set; }
            public string? Equal { get; set; }
            public string ComparisonType { get; set; }
            public int? DecimalPlaces { get; set; }
        }

        public class RuleSet
        {
            public string Logic { get; set; }
            public List<Rule2> Rules { get; set; } // Rule veya RuleGroup olabilir
        }

        public class Rule2
        {
            public int EtiketId { get; set; }
            public string Comparison { get; set; }
            public decimal? Min { get; set; }
            public decimal? Max { get; set; }
            public decimal? Equal { get; set; }
            public decimal? BelowMin { get; set; }
            public decimal? AboveMax { get; set; }
        }
    }
}