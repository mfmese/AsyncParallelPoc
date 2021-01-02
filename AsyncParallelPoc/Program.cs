using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AsyncParallelPoc
{
    class Program
    {
        static void Main(string[] args)
        {
            //WhenAny.Test();
            //WhenAnyWithCancelationToken.Test();

            //WhenAll.Test();

            //WhenAllIntermediary.Test();

            //Singleton.TestNotLocked(); 
            //Singleton.TestOneCheck();
            //Singleton.TestOneCheck2();
            Singleton.TestTwoCheck();

            Console.Read();
        }
    }

    public class WhenAny
    {
        public static void Test()
        {
            var value1 = AsyncServices.GetFirstValueAsync();
            var value2 = AsyncServices.GetSecondValueAsync();
            var throwExceptipon = AsyncServices.GetThrowExceptionAsync();

            var completedTask = Task.WhenAny(value1, value2, throwExceptipon);

            string completedTaskResult = completedTask.Result.Result.ToString();

            Console.WriteLine("completedTask: " + completedTaskResult);
        }

    }

    public class WhenAnyWithCancelationToken
    {
        public static void Test()
        {
            var cancelationToken = new CancellationTokenSource();

            var value1 = AsyncServicesWithToken.GetFirstValueAsync(cancelationToken.Token);
            var value2 = AsyncServicesWithToken.GetSecondValueAsync(cancelationToken.Token);
            var throwExceptipon = AsyncServicesWithToken.GetThrowExceptionAsync(cancelationToken.Token);

            var completedTask = Task.WhenAny(value1, value2, throwExceptipon);

            string completedTaskResult = completedTask.Result.Result.ToString();

            Console.WriteLine("completedTask: " + completedTaskResult);

            cancelationToken.Cancel();
        }

    }

    public class NotUsingWhenAll
    {
        public static async void Test()
        {
            var value1 = await AsyncServices.GetFirstValueAsync();
            var value2 = await AsyncServices.GetSecondValueAsync();
            var throwException = await AsyncServices.GetThrowExceptionAsync();

            for (int i = 0; i < 10; i++)
            {
                var value3 = await AsyncServices.GetSecondValueAsync();
                Console.WriteLine($"Value3: {0}", value3);
            }

            Console.WriteLine($"Value1: {0}", value1);
            Console.WriteLine($"Value2: {0}", value2);
            Console.WriteLine($"ThrowException: {0}", throwException);
        }
    }

    public class WhenAll
    {
        public static async void Test()
        {
            var value1 = AsyncServices.GetFirstValueAsync();
            var value2 = AsyncServices.GetSecondValueAsync();
            var throwException = AsyncServices.GetThrowExceptionAsync();

            var tasks = new List<Task<string>>();
            for (int i = 0; i < 10; i++)
            {
                var value3 = AsyncServices.GetSecondValueAsync();
                tasks.Add(value3);
            }

            tasks.Add(value1);
            tasks.Add(value2);
            tasks.Add(throwException);
            Task allTasks = Task.WhenAll(tasks);
            try
            {
                await allTasks;

                tasks.ForEach(task => Console.WriteLine($"Task {0}", task.Result));
            }
            catch (Exception)
            {
                AggregateException allExceptions = allTasks.Exception;
                Console.WriteLine("Exception: " + allExceptions.Message);
            }
        }
    }

    public class WhenAllIntermediary
    {
        public static async void Test()
        {
            var value1 = AsyncServices.GetFirstValueAsync();
            var value2 = AsyncServices.GetSecondValueAsync();
            var throwException = AsyncServices.GetThrowExceptionAsync();


            var tasks = new[] { value1, value2, throwException };

            var processingTask = tasks.Select(AwaitAndProcessAsync).ToList();

            var allTasks = Task.WhenAll(processingTask);

            try
            {
                await allTasks;
            }
            catch (Exception)
            {
                AggregateException allExceptions = allTasks.Exception;
                Console.WriteLine("Exception: " + allExceptions.Message);
            }

        }

        public static async Task AwaitAndProcessAsync(Task<string> task)
        {
            var result = await task;
            Trace.WriteLine(result);
            Console.WriteLine("AwaitAndProcessAsync: " + result);
        }
    }

    public class AsyncServices
    {
        public static async Task<string> GetFirstValueAsync()
        {
            string result = "FirstValue";
            Console.WriteLine(result + " inside");
            await Task.Delay(5000);
            Console.WriteLine(result + " inside after delay");
            return result;
        }

        public static async Task<string> GetSecondValueAsync()
        {
            string result = "SecondValue";
            Console.WriteLine(result + " inside");
            await Task.Delay(1000);
            Console.WriteLine(result + " inside after delay");
            return result;
        }

        public static async Task<string> GetThrowExceptionAsync()
        {
            string result = "ThrowException";
            Console.WriteLine(result + " inside");
            await Task.Delay(4000);
            Console.WriteLine(result + " inside after delay");
            throw new Exception("Error occured");
        }
    }

    public class AsyncServicesWithToken
    {
        public static async Task<string> GetFirstValueAsync(CancellationToken cts)
        {
            string result = "FirstValue";
            Console.WriteLine(result + " inside");
            await Task.Delay(50000);
            Console.WriteLine(result + " inside after delay");
            return result;
        }

        public static async Task<string> GetSecondValueAsync(CancellationToken cts)
        {
            string result = "SecondValue";
            Console.WriteLine(result + " inside");
            await Task.Delay(1000);
            Console.WriteLine(result + " inside after delay");
            return result;
        }

        public static async Task<string> GetThrowExceptionAsync(CancellationToken cts)
        {
            string result = "ThrowException";
            Console.WriteLine(result + " inside");
            await Task.Delay(20000);
            Console.WriteLine(result + " inside after delay");
            throw new Exception("Error occured");
        }
    }

    public class Singleton
    {
        private static object lockSync = new object();
        private static Singleton instance = null;
        private Singleton()
        {
            Console.WriteLine("instance created");
        }

        public static Singleton InstanceTwoCheck()
        {
            if (instance == null)
            {
                lock (lockSync)
                {
                    if (instance == null)
                    {
                        instance = new Singleton();
                    }
                }
            }
            return instance;
        }

        public static Singleton InstanceOneCheck()
        {
            if (instance == null)
            {
                lock (lockSync)
                {
                    instance = new Singleton();
                }
            }
            return instance;
        }
        public static Singleton InstanceOneCheck2()
        {
            lock (lockSync)
            {
                if (instance == null)
                    instance = new Singleton();
            }

            return instance;
        }
        public static Singleton InstanceNotLocked()
        {
            if (instance == null)
            {
                instance = new Singleton();
            }

            return instance;
        }

        public static void TestTwoCheck()
        {
            Parallel.Invoke(() => Singleton.InstanceTwoCheck(), () => Singleton.InstanceTwoCheck(), () => Singleton.InstanceTwoCheck());
        }
        public static void TestOneCheck()
        {
            Parallel.Invoke(() => Singleton.InstanceOneCheck(), () => Singleton.InstanceOneCheck(), () => Singleton.InstanceOneCheck());
        }
        public static void TestOneCheck2()
        {
            Parallel.Invoke(() => Singleton.InstanceOneCheck2(), () => Singleton.InstanceOneCheck2(), () => Singleton.InstanceOneCheck2());
        }
        public static void TestNotLocked()
        {
            Parallel.Invoke(() => Singleton.InstanceNotLocked(), () => Singleton.InstanceNotLocked(), () => Singleton.InstanceNotLocked());
        }
    }
}
