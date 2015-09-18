using System;
using System.Threading.Tasks;
using FluentPolicy.Exceptions;
using Should;
using Xunit;

namespace FluentPolicy.Tests
{
    public class PolicyBuilderAsyncTests
    {
        private const string SampleExceptionMessage = "qwertyuiop";
        private const string SampleReturnString = "zxccvbnm";

        [Fact]
        public void CanBuildPolicy()
        {
            var policy =
                As.Func(TestFunction).WithAsyncPolicy()
                    .For().Exception<TestException>().Throw(ae => new Exception(SampleExceptionMessage, ae))
                    .For().Exception<ArgumentNullException>().Rethrow()
                    .For().ReturnValue(s => s.Equals(SampleReturnString)).Return(s => s.ToUpper())
                ;
            policy.ShouldNotBeNull();
        }

        [Fact]
        public async Task NoExceptions()
        {
            var result = await As.Func(TestFunction).WithAsyncPolicy()
                .For().Exception<TestException>().Rethrow()
                .ExecuteAsync();
                
                result.ShouldEqual(SampleReturnString);
        }

        [Fact]
        public async Task ReturnValueTransformation()
        {
            const string testString = "fnord";

            var result = await As.Func(TestFunction).WithAsyncPolicy()
                .For().ReturnValue(s => s.Equals(SampleReturnString)).Return(testString)
                .ExecuteAsync();

            result.ShouldEqual(testString);
        }

        [Fact]
        public async Task ReturnValueExceptionThrowing()
        {
            const string testString = "fnord";

            var ex = await Assert.ThrowsAsync<Exception>(() => As.Func(TestFunction).WithAsyncPolicy()
                .For().ReturnValue(s => s.Equals(SampleReturnString)).Throw(s => new Exception(s))
                .ExecuteAsync()
                );

            ex.Message.ShouldEqual(SampleReturnString);
        }

        [Fact]
        public async Task ForExceptionReturnValue()
        {
            const string otherReturnValue = "discordia";
            var result = await As.Func(TestFunctionException).WithAsyncPolicy()
                .For().Exception<TestException>().Return(otherReturnValue)
                .For().Exception<ArgumentNullException>().Rethrow()
                .ExecuteAsync();
            
            result.ShouldEqual(otherReturnValue);
        }

        [Fact]
        public async Task ForOrExceptionReturnValue()
        {
            const string otherReturnValue = "discordia";
            var result = await As.Func(TestFunctionException).WithAsyncPolicy()
                .For().Exception<OtherTestException>().Or<TestException>().Return(otherReturnValue)
                .For().Exception<ArgumentNullException>().Rethrow()
                .ExecuteAsync();
            
            result.ShouldEqual(otherReturnValue);
        }


        ///<remarks> This also tests for correct order of evaluation</remarks>
        [Fact]
        public async Task ForPredicatedExceptionReturnValue()
        {
            const string otherReturnValue = "discordia";
            var result = await As.Func(TestFunctionExceptionWithMessage).WithAsyncPolicy()
                .For().Exception<TestException>(e => e.Message.Equals(SampleExceptionMessage)).Return(otherReturnValue)
                .For().Exception<TestException>(e => e.Message.Equals(SampleReturnString)).Rethrow()
                .For().Exception<TestException>().Throw(e => new Exception("This should not get thrown"))
                .ExecuteAsync();
            
            result.ShouldEqual(otherReturnValue);
        }

        [Fact]
        public async Task ForOrPredicatedExceptionReturnValue()
        {
            const string otherReturnValue = "discordia";

            var result = await As.Func(TestFunctionExceptionWithMessage).WithAsyncPolicy()
                .For().Exception<OtherTestException>().Or<TestException>(e => e.Message.Equals(SampleExceptionMessage)).Return(otherReturnValue)
                .For().Exception<TestException>(e => e.Message.Equals(SampleReturnString)).Rethrow()
                .For().Exception<TestException>().Throw(e => new Exception("This should not get thrown"))
                .ExecuteAsync();
            
            result.ShouldEqual(otherReturnValue);
        }

        [Fact]
        public async Task ForExceptionRethrow()
        {
            var ex = await Assert.ThrowsAsync<TestException>(() => As.Func(TestFunctionReusableException).WithAsyncPolicy()
                .For().Exception<TestException>(e => e.Message.Equals(SampleExceptionMessage)).Rethrow()
                .For().Exception<TestException>().Return(SampleReturnString)
                .ExecuteAsync()
                );

            ex.ShouldBeSameAs(ReusableException);
        }

        [Fact]
        public void ForExecuteIfMadeFromTaskThrow()
        {
            Assert.Throws<AsyncPolicyException>(() => As.Func(TestFunctionReusableException).WithAsyncPolicy()
                .For().Exception<TestException>(e => e.Message.Equals(SampleExceptionMessage)).Rethrow()
                .For().Exception<TestException>().Return(SampleReturnString)
                .Execute()
                );
        }

        [Fact]
        public async Task ForPredicatedExceptionThrow()
        {
            const string otherReturnValue = "discordia";
            const string testMessage = "my message";

            var ex = await Assert.ThrowsAsync<OtherTestException>(() => As.Func(TestFunctionExceptionWithMessage).WithAsyncPolicy()
                .For().Exception<TestException>(e => e.Message.Equals(SampleExceptionMessage)).Throw(e => new OtherTestException(testMessage, e))
                .For().Exception<TestException>(e => e.Message.Equals(SampleReturnString)).Return(otherReturnValue)
                .For().Exception<TestException>().Return(otherReturnValue)
                .ExecuteAsync());

            ex.ShouldNotBeNull();
            ex.Message.ShouldEqual(testMessage);
        }

        [Fact]
        public async Task NotDefinedExceptionRethrowException()
        {
            await Assert.ThrowsAsync<TestException>(() => As.Func(TestFunctionException).WithAsyncPolicy()
                .For().Exception<OtherTestException>().Rethrow()
                .For().Exception<DifferentException>().Return(string.Empty)
                .ExecuteAsync()
                );
        }

        [Fact]
        public async Task ForAllOtherExceptionsRethrows()
        {
            await Assert.ThrowsAsync<TestException>(() => As.Func(TestFunctionException).WithAsyncPolicy()
                .For().Exception<OtherTestException>().Rethrow()
                .For().Exception<DifferentException>().Return(string.Empty)
                .For().AllOtherExceptions().Rethrow()
                .ExecuteAsync()
                );
        }

        [Fact]
        public async Task ForAllOtherExceptionsThrows()
        {
            const string testMessage = "some message";

            var ex = await Assert.ThrowsAsync<TestException>(() => As.Func(TestFunctionException).WithAsyncPolicy()
                .For().Exception<OtherTestException>().Rethrow()
                .For().Exception<DifferentException>().Return(string.Empty)
                .For().AllOtherExceptions().Throw(e => new TestException(testMessage))
                .ExecuteAsync()
                );

            ex.Message.ShouldEqual(testMessage);
        }

        [Fact]
        public async Task ForAllOtherExceptionsReturnsValue()
        {
            const string otherMessage = "Iä! Iä! Cthulhu Fhtagn!";

            var result = await As.Func(TestFunctionException).WithAsyncPolicy()
                .For().Exception<OtherTestException>().Rethrow()
                .For().Exception<DifferentException>().Return(string.Empty)
                .For().AllOtherExceptions().Return(otherMessage)
                .ExecuteAsync();

                result.ShouldEqual(otherMessage);
        }

        [Fact]
        public async Task ReturnsAsync()
        {
            var result = await As.Func(TestFunctionException).WithAsyncPolicy()
                .For().Exception<TestException>().ReturnAsync(TestFunction)
                .ExecuteAsync();

            result.ShouldEqual(SampleReturnString);
        }

        [Fact]
        public async Task ReturnsAsyncFromTask()
        {
            var result = await As.Func(TestFunctionException).WithAsyncPolicy()
                .For().Exception<TestException>().ReturnAsync(TestFunction())
                .ExecuteAsync();

            result.ShouldEqual(SampleReturnString);
        }


        // --- methods to call ---

        private static Task<string> TestFunction()
        {
            return Task.Run(() => SampleReturnString);
        }

        private static Task<string> TestFunctionException()
        {
            return Task.Run(() =>
            {
                throw new TestException();
                return string.Empty;
            });
        }

        private static Task<string> TestFunctionExceptionWithMessage()
        {
            return Task.Run(() =>
            {
                throw new TestException(SampleExceptionMessage);
                return string.Empty;
            });
        }

        private static readonly TestException ReusableException = new TestException(SampleExceptionMessage);

        private static Task<string> TestFunctionReusableException()
        {
            return Task.Run(() =>
            {
                throw ReusableException;
                return string.Empty;
            });
        }

        private class TestException : Exception
        {
            public TestException()
            {
            }

            public TestException(string message)
                : base(message)
            {
            }
        }

        private class OtherTestException : Exception
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

        private class DifferentException : Exception
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
