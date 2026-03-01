public class AnalysisResult
{
    public string OriginalFileName { get; set; }
    public string SavedFileName { get; set; }
    public int LineCount { get; set; }
    public int WordCount { get; set; }
    public int CharCount { get; set; }
    public string AnalysisFilePath { get; set; }
}