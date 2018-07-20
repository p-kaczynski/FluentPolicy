using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentPolicy.Modules.Logging;
using FluentAssertions;
using Xunit;

namespace FluentPolicy.Tests.Modules.Logging
{
    public class LoggingTests
    {
        private const string SampleExceptionMessage = "qwertyuiop";
        private const string SampleReturnString = "zxccvbnm";

        [Fact]
        public void LoggingModuleExecutesLambda()
        {
            var exceptionList = new List<Exception>();

            var returnStr = As.Func(TestFunctionExceptionWithMessage).WithPolicy()
                .For().AllOtherExceptions().Return(SampleReturnString)
                .AddModule(new LambdaLogger(exceptionList.Add))
                .Execute();

            // Make sure the test policy executed correctly
            returnStr.Should().Be(SampleReturnString);

            exceptionList.Count.Should().Be(1);
            exceptionList.First().Message.Should().Be(SampleExceptionMessage);
        }


        [Fact]
        public void LoggingModuleDoesNothingWhenNoException()
        {
            var exceptionList = new List<Exception>();

            var returnStr = As.Func(TestFunction).WithPolicy()
                .For().AllOtherExceptions().Rethrow()
                .AddModule(new LambdaLogger(exceptionList.Add))
                .Execute();

            // Make sure the test policy executed correctly
            returnStr.Should().Be(SampleReturnString);

            exceptionList.Count.Should().Be(0);
        }

        [Fact]
        public async Task  LoggingModuleExecutesLambdaAsync()
        {
            var exceptionList = new List<Exception>();

            var returnStr = await As.Func(TestFunctionExceptionWithMessageAsync).WithAsyncPolicy()
                .For().AllOtherExceptions().Return(SampleReturnString)
                .AddModule(new LambdaLogger(exceptionList.Add))
                .ExecuteAsync();

            // Make sure the test policy executed correctly
            returnStr.Should().Be(SampleReturnString);

            exceptionList.Count.Should().Be(1);
            exceptionList.First().Message.Should().Be(SampleExceptionMessage);
        }


        [Fact]
        public async Task LoggingModuleDoesNothingWhenNoExceptionAsync()
        {
            var exceptionList = new List<Exception>();

            var returnStr = await As.Func(TestFunctionAsync).WithAsyncPolicy()
                .For().AllOtherExceptions().Rethrow()
                .AddModule(new LambdaLogger(exceptionList.Add))
                .ExecuteAsync();

            // Make sure the test policy executed correctly
            returnStr.Should().Be(SampleReturnString);

            exceptionList.Count.Should().Be(0);
        }

        // Helper methods
        private static string TestFunction()
        {
            return SampleReturnString;
        }

        private static string TestFunctionExceptionWithMessage()
        {
            throw new TestException(SampleExceptionMessage);
        }

        private static async Task<string> TestFunctionAsync()
        {
            return SampleReturnString;
        }

        private static async Task<string> TestFunctionExceptionWithMessageAsync()
        {
            throw new TestException(SampleExceptionMessage);
        }
    }
}