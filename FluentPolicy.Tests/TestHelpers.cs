using System;

namespace FluentPolicy.Tests
{
    public class TestHelpers
    {
         
    }

    // --- test exceptions to make sure actual exceptions from framework doesn't get picked up by asserts ---

    class TestException : Exception
    {
        public TestException()
        {
        }

        public TestException(string message)
            : base(message)
        {
        }
    }

    class OtherTestException : Exception
    {
        public OtherTestException()
        {
        }

        public OtherTestException(string message)
            : base(message)
        {
        }

        public OtherTestException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }

    class DifferentException : Exception
    {
        public DifferentException()
        {
        }

        public DifferentException(string message)
            : base(message)
        {
        }
    }
}