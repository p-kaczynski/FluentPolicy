using System;
using Moq;
using Should;
using Xunit;

namespace FluentPolicy.Tests
{
    public class PolicyBuilderTests
    {
        private readonly Mock<ITestRepeat> _repeatMock = new Mock<ITestRepeat>();
        private readonly ITestRepeat _repeat;

        private const string SampleExceptionMessage = "qwertyuiop";
        private const string SampleReturnString = "zxccvbnm";

        public PolicyBuilderTests()
        {
            _repeat = _repeatMock.Object;
        }

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
        public void ReturnValueReturnDefault()
        {
            As.Func(TestFunction).WithPolicy()
                .For().ReturnValue(s => s.Equals(SampleReturnString)).ReturnDefault()
                .Execute().ShouldBeNull();
        }

        [Fact]
        public void ReturnValueExceptionThrowing()
        {
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

        [Fact]
        public void ForExceptionReturnDefault()
        {
            As.Func(TestFunctionException).WithPolicy()
                .For().Exception<TestException>().ReturnDefault()
                .For().Exception<ArgumentNullException>().Rethrow()
                .Execute().ShouldBeNull();
        }

        [Fact]
        public void ForOrExceptionReturnDefault()
        {
            As.Func(TestFunctionException).WithPolicy()
                .For().Exception<OtherTestException>().Or<TestException>().ReturnDefault()
                .For().Exception<ArgumentNullException>().Rethrow()
                .Execute().ShouldBeNull();
        }


        ///<remarks> This also tests for correct order of evaluation</remarks>
        [Fact]
        public void ForPredicatedExceptionReturnValue()
        {
            const string otherReturnValue = "discordia";
            As.Func(TestFunctionExceptionWithMessage).WithPolicy()
                .For().Exception<TestException>(e => e.Message.Equals(SampleExceptionMessage)).Return(otherReturnValue)
                .For().Exception<TestException>(e => e.Message.Equals(SampleReturnString)).Rethrow()
                .For().Exception<TestException>().Throw(e => new Exception("This should not get thrown"))
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

        ///<remarks> This also tests for correct order of evaluation</remarks>
        [Fact]
        public void ForPredicatedExceptionReturnDefault()
        {
            As.Func(TestFunctionExceptionWithMessage).WithPolicy()
                .For().Exception<TestException>(e => e.Message.Equals(SampleExceptionMessage)).ReturnDefault()
                .For().Exception<TestException>(e => e.Message.Equals(SampleReturnString)).Rethrow()
                .For().Exception<TestException>().Throw(e => new Exception("This should not get thrown"))
                .Execute().ShouldBeNull();
        }

        [Fact]
        public void ForOrPredicatedExceptionReturDefault()
        {
            As.Func(TestFunctionExceptionWithMessage).WithPolicy()
                .For().Exception<OtherTestException>().Or<TestException>(e => e.Message.Equals(SampleExceptionMessage)).ReturnDefault()
                .For().Exception<TestException>(e => e.Message.Equals(SampleReturnString)).Rethrow()
                .For().Exception<TestException>().Throw(e => new Exception("This should not get thrown"))
                .Execute().ShouldBeNull();
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
                .Execute());
            ex.ShouldNotBeNull();
            ex.Message.ShouldEqual(testMessage);
        }

        [Fact]
        public void NotDefinedExceptionRethrowException()
        {
            Assert.Throws<TestException>(() => As.Func(TestFunctionException).WithPolicy()
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

        [Fact]
        public void ForAllOtherExceptionsReturnsDefault()
        {
            const string otherMessage = "Iä! Iä! Cthulhu Fhtagn!";

            As.Func(TestFunctionException).WithPolicy()
                .For().Exception<OtherTestException>().Rethrow()
                .For().Exception<DifferentException>().Return(string.Empty)
                .For().AllOtherExceptions().ReturnDefault()
                .Execute()
                .ShouldBeNull();
        }

        // Retries

        [Fact]
        public void Repeat_Once()
        {
            _repeatMock.Setup(r => r.Call()).Throws<TestException>();
            var result = As.Func(() => _repeat.Call()).WithPolicy()
                .For().Exception<TestException>().Repeat().Then().Return(-1)
                .Execute();

            result.ShouldEqual(-1);
            _repeatMock.Verify(r => r.Call(), Times.Exactly(2));
        }

        [Fact]
        public void Repeat_NTimes()
        {
            const int howMany = 5;

            _repeatMock.Setup(r => r.Call()).Throws<TestException>();
            var result = As.Func(() => _repeat.Call()).WithPolicy()
                .For().Exception<TestException>().Repeat(howMany).Then().Return(-1)
                .Execute();

            result.ShouldEqual(-1);
            _repeatMock.Verify(r => r.Call(), Times.Exactly(howMany + 1));
        }

        [Fact]
        public void Repeat_CallsCallback()
        {
            const int howMany = 5;

            _repeatMock.Setup(r => r.Call());

            var result = As.Func(TestFunctionException).WithPolicy()
                .For().Exception<TestException>().Repeat(howMany,(e, i)=>_repeat.Call()).Then().Return(SampleReturnString)
                .Execute();

            result.ShouldEqual(SampleReturnString);
            _repeatMock.Verify(r => r.Call(), Times.Exactly(howMany));
        }

        [Fact]
        public void WaitAndRepeat_FromTimeSpans()
        {
            var timeSpans = new[] { TimeSpan.FromMilliseconds(100), TimeSpan.FromMilliseconds(200), TimeSpan.FromMilliseconds(300) };

            _repeatMock.Setup(r => r.Call()).Throws<TestException>();
            var result = As.Func(() => _repeat.Call()).WithPolicy()
                .For().Exception<TestException>().WaitAndRepeat(timeSpans).Then().Return(-1)
                .Execute();

            result.ShouldEqual(-1);
            _repeatMock.Verify(r => r.Call(), Times.Exactly(timeSpans.Length + 1));
        }

        [Fact]
        public void WaitAndRepeat_FromTimeSpansWithCallback()
        {
            var timeSpans = new[] { TimeSpan.FromMilliseconds(100), TimeSpan.FromMilliseconds(200), TimeSpan.FromMilliseconds(300) };

            _repeatMock.Setup(r => r.Call());
            var result = As.Func(TestFunctionException).WithPolicy()
                .For().Exception<TestException>().WaitAndRepeat(timeSpans, (exception, i) => _repeat.Call()).Then().Return(SampleReturnString)
                .Execute();

            result.ShouldEqual(SampleReturnString);
            _repeatMock.Verify(r => r.Call(), Times.Exactly(timeSpans.Length));
        }

        [Fact]
        public void WaitAndRepeat_FromFunction()
        {
            const int howMany = 5;

            _repeatMock.Setup(r => r.Call()).Throws<TestException>();
            var result = As.Func(() => _repeat.Call()).WithPolicy()
                .For().Exception<TestException>().WaitAndRepeat(howMany, i=>TimeSpan.FromMilliseconds(10*Math.Pow(2,i))).Then().Return(-1)
                .Execute();

            result.ShouldEqual(-1);
            _repeatMock.Verify(r => r.Call(), Times.Exactly(howMany + 1));
        }

        [Fact]
        public void WaitAndRepeat_FromFunctionWithCallback()
        {
            const int howMany = 5;

            _repeatMock.Setup(r => r.Call());
            var result = As.Func(TestFunctionException).WithPolicy()
                .For().Exception<TestException>().WaitAndRepeat(howMany, i => TimeSpan.FromMilliseconds(10 * Math.Pow(2, i)), (exception, i) => _repeat.Call()).Then().Return(SampleReturnString)
                .Execute();

            result.ShouldEqual(SampleReturnString);
            _repeatMock.Verify(r => r.Call(), Times.Exactly(howMany));
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

        
    }
    // --- interface for repeating testing ---

    public interface ITestRepeat
    {
        int Call();
    }
}
