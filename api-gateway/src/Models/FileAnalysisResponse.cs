namespace ApiGateway.Models
{
    /// <summary>
    /// Результат анализа текстового файла
    /// </summary>
    public class FileAnalysisResponse
    {
        /// <summary>
        /// Идентификатор анализируемого файла
        /// </summary>
        public Guid FileId { get; set; }
        
        /// <summary>
        /// Имя анализируемого файла
        /// </summary>
        public string FileName { get; set; } = string.Empty;
        
        /// <summary>
        /// Количество абзацев в тексте
        /// </summary>
        public int ParagraphCount { get; set; }
        
        /// <summary>
        /// Количество слов в тексте
        /// </summary>
        public int WordCount { get; set; }
        
        /// <summary>
        /// Количество символов в тексте
        /// </summary>
        public int CharacterCount { get; set; }
        
        /// <summary>
        /// Дата и время выполнения анализа
        /// </summary>
        public DateTime AnalysisDate { get; set; }
        
        /// <summary>
        /// Флаг наличия ошибки при анализе
        /// </summary>
        public bool IsError { get; set; }
        
        /// <summary>
        /// Сообщение об ошибке (если IsError = true)
        /// </summary>
        public string? ErrorMessage { get; set; }
    }
}
