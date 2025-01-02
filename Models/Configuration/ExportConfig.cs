public class ExportConfig
{
    public bool Enabled { get; set; }
    public string Format { get; set; } = "csv";  // csv, excel, json
    public string ValuesFilename { get; set; } = "crypto-portfolio-values";  // Individual coin values
    public string TotalsFilename { get; set; } = "crypto-portfolio-totals";  // Portfolio totals
    public string OutputPath { get; set; } = "exports";
} 