using System;
using Should;
using Xunit;

namespace FluentPolicy.Tests
{
    public class PolicyBuilderTests
    {
        private const string SampleExceptionMessage = "qwertyuiop";
        private const string SampleReturnString = "zxccvbnm";

        [Fact]
        public void CanBuildPolicy()
        {
            var policy =
                As.Func(TestFunction).WithPolicy()
                    .For().Exception<TestException>().Throw(ae => new Exception(SampleExceptionMessage, ae))
                    .For().Exception<ArgumentNullException>().Rethrow()
                    .For().ReturnValue(s => s.Equals(SampleReturnString)).Return(s => s.ToUpper())
                    ;
            policy.ShouldNotBeNull();
        }

        [Fact]
        public void NoExceptions()
        {
            As.Func(TestFunction).WithPolicy()
                .For().Exception<TestException>().Rethrow()
                .Execute().ShouldEqual(SampleReturnString);
        }

        [Fact]
        public void ReturnValueTransformation()
        {
            const string testString = "fnord";

            As.Func(TestFunction).WithPolicy()
                .For().ReturnValue(s => s.Equals(SampleReturnString)).Return(testString)
                .Execute().ShouldEqual(testString);
        }

        [Fact]
        public void ReturnValueExceptionThrowing()
        {
            const string testString = "fnord";

            Assert.Throws<Exception>(() => As.Func(TestFunction).WithPolicy()
                .For().ReturnValue(s => s.Equals(SampleReturnString)).Throw(s => new Exception(s))
                .Execute()
                ).Message.ShouldEqual(SampleReturnString);
        }

        [Fact]
        public void ForExceptionReturnValue()
        {
            const string otherReturnValue = "discordia";
            As.Func(TestFunctionException).WithPolicy()
                .For().Exception<TestException>().Return(otherReturnValue)
                .For().Exception<ArgumentNullException>().Rethrow()
                .Execute().ShouldEqual(otherReturnValue);
        }

        [Fact]
        public void ForOrExceptionReturnValue()
        {
            const string otherReturnValue = "discordia";
            As.Func(TestFunctionException).WithPolicy()
                .For().Exception<OtherTestException>().Or<TestException>().Return(otherReturnValue)
                .For().Exception<ArgumentNullException>().Rethrow()
                .Execute().ShouldEqual(otherReturnValue);
        }


        ///<remarks> This also tests for correct order of evaluation</remarks>
        [Fact]
        public void ForPredicatedExceptionReturnValue()
        {
            const string otherReturnValue = "discordia";
            As.Func(TestFunctionExceptionWithMessage).WithPolicy()
                .For().Exception<TestException>(e => e.Message.Equals(SampleExceptionMessage)).Return(otherReturnValue)
                .For().Exception<TestException>(e=>e.Message.Equals(SampleReturnString)).Rethrow()
                .For().Exception<TestException>().Throw(e=>new Exception("This should not get thrown"))
                .Execute().ShouldEqual(otherReturnValue);
        }

        [Fact]
        public void ForOrPredicatedExceptionReturnValue()
        {
            const string otherReturnValue = "discordia";
            As.Func(TestFunctionExceptionWithMessage).WithPolicy()
                .For().Exception<OtherTestException>().Or<TestException>(e => e.Message.Equals(SampleExceptionMessage)).Return(otherReturnValue)
                .For().Exception<TestException>(e => e.Message.Equals(SampleReturnString)).Rethrow()
                .For().Exception<TestException>().Throw(e => new Exception("This should not get thrown"))
                .Execute().ShouldEqual(otherReturnValue);
        }

        [Fact]
        public void ForExceptionRethrow()
        {
            var ex = Assert.Throws<TestException>(() => As.Func(TestFunctionReusableException).WithPolicy()
                .For().Exception<TestException>(e => e.Message.Equals(SampleExceptionMessage)).Rethrow()
                .For().Exception<TestException>().Return(SampleReturnString)
                .Execute()
                );
            ex.ShouldBeSameAs(ReusableException);
        }

        [Fact]
        public void ForPredicatedExceptionThrow()
        {
            const string otherReturnValue = "discordia";
            const string testMessage = "my message";
            var ex = Assert.Throws<OtherTestException>(() => As.Func(TestFunctionExceptionWithMessage).WithPolicy()
                .For().Exception<TestException>(e => e.Message.Equals(SampleExceptionMessage)).Throw(e => new OtherTestException(testMessage, e))
                .For().Exception<TestException>(e => e.Message.Equals(SampleReturnString)).Return(otherReturnValue)
                .For().Exception<TestException>().Return(otherReturnValue)
                .Execute().ShouldEqual(otherReturnValue));
            ex.ShouldNotBeNull();
            ex.Message.ShouldEqual(testMessage);
        }

        [Fact]
        public void NotDefinedExceptionThrowException()
        {
            Assert.Throws<NoMatchingPolicyException>(() => As.Func(TestFunctionException).WithPolicy()
                .For().Exception<OtherTestException>().Rethrow()
                .For().Exception<DifferentException>().Return(string.Empty)
                .Execute()
                );
        }

        [Fact]
        public void ForAllOtherExceptionsRethrows()
        {
            Assert.Throws<TestException>(() => As.Func(TestFunctionException).WithPolicy()
                .For().Exception<OtherTestException>().Rethrow()
                .For().Exception<DifferentException>().Return(string.Empty)
                .For().AllOtherExceptions().Rethrow()
                .Execute()
                );
        }

        [Fact]
        public void ForAllOtherExceptionsThrows()
        {
            const string testMessage = "some message";

            var ex = Assert.Throws<TestException>(() => As.Func(TestFunctionException).WithPolicy()
                .For().Exception<OtherTestException>().Rethrow()
                .For().Exception<DifferentException>().Return(string.Empty)
                .For().AllOtherExceptions().Throw(e => new TestException(testMessage))
                .Execute()
                );
            ex.Message.ShouldEqual(testMessage);
        }

        [Fact]
        public void ForAllOtherExceptionsReturnsValue()
        {
            const string otherMessage = "Iä! Iä! Cthulhu Fhtagn!";

            As.Func(TestFunctionException).WithPolicy()
                .For().Exception<OtherTestException>().Rethrow()
                .For().Exception<DifferentException>().Return(string.Empty)
                .For().AllOtherExceptions().Return(otherMessage)
                .Execute()
                .ShouldEqual(otherMessage);
        }


        // --- methods to call ---

        private static string TestFunction()
        {
            return SampleReturnString;
        }

        private static string TestFunctionException()
        {
            throw new TestException();
        }

        private static string TestFunctionExceptionWithMessage()
        {
            throw new TestException(SampleExceptionMessage);
        }

        private static readonly TestException ReusableException = new TestException(SampleExceptionMessage);

        private static string TestFunctionReusableException()
        {
            throw ReusableException;
        }

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

            public OtherTestException(string message, Exception innerException) : base(message, innerException)
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
}
