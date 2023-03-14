namespace CefSharp.OutOfProcess.Model
{
    /// <summary>
    /// Response when setting a Preference
    /// </summary>
    public class SetPreferenceResponse
    {
        /// <summary>
        /// Success
        /// </summary>
        public bool Success { get; private set; }

        /// <summary>
        /// Error Message
        /// </summary>
        public string ErrorMessage { get; private set; }

        /// <summary>
        /// Initializes a new instance of the SetPreferenceResponse class.
        /// </summary>
        /// <param name="success">success</param>
        /// <param name="errorMessage">error message</param>
        public SetPreferenceResponse(bool success, string errorMessage)
        {
            Success = success;
            ErrorMessage = errorMessage;
        }
    }
}
