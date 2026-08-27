namespace 崴正.Services
{
    public enum ServiceErrorType
    {
        None,
        Validation,
        NotFound,
        Conflict
    }

    public class ServiceResult
    {
        public bool Success { get; private set; }

        public ServiceErrorType ErrorType { get; private set; }

        public string? ErrorMessage { get; private set; }

        public object? Data { get; private set; }

        private ServiceResult()
        {
        }

        public static ServiceResult Ok(object? data = null)
        {
            return new ServiceResult
            {
                Success = true,
                ErrorType = ServiceErrorType.None,
                Data = data
            };
        }

        public static ServiceResult Validation(string message)
        {
            return new ServiceResult
            {
                Success = false,
                ErrorType = ServiceErrorType.Validation,
                ErrorMessage = message
            };
        }

        public static ServiceResult NotFound(string message)
        {
            return new ServiceResult
            {
                Success = false,
                ErrorType = ServiceErrorType.NotFound,
                ErrorMessage = message
            };
        }

        public static ServiceResult Conflict(string message)
        {
            return new ServiceResult
            {
                Success = false,
                ErrorType = ServiceErrorType.Conflict,
                ErrorMessage = message
            };
        }
    }
}