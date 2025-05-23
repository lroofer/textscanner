namespace ApiGateway.Models
{
    /// <summary>
    /// Модель ответа при возникновении ошибки
    /// </summary>
    public class ErrorResponse
    {
        /// <summary>
        /// Статус ошибки
        /// </summary>
        public string Status { get; set; } = "Error";
        
        /// <summary>
        /// Код ошибки
        /// </summary>
        public int Code { get; set; }
        
        /// <summary>
        /// Сообщение об ошибке
        /// </summary>
        public string Message { get; set; } = string.Empty;
        
        /// <summary>
        /// Дополнительные детали ошибки (опционально)
        /// </summary>
        public object? Details { get; set; }
    }
}
