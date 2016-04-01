namespace FluentPolicy.Exceptions
{
    public class ImplementationNotSetException : FluentPolicyPublicExceptionBase
    {
        public ImplementationNotSetException()
            : base("The policy was build without implementation - use overload that takes a function to execute.")
        {
            
        }
    }
}