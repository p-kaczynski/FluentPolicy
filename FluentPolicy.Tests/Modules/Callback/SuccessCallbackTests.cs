using System.Threading.Tasks;
using FluentPolicy.Modules.Callback;
using Should;
using Xunit;

namespace FluentPolicy.Tests.Modules.Callback
{
    public class SuccessCallbackTests
    {
        private const string SampleReturnString = "zxccvbnm";

        [Fact]
        public void CallbackModuleExecutesAction_IfSuccess()
        {
            var toBeTrue = false;

            var returnStr = As.Func(TestFunction).WithPolicy()
                .For().AllOtherExceptions().Return(SampleReturnString)
                .AddModule(new SuccessCallback(() => toBeTrue = true))
                .Execute();

            // Make sure the test policy executed correctly
            returnStr.ShouldEqual(SampleReturnString);
            toBeTrue.ShouldBeTrue();
        }

        [Fact]
        public void CallbackModule_DoesntExecuteAction_IfFailure()
        {
            var toBeTrue = false;

            var returnStr = As.Func(TestFunctionException).WithPolicy()
                .For().AllOtherExceptions().Return(SampleReturnString)
                .AddModule(new SuccessCallback(() => toBeTrue = true))
                .Execute();

            // Make sure the test policy executed correctly
            returnStr.ShouldEqual(SampleReturnString);
            toBeTrue.ShouldBeFalse();
        }

        [Fact]
        public async Task CallbackModuleExecutesAction_IfSuccess_Async()
        {
            var toBeTrue = false;

            var returnStr = await As.Func(TestFunctionAsync).WithAsyncPolicy()
                .For().AllOtherExceptions().Return(SampleReturnString)
                .AddModule(new SuccessCallback(() => toBeTrue = true))
                .ExecuteAsync();

            // Make sure the test policy executed correctly
            returnStr.ShouldEqual(SampleReturnString);
            toBeTrue.ShouldBeTrue();
        }

        [Fact]
        public async Task CallbackModule_DoesntExecuteAction_IfFailure_Async()
        {
            var toBeTrue = false;

            var returnStr = await As.Func(TestFunctionExceptionAsync).WithAsyncPolicy()
                .For().AllOtherExceptions().Return(SampleReturnString)
                .AddModule(new SuccessCallback(() => toBeTrue = true))
                .ExecuteAsync();

            // Make sure the test policy executed correctly
            returnStr.ShouldEqual(SampleReturnString);
            toBeTrue.ShouldBeFalse();
        }

        private static async Task<string> TestFunctionAsync()
        {
            return SampleReturnString;
        }

        private static string TestFunction()
        {
            return SampleReturnString;
        }

        private static string TestFunctionException()
        {
            throw new TestException();
        }

        private static async Task<string> TestFunctionExceptionAsync()
        {
            throw new TestException();
        }
    }
}