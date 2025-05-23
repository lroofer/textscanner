namespace ApiGateway.Models
{
    /// <summary>
    /// Ответ на запрос загрузки файла
    /// </summary>
    public class FileUploadResponse
    {
        /// <summary>
        /// Уникальный идентификатор файла
        /// </summary>
        public Guid Id { get; set; }
        
        /// <summary>
        /// Имя загруженного файла
        /// </summary>
        public string FileName { get; set; } = string.Empty;
        
        /// <summary>
        /// Флаг, указывающий, что файл был загружен впервые (не является дубликатом)
        /// </summary>
        public bool IsNew { get; set; }
    }
}
