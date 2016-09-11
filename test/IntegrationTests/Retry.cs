using System;
using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;
using NUnit.Framework.Internal.Commands;
using NUnit.Framework;
using System.Threading.Tasks;
using static IntegrationTests.Retrier;

namespace IntegrationTests
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public sealed class RetryAttribute : PropertyAttribute, IWrapSetUpTearDown
    {
        private int count;
        private int waitInMilliseconds;

        public RetryAttribute(int count = 20, int waitInMilliseconds = 500) : base(count)
        {
            this.count = count;
            this.waitInMilliseconds = waitInMilliseconds;
        }

        public TestCommand Wrap(TestCommand command) => new RetryCommand(command, count, waitInMilliseconds);

        private class RetryCommand : DelegatingTestCommand
        {
            private int retryCount;
            private int waitInMilliseconds;

            public RetryCommand(TestCommand innerCommand, int retryCount, int waitInMilliseconds)
                : base(innerCommand)
            {
                this.retryCount = retryCount;
                this.waitInMilliseconds = waitInMilliseconds;
            }

            public override TestResult Execute(TestExecutionContext context)
            {
                var count = retryCount;
                while (count-- > 0)
                {
                    context.CurrentResult = innerCommand.Execute(context);
                    if (context.CurrentResult.ResultState != ResultState.Failure
                        && context.CurrentResult.ResultState != ResultState.Error)
                        break;
                    System.Threading.Thread.Sleep(waitInMilliseconds);
                }
                return context.CurrentResult;
            }
        }
    }

    public static class Retrier
    {
        public static void Retry(Action action, int count = 20, int waitInMilliseconds = 500)
        {
            if (count <= 1) throw new ArgumentException("Retry count needs to be larger than 1.", nameof(count));
            while (true)
            {
                try
                {
                    action();
                    break;
                }
                catch (Exception ex) when (--count > 0)
                {
                    WriteMessage(ex.Message, count);
                    System.Threading.Thread.Sleep(waitInMilliseconds);
                }
            }
        }

        public async static Task RetryAsync(Func<Task> asyncAction, int count = 20, int waitInMilliseconds = 500)
        {
            if (count <= 1) throw new ArgumentException("Retry count needs to be larger than 1.", nameof(count));
            while (true)
            {
                try
                {
                    await asyncAction();
                    break;
                }
                catch (Exception ex) when (--count > 0)
                {
                    WriteMessage(ex.Message, count);
                    await Task.Delay(waitInMilliseconds);
                }
            }
        }

        private static void WriteMessage(string message, int count)
        {
            string testName;
            try
            {
                testName = TestContext.CurrentContext.Test?.Name ?? "test";
            }
            catch
            {
                testName = "test";
            }
            Console.WriteLine($"Caught exception when running '{testName}' and will retry {count} more times. Exception message: '{message}'.");
        }
    }

    [TestFixture]
    public class TestRetryAttribute
    {
        public static int i, j;
        [Test, Retry(3)]
        public void CheckThrow()
        {
            if (++i < 2) throw new Exception("Foooo");
        }
        [Test, Retry(3)]
        public void CheckAssert()
        {
            Assert.True(++j > 2);
        }
    }

    [TestFixture]
    public class TestRetryMethod
    {
        public static int i;
        [OneTimeSetUp]
        public Task OneTimeSetUp() => RetryAsync(Setup, 2);

        private Task Setup()
        {
            if (++i < 2) throw new Exception("Test for retry operation.");
            return Task.CompletedTask;
        }

        [Test]
        public void RetryPlaceholder() { }
    }
}